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

    this._bus.on('layout:change', ({ layout: l }) => 
    {
      if (!this._currentData) return;
      this._layout = l;
      
      const result = l.apply(
        this._ctx.nodes,
        this._currentData
          .sourceRelEdges
          .concat(this._currentData.useRelEdges) as any,
        { width: 800, height: 600 },
        this._config
      );
      
      if (result && result.positions) 
      {
        if (result.animationHint === 'instant') 
        {
          this._bus.emit('render:snap-positions', 
            { positions: result.positions });
        }
        else 
        {
          this._bus.emit('render:tween-positions', 
            { positions: result.positions });
        }
      }
    });
    return this;
  }

  /**
   * Sets a global node filter and re-renders the graph.
   * If the filter changes, the simulation restarts with the current data.
   */
  setNodeFilter(filter: ((node: any) => boolean) | null): void 
  {
    this._nodeFilter = filter;
    if (this._currentData && this._isMounted) 
    {
      this._sim.stop();
      this._registry.teardownAll();
      this.render(this._currentData);
    }
  }

  /**
   * The main entry point to render or re-render the graph with new data.
   * Hydrates string IDs into D3 node objects, applies the current layout synchronously,
   * runs the render pipeline to draw the SVG elements, and starts the simulation physics.
   * 
   * @param data The raw graph data (nodes, source relations, use relations).
   */
  render(data: GraphData): void 
  {
    const t0 = performance.now();

    this._currentData = data; // Save reference for potential future updates or layout changes

    // Apply node filter
    const filteredNodes = this._nodeFilter ? data.nodes.filter(this._nodeFilter) : data.nodes;
    const d3Nodes: D3Node[] = filteredNodes.map(n => ({ ...n, id: n.pathId })) as D3Node[];
    const nodeMap = new Map(d3Nodes.map(n => [n.id, n])); // Map for quick lookup when constructing edges 
    
    // Combine sourceRelEdges and useRelEdges into a single array of D3Edge,
    // while also replacing 'from' and 'to' with actual node references.
    // Filter out edges that refer to nodes that were filtered out
    const d3Edges = [...data.sourceRelEdges, ...data.useRelEdges]
      .filter(e => nodeMap.has(e.from) && nodeMap.has(e.to))
      .map(e => ({ 
        ...e, 
        source: nodeMap.get(e.from)!, 
        target: nodeMap.get(e.to)! 
      }));

    // Apply initial layout if available before rendering and starting simulation. 
    // This ensures nodes start in a reasonable position.
    let layoutResult: LayoutResult | null = null;
    if (this._layout) 
    {
      layoutResult = this._layout.apply(
        d3Nodes as any, d3Edges as any, 
        { width: 800, height: 600 }, 
        this._config
      );
      if (layoutResult && layoutResult.positions) 
      {
        for (const n of d3Nodes) 
        {
          const pos = layoutResult.positions.get(n.id);
          if (pos) 
          {
            n.x = pos.x;
            n.y = pos.y;
          }
        }
      }
    }

    const output = this._pipeline.run(d3Nodes as any, d3Edges as any);

    this._ctx.updateRefs(
      output.svg,
      output.viewport,
      output.nodeSelection,
      output.edgeSelection,
      output.liveNodes,
    );

    this._sim.start(output.liveNodes, output.liveEdges as any, output.dimensions, () => 
    {
      this._pipeline.onTick();
    });
    
    // Now that simulation has started (and initialized its default chaotic forces),
    // we emit the layout positions so SimulationController can inject the magnetic anchors!
    if (layoutResult && layoutResult.positions) 
    {
      if (layoutResult.animationHint === 'instant') 
      {
        this._bus.emit('render:snap-positions', 
          { positions: layoutResult.positions });
      } 
      else 
      {
        this._bus.emit('render:tween-positions', 
          { positions: layoutResult.positions });
      }
    }

    this._registry.setupAll(this._ctx, data);

    this._bus.emit('render:complete', 
      {
        elapsed:   performance.now() - t0,
        nodeCount: d3Nodes.length,
        edgeCount: d3Edges.length,
      });
  }

  update(data: GraphData): void 
  {
    this._registry.teardownAll();
    this._sim.stop();
    this.render(data);
    // _ctx tetap instance yang sama — tidak ada stale refs
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
