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
  private svg:  d3.Selection<SVGSVGElement, unknown, null, undefined> | null = null;

  setup(graph: IGraphD3): void 
  {
    const svg = graph.renderer.getSvg();
    const viewport = graph.renderer.getViewport();

    if (!svg || !viewport) return;

    this.svg = svg;

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
    this.svg  = null;
  }

  /**
   * Programmatically pans and zooms to a point in graph-space.
   * Triggers through the official D3 zoom behavior so subsequent
   * user interactions start from the correct zoom state.
   *
   * @param x        Target X coordinate in graph space
   * @param y        Target Y coordinate in graph space
   * @param scale    Zoom scale (default: 2)
   * @param duration Transition duration in ms (default: 750)
   */
  zoomTo(x: number, y: number, scale = 2, duration = 750): void
  {
    if (!this.svg || !this.zoom) return;

    const svgEl = this.svg.node();
    if (!svgEl) return;

    const width  = svgEl.clientWidth;
    const height = svgEl.clientHeight;
    const tx     = width  / 2 - x * scale;
    const ty     = height / 2 - y * scale;

    this.svg
      .transition()
      .duration(duration)
      .call(
        this.zoom.transform,
        d3.zoomIdentity.translate(tx, ty).scale(scale),
      );
  }
}
