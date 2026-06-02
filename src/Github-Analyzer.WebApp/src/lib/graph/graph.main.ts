import * as d3 from 'd3';
import type {
  GraphData, GraphConfig, 
  D3Node, D3Edge, GraphView,
  NodeSelection, EdgeSelection,
  IGraphRenderer, GraphPlugin,
} from '@graph.types';

import { LayoutManager } from './layout/layout.manager';

import { defaultGraphConfig } from './graph.config';
import { D3Renderer } from './renderer/d3.renderer';

export interface GraphD3Options
{
  data:      GraphData;
  container: HTMLElement;
  config?:   Partial<GraphConfig>;
}

/**
 * GraphD3 — facade dan entry point untuk graph visualization library.
 *
 * Usage:
 * ```ts
 * const graph = new GraphD3({ container, data });
 *
 * graph
 *   .use(new ZoomDragPlugin())
 *   .use(new SelectionPlugin(node => console.log(node)))
 *   .use(new HoverPlugin());
 *
 * graph.render();
 *
 * // Later, when data changes:
 * graph.update(newData);
 *
 * // On component unmount:
 * graph.destroy();
 * ```
 */
export class GraphD3
{
  readonly container: HTMLElement;
  readonly renderer:  IGraphRenderer;

  // Graph Simulation
  layoutManager: LayoutManager | null = null;

  get simulation(): d3.Simulation<D3Node, D3Edge> | null {
    return this.layoutManager?.getSimulation() ?? null;
  }
  private data:    GraphData;
  private config:  GraphConfig;
  private plugins: Map<string, GraphPlugin> = new Map();

  // Built-in zoom behavior — diekspos via GraphView.zoomTo()
  private zoom: d3.ZoomBehavior<SVGSVGElement, unknown> | null = null;

  // GraphView saat ini — dibuild ulang setiap render()
  private currentView: GraphView | null = null;

  constructor({ container, data, config }: GraphD3Options)
  {
    this.data      = data;
    this.config    = { ...defaultGraphConfig, ...(config ?? {}) };
    this.renderer  = new D3Renderer();
    this.container = container;
  }

  /**
   * Mendaftarkan plugin. Returns `this` untuk chaining.
   * Plugin dengan nama yang sama mengganti registrasi sebelumnya.
   */
  use(plugin: GraphPlugin): this
  {
    this.plugins.set(plugin.name, plugin);
    return this;
  }

  /** Render graph. Harus dipanggil setelah semua plugin didaftarkan. */
  render(): void
  {
    const width  = this.container.clientWidth  || 800;
    const height = this.container.clientHeight || 600;

    // Prepare nodes and edges for D3. 
    // We augment nodes with id (pathId) and map edges to D3 format (source, target).
    const d3Nodes = this.data.nodes.map((n) => ({ ...n, id: n.pathId }));
    const d3Edges = this.data.sourceRelEdges
      .concat(this.data.useRelEdges)
      .map((e) => ({ ...e, source: e.from, target: e.to })) as D3Edge[];

    // 1. Init SVG (re-entrant: renderer.destroy() sudah clear SVG lama)
    this.renderer.init(this.container);
    this.renderer.render(d3Nodes, d3Edges, this.config);

    // 2. Built-in zoom behavior — diinisialisasi setelah SVG ada
    this._initZoom();

    // 3. Buat layout manager dan force simulation
    this.layoutManager = new LayoutManager(d3Nodes, d3Edges, width, height, this.config);
    this.layoutManager.getSimulation().on('tick', () => this.renderer.onTick());

    // 4. Build GraphView — snapshot semua refs + helper methods
    this.currentView = this._buildView(d3Nodes, d3Edges);

    // 5. Setup plugins — setelah render() dan simulation siap
    for (const plugin of this.plugins.values())
    {
      plugin.setup(this.data, this.currentView);
    }
  }

  /**
   * Mengganti data graph dan re-render.
   * Plugin dipertahankan dan di-setup ulang otomatis.
   */
  update(data: GraphData): void
  {
    this._teardown();
    this.data = data;
    this.render();
  }

  /**
   * Cleanup penuh: stop simulation, teardown plugins, hapus SVG.
   * Panggil ini saat Vue component unmount.
   */
  destroy(): void
  {
    this._teardown();
    this.plugins.clear();
  }

