import * as d3 from 'd3';
import type { GraphPlugin, IGraphD3, D3Node } from '../graph.types';
import type { ZoomDragPlugin } from './zoom-drag.plugin';

// ─── Visual constants ─────────────────────────────────────────────────────────

/** Opacity applied to nodes that do NOT match the current search query. */
const DIM_OPACITY       = 0.12;

/** Stroke color for nodes that DO match the search query. */
const HIGHLIGHT_COLOR   = '#FCD34D'; // amber-300

/** Stroke width for highlighted nodes. */
const HIGHLIGHT_STROKE  = 3;

// ─── SearchPlugin ─────────────────────────────────────────────────────────────

/**
 * SearchPlugin — find nodes by label / path and zoom-focus to results.
 *
 * Requires a `ZoomDragPlugin` reference for programmatic zoom.
 * Register it AFTER `ZoomDragPlugin`:
 *
 * ```ts
 * const zoomPlugin   = new ZoomDragPlugin();
 * const searchPlugin = new SearchPlugin(zoomPlugin);
 *
 * graph
 *   .use(zoomPlugin)
 *   .use(searchPlugin);
 *
 * graph.render();
 *
 * // Later, from a Vue component:
 * const results = searchPlugin.search('UserService');
 * if (results[0]) searchPlugin.focusNode(results[0]);
 * ```
 */
export class SearchPlugin implements GraphPlugin
{
  readonly name = 'search';

  private graph:      IGraphD3 | null  = null;
  private zoomPlugin: ZoomDragPlugin;
  private onResults?: (results: D3Node[]) => void;

  /**
   * @param zoomPlugin  Reference to the ZoomDragPlugin — used for `zoomTo`.
   * @param onResults   Optional callback fired every time `search()` returns results.
   */
  constructor(zoomPlugin: ZoomDragPlugin, onResults?: (results: D3Node[]) => void)
  {
    this.zoomPlugin = zoomPlugin;
    this.onResults  = onResults;
  }

  setup(graph: IGraphD3): void
  {
    this.graph = graph;
  }

  teardown(): void
  {
    this.clearSearch();
    this.graph = null;
  }

  // ─── Public API ─────────────────────────────────────────────────────────────

  /**
   * Searches all nodes by label or pathId (case-insensitive substring match).
   * Automatically highlights matching nodes and dims the rest.
   * Call `clearSearch()` to reset visual state.
   *
   * @param query  Search string. Empty string clears the search.
   * @returns      Array of matching D3Node objects.
   */
  search(query: string): D3Node[]
  {
    const trimmed = query.trim().toLowerCase();

    if (!trimmed)
    {
      this.clearSearch();
      return [];
    }

    const nodes   = this.graph?.simulation?.nodes() ?? [];
    const results = nodes.filter(
      (n) => n.label.toLowerCase().includes(trimmed)
           || n.pathId.toLowerCase().includes(trimmed),
    );

    this.applyHighlight(results);
    this.onResults?.(results);

    return results;
  }

  /**
   * Smoothly pans and zooms the viewport to center on a specific node.
   *
   * @param node   The node to focus on.
   * @param scale  Zoom level to apply (default: 2).
   */
  focusNode(node: D3Node, scale = 2): void
  {
    const x = node.x ?? 0;
    const y = node.y ?? 0;
    this.zoomPlugin.zoomTo(x, y, scale);
  }

  /**
   * Zooms out to show all matching results at once.
   * Computes the bounding box of all result nodes and fits it in the viewport.
   *
   * @param results  Array of matching nodes (from `search()`).
   * @param padding  Extra padding around the bounding box in px (default: 60).
   */
  focusResults(results: D3Node[], padding = 60): void
  {
    if (!results.length || !this.graph) return;

    const svg = this.graph.renderer.getSvg();
    if (!svg) return;

    const svgEl = svg.node();
    if (!svgEl) return;

    const xs = results.map((n) => n.x ?? 0);
    const ys = results.map((n) => n.y ?? 0);

    const minX = Math.min(...xs);
    const maxX = Math.max(...xs);
    const minY = Math.min(...ys);
    const maxY = Math.max(...ys);

    const boxW  = maxX - minX + padding * 2;
    const boxH  = maxY - minY + padding * 2;
    const cx    = (minX + maxX) / 2;
    const cy    = (minY + maxY) / 2;

    const svgW  = svgEl.clientWidth;
    const svgH  = svgEl.clientHeight;

    // Fit scale: show the whole bounding box, capped at 2×
    const scale = Math.min(2, svgW / boxW, svgH / boxH);

    this.zoomPlugin.zoomTo(cx, cy, scale);
  }

  /** Resets all visual highlighting applied by `search()`. */
  clearSearch(): void
  {
    this.applyHighlight([]);
  }

  // ─── Private ────────────────────────────────────────────────────────────────

  private applyHighlight(results: D3Node[]): void
  {
    const nodeSelection = this.graph?.renderer.nodeRenderer.getSelection();
    if (!nodeSelection) return;

    const matchIds = new Set(results.map((n) => n.id));
    const hasQuery = results.length > 0;

    // Operate on each <g> and its children via d3.select(this)
    nodeSelection.each(function (d)
    {
      const g       = d3.select<SVGGElement, D3Node>(this);
      const matched = matchIds.has(d.id);

      // Dim / restore opacity on the whole group
      g.transition()
        .duration(200)
        .attr('opacity', hasQuery && !matched ? DIM_OPACITY : 1);

      // Highlight circle stroke for matching nodes
      g.select<SVGCircleElement>('circle')
        .transition()
        .duration(200)
        .attr('stroke',       matched ? HIGHLIGHT_COLOR : '#fff')
        .attr('stroke-width', matched ? HIGHLIGHT_STROKE  : 1.5);
    });
  }
}
