import * as d3 from 'd3';
import type { 
  D3Node, GraphConfig, NodeCardConfig, 
  NodeTypeStyle, NodeSelection, INodeRenderer 
} from '@graph.types';

import { NODE_TYPE_KEYS } from '../graph.config';
import { getLucideIconBody } from '../utils/icon';
import { truncateLabel } from '../utils/label';

// ─── Default Card Config ──────────────────────────────────────────────────────
// Used as fallback when config.nodeCard is not provided.

const DEFAULT_CARD: NodeCardConfig =
{
  height:          24,
  iconSize:        14,
  paddingLeft:      8,
  paddingRight:     8,
  gap:              4,

  labelFontFamily: '\'Segoe UI\', sans-serif',
  labelLetterSpacing: 0, 
  labelFontSize:   12,
   
  cornerRadius:    12,
  approxCharWidth:  7.5,
  arrowGap:         4,
};

// ─── NodeRenderer ─────────────────────────────────────────────────────────────

export class NodeRenderer implements INodeRenderer
{
  // Selection semua node group — merepresentasikan apa yang tampil di layar
  private selection: NodeSelection | null = null;

  /**
   * Render awal: buat <g> per node sebagai kartu persegi panjang.
   * Menggunakan D3 general update pattern agar dapat di-reuse oleh applyNodes().
   * Menganotasi d._radius (circumscribed circle) untuk collision force.
   */
  render(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    nodes:    D3Node[],
    config:   GraphConfig,
  ): void
  {
    let container = viewport.select<SVGGElement>('g.nodes');
    if (container.empty())
      container = viewport.append('g').attr('class', 'nodes');

    this.selection = this._applyUpdatePattern(container, nodes, config);
  }

  /**
   * Mengganti set node yang ditampilkan TANPA full re-render.
   * D3 general update pattern:
   * - Node sama (by id) → dipertahankan, posisi tidak direset
   * - Node baru → di-append ke DOM
   * - Node tidak ada → di-remove dari DOM
   */
  applyNodes(nodes: D3Node[], config: GraphConfig): void
  {
    if (!this.selection) return;

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
    const card = { ...DEFAULT_CARD, ...config.nodeCard };

    const bound = container
      .selectAll<SVGGElement, D3Node>('g.node')
      .data(nodes, (d) => d.id);

    // ── Exit: hapus node yang tidak ada di set baru ──────────────────────────
    bound.exit().remove();

    // ── Enter: tambah node baru ──────────────────────────────────────────────
    const entered = bound
      .enter()
      .append('g')
      .attr('class',  'node')
      .attr('cursor', 'grab');

    entered.each(function (d)
    {
      const typeKey = NODE_TYPE_KEYS[d.type] ?? 'default';
      const style   = config.nodeTypes[typeKey] ?? config.nodeTypes['default'];
      const g       = d3.select<SVGGElement, D3Node>(this);

      _renderNodeCard(g, d, style, card);
    });

    // ── Merge: gabungkan entered + existing untuk selection yang lengkap ─────
    return entered.merge(bound);
  }
}

// ─── Card Render Helper ───────────────────────────────────────────────────────

/**
 * Renders the full card anatomy for a single node group <g>.
 * Extracted as a module-level function to keep _applyUpdatePattern readable.
 *
 * Card layout (centered at 0,0):
 *   ┌──────────────────────────────┐
 *   │  [icon]  Label Text...       │
 *   │     ●                        │
 *   └──────────────────────────────┘
 *    ↑ color dot (below icon area)
 */
function _renderNodeCard(
  g:     d3.Selection<SVGGElement, D3Node, null, undefined>,
  d:     D3Node,
  style: NodeTypeStyle,
  card:  NodeCardConfig,
): void
{
  const shortLabel  = truncateLabel(d.label);
  const isTruncated = shortLabel !== d.label;
  
  // 1. Render teks terlebih dahulu secara sementara untuk mengukur lebar aslinya di DOM
  const textEl = g.append('text')
    .attr('dominant-baseline', 'middle')
    .attr('font-family',       card.labelFontFamily)
    .attr('font-size',         card.labelFontSize)
    .attr('letter-spacing',    card.labelLetterSpacing)
    .attr('fill',              'currentColor') // Auto adapt text color
    // NOTE: pointer-events TIDAK di-set ke none agar saat label meluber (expand),
    // mouse yang berada di atas teks yang meluber tetap mempertahankan status :hover pada node <g>
    .text(shortLabel);

  // Ambil ukuran aslinya dari browser (fallback ke hitungan kasar jika gagal)
  const textNode  = textEl.node();
  
  // Tambahkan buffer 4px untuk mengatasi perbedaan font-family fallback saat render awal
  const textWidth = (textNode 
    ? textNode.getComputedTextLength() 
    : (shortLabel.length * card.approxCharWidth)) + 4;

  // 2. Kalkulasi dimensi presisi
  const cardWidth = card.paddingLeft + card.iconSize + card.gap + textWidth + card.paddingRight;
  const hw = cardWidth / 2;
  const hh = card.height / 2;
  
  // Simpan dimensi untuk digunakan oleh edge.renderer
  d._hw = hw;
  d._hh = hh;

  // Radius tabrakan untuk simulasi
  d._radius = Math.sqrt(hw * hw + hh * hh);

  const iconLeft = -hw + card.paddingLeft;
  const labelX   = iconLeft + card.iconSize + card.gap;

  // 3. Posisikan teks ke koordinat akhir
  textEl
    .attr('x', labelX)
    .attr('y', 0);

  // 4. Background card rect 
  // Gunakan warna dari tipe node agar lebih mudah dibedakan, tanpa border
  g.insert('rect', 'text')
    .attr('x', -hw)
    .attr('y', -hh)
    .attr('width',  cardWidth)
    .attr('height', card.height)
    .attr('rx',     card.cornerRadius)
    .attr('fill',   style.color)
    .attr('fill-opacity', 0.2)
    .attr('stroke', 'none');

  // 5. Lucide icon
  g.insert('svg', 'text')
    .attr('x', iconLeft)
    .attr('y', -card.iconSize / 2)
    .attr('width',  card.iconSize)
    .attr('height', card.iconSize)
    .attr('viewBox', '0 0 24 24')
    .style('color',   style.color)
    .html(getLucideIconBody(style.icon));

  // 6. Hover Behavior
  if (isTruncated)
  {
    g.on('mouseenter.label', function()
    {
      textEl.text(d.label);
      // NOTE: Tidak menggunakan appendChild(this) karena akan men-detach elemen
      // dari DOM dan memicu mouseleave pada hover plugin.
    })
      .on('mouseleave.label', function()
      {
        textEl.text(shortLabel);
      });
  }
}
