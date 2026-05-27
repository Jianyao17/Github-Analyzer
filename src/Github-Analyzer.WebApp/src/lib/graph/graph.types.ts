import type * as d3 from 'd3';

// ─── Re-export domain types from existing source of truth ────────────────────
// GraphNode, GraphEdge, CodeGraph are defined in the application types layer
// and shared here to avoid duplication.
export type { GraphNode, GraphEdge } from '../../types/analysis/code-graph';
export type { CodeGraph as GraphData } from '../../types/analysis/code-graph';

// Import locally for use in augmented types below
import type { GraphNode, GraphEdge } from '../../types/analysis/code-graph';

// ─── D3 Augmented Types ───────────────────────────────────────────────────────
// These extend base domain types with D3 simulation properties.

export type D3Node = d3.SimulationNodeDatum & GraphNode &
{
  id: string;       // Alias for pathId, required by forceLink id accessor
  _radius?: number; // Annotated at render time, used by collision force
};

export type D3Edge = d3.SimulationLinkDatum<D3Node> & GraphEdge;

// ─── Style Config Types ───────────────────────────────────────────────────────

export interface NodeTypeStyle 
{
  color: string;
  radius: number;
}

export interface EdgeTypeStyle 
{
  color: string;
  strokeWidth: number;
  dashArray: string; // e.g. 'none' | '4,4'
}

export interface GraphConfig 
{
  width?: number;
  height?: number;
  nodeTypes: Record<string, NodeTypeStyle>;
  edgeTypes: Record<string, EdgeTypeStyle>;
  simulation?:
  {
    /**
     * Desired average distance between connected nodes.
     * Higher values = more spread out graph.
     */
    linkDistance?: number;
    /**
     * Strength of the force attracting or repelling nodes.
     * Negative = repulsion (default).
     * Higher absolute values = stronger forces.
     */
    chargeStrength?: number;
    /**
     * Simulation stops when alpha drops below this value.
     * Default D3 value: 0.001 (runs ~300 ticks).
     * Raise to ~0.05 to stop as soon as the graph is visually stable.
     */
    alphaMin?: number;
    /**
     * Rate at which alpha decays each tick.
     * Default D3 value: ~0.0228 (targets 300 ticks).
     * Raise to decay faster and settle in fewer ticks.
     */
    alphaDecay?: number;
  };
}

// ─── Structural Interfaces (avoids circular imports with graph.main.ts) ───────
// Plugins depend on IGraphD3, not the concrete GraphD3 class.

export interface INodeRenderer 
{
  /**
   * Returns the D3 selection for the node group.
   * Needed by plugins to attach event listeners or modify node elements.
   */
  getSelection(): d3.Selection<SVGGElement, D3Node, SVGGElement, unknown> | null;
}

export interface IGraphRenderer 
{
  /**
   * Returns the root SVG selection.
   * Needed by plugins for tasks like pan/zoom (when handled by plugins) or styling the SVG element.
   */
  getSvg(): d3.Selection<SVGSVGElement, unknown, null, undefined> | null;

  /**
   * Returns the viewport group selection.
   * Needed by plugins that need to attach content to the graph coordinate space (e.g., overlays).
   */
  getViewport(): d3.Selection<SVGGElement, unknown, null, undefined> | null;

  readonly nodeRenderer: INodeRenderer;
}

export interface IGraphD3 
{
  /**
   * The root DOM element where the graph is rendered.
   */
  readonly container: HTMLElement;

  /**
   * The renderer instance, providing access to SVG and node/edge selections.
   */
  readonly renderer: IGraphRenderer;

  /**
   * The force simulation, which controls node positions and updates over time.
   */
  simulation: d3.Simulation<D3Node, D3Edge> | null;
}

// ─── Plugin Interface ─────────────────────────────────────────────────────────
export interface GraphPlugin 
{
  /**
   * Unique identifier — used to prevent duplicate plugin registration.
   */
  readonly name: string;

  /**
   * Called after render() and simulation are ready.
   */
  setup(graph: IGraphD3): void;

  /**
   * Called on destroy() and before each update().
   * Clean up event listeners here.
   */
  teardown?(): void;
}
