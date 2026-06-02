import * as d3 from 'd3';
import type { 
  D3Node, D3Edge, GraphConfig, 
  EdgeSelection, IEdgeRenderer 
} from '@graph.types';

import { EDGE_TYPE_KEYS } from '../graph.config';
import { getRectEdgeEndpoint } from '../utils/geometry';

export class EdgeRenderer implements IEdgeRenderer
{
  // Selection semua edge line — merepresentasikan apa yang tampil di layar
  private selection: EdgeSelection | null = null;

  // Cached config — needed in updatePositions() to know node card dimensions
  private config: GraphConfig | null = null;

  /**
   * Render awal: buat SVG arrow markers dan <line> per edge.
   * Menggunakan D3 general update pattern agar bisa di-reuse oleh applyEdges().
   */
  render(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    svg:      d3.Selection<SVGSVGElement, unknown, null, undefined>,
    edges:    D3Edge[],
    config:   GraphConfig,
  ): void
  {
    this.config = config;
    this.renderArrowMarkers(svg, config);

    // Buat atau ambil container group <g class="edges">
    let container = viewport.select<SVGGElement>('g.edges');

    if (container.empty())
      container = viewport.append('g').attr('class', 'edges');

    this.selection = this._applyUpdatePattern(container, edges, config);
  }

  /**
   * Mengganti set edge yang ditampilkan TANPA full re-render.
   * D3 general update pattern: edge sama dipertahankan, baru ditambah, hilang dihapus.
   * Dipanggil oleh GraphView.applyEdges().
   */
  applyEdges(edges: D3Edge[], config: GraphConfig): void
  {
    this.config = config;

    if (!this.selection) return;

    const firstEdge = this.selection.node();
    if (!firstEdge) return;

    const parentEl = firstEdge.parentNode as SVGGElement | null;
    if (!parentEl) return;

    const parentSel = d3.select<SVGGElement, unknown>(parentEl);
    this.selection  = this._applyUpdatePattern(parentSel, edges, config);
  }

  /**
   * Called on every simulation tick — repositions all edge endpoints.
   * Endpoints are shortened to the node card boundary (not center)
   * so arrows land correctly on rectangular nodes.
   */
  updatePositions(): void
  {
    const card = this.config?.nodeCard;
    const gap  = card?.arrowGap ?? 2;

    this.selection?.each(function(d)
    {
      const s  = d.source as D3Node;
      const t  = d.target as D3Node;
      const sx = s.x ?? 0, sy = s.y ?? 0;
      const tx = t.x ?? 0, ty = t.y ?? 0;

      const sHw = s._hw ?? 8;
      const sHh = s._hh ?? 8;
      const tHw = t._hw ?? 8;
      const tHh = t._hh ?? 8;

      // Shorten both endpoints to the respective card boundaries
      const srcPt = getRectEdgeEndpoint(tx, ty, sx, sy, sHw, sHh, gap);
      const tgtPt = getRectEdgeEndpoint(sx, sy, tx, ty, tHw, tHh, gap);

      // Draw curved line only for 'call' relations
      if (d.type === 2)
      {
        const dx = tgtPt.x - srcPt.x;
        const dy = tgtPt.y - srcPt.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        
        if (dist === 0) return; // Prevent division by zero

        const cx = (srcPt.x + tgtPt.x) / 2;
        const cy = (srcPt.y + tgtPt.y) / 2;

        // Normal vector of the straight line between endpoints
        const nx = -dy / dist;
        const ny = dx / dist;

        // Sangat landai (very gentle offset) agar mengambil jarak terpendek
        const offset = Math.min(dist * 0.1, 15);
        const cpx = cx + nx * offset;
        const cpy = cy + ny * offset;

        d3.select(this).attr('d', `M${srcPt.x},${srcPt.y} Q${cpx},${cpy} ${tgtPt.x},${tgtPt.y}`);
      }
      else
      {
        // Straight line
        d3.select(this).attr('d', `M${srcPt.x},${srcPt.y} L${tgtPt.x},${tgtPt.y}`);
      }
    });
  }

  /** Returns the D3 selection for edge lines, used by plugins and GraphView. */
  getSelection(): EdgeSelection | null
  {
    return this.selection;
  }

  /** Reset internal state — dipanggil oleh D3Renderer.destroy(). */
  clear(): void
  {
    this.selection = null;
    this.config    = null;
  }

  // ─── Private ───────────────────────────────────────────────────────────────

  private renderArrowMarkers(
    svg:    d3.Selection<SVGSVGElement, unknown, null, undefined>,
    config: GraphConfig,
  ): void
  {
    // Render arrow markers untuk tiap edge type
    const edgeTypeNums = Object.keys(EDGE_TYPE_KEYS).map(Number);

    svg
      .append('defs')
      .selectAll('marker')
      .data(edgeTypeNums)
      .enter()
      .append('marker')
      .attr('id', (typeNum) => `graph-arrow-${typeNum}`)
      .attr('viewBox', '0 -5 10 10')
      // refX=10 places the marker tip exactly at the line endpoint.
      // Since updatePositions() already shortens lines to the card boundary,
      // the tip lands precisely at the card border.
      .attr('refX',         10)
      .attr('refY',         0)
      .attr('markerWidth',  6)
      .attr('markerHeight', 6)
      .attr('orient',       'auto')
      .append('path')
      .attr('d',    'M0,-5L10,0L0,5')
      .attr('fill', (typeNum) =>
      {
        const key = EDGE_TYPE_KEYS[typeNum] ?? 'default';
        return (config.edgeTypes[key] ?? config.edgeTypes['default']).color;
      });
  }

  /**
   * D3 general update pattern — inti dari both render() dan applyEdges().
   * Menangani enter (edge baru), exit (edge dihapus), update (edge tetap ada).
   */
  private _applyUpdatePattern(
    container: d3.Selection<SVGGElement, unknown, null, undefined>,
    edges:     D3Edge[],
    config:    GraphConfig,
  ): EdgeSelection
  {
    const bound = container
      .selectAll<SVGPathElement, D3Edge>('path')
      .data(edges);

    // ── Exit: hapus edge yang tidak ada di set baru ──────────────────────────
    bound.exit().remove();

    // ── Enter: tambah edge baru ──────────────────────────────────────────────
    const entered = bound
      .enter()
      .append('path')
      .attr('fill', 'none') // Ensure paths don't fill
      .each(function (d)
      {
        const key   = EDGE_TYPE_KEYS[d.type] ?? 'default';
        const style = config.edgeTypes[key] ?? config.edgeTypes['default'];

        d3.select(this)
          .attr('stroke',           style.color)
          .attr('stroke-opacity',   0.6)
          .attr('stroke-width',     style.strokeWidth)
          .attr('stroke-dasharray', style.dashArray === 'none' ? null : style.dashArray)
          .attr('marker-end',       `url(#graph-arrow-${d.type})`);
      });

    // ── Merge: gabungkan entered + existing ──────────────────────────────────
    return entered.merge(bound);
  }
}
