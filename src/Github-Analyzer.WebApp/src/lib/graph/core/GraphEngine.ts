import type { 
  GraphData, IGraphLayout, D3Node, 
  GraphConfig, GraphPlugin, LayoutResult 
} from '@graph.types';

import { EventBus }            from './EventBus';
import { GraphContext }        from './GraphContext';
import { PluginRegistry }      from './PluginRegistry';
import { SimulationController } from './SimulationController';
import { RenderPipeline }      from '@graph/render/RenderPipeline';
import { defaultGraphConfig }  from '@graph/config';

/**
 * Options for initializing the GraphEngine.
 */
export interface GraphEngineOptions 
{
  /** Optional partial configuration to override default settings. */
  config?: Partial<GraphConfig>;
}

/**
 * The main orchestrator of the D3 graph library.
 * Manages the rendering pipeline, force simulation, plugins, and layout application.
 */
export class GraphEngine 
{
  private readonly _bus:      EventBus;
  private readonly _sim:      SimulationController;
  private readonly _ctx:      GraphContext;
  private readonly _registry: PluginRegistry;
  private readonly _pipeline: RenderPipeline;
  private readonly _config:   GraphConfig;

  private _currentData: GraphData | null = null;
  private _layout:      IGraphLayout | null = null;
  
  private _nodeFilter:  ((node: any) => boolean) | null = null;
  private _isMounted = false;

  constructor({ config }: GraphEngineOptions = {}) 
  {
    this._config   = { ...defaultGraphConfig, ...(config ?? {}) };
    
    this._bus      = new EventBus();
    this._registry = new PluginRegistry();
    
    this._sim      = new SimulationController(this._bus, this._config);
    this._pipeline = new RenderPipeline(this._config, this._bus);
    this._ctx      = new GraphContext(this._bus, this._sim);

    this._bus.on('view:refresh-requested', () => this.refreshView());
  }

  /**
   * Mounts the graph to a DOM container.
   */
  mount(container: HTMLElement): void 
  {
    this._pipeline.mount(container);
    this._isMounted = true;
  }

  /**
   * Unmounts the graph from the DOM container.
   */
  unmount(): void 
  {
    this._pipeline.unmount();
    this._isMounted = false;
  }

  /**
   * Returns true if the engine has been successfully mounted to a DOM container.
   */
  isMounted(): boolean
  {
    return this._isMounted;
  }

  /**
   * Resets the engine state, stopping simulations and clearing the active graph.
   */
  reset(): void 
  {
    this._sim.stop();
    this._registry.teardownAll();
    
    this._currentData = null;
    this._bus.emit('highlight:clear', undefined as never);
  }

  /**
   * Expose context for consumers (e.g. LayoutManager or Plugins).
   * Context holds live references to nodes, edges, and active selections.
   */
  get ctx(): GraphContext 
  {
    return this._ctx;
  }

  /**
   * Registers a plugin into the graph engine.
   * Plugins can hook into the EventBus or manipulate the GraphContext.
   * 
   * @param plugin The plugin instance to register.
   * @param priority Optional execution priority (lower runs first).
   */
  use(plugin: GraphPlugin, priority?: number): this 
  {
    this._registry.register(plugin, priority);
    return this;
  }

  /**
   * Assigns a specific layout algorithm (e.g., Hierarchical or StarBalloon) to the engine.
   * Automatically listens for layout changes and computes initial target positions.
   * 
   * @param layout The layout algorithm instance.
   */
  useLayout(layout: IGraphLayout): this 
  {
    this._layout = layout;
    if (this._currentData && this._isMounted) 
    {
      this.refreshView();
    }
    return this;
  }

  /**
   * Sets a global node filter and re-renders the graph.
   */
  setNodeFilter(filter: ((node: any) => boolean) | null): void 
  {
    this._nodeFilter = filter;
    if (this._currentData && this._isMounted) 
    {
      this.refreshView();
    }
  }

