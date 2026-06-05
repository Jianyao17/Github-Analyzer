import type { 
  NodeSelection, EdgeSelection, 
  SvgSelection, ViewportSelection } from '@graph/types/graph-view';
import type { SimulationController } from './SimulationController';
import type { D3Node, D3Edge } from '@graph/types/node-edge';
import type { EventBus } from './EventBus';

/**
 * Central state container for the graph engine.
 * Holds references to the event bus, simulation, and all live D3 selections.
 * This context is passed to plugins so they can interact with the graph DOM and physics.
 */
export class GraphContext 
{
  readonly bus: EventBus;
  readonly sim: SimulationController;

  // Selections diupdate in-place — tidak pernah diganti dengan objek baru
  private _svg:           SvgSelection      | null = null;
  private _viewport:      ViewportSelection | null = null;
  private _nodeSelection: NodeSelection     | null = null;
  private _edgeSelection: EdgeSelection     | null = null;

  // Live data arrays (referensi yang sama dengan yang di-bind ke D3)
  private _nodes: D3Node[] = [];
  private _edges: D3Edge[] = [];

  constructor(bus: EventBus, sim: SimulationController) 
  {
    this.bus = bus;
    this.sim = sim;
  }

  /**
   * Called by the RenderPipeline after each render/update cycle to ensure
   * the context always points to the latest active D3 selections and data arrays.
   * 
   * @param svg The root SVG selection.
   * @param viewport The zoomable viewport `<g>` selection.
   * @param nodes The live D3 selection of node elements.
   * @param edges The live D3 selection of edge elements.
   * @param liveNodes The live array of node data bound to the DOM.
   * @param liveEdges The live array of edge data bound to the DOM.
   */
  updateRefs(
    svg:      SvgSelection,
    viewport: ViewportSelection,
    nodes:    NodeSelection,
    edges:    EdgeSelection,
    liveNodes: D3Node[],
    liveEdges: D3Edge[],
  ): void 
  {
    this._svg           = svg;
    this._viewport      = viewport;
    this._nodeSelection = nodes;
    this._edgeSelection = edges;
    this._nodes         = liveNodes;
    this._edges         = liveEdges;
  }

  get svg():           SvgSelection      | null { return this._svg; }
  get viewport():      ViewportSelection | null { return this._viewport; }
  get nodeSelection(): NodeSelection     | null { return this._nodeSelection; }
  get edgeSelection(): EdgeSelection     | null { return this._edgeSelection; }
  
  get nodes():         D3Node[]                 { return this._nodes; }
  set nodes(value:     D3Node[])                { this._nodes = value; }

  get edges():         D3Edge[]                 { return this._edges; }
  set edges(value:     D3Edge[])                { this._edges = value; }
}
