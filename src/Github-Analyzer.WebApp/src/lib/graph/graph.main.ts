import * as d3 from 'd3';
import type { 
  GraphData, GraphConfig, GraphPlugin, 
  D3Node, D3Edge, IGraphD3 
} from './graph.types';

import { defaultGraphConfig } from './graph.config';
import { createSimulation, stopSimulation } from './utils/simulation';
import { D3Renderer } from './renderer/d3.renderer';

export interface GraphD3Options 
{
  data: GraphData;
  container: HTMLElement;
  config?: Partial<GraphConfig>;
}

/**
 * GraphD3 — facade and entry point for the graph visualization library.
 *
 * Usage:
 * ```ts
 * const graph = new GraphD3({ container, data });
 *
 * graph
 *   .use(new ZoomDragPlugin())
 *   .use(new SelectionPlugin(node => console.log(node)))
 *   .use(new HoverPlugin());
 *
 * graph.render();
 *
 * // Later, when data changes:
 * graph.update(newData);
 *
 * // On component unmount:
 * graph.destroy();
 * ```
 */
export class GraphD3 implements IGraphD3 
{
  readonly container: HTMLElement;
  readonly renderer: D3Renderer;

  simulation: d3.Simulation<D3Node, D3Edge> | null = null;

  private data: GraphData;
  private config: GraphConfig;
  private plugins: Map<string, GraphPlugin> = new Map();

  private d3Nodes: D3Node[] = [];
  private d3Edges: D3Edge[] = [];

  constructor({ container, data, config }: GraphD3Options) 
  {
    this.data = data;
    this.config = { ...defaultGraphConfig, ...(config ?? {}) };
    this.renderer = new D3Renderer();
    this.container = container;
  }

  /**
   * Registers a plugin. Returns `this` for chaining.
   * Plugins with the same name replace the previous registration.
   */
  use(plugin: GraphPlugin): this 
  {
    this.plugins.set(plugin.name, plugin);
    return this;
  }

  /** Renders the graph. Must be called after all plugins are registered. */
  render(): void 
  {
    const width  = this.container.clientWidth  || 800;
    const height = this.container.clientHeight || 600;

    // sourceRelEdges and useRelEdges are kept separate at the GraphData level.
    // They are combined here, internally, only for rendering purposes.
    const allEdges = [...this.data.sourceRelEdges, ...this.data.useRelEdges];

    this.d3Nodes = this.data.nodes.map((n) => ({ ...n, id: n.pathId }));
    this.d3Edges = allEdges.map((e) => ({ ...e, source: e.from, target: e.to })) as D3Edge[];

    // 1. Init SVG (re-entrant: renderer.destroy() cleared old SVG, init() creates new)
    this.renderer.init(this.container);
    this.renderer.render(this.d3Nodes, this.d3Edges, this.config);

    // 2. Create force simulation
    this.simulation = createSimulation(this.d3Nodes, this.d3Edges, width, height, this.config);
    this.simulation.on('tick', () => this.renderer.onTick());

    // 3. Setup plugins — must run after render() and simulation are ready
    for (const plugin of this.plugins.values()) 
    {
      plugin.setup(this);
    }
  }

  /**
   * Replaces graph data and re-renders.
   * Plugins are preserved and re-setup automatically.
   * More efficient than creating a new GraphD3 instance.
   */
  update(data: GraphData): void 
  {
    this._teardown(); // stop simulation, teardown plugins, remove SVG from DOM
    this.data = data;
    this.render();   // renderer instance reused; init() creates fresh SVG
  }

  /**
   * Fully cleans up the graph: stops simulation, tears down plugins, removes SVG.
   * Call this on Vue component unmount.
   */
  destroy(): void 
  {
    this._teardown();
    this.plugins.clear();
  }

  /**
   * Internal teardown used by both update() and destroy().
   * Does NOT clear plugins so update() can re-setup them after re-render.
   */
  private _teardown(): void 
  {
    if (this.simulation) 
    {
      stopSimulation(this.simulation);
      this.simulation = null;
    }

    for (const plugin of this.plugins.values()) 
    {
      plugin.teardown?.();
    }

    // renderer.destroy() removes the SVG DOM element and nulls internal refs.
    // The D3Renderer instance (this.renderer) remains alive for reuse.
    this.renderer.destroy();
  }
}
