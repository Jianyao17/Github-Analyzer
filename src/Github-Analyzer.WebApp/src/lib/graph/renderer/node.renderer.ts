import * as d3 from 'd3';
import type { D3Node, GraphConfig, NodeSelection, INodeRenderer } from '@graph.types';
import { NODE_TYPE_KEYS } from '../graph.config';

export class NodeRenderer implements INodeRenderer
{
  // Selection semua node group — merepresentasikan apa yang tampil di layar
  private selection: NodeSelection | null = null;

  /**
   * Render awal: buat <g> per node dengan <circle>, <text>, <title>.
   * Menggunakan D3 general update pattern agar bisa di-reuse oleh applyNodes().
   * Juga menganotasi d._radius pada tiap datum untuk collision force.
   */
  render(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    nodes:    D3Node[],
    config:   GraphConfig,
  ): void
  {
    // Buat atau ambil container group <g class="nodes">
    let container = viewport.select<SVGGElement>('g.nodes');
    
    if (container.empty())
      container = viewport.append('g').attr('class', 'nodes');

    this.selection = this._applyUpdatePattern(container, nodes, config);
  }

  /**
   * Mengganti set node yang ditampilkan TANPA full re-render.
   * Menggunakan D3 general update pattern:
   * - Node yang sama (by id) → dipertahankan, posisi tidak direset
   * - Node baru → di-append ke DOM
   * - Node tidak ada → di-remove dari DOM
   *
   * Dipanggil oleh GraphView.applyNodes().
   */
  applyNodes(nodes: D3Node[], config: GraphConfig): void
  {
    if (!this.selection) return;

    // Ambil parent container dari selection yang ada
    this.selection.select<SVGGElement>(function () 
    {
      return (this as SVGGElement).parentNode as SVGGElement;
    });

    // Jika parent tidak bisa diambil lewat selection (karena selection adalah children),
    // kita ambil langsung dari DOM node pertama
    const firstNode = this.selection.node();
    if (!firstNode) return;

    const parentEl = firstNode.parentNode as SVGGElement | null;
    if (!parentEl) return;

    const parentSel = d3.select<SVGGElement, unknown>(parentEl);
    this.selection  = this._applyUpdatePattern(parentSel, nodes, config);
  }

  /** Called on every simulation tick — repositions all node groups. */
  updatePositions(): void
  {
    this.selection?.attr('transform', (d) => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }

  /** Returns the D3 selection for node groups, used by plugins and GraphView. */
  getSelection(): NodeSelection | null
  {
    return this.selection;
  }

  /** Reset internal state — dipanggil oleh D3Renderer.destroy(). */
  clear(): void
  {
    this.selection = null;
  }

  // ─── Private ───────────────────────────────────────────────────────────────

  /**
   * D3 general update pattern — inti dari both render() dan applyNodes().
   * Menangani enter (node baru), exit (node dihapus), update (node tetap ada).
   */
  private _applyUpdatePattern(
    container: d3.Selection<SVGGElement, unknown, null, undefined>,
    nodes:     D3Node[],
    config:    GraphConfig,
  ): NodeSelection
  {
    const bound = container
      .selectAll<SVGGElement, D3Node>('g.node')
      .data(nodes, (d) => d.id); // key function: stable binding by id

    // ── Exit: hapus node yang tidak ada di set baru ──────────────────────────
    bound.exit().remove();

    // ── Enter: tambah node baru ──────────────────────────────────────────────
    const entered = bound
      .enter()
      .append('g')
      .attr('class', 'node')
      .attr('cursor', 'grab');

    entered.each(function (d)
    {
      const typeKey = NODE_TYPE_KEYS[d.type] ?? 'default';
      const style   = config.nodeTypes[typeKey] ?? config.nodeTypes['default'];

      // Anotasi radius untuk collision force
      d._radius = style.radius;

      const g = d3.select<SVGGElement, D3Node>(this);

      g.append('circle')
        .attr('r',            style.radius)
        .attr('fill',         style.color)
        .attr('stroke',       '#fff')
        .attr('stroke-width', 1.5);

      g.append('text')
        .attr('dx',             style.radius + 4)
        .attr('dy',             '0.35em')
        .attr('font-size',      '10px')
        .attr('fill',           'currentColor')
        .attr('pointer-events', 'none')
        .text(d.label);

      // Native browser tooltip (lightweight fallback)
      g.append('title').text(`${d.label}\n${d.pathId}`);
    });

    // ── Merge: gabungkan entered + existing untuk selection yang lengkap ─────
    return entered.merge(bound);
  }
}
