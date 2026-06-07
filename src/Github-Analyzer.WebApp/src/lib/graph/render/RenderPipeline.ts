import * as d3 from 'd3';
import type { EventBus }       from '@graph/core/EventBus';
import type { D3Node, D3Edge } from '@graph/types/node-edge';
import type { GraphConfig }    from '@graph/types/config';
import type { 
  SvgSelection, ViewportSelection, 
  NodeSelection, EdgeSelection 
} from '@graph/types/graph-view';

import { NodePass     } from './passes/NodePass';
import { EdgePass     } from './passes/EdgePass';
import { MarkerPass   } from './passes/MarkerPass';
import { TextMeasurer } from './measure/TextMeasurer';

export interface RenderOutput {
  svg:           SvgSelection;
  viewport:      ViewportSelection;
  nodeSelection: NodeSelection;
  edgeSelection: EdgeSelection;
  liveNodes:     D3Node[];
  liveEdges:     D3Edge[];
  dimensions:    { width: number; height: number };
}

/**
 * Encapsulates the D3 rendering logic for the graph.
 * Composes multiple "passes" (Markers, Edges, Nodes) to draw the SVG elements.
 * Responsibilities include setting up the root SVG, managing the viewport layer,
 * and orchestrating updates on every physics tick.
 */
export class RenderPipeline 
{
  private _container: HTMLElement | null = null;
  private _config:    GraphConfig;
  private _bus:       EventBus;
  private _measurer:  TextMeasurer;

  private _svg:       SvgSelection      | null = null;
  private _viewport:  ViewportSelection | null = null;
  private _unsubs:    (() => void)[]    = [];

  readonly nodePass:  NodePass;
  readonly edgePass:  EdgePass;
  readonly markerPass: MarkerPass;

  constructor(config: GraphConfig, bus: EventBus) 
  {
    this._config     = config;
    this._bus        = bus;
    this._measurer   = new TextMeasurer();
    this.nodePass    = new NodePass(this._measurer, this._bus);
    this.edgePass    = new EdgePass();
    this.markerPass  = new MarkerPass();
    
    this._listenBusEvents();
  }

  mount(container: HTMLElement): void 
  {
    this._container = container;
    if (this._svg) 
    {
      this._container.appendChild(this._svg.node()!);
    }
  }

  unmount(): void 
  {
    this._svg?.remove();
    this._container = null;
  }

  /**
   * Initializes the SVG DOM structure, runs all rendering passes, and listens to view events.
   * 
   * @param nodes The array of node data objects.
   * @param edges The array of edge data objects.
   * @returns The RenderOutput containing the active D3 selections and dimensions.
   */
  run(nodes: D3Node[], edges: D3Edge[]): RenderOutput 
  {
    if (!this._container) throw new Error('RenderPipeline must be mounted before running.');

    const width  = this._container.clientWidth  || 800;
    const height = this._container.clientHeight || 600;

    // Init SVG if it doesn't exist
    if (!this._svg) 
    {
      this._svg = d3.select(this._container)
        .append('svg')
        .attr('width',   '100%')
        .attr('height',  '100%')
        .attr('viewBox', `0 0 ${width} ${height}`);

      this._viewport = this._svg.append('g').attr('class', 'viewport');
      this.markerPass.run(this._svg, this._config);
    } 
    else 
    {
      this._svg.attr('viewBox', `0 0 ${width} ${height}`);
    }

    // Jalankan passes
    const edgeSel = this.edgePass.run(this._viewport!, edges, this._config);
    const nodeSel = this.nodePass.run(this._viewport!, nodes, this._config);

    return {
      svg:           this._svg,
      viewport:      this._viewport!,
      nodeSelection: nodeSel,
      edgeSelection: edgeSel,
      liveNodes:     nodes,
      liveEdges:     edges,
      dimensions:    { width, height },
    };
  }

  /**
   * Called on every frame of the D3 force simulation.
   * Delegates the position updates to the respective rendering passes.
   */
  onTick(): void 
  {
    this.edgePass.updatePositions();
    this.nodePass.updatePositions();
  }

  /**
   * Destroys the pipeline permanently.
   */
  destroy(): void 
  {
    this.unmount();
    this._unsubs.forEach(u => u());
    this._unsubs = [];
    this._svg     = null;
    this._viewport = null;
    this.nodePass.clear();
    this.edgePass.clear();
    this._measurer.destroy();
  }

  private _listenBusEvents(): void 
  {
    this._unsubs.push(
      this._bus.on('view:nodes-changed', ({ nodes, edges }) => 
      {
        this.nodePass.apply(nodes, this._config);
        this.edgePass.apply(edges, this._config);
      }),

      this._bus.on('highlight:nodes', ({ ids, dimOpacity }) => 
      {
        this.nodePass.applyHighlight(ids, dimOpacity);
      }),

      this._bus.on('highlight:clear', () => 
      {
        this.nodePass.clearHighlight();
      })
    );
  }
}
