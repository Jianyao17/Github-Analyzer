import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig, EdgeSelection, IEdgeRenderer } from '@graph.types';
import { EDGE_TYPE_KEYS } from '../graph.config';

export class EdgeRenderer implements IEdgeRenderer
{
  // Selection semua edge line — merepresentasikan apa yang tampil di layar
  private selection: EdgeSelection | null = null;

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
    if (!this.selection) return;

    const firstEdge = this.selection.node();
    if (!firstEdge) return;

    const parentEl = firstEdge.parentNode as SVGGElement | null;
    if (!parentEl) return;

    const parentSel = d3.select<SVGGElement, unknown>(parentEl);
    this.selection  = this._applyUpdatePattern(parentSel, edges, config);
  }

  /** Called on every simulation tick — repositions all edge endpoints. */
  updatePositions(): void
  {
    this.selection
      ?.attr('x1', (d) => (d.source as D3Node).x ?? 0)
      .attr('y1',  (d) => (d.source as D3Node).y ?? 0)
      .attr('x2',  (d) => (d.target as D3Node).x ?? 0)
      .attr('y2',  (d) => (d.target as D3Node).y ?? 0);
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
      .attr('id',           (typeNum) => `graph-arrow-${typeNum}`)
      .attr('viewBox',      '0 -5 10 10')
      .attr('refX',         18)
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
      .selectAll<SVGLineElement, D3Edge>('line')
      .data(edges);

    // ── Exit: hapus edge yang tidak ada di set baru ──────────────────────────
    bound.exit().remove();

    // ── Enter: tambah edge baru ──────────────────────────────────────────────
    const entered = bound
      .enter()
      .append('line')
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
