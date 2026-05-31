import type * as d3 from 'd3';
import type { GraphConfig } from './config';
import type { D3Node, D3Edge } from './node-edge';
import type { NodeSelection, EdgeSelection } from './graph-view';

// ─── Renderer Interface Types ─────────────────────────────────────────────────
// Plugin dan GraphD3 bergantung pada interface ini, bukan implementasi konkretnya.
// Ini memisahkan kontrak dari implementasi dan menghindari circular import.

export interface INodeRenderer
{
  getSelection(): NodeSelection | null;
  clear(): void;

  /**
   * Mengganti set node yang ditampilkan tanpa full re-render.
   * Menggunakan D3 general update pattern (enter/exit/merge).
   */
  applyNodes(nodes: D3Node[], config: GraphConfig): void;
}

export interface IEdgeRenderer
{
  getSelection(): EdgeSelection | null;
  clear(): void;

  /**
   * Mengganti set edge yang ditampilkan tanpa full re-render.
   * Menggunakan D3 general update pattern (enter/exit/merge).
   */
  applyEdges(edges: D3Edge[], config: GraphConfig): void;
}

export interface IGraphRenderer
{
  // ── Sub-renderers ─────────────────────────────────────────────────────────
  readonly nodeRenderer: INodeRenderer;
  readonly edgeRenderer: IEdgeRenderer;

  // ── Accessor ─────────────────────────────────────────────────────────────
  getSvg():      d3.Selection<SVGSVGElement, unknown, null, undefined> | null;
  getViewport(): d3.Selection<SVGGElement,   unknown, null, undefined> | null;

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  init(container: HTMLElement): void;
  render(nodes: D3Node[], edges: D3Edge[], config: GraphConfig): void;
  onTick(): void;
  destroy(): void;
}
