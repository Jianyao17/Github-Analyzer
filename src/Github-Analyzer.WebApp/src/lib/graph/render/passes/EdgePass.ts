import * as d3 from 'd3';
import type { 
  D3Node, D3Edge, 
  GraphConfig, EdgeSelection } from '@graph.types';
import { getRectEdgeEndpoint } from '@graph/utils/geometry';
import { EDGE_TYPE_KEYS } from '@graph/config';

export class EdgePass 
{
  private _selection: EdgeSelection | null = null;
  private _config: GraphConfig | null = null;

  run(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    edges:    D3Edge[],
    config:   GraphConfig,
  ): EdgeSelection 
  {
    this._config = config;

    let container = viewport.select<SVGGElement>('g.edges');
    if (container.empty()) 
    {
      container = viewport.append('g')
        .attr('class', 'edges')
        .attr('pointer-events', 'none')
        // Disables anti-aliasing to massively improve rendering fps on large graphs
        .attr('shape-rendering', edges.length > 500 ? 'optimizeSpeed' : 'auto');
    }

    this._selection = this._applyUpdatePattern(container, edges, config);
    return this._selection;
  }

  apply(edges: D3Edge[], config: GraphConfig): void 
  {
    this._config = config;
    if (!this._selection) return;

    const firstEdge = this._selection.node();
    if (!firstEdge) return;

    const parentEl = firstEdge.parentNode as SVGGElement | null;
    if (!parentEl) return;

    const parentSel = d3.select<SVGGElement, unknown>(parentEl);
    this._selection  = this._applyUpdatePattern(parentSel, edges, config);
  }

  updatePositions(): void 
  {
    const card = this._config?.nodeCard;
    const gap  = card?.arrowGap ?? 2;

    this._selection?.each(function(d) 
    {
      const s  = d.source as D3Node;
      const t  = d.target as D3Node;
      const sx = s.x ?? 0, sy = s.y ?? 0;
      const tx = t.x ?? 0, ty = t.y ?? 0;

      const sHw = s._hw ?? 8;
      const sHh = s._hh ?? 8;
      const tHw = t._hw ?? 8;
      const tHh = t._hh ?? 8;

      const srcPt = getRectEdgeEndpoint(tx, ty, sx, sy, sHw, sHh, gap);
      const tgtPt = getRectEdgeEndpoint(sx, sy, tx, ty, tHw, tHh, gap);

      if (d.type === 2) 
      {
        const dx = tgtPt.x - srcPt.x;
        const dy = tgtPt.y - srcPt.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        
        if (dist === 0) return;

        const cx = (srcPt.x + tgtPt.x) / 2;
        const cy = (srcPt.y + tgtPt.y) / 2;

        const nx = -dy / dist;
        const ny = dx / dist;

        const offset = Math.min(dist * 0.1, 15);
        const cpx = cx + nx * offset;
        const cpy = cy + ny * offset;

        d3.select(this).attr('d', `M${srcPt.x},${srcPt.y} Q${cpx},${cpy} ${tgtPt.x},${tgtPt.y}`);
      }
      else 
      {
        d3.select(this).attr('d', `M${srcPt.x},${srcPt.y} L${tgtPt.x},${tgtPt.y}`);
      }
    });
  }

  clear(): void 
  {
    this._selection = null;
    this._config    = null;
  }

  private _applyUpdatePattern(
    container: d3.Selection<SVGGElement, unknown, null, undefined>,
    edges:     D3Edge[],
    config:    GraphConfig,
  ): EdgeSelection 
  {
    const bound = container
      .selectAll<SVGPathElement, D3Edge>('path')
      .data(edges);

    bound.exit().remove();

    const entered = bound
      .enter()
      .append('path')
      .attr('fill', 'none')
      .each(function (d) 
      {
        const key   = EDGE_TYPE_KEYS[d.type] ?? 'default';
        const style = config.edgeTypes[key] ?? config.edgeTypes['default'];

        d3.select(this)
          .attr('stroke',           style.color)
          .attr('stroke-opacity',   0.6)
          .attr('stroke-width',     style.strokeWidth)
          // Disable dashed lines for very large graphs because bezier + dash calculation kills GPU
          .attr('stroke-dasharray', edges.length > 500 ? null : (style.dashArray === 'none' ? null : style.dashArray))
          .attr('marker-end',       `url(#graph-arrow-${d.type})`);
      });

    return entered.merge(bound);
  }
}
