import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig, IGraphRenderer } from '@graph.types';
import { NodeRenderer } from './node.renderer';
import { EdgeRenderer } from './edge.renderer';

/**
 * D3Renderer — orchestrates SVG setup dan mendelegasikan rendering ke sub-renderers.
 *
 * Responsibilities:
 *   - Membuat dan memiliki root <svg> dan <g class="viewport">
 *   - Render edges dulu (z-order: edges di bawah nodes)
 *   - Proxy tick updates ke sub-renderers
 *
 * Tidak mengandung: business logic, selection state, atau interaction handling.
 */
export class D3Renderer implements IGraphRenderer
{
  private svg:      d3.Selection<SVGSVGElement, unknown, null, undefined> | null = null;
  private viewport: d3.Selection<SVGGElement,   unknown, null, undefined> | null = null;

  // Sub-renderers — typed oleh interface agar konsumen hanya akses kontrak yang didefinisikan
  readonly nodeRenderer: NodeRenderer = new NodeRenderer();
  readonly edgeRenderer: EdgeRenderer = new EdgeRenderer();

  /** Membuat root SVG dan viewport group di dalam container. */
  init(container: HTMLElement): void
  {
    // Hapus SVG lama yang ada (safe untuk re-render setelah destroy)
    d3.select(container).selectAll('svg').remove();

    const width  = container.clientWidth  || 800;
    const height = container.clientHeight || 600;

    this.svg = d3
      .select(container)
      .append('svg')
      .attr('width',   '100%')
      .attr('height',  '100%')
      .attr('viewBox', `0 0 ${width} ${height}`);

    // Semua konten yang dirender ada di dalam viewport agar zoom transform bersih
    this.viewport = this.svg.append('g').attr('class', 'viewport');
  }

  /**
   * Render edges lalu nodes (z-order: edges di bawah nodes).
   * Harus dipanggil setelah init().
   */
  render(nodes: D3Node[], edges: D3Edge[], config: GraphConfig): void
  {
    if (!this.svg || !this.viewport) return;

    this.edgeRenderer.render(this.viewport, this.svg, edges, config);
    this.nodeRenderer.render(this.viewport, nodes, config);
  }

  /** Proxy position updates ke sub-renderers. Dipanggil setiap simulation tick. */
  onTick(): void
  {
    this.edgeRenderer.updatePositions();
    this.nodeRenderer.updatePositions();
  }

  /** Returns root SVG selection. */
  getSvg(): d3.Selection<SVGSVGElement, unknown, null, undefined> | null
  {
    return this.svg;
  }

  /** Returns viewport group selection. */
  getViewport(): d3.Selection<SVGGElement, unknown, null, undefined> | null
  {
    return this.viewport;
  }

  /**
   * Menghapus SVG dari DOM dan mereset internal references.
   * Instance D3Renderer TIDAK dihancurkan — bisa reuse dengan init() + render() lagi.
   */
  destroy(): void
  {
    this.svg?.remove();
    this.svg      = null;
    this.viewport = null;
    this.nodeRenderer.clear();
    this.edgeRenderer.clear();
  }
}
