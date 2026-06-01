import * as d3 from 'd3';
import type { GraphPlugin, GraphData, GraphView, D3Node } from '@graph.types';

// ─── Visual constants ─────────────────────────────────────────────────────────

/** Opacity untuk node yang TIDAK cocok dengan query pencarian. */
const DIM_OPACITY      = 0.24;

/** Stroke color untuk node yang cocok dengan query pencarian. */
const HIGHLIGHT_COLOR  = '#FCD34D'; // amber-300

/** Stroke width untuk node yang ter-highlight. */
const HIGHLIGHT_STROKE = 3;

// ─── SearchPlugin ─────────────────────────────────────────────────────────────

/**
 * SearchPlugin — cari node by label / path dan zoom-focus ke hasil.
 *
 * Tidak memerlukan dependency ke plugin lain.
 * Zoom menggunakan view.zoomTo() yang sudah built-in di GraphView.
 *
 * ```ts
 * const searchPlugin = new SearchPlugin();
 *
 * graph
 *   .use(new ZoomDragPlugin())
 *   .use(searchPlugin);
 *
 * graph.render();
 *
 * // Dari Vue component:
 * const results = searchPlugin.search('UserService');
 * if (results[0]) searchPlugin.focusNode(results[0]);
 * ```
 */
export class SearchPlugin implements GraphPlugin
{
  readonly name = 'search';

  private view:      GraphView | null = null;
  private onResults?: (results: D3Node[]) => void;

  /**
   * @param onResults  Optional callback yang dipanggil setiap kali search() menemukan hasil.
   */
  constructor(onResults?: (results: D3Node[]) => void)
  {
    this.onResults = onResults;
  }

  setup(_data: GraphData, view: GraphView): void
  {
    this.view = view;
  }

  teardown(): void
  {
    this.clearSearch();
    this.view = null;
  }

  // ─── Public API ─────────────────────────────────────────────────────────────

  /**
   * Mencari semua node by label atau pathId (case-insensitive substring match).
   * Otomatis highlight node yang cocok dan dim sisanya.
   * Panggil clearSearch() untuk reset visual state.
   *
   * @param query  Search string. String kosong untuk clear search.
   * @returns      Array D3Node yang cocok.
   */
  search(query: string): D3Node[]
  {
    const trimmed = query.trim().toLowerCase();

    if (!trimmed)
    {
      this.clearSearch();
      return [];
    }

    const nodes   = this.view?.nodes ?? [];
    const results = nodes.filter(
      (n) => n.label.toLowerCase().includes(trimmed)
          || n.pathId.toLowerCase().includes(trimmed),
    );

    this.applyHighlight(results);
    this.onResults?.(results);

    return results;
  }

  /**
   * Pan dan zoom secara smooth ke node tertentu.
   *
   * @param node   Node yang akan di-focus.
   * @param scale  Zoom level (default 2).
   */
  focusNode(node: D3Node, scale = 2): void
  {
    this.view?.zoomTo(node.x ?? 0, node.y ?? 0, scale);
  }

  /**
   * Zoom out untuk menampilkan semua hasil sekaligus.
   * Menghitung bounding box dari semua result nodes dan fit di viewport.
   *
   * @param results  Array node hasil search().
   * @param padding  Padding ekstra di sekitar bounding box dalam px (default 60).
   */
  focusResults(results: D3Node[], padding = 60): void
  {
    if (!results.length || !this.view) return;

    const svgEl = this.view.svg?.node();
    if (!svgEl) return;

    const xs = results.map((n) => n.x ?? 0);
    const ys = results.map((n) => n.y ?? 0);

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

    // Fit scale: tampilkan seluruh bounding box, cap di 2×
    const scale = Math.min(2, svgW / boxW, svgH / boxH);

    this.view.zoomTo(cx, cy, scale);
  }

  /** Reset semua visual highlighting yang diapply oleh search(). */
  clearSearch(): void
  {
    this.applyHighlight([]);
  }

  // ─── Private ────────────────────────────────────────────────────────────────

  private applyHighlight(results: D3Node[]): void
  {
    if (!this.view) return;

    const matchIds = new Set(results.map((n) => n.id));
    const hasQuery = results.length > 0;

    this.view.updateNodes((sel) =>
    {
      sel.each(function (d)
      {
        const g       = d3.select<SVGGElement, D3Node>(this);
        const matched = matchIds.has(d.id);

        // Dim / restore opacity pada seluruh group
        g.transition()
          .duration(200)
          .attr('opacity', hasQuery && !matched ? DIM_OPACITY : 1);

        // Highlight rect stroke untuk node yang cocok
        g.select<SVGRectElement>('rect')
          .transition()
          .duration(200)
          .attr('stroke',       matched ? HIGHLIGHT_COLOR  : 'none')
          .attr('stroke-width', matched ? HIGHLIGHT_STROKE : 0);
      });
    });
  }
}