  /**
   * Refreshes the active graph view. 
   * Runs the data through the plugin transformation middleware, applies layout,
   * updates the DOM via the render pipeline, and restarts the simulation.
   */
  refreshView(isInitialRender = false): void
  {
    if (!this._currentData) return;

    const t0 = performance.now();

    // 1. Re-hydrate base nodes/edges, preserving existing D3 objects to maintain layout/physics stability
    const oldNodeMap = new Map(this._ctx.nodes.map(n => [n.id, n]));
    
    // Apply global node filter
    const baseNodes = this._nodeFilter 
      ? this._currentData.nodes.filter(this._nodeFilter) 
      : this._currentData.nodes;

    const d3Nodes: D3Node[] = baseNodes.map(n => 
    {
      const existing = oldNodeMap.get(n.pathId);
      if (existing) 
      {
        // Preserve D3 physics state (x, y, vx, vy) but update source data fields
        return Object.assign(existing, n);
      }
      
      // Inherit parent position for smooth bloom animation: find nearest visible ancestor
      let parentX: number | undefined;
      let parentY: number | undefined;
      
      let currId = n.pathId;
      while (true) 
      {
        const edges = this._currentData!.indexes.edgesByTarget.get(currId) || [];
        const parentEdge = edges.find(e => e.type !== 2);
        if (!parentEdge) break;
        
        currId = parentEdge.from;
        const p = oldNodeMap.get(currId);
        if (p && typeof p.x === 'number') 
        {
          // Add a tiny random jitter to prevent D3 force collision explosions
          parentX = p.x + (Math.random() - 0.5) * 5;
          parentY = (p.y || 0) + (Math.random() - 0.5) * 5;
          break;
        }
      }
      
      return { ...n, id: n.pathId, x: parentX, y: parentY } as D3Node;
    });

    const nodeMap = new Map(d3Nodes.map(n => [n.id, n]));

    // Re-hydrate edges based on valid nodes
    const d3Edges = [...this._currentData.sourceRelEdges, ...this._currentData.useRelEdges]
      .filter(e => nodeMap.has(e.from) && nodeMap.has(e.to))
      .map(e => ({ 
        ...e, 
        source: nodeMap.get(e.from)!, 
        target: nodeMap.get(e.to)! 
      }));

    // 2. Set full active state to Context for middleware to mutate
    this._ctx.nodes = d3Nodes;
    this._ctx.edges = d3Edges;

    // 3. Run Data Transformation Middleware Chain
    this._registry.transformAll(this._ctx, this._currentData);

    const activeNodes = this._ctx.nodes;
    const activeEdges = this._ctx.edges;

    // 4. Layout
    let layoutResult: LayoutResult | null = null;
    if (this._layout) 
    {
      layoutResult = this._layout.apply(
        activeNodes as any, activeEdges as any, 
        { width: 800, height: 600 }, 
        this._config
      );
      if (layoutResult && layoutResult.positions) 
      {
        for (const n of activeNodes) 
        {
          const pos = layoutResult.positions.get(n.id);
          if (pos) 
          {
            n.targetX = pos.x;
            n.targetY = pos.y;
            
            if (typeof n.x !== 'number') // Only snap if it doesn't have an existing position
            {
              n.x = pos.x;
              n.y = pos.y;
            }
          }
        }
      }
    }

    // 5. Render
    const output = this._pipeline.run(activeNodes as any, activeEdges as any);

    this._ctx.updateRefs(
      output.svg,
      output.viewport,
      output.nodeSelection,
      output.edgeSelection,
      output.liveNodes,
      output.liveEdges
    );

    // 6. Simulation
    this._sim.start(output.liveNodes, output.liveEdges as any, output.dimensions, () => 
    {
      this._pipeline.onTick();
    });

    // Emitting layout positions for magnetic simulation
    if (layoutResult && layoutResult.positions) 
    {
      if (layoutResult.animationHint === 'instant') 
      {
        this._bus.emit('render:snap-positions', { positions: layoutResult.positions });
      } 
      else 
      {
        this._bus.emit('render:tween-positions', { positions: layoutResult.positions });
      }
    }

    // 7. Setup Plugins on initial render
    if (isInitialRender) 
    {
      this._registry.setupAll(this._ctx, this._currentData);
    }

    this._bus.emit('render:complete', 
      {
        elapsed:   performance.now() - t0,
        nodeCount: activeNodes.length,
        edgeCount: activeEdges.length,
      });
  }

  /**
   * The main entry point to render the graph from scratch with new data.
   */
  render(data: GraphData): void 
  {
    this._currentData = data;
    
    // Reset ctx nodes/edges to force complete hydration instead of object reuse
    this._ctx.nodes = [];
    this._ctx.edges = [];
    
    this.refreshView(true);
  }

  setData(data: GraphData): void 
  {
    this._currentData = data;
    
    if (this._isMounted) 
    {
      this.refreshView();
    }
  }

  update(data: GraphData): void 
  {
    this._registry.teardownAll();
    this._sim.stop();
    this.render(data);
  }

  /**
   * Destroys the engine, clears the container DOM, stops the simulation,
   * unregisters all plugins, and cleans up the EventBus to prevent memory leaks.
   */
  destroy(): void 
  {
    this._registry.teardownAll();
    this._sim.stop();
    this._pipeline.destroy();
    this._bus.clear();
  }

  getPlugin<T extends GraphPlugin>(name: string): T | undefined 
  {
    return this._registry.getPlugin<T>(name);
  }
}
