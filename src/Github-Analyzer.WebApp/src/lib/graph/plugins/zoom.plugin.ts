import * as d3 from 'd3';
import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

export class ZoomPlugin implements GraphPlugin 
{
  readonly name = 'zoom';
  readonly priority = 0;

  private _unsub: (() => void)[] = [];
  private _zoom: d3.ZoomBehavior<SVGSVGElement, unknown> | null = null;

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    if (!ctx.svg || !ctx.viewport) return;

    this._zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => 
      {
        ctx.viewport?.attr('transform', event.transform);
      });

    ctx.svg.call(this._zoom);

    this._unsub.push(
      ctx.bus.on('zoom:to', (payload: any) => 
      {
        const { x, y, scale, duration = 750 } = payload;
        if (!ctx.svg || !this._zoom) return;
        const svgEl = ctx.svg.node();
        if (!svgEl) return;

        const tx = svgEl.clientWidth  / 2 - x * scale;
        const ty = svgEl.clientHeight / 2 - y * scale;

        ctx.svg
          .transition()
          .duration(duration)
          .call(this._zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
      }),

      ctx.bus.on('zoom:fit', ({ padding = 60 }) => 
      {
        if (!ctx.svg || !this._zoom || !ctx.nodes.length) return;
        const svgEl = ctx.svg.node();
        if (!svgEl) return;

        const xs = ctx.nodes.map((n: D3Node) => n.x ?? 0);
        const ys = ctx.nodes.map((n: D3Node) => n.y ?? 0);

        const minX = Math.min(...xs);
        const maxX = Math.max(...xs);
        const minY = Math.min(...ys);
        const maxY = Math.max(...ys);

        const boxW = maxX - minX + padding * 2;
        const boxH = maxY - minY + padding * 2;
        const cx   = (minX + maxX) / 2;
        const cy   = (minY + maxY) / 2;

        const svgW = svgEl.clientWidth;
        const svgH = svgEl.clientHeight;

        const scale = Math.min(2, svgW / boxW, svgH / boxH);
        
        const tx = svgW / 2 - cx * scale;
        const ty = svgH / 2 - cy * scale;

        ctx.svg
          .transition()
          .duration(750)
          .call(this._zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
      })
    );
  }

  teardown(): void 
  {
    this._unsub.forEach(fn => fn());
    this._unsub = [];
    this._zoom = null;
  }
}
