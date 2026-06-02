import type * as d3 from 'd3';
import type { D3Node, D3Edge } from './node-edge';

// ─── D3 Selection Type Aliases ────────────────────────────────────────────────
// Digunakan di GraphView dan plugin untuk keterbacaan.

export type SvgSelection      = d3.Selection<SVGSVGElement,   unknown, null,       undefined>;
export type ViewportSelection = d3.Selection<SVGGElement,     unknown, null,       undefined>;
export type NodeSelection     = d3.Selection<SVGGElement,     D3Node,  SVGGElement, unknown>;
export type EdgeSelection     = d3.Selection<SVGPathElement,  D3Edge,  SVGGElement, unknown>;

// ─── GraphView ────────────────────────────────────────────────────────────────
// State dari view SVG yang dibaca dan dimodifikasi oleh plugin.
// GraphView di-build ulang setiap render() / update() — referensinya segar.
// Plugin wajib meng-null-kan reference view di teardown().

export interface GraphView
{
  // ── D3 Selections (read-only, merepresentasikan apa yang ada di DOM) ───────

  /** Root SVG element selection. */
  readonly svg: SvgSelection | null;

  /** Group <g class="viewport"> — target zoom transform. */
  readonly viewport: ViewportSelection | null;

  /**
   * Semua node <g class="node"> group, ter-bind dengan D3Node data.
   * State sinkron dengan `nodes` — objek yang sama di-bind ke selection ini.
   */
  readonly nodeSelection: NodeSelection | null;

  /**
   * Semua edge <line> elements, ter-bind dengan D3Edge data.
   * State sinkron dengan `edges` — objek yang sama di-bind ke selection ini.
   */
  readonly edgeSelection: EdgeSelection | null;

  // ── Live data (referensi yang SAMA dengan yang di-bind ke selection) ────────

  /**
   * Array node yang aktif ditampilkan di layar.
   * Sama persis dengan objek yang di-bind ke nodeSelection.
   * D3 simulation mutasi x/y langsung di sini — selalu up-to-date.
   */
  readonly nodes: D3Node[];

  /**
   * Array edge yang aktif ditampilkan di layar.
   * Sama persis dengan objek yang di-bind ke edgeSelection.
   */
  readonly edges: D3Edge[];

  // ── Built-in capabilities ────────────────────────────────────────────────────

  /**
   * Panaskan simulasi kembali — dipakai setelah drag node atau layout change.
   * @param alpha Target alpha (default 0.3)
   */
  reheat(alpha?: number): void;

  /** Dinginkan simulasi — dipakai saat drag node selesai. */
  cool(): void;

  /**
   * Programmatic zoom ke titik di graph-space.
   * Terintegrasi dengan d3.zoom — interaksi manual selanjutnya
   * konsisten dari state zoom yang baru.
   *
   * @param x        Target X di graph-space
   * @param y        Target Y di graph-space
   * @param scale    Zoom level (default 2)
   * @param duration Durasi transisi ms (default 750)
   */
  zoomTo(x: number, y: number, scale?: number, duration?: number): void;

  // ── Visual helpers ────────────────────────────────────────────────────────────

  /**
   * Jalankan updater function pada nodeSelection saat ini.
   * Untuk modifikasi visual (opacity, stroke, warna) tanpa mengubah set node.
   *
   * @example
   * view.updateNodes(sel => sel.attr('opacity', d => dimmed.has(d.id) ? 0.1 : 1));
   */
  updateNodes(updater: (sel: NodeSelection) => void): void;

  /**
   * Jalankan updater function pada edgeSelection saat ini.
   * Untuk modifikasi visual tanpa mengubah set edge.
   */
  updateEdges(updater: (sel: EdgeSelection) => void): void;

  // ── Incremental set changes ───────────────────────────────────────────────────

  /**
   * Mengganti set node yang ditampilkan TANPA full re-render.
   * Menggunakan D3 general update pattern (enter / exit / update):
   * - Node di set baru DAN lama → dipertahankan, posisi x/y tidak direset
   * - Node baru → di-append ke DOM dengan style awal
   * - Node tidak ada di set baru → di-remove dari DOM
   *
   * Simulation diupdate otomatis agar hanya simulate node yang aktif.
   * Panggil `view.reheat()` setelahnya jika perlu relayout.
   *
   * @example
   * // Collapse plugin — sembunyikan child nodes
   * const visible = view.nodes.filter(n => !collapsedIds.has(n.pathId));
   * view.applyNodes(visible);
   * view.applyEdges(view.edges.filter(e => isEdgeVisible(e, visible)));
   * view.reheat(0.2);
   */
  applyNodes(nodes: D3Node[]): void;

  /**
   * Mengganti set edge yang ditampilkan TANPA full re-render.
   * Sama seperti applyNodes — D3 enter/exit/update pattern.
   * Simulation forceLink diupdate dengan edge set baru.
   */
  applyEdges(edges: D3Edge[]): void;
}