  // ─── Private ─────────────────────────────────────────────────────────────────

  /**
   * Inisialisasi d3.zoom dan attach ke SVG.
   * Zoom transform di-apply ke viewport group agar konten ikut pan/zoom.
   */
  private _initZoom(): void
  {
    const svg      = this.renderer.getSvg();
    const viewport = this.renderer.getViewport();
    if (!svg || !viewport) return;

    this.zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => viewport.attr('transform', event.transform));

    svg.call(this.zoom);
  }

  /**
   * Build GraphView dari state saat ini.
   * View di-build ulang setiap render() agar references selalu segar.
   */
  private _buildView(d3Nodes: D3Node[], d3Edges: D3Edge[]): GraphView
  {
    // Capture references untuk dipakai dalam closures
    const renderer         = this.renderer;
    const getZoom          = () => this.zoom;
    const getLayoutManager = () => this.layoutManager;
    const config           = this.config;

    const view: GraphView =
    {
      // ── D3 Selections ───────────────────────────────────────────────────────
      get svg()           { return renderer.getSvg(); },
      get viewport()      { return renderer.getViewport(); },
      get nodeSelection() { return renderer.nodeRenderer.getSelection() as NodeSelection | null; },
      get edgeSelection() { return renderer.edgeRenderer.getSelection() as EdgeSelection | null; },

      // ── Live data ──────────────────────────────────────────────────────────
      // Referensi ke array yang sama dengan yang di-bind ke D3 selection.
      // Saat simulation mutasi x/y, perubahan langsung terlihat di sini.
      nodes: d3Nodes,
      edges: d3Edges,

      // ── Built-in capabilities ──────────────────────────────────────────────

      reheat(alpha = 0.3): void
      {
        const manager = getLayoutManager();
        if (manager) manager.reheat(alpha);
      },

      cool(): void
      {
        const manager = getLayoutManager();
        if (manager) manager.cool();
      },

      zoomTo(x: number, y: number, scale = 2, duration = 750): void
      {
        const svg    = renderer.getSvg();
        const zoom   = getZoom();
        if (!svg || !zoom) return;

        const svgEl = svg.node();
        if (!svgEl) return;

        const tx = svgEl.clientWidth  / 2 - x * scale;
        const ty = svgEl.clientHeight / 2 - y * scale;

        svg
          .transition()
          .duration(duration)
          .call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
      },

      // ── Visual helpers ─────────────────────────────────────────────────────

      updateNodes(updater): void
      {
        const sel = renderer.nodeRenderer.getSelection() as NodeSelection | null;
        if (sel) updater(sel);
      },

      updateEdges(updater): void
      {
        const sel = renderer.edgeRenderer.getSelection() as EdgeSelection | null;
        if (sel) updater(sel);
      },

      // ── Incremental set changes ────────────────────────────────────────────

      applyNodes(nodes: D3Node[]): void
      {
        renderer.nodeRenderer.applyNodes(nodes, config);

        // Sync live data array — hapus lama, isi dengan yang baru
        d3Nodes.length = 0;
        d3Nodes.push(...nodes);

        // Update layout manager
        const manager = getLayoutManager();
        if (manager) {
          manager.updateData(d3Nodes, d3Edges);
          manager.recalculate();
        }
      },

      applyEdges(edges: D3Edge[]): void
      {
        renderer.edgeRenderer.applyEdges(edges, config);

        // Sync live data array
        d3Edges.length = 0;
        d3Edges.push(...edges);

        // Update layout manager
        const manager = getLayoutManager();
        if (manager) {
          manager.updateData(d3Nodes, d3Edges);
          manager.recalculate();
        }
      },
    };

    return view;
  }

  /**
   * Internal teardown — dipakai oleh update() dan destroy().
   * TIDAK membersihkan plugins agar update() bisa re-setup mereka setelah re-render.
   */
  private _teardown(): void
  {
    if (this.layoutManager)
    {
      this.layoutManager.destroy();
      this.layoutManager = null;
    }

    for (const plugin of this.plugins.values())
    {
      plugin.teardown?.();
    }

    // renderer.destroy() menghapus SVG DOM element dan meng-null internal refs.
    // Instance D3Renderer (this.renderer) tetap hidup untuk reuse.
    this.renderer.destroy();

    this.zoom        = null;
    this.currentView = null;
  }
}
