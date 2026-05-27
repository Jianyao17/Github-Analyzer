import * as d3 from 'd3';
import type { GraphPlugin, IGraphD3 } from '../graph.types';
import { restartSimulation, coolSimulation } from '../utils/simulation';

/**
 * ZoomDragPlugin — adds pan/zoom on the SVG and drag behavior on nodes.
 *
 * - Zoom/pan: transforms the viewport <g> via d3.zoom
 * - Drag: fixes node position during drag, releases on end
 */
export class ZoomDragPlugin implements GraphPlugin 
{
  readonly name = 'zoom-drag';

  private zoom: d3.ZoomBehavior<SVGSVGElement, unknown> | null = null;

  setup(graph: IGraphD3): void 
  {
    const svg = graph.renderer.getSvg();
    const viewport = graph.renderer.getViewport();

    if (!svg || !viewport) return;

    // ── Zoom & Pan ──────────────────────────────────────────────
    this.zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => 
        viewport.attr('transform', event.transform));

    svg.call(this.zoom);

    // ── Node Drag ───────────────────────────────────────────────
    const nodeSelection = graph.renderer.nodeRenderer.getSelection();
    if (!nodeSelection) return;

    nodeSelection
      .attr('cursor', 'grab')
      .call(
        d3.drag<SVGGElement, any>()
          .on('start', (event, d) => 
          {
            if (!event.active) restartSimulation(graph.simulation!);
            d.fx = d.x;
            d.fy = d.y;
          })
          .on('drag', (event, d) => 
          {
            d.fx = event.x;
            d.fy = event.y;
          })
          .on('end', (event, d) => 
          {
            if (!event.active) coolSimulation(graph.simulation!);
            d.fx = null;
            d.fy = null;
          }),
      );
  }

  teardown(): void 
  {
    this.zoom = null;
  }
}
