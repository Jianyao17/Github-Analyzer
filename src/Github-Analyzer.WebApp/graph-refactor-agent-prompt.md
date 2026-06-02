# Agent Prompt: Graph API Refactor

## Peran & Konteks

Kamu adalah AI coding agent yang bertugas melakukan refactor pada **graph visualization library** berbasis D3.js di project Vue 3 + TypeScript. Library ini digunakan untuk merender `CodeGraph` dari backend sebagai interactive force-directed graph.

Sebelum menulis satu baris kode pun, kamu **wajib** menyelesaikan tiga fase berurutan:

1. **Fase Baca** — pahami codebase yang ada
2. **Fase Verifikasi** — konfirmasi rencana arsitektur berdasarkan temuan
3. **Fase Implementasi** — baru tulis kode

Jangan melompat ke implementasi sebelum verifikasi disetujui.

---

## FASE 1: BACA CODEBASE

Baca **semua file berikut** secara lengkap sebelum melanjutkan. Jangan skip file apapun.

### File yang wajib dibaca

#### Entry point & composable
```
src/lib/graph/composables/useGraphD3.ts
```

#### Core engine (saat ini)
```
src/lib/graph/graph.main.ts
src/lib/graph/graph.config.ts
src/lib/graph/graph.debug.ts
```

#### Renderer
```
src/lib/graph/renderer/d3.renderer.ts
src/lib/graph/renderer/node.renderer.ts
src/lib/graph/renderer/edge.renderer.ts
```

#### Plugins
```
src/lib/graph/plugins/zoom-drag.plugin.ts
src/lib/graph/plugins/hover.plugin.ts
src/lib/graph/plugins/search.plugin.ts
src/lib/graph/plugins/selection.plugin.ts
```

#### Types
```
src/lib/graph/types/_index.ts
src/lib/graph/types/plugin.ts
src/lib/graph/types/graph-view.ts
src/lib/graph/types/graph-data.ts
src/lib/graph/types/node-edge.ts
src/lib/graph/types/config.ts
src/lib/graph/types/renderer.ts
```

#### Utils
```
src/lib/graph/utils/simulation.ts
src/lib/graph/utils/graph-data.ts
src/lib/graph/utils/geometry.ts
src/lib/graph/utils/label.ts
src/lib/graph/utils/icon.ts
src/lib/graph/utils/colors.ts
```

#### Layout (existing)
```
src/lib/graph/layout/layout.interface.ts
src/lib/graph/layout/layout.manager.ts
src/lib/graph/layout/hierarchical.layout.ts
src/lib/graph/layout/star-balloon.layout.ts
```

### Yang harus dipahami dari pembacaan

Setelah membaca, kamu harus bisa menjawab pertanyaan-pertanyaan ini secara internal (tidak perlu ditulis, tapi harus jadi landasan analisis):

1. Bagaimana `GraphD3` saat ini memegang state simulation, zoom, dan plugin?
2. Bagaimana `GraphView` dibangun — apakah ia stabil antara `render()` dan `update()` atau dibangun ulang?
3. Apakah ada method wrapping atau type cast berbahaya (`as unknown as ...`)?
4. Bagaimana plugin saat ini mengakses DOM (langsung lewat `view.updateNodes` / `view.nodeSelection`, atau ada abstraksi)?
5. Bagaimana `GraphDebugger` bekerja — apakah ia memonkey-patch method?
6. Apakah ada `getComputedTextLength()` atau DOM measurement lain di hot path?
7. Di mana `d3.zoom` diinisialisasi dan bagaimana ia dihubungkan ke viewport?
8. Bagaimana teardown order plugin saat ini ditentukan?
9. Apa kontrak `ILayout` yang sudah ada — apakah sudah ada atau belum?
10. Bagaimana `useGraphD3.ts` mengonsumsi `GraphD3` — API apa yang di-expose ke Vue?

---

## FASE 2: VERIFIKASI RENCANA ARSITEKTUR

Setelah membaca codebase, lakukan verifikasi berikut **sebelum implementasi**. Untuk setiap poin, konfirmasi apakah rencana sesuai dengan kondisi aktual codebase, dan catat jika ada penyesuaian yang diperlukan.

### 2.1 Verifikasi struktur folder target

Konfirmasi bahwa struktur folder berikut dapat diterapkan ke project ini tanpa konflik path atau build config:

```
src/lib/graph/
├── core/
│   ├── GraphEngine.ts
│   ├── GraphContext.ts
│   ├── EventBus.ts
│   ├── PluginRegistry.ts
│   └── SimulationController.ts
├── render/
│   ├── RenderPipeline.ts
│   ├── passes/
│   │   ├── NodePass.ts
│   │   ├── EdgePass.ts
│   │   └── MarkerPass.ts
│   └── measure/
│       └── TextMeasurer.ts
├── layout/
│   ├── layout.interface.ts     ← mungkin sudah ada, verifikasi
│   ├── layout.manager.ts       ← mungkin sudah ada, verifikasi
│   ├── hierarchical.layout.ts  ← mungkin sudah ada, verifikasi
│   └── star-balloon.layout.ts  ← mungkin sudah ada, verifikasi
├── plugins/
│   ├── zoom.plugin.ts          ← BARU (pisah dari zoom-drag)
│   ├── drag.plugin.ts          ← BARU (pisah dari zoom-drag)
│   ├── hover.plugin.ts         ← MODIFIKASI
│   ├── search.plugin.ts        ← MODIFIKASI
│   └── selection.plugin.ts     ← MODIFIKASI
├── types/
│   ├── events.ts               ← BARU
│   └── ...existing             ← sesuaikan, jangan hapus yang masih dipakai
├── utils/                      ← sebagian besar tetap
└── composables/
    └── useGraphD3.ts           ← MODIFIKASI public API
```

**Yang harus dikonfirmasi:**
- Apakah folder `layout/` sudah ada dengan file-file tersebut?
- Apakah ada alias path (`@graph`, `@graph.types`) di tsconfig atau vite config yang perlu diperbarui?
- Apakah ada file index barrel (`_index.ts`) yang mengekspor ulang dan perlu diperbarui?

### 2.2 Verifikasi kontrak yang berubah

Untuk setiap perubahan kontrak berikut, konfirmasi tidak ada consumer lain di luar `lib/graph/` yang bergantung pada kontrak lama:

| Kontrak lama | Kontrak baru | Risk |
|---|---|---|
| `new GraphD3({...})` | `new GraphEngine({...})` | Cek semua import `graph.main.ts` |
| `graph.use(plugin)` | `engine.use(plugin, {priority})` | Cek semua penggunaan `.use()` |
| `plugin.setup(data, view)` | `plugin.setup(ctx, view)` | Semua plugin harus diupdate |
| `GraphView` type | `GraphContext` + `RenderView` | Cek semua type import |
| `view.updateNodes(fn)` | `ctx.bus.emit('highlight:nodes', ...)` | Semua plugin yang memanggil ini |
| `view.zoomTo(x, y, scale)` | `ctx.bus.emit('zoom:to', {...})` | Search plugin |

**Untuk setiap baris di tabel:** cari seluruh codebase (termasuk di luar `lib/graph/`) apakah ada penggunaan yang perlu diupdate.

### 2.3 Verifikasi GraphDebugger

Konfirmasi bahwa `GraphDebugger` saat ini menggunakan pattern berikut:
```ts
(graph as unknown as Record<string, unknown>)[methodName] = (...args) => { ... }
```

Jika iya, rencana adalah mengganti ini dengan `DebugPlugin` yang mendengarkan event `render:complete` dari `EventBus`. Konfirmasi apakah ada consumer `GraphDebugger` di luar `useGraphD3.ts`.

### 2.4 Verifikasi DOM measurement

Cari semua penggunaan `getComputedTextLength()` atau `getBoundingClientRect()` di hot path render. Rencana adalah menggantinya dengan `TextMeasurer` berbasis `canvas.getContext('2d').measureText()`. Konfirmasi lokasi pastinya.

### 2.5 Verifikasi plugin ZoomDrag

Konfirmasi bahwa `zoom-drag.plugin.ts` saat ini menggabungkan dua concern:
- D3 zoom behavior (pan & zoom SVG)
- D3 drag behavior (drag individual nodes)

Rencana adalah memisah menjadi `zoom.plugin.ts` dan `drag.plugin.ts`. Konfirmasi tidak ada plugin lain yang mengimport atau bergantung pada `ZoomDragPlugin` secara langsung.

### 2.6 Verifikasi ILayout contract

Cek apakah `layout.interface.ts` sudah ada. Jika sudah ada, bandingkan kontrak aktualnya dengan kontrak target berikut:

```ts
export interface LayoutResult {
  positions: Map<string, { x: number; y: number }> | null;
  animationHint: 'instant' | 'tween' | 'simulate';
}

export interface ILayout {
  readonly name: string;
  apply(nodes: D3Node[], edges: D3Edge[], dims: Dimensions): LayoutResult;
}
```

Jika kontrak aktual berbeda, catat perbedaannya dan tentukan mana yang lebih sesuai dengan implementasi yang sudah ada.

### 2.7 Output verifikasi

Setelah selesai verifikasi, buat ringkasan singkat dengan format:

```
VERIFIKASI SELESAI

Temuan kritis:
- [list temuan yang mengubah rencana implementasi]

Penyesuaian rencana:
- [list penyesuaian dari rencana awal]

Siap implementasi: [Ya / Tidak — jika Tidak, sebutkan blocker]
```

**Hanya lanjut ke Fase 3 jika output verifikasi menyatakan "Siap implementasi: Ya".**

---

## FASE 3: IMPLEMENTASI

Implementasi dilakukan **berurutan sesuai urutan di bawah**. Setiap langkah harus selesai dan tidak memunculkan TypeScript error sebelum lanjut ke langkah berikutnya. Jalankan `tsc --noEmit` setelah setiap langkah jika memungkinkan.

### Langkah 1 — Types & Events

**File baru:** `src/lib/graph/types/events.ts`

Buat typed event map yang menjadi kontrak antara plugin dan engine. Ini adalah langkah pertama karena semua langkah selanjutnya bergantung pada types ini.

```ts
import type { D3Node, D3Edge } from './node-edge';
import type { ILayout } from '../layout/layout.interface';

export interface GraphEvents {
  // View mutations (dari plugin → RenderPipeline)
  'view:nodes-changed':  { nodes: D3Node[]; edges: D3Edge[] };
  'view:reset':           void;

  // Highlight (dari SearchPlugin / CollapsePlugin → NodePass)
  'highlight:nodes':     { ids: Set<string>; dimOpacity: number };
  'highlight:clear':      void;

  // Node interaction (dari DragPlugin / HoverPlugin → consumer)
  'node:click':          { node: D3Node; event: MouseEvent };
  'node:hover':          { node: D3Node | null };

  // Zoom (dari SearchPlugin → ZoomPlugin)
  'zoom:to':             { x: number; y: number; scale: number; duration?: number };
  'zoom:fit':            { padding?: number };

  // Simulation (dari CollapsePlugin / LayoutManager → SimulationController)
  'simulation:reheat':   { alpha?: number };
  'simulation:cool':      void;
  'simulation:settled':   void;

  // Layout (dari LayoutManager → SimulationController)
  'layout:change':       { layout: ILayout };

  // Lifecycle (dari GraphEngine → DebugPlugin)
  'render:complete':     { elapsed: number; nodeCount: number; edgeCount: number };
  'render:tween-positions': { positions: Map<string, { x: number; y: number }> };
}
```

Tambahkan juga type `Dimensions`:
```ts
export interface Dimensions {
  width:  number;
  height: number;
}
```

Update `types/_index.ts` untuk mengekspor `GraphEvents` dan `Dimensions`.

---

### Langkah 2 — EventBus

**File baru:** `src/lib/graph/core/EventBus.ts`

```ts
import type { GraphEvents } from '../types/events';

type Listener<T> = (payload: T) => void;
type Unsubscribe  = () => void;

export class EventBus {
  private _listeners = new Map<string, Set<Listener<unknown>>>();

  on<K extends keyof GraphEvents>(
    event:    K,
    listener: Listener<GraphEvents[K]>,
  ): Unsubscribe {
    let set = this._listeners.get(event as string);
    if (!set) {
      set = new Set();
      this._listeners.set(event as string, set);
    }
    set.add(listener as Listener<unknown>);
    return () => set!.delete(listener as Listener<unknown>);
  }

  emit<K extends keyof GraphEvents>(event: K, payload: GraphEvents[K]): void {
    this._listeners
      .get(event as string)
      ?.forEach(fn => fn(payload));
  }

  clear(): void {
    this._listeners.clear();
  }
}
```

---

### Langkah 3 — SimulationController

**File baru:** `src/lib/graph/core/SimulationController.ts`

Pindahkan semua logika D3 simulation dari `utils/simulation.ts` dan `graph.main.ts` ke sini. `SimulationController` adalah satu-satunya class yang boleh memanggil `d3.forceSimulation`.

```ts
import * as d3 from 'd3';
import type { D3Node, D3Edge } from '../types/node-edge';
import type { Dimensions }     from '../types/events';
import type { GraphConfig }    from '../types/config';
import type { EventBus }       from './EventBus';

export class SimulationController {
  private _sim:    d3.Simulation<D3Node, D3Edge> | null = null;
  private _bus:    EventBus;
  private _config: GraphConfig;

  constructor(bus: EventBus, config: GraphConfig) {
    this._bus    = bus;
    this._config = config;
    this._listenBusEvents();
  }

  get sim(): d3.Simulation<D3Node, D3Edge> | null {
    return this._sim;
  }

  start(
    nodes:    D3Node[],
    edges:    D3Edge[],
    dims:     Dimensions,
    onTick:   () => void,
  ): void {
    this.stop();

    const { linkDistance = 60, chargeStrength = -150, alphaMin = 0.005, alphaDecay = 0.025 } =
      this._config.simulation ?? {};

    this._sim = d3
      .forceSimulation<D3Node, D3Edge>(nodes)
      .alphaMin(alphaMin)
      .alphaDecay(alphaDecay)
      .force('link',
        d3.forceLink<D3Node, D3Edge>(edges)
          .id(d => d.id)
          .distance(linkDistance),
      )
      .force('charge', d3.forceManyBody().strength(chargeStrength))
      .force('center',  d3.forceCenter(dims.width / 2, dims.height / 2))
      .force('collide', d3.forceCollide<D3Node>().radius(d => (d._radius ?? 5) + 2))
      .on('tick', onTick)                                        // ← direct call, no bus
      .on('end',  () => this._bus.emit('simulation:settled', undefined as never));
  }

  stop(): void {
    if (!this._sim) return;
    (this._sim.force('link') as d3.ForceLink<D3Node, D3Edge> | null)?.links([]);
    this._sim.force('charge',  null);
    this._sim.force('center',  null);
    this._sim.force('collide', null);
    this._sim.force('link',    null);
    this._sim.nodes([]);
    this._sim.stop();
    this._sim = null;
  }

  private _listenBusEvents(): void {
    this._bus.on('simulation:reheat', ({ alpha = 0.3 }) => {
      this._sim?.alphaTarget(alpha).restart();
    });
    this._bus.on('simulation:cool', () => {
      this._sim?.alphaTarget(0);
    });
  }
}
```

---

### Langkah 4 — GraphContext

**File baru:** `src/lib/graph/core/GraphContext.ts`

`GraphContext` adalah objek stabil yang di-share ke semua plugin. Tidak pernah dibangun ulang antara `render()` dan `update()`.

```ts
import type * as d3        from 'd3';
import type { D3Node }      from '../types/node-edge';
import type { NodeSelection, EdgeSelection, SvgSelection, ViewportSelection } from '../types/graph-view';
import type { EventBus }    from './EventBus';
import type { SimulationController } from './SimulationController';

export class GraphContext {
  readonly bus: EventBus;
  readonly sim: SimulationController;

  // Selections diupdate in-place — tidak pernah diganti dengan objek baru
  private _svg:           SvgSelection      | null = null;
  private _viewport:      ViewportSelection | null = null;
  private _nodeSelection: NodeSelection     | null = null;
  private _edgeSelection: EdgeSelection     | null = null;

  // Live data arrays (referensi yang sama dengan yang di-bind ke D3)
  private _nodes: D3Node[] = [];

  constructor(bus: EventBus, sim: SimulationController) {
    this.bus = bus;
    this.sim = sim;
  }

  // Dipanggil oleh RenderPipeline setelah setiap render/update
  updateRefs(
    svg:      SvgSelection,
    viewport: ViewportSelection,
    nodes:    NodeSelection,
    edges:    EdgeSelection,
    liveNodes: D3Node[],
  ): void {
    this._svg           = svg;
    this._viewport      = viewport;
    this._nodeSelection = nodes;
    this._edgeSelection = edges;
    this._nodes         = liveNodes;
  }

  get svg():           SvgSelection      | null { return this._svg; }
  get viewport():      ViewportSelection | null { return this._viewport; }
  get nodeSelection(): NodeSelection     | null { return this._nodeSelection; }
  get edgeSelection(): EdgeSelection     | null { return this._edgeSelection; }
  get nodes():         D3Node[]                 { return this._nodes; }
}
```

---

### Langkah 5 — PluginRegistry

**File baru:** `src/lib/graph/core/PluginRegistry.ts`

```ts
import type { GraphPlugin }  from '../types/plugin';
import type { GraphContext }  from './GraphContext';
import type { GraphData }     from '../types/graph-data';

interface RegisteredPlugin {
  plugin:   GraphPlugin;
  priority: number;
}

export class PluginRegistry {
  private _plugins: RegisteredPlugin[] = [];

  /**
   * Daftarkan plugin dengan priority.
   * Priority rendah = setup lebih dulu, teardown paling akhir.
   * Gunakan priority 0 untuk ZoomPlugin (harus aktif sebelum plugin lain).
   */
  register(plugin: GraphPlugin, priority = 5): this {
    this._plugins.push({ plugin, priority });
    this._plugins.sort((a, b) => a.priority - b.priority);
    return this;
  }

  setupAll(ctx: GraphContext, data: GraphData): void {
    for (const { plugin } of this._plugins) {
      plugin.setup(ctx, data);
    }
  }

  /** Teardown dalam urutan terbalik dari setup (priority tinggi dulu). */
  teardownAll(): void {
    for (const { plugin } of [...this._plugins].reverse()) {
      try {
        plugin.teardown?.();
      } catch (err) {
        console.warn(`[PluginRegistry] teardown error in '${plugin.name}':`, err);
      }
    }
  }

  get names(): string[] {
    return this._plugins.map(p => p.plugin.name);
  }
}
```

---

### Langkah 6 — Update kontrak GraphPlugin

**File modifikasi:** `src/lib/graph/types/plugin.ts`

Update signature `setup()` agar menerima `GraphContext` bukan `GraphView`:

```ts
import type { GraphContext } from '../core/GraphContext';
import type { GraphData }    from './graph-data';

export interface GraphPlugin {
  readonly name: string;

  /**
   * Priority teardown (opsional, informational).
   * PluginRegistry menggunakan nilai saat registrasi, bukan dari sini.
   */
  readonly priority?: number;

  /**
   * Dipanggil setelah render selesai dan simulation berjalan.
   * @param ctx   GraphContext stabil — aman disimpan sebagai field plugin.
   * @param data  GraphData read-only — jangan mutasi.
   */
  setup(ctx: GraphContext, data: GraphData): void;

  /** Bersihkan semua event listener dan referensi eksternal. */
  teardown?(): void;
}
```

---

### Langkah 7 — TextMeasurer

**File baru:** `src/lib/graph/render/measure/TextMeasurer.ts`

Mengganti `getComputedTextLength()` yang memerlukan DOM reflow.

```ts
export class TextMeasurer {
  private _canvas: HTMLCanvasElement;
  private _ctx:    CanvasRenderingContext2D;
  private _cache = new Map<string, number>();

  constructor() {
    this._canvas = document.createElement('canvas');
    this._ctx    = this._canvas.getContext('2d')!;
  }

  /**
   * Mengukur lebar teks tanpa menyentuh DOM / memicu reflow.
   * Hasil di-cache per kombinasi (text, font).
   *
   * @param text  String yang diukur
   * @param font  CSS font string, contoh: '12px "Segoe UI", sans-serif'
   */
  measure(text: string, font: string): number {
    const key = `${font}|${text}`;
    if (this._cache.has(key)) return this._cache.get(key)!;

    this._ctx.font = font;
    const width    = this._ctx.measureText(text).width;
    this._cache.set(key, width);
    return width;
  }

  /** Bersihkan cache jika font config berubah. */
  clearCache(): void {
    this._cache.clear();
  }
}
```

---

### Langkah 8 — RenderPipeline & Passes

**File baru:** `src/lib/graph/render/RenderPipeline.ts`

`RenderPipeline` menggantikan `D3Renderer` sebagai orchestrator render. Ia mendelegasikan ke pass-pass yang bisa dikonfigurasi.

```ts
import * as d3 from 'd3';
import type { D3Node, D3Edge } from '../types/node-edge';
import type { GraphConfig }    from '../types/config';
import type { SvgSelection, ViewportSelection, NodeSelection, EdgeSelection } from '../types/graph-view';
import type { EventBus }       from '../core/EventBus';
import { NodePass }            from './passes/NodePass';
import { EdgePass }            from './passes/EdgePass';
import { MarkerPass }          from './passes/MarkerPass';
import { TextMeasurer }        from './measure/TextMeasurer';

export interface RenderOutput {
  svg:           SvgSelection;
  viewport:      ViewportSelection;
  nodeSelection: NodeSelection;
  edgeSelection: EdgeSelection;
  liveNodes:     D3Node[];
  liveEdges:     D3Edge[];
  dimensions:    { width: number; height: number };
}

export class RenderPipeline {
  private _container: HTMLElement;
  private _config:    GraphConfig;
  private _bus:       EventBus;
  private _measurer:  TextMeasurer;

  private _svg:       SvgSelection      | null = null;
  private _viewport:  ViewportSelection | null = null;

  readonly nodePass:  NodePass;
  readonly edgePass:  EdgePass;
  readonly markerPass: MarkerPass;

  constructor(container: HTMLElement, config: GraphConfig, bus: EventBus) {
    this._container  = container;
    this._config     = config;
    this._bus        = bus;
    this._measurer   = new TextMeasurer();
    this.nodePass    = new NodePass(this._measurer);
    this.edgePass    = new EdgePass();
    this.markerPass  = new MarkerPass();
  }

  run(nodes: D3Node[], edges: D3Edge[]): RenderOutput {
    const width  = this._container.clientWidth  || 800;
    const height = this._container.clientHeight || 600;

    // Init SVG
    d3.select(this._container).selectAll('svg').remove();
    this._svg = d3.select(this._container)
      .append('svg')
      .attr('width',   '100%')
      .attr('height',  '100%')
      .attr('viewBox', `0 0 ${width} ${height}`);

    this._viewport = this._svg.append('g').attr('class', 'viewport');

    // Jalankan passes
    this.markerPass.run(this._svg, this._config);
    const edgeSel = this.edgePass.run(this._viewport, edges, this._config);
    const nodeSel = this.nodePass.run(this._viewport, nodes, this._config);

    // Listen ke view mutations dari bus
    this._listenBusEvents(nodes, edges);

    return {
      svg:           this._svg,
      viewport:      this._viewport,
      nodeSelection: nodeSel,
      edgeSelection: edgeSel,
      liveNodes:     nodes,
      liveEdges:     edges,
      dimensions:    { width, height },
    };
  }

  onTick(): void {
    this.edgePass.updatePositions();
    this.nodePass.updatePositions();
  }

  destroy(): void {
    this._svg?.remove();
    this._svg     = null;
    this._viewport = null;
    this.nodePass.clear();
    this.edgePass.clear();
  }

  private _listenBusEvents(liveNodes: D3Node[], liveEdges: D3Edge[]): void {
    this._bus.on('view:nodes-changed', ({ nodes, edges }) => {
      this.nodePass.apply(nodes, this._config);
      this.edgePass.apply(edges, this._config);
    });

    this._bus.on('highlight:nodes', ({ ids, dimOpacity }) => {
      this.nodePass.applyHighlight(ids, dimOpacity);
    });

    this._bus.on('highlight:clear', () => {
      this.nodePass.clearHighlight();
    });
  }
}
```

**Pass files** (`NodePass.ts`, `EdgePass.ts`, `MarkerPass.ts`) adalah hasil **memindahkan** logika dari `node.renderer.ts`, `edge.renderer.ts`, dan bagian arrow marker di `edge.renderer.ts`. Logika tidak berubah, hanya dipindah dan digabung dengan highlight logic dari `SearchPlugin` (yang sebelumnya ada di plugin, sekarang masuk ke `NodePass`).

---

### Langkah 9 — GraphEngine

**File baru:** `src/lib/graph/core/GraphEngine.ts`

Ini menggantikan `GraphD3` (file `graph.main.ts`). Jangan hapus `graph.main.ts` dulu — rename ke `graph.main.legacy.ts` sebagai fallback selama transisi.

```ts
import type { GraphData }      from '../types/graph-data';
import type { GraphConfig }    from '../types/config';
import type { GraphPlugin }    from '../types/plugin';
import type { ILayout }        from '../layout/layout.interface';
import { EventBus }            from './EventBus';
import { GraphContext }        from './GraphContext';
import { PluginRegistry }      from './PluginRegistry';
import { SimulationController } from './SimulationController';
import { RenderPipeline }      from '../render/RenderPipeline';
import { defaultGraphConfig }  from '../graph.config';

export interface GraphEngineOptions {
  container: HTMLElement;
  config?:   Partial<GraphConfig>;
}

export class GraphEngine {
  private readonly _bus:      EventBus;
  private readonly _sim:      SimulationController;
  private readonly _ctx:      GraphContext;
  private readonly _registry: PluginRegistry;
  private readonly _pipeline: RenderPipeline;
  private readonly _config:   GraphConfig;

  private _currentData: GraphData | null = null;

  constructor({ container, config }: GraphEngineOptions) {
    this._config   = { ...defaultGraphConfig, ...(config ?? {}) };
    this._bus      = new EventBus();
    this._sim      = new SimulationController(this._bus, this._config);
    this._ctx      = new GraphContext(this._bus, this._sim);
    this._registry = new PluginRegistry();
    this._pipeline = new RenderPipeline(container, this._config, this._bus);
  }

  /** Expose context untuk consumer (misalnya LayoutManager). */
  get ctx(): GraphContext {
    return this._ctx;
  }

  use(plugin: GraphPlugin, priority?: number): this {
    this._registry.register(plugin, priority);
    return this;
  }

  useLayout(layout: ILayout): this {
    // LayoutManager bisa diterima juga jika mengimplementasi ILayout
    this._bus.on('layout:change', ({ layout: l }) => {
      if (!this._currentData) return;
      const result = l.apply(
        this._ctx.nodes,
        [],  // edges dari sim
        { width: 800, height: 600 }, // dims dari pipeline
      );
      if (result.positions) {
        this._bus.emit('render:tween-positions', { positions: result.positions });
      }
    });
    return this;
  }

  render(data: GraphData): void {
    const t0 = performance.now();

    this._currentData   = data;
    const d3Nodes       = data.nodes.map(n => ({ ...n, id: n.pathId }));
    const d3Edges       = [...data.sourceRelEdges, ...data.useRelEdges]
      .map(e => ({ ...e, source: e.from, target: e.to }));

    const output = this._pipeline.run(d3Nodes, d3Edges as any);

    this._ctx.updateRefs(
      output.svg,
      output.viewport,
      output.nodeSelection,
      output.edgeSelection,
      output.liveNodes,
    );

    this._sim.start(output.liveNodes, output.liveEdges as any, output.dimensions, () => {
      this._pipeline.onTick();
    });

    this._registry.setupAll(this._ctx, data);

    this._bus.emit('render:complete', {
      elapsed:   performance.now() - t0,
      nodeCount: d3Nodes.length,
      edgeCount: d3Edges.length,
    });
  }

  update(data: GraphData): void {
    this._registry.teardownAll();
    this._sim.stop();
    this.render(data);
    // _ctx tetap instance yang sama — tidak ada stale refs
  }

  destroy(): void {
    this._registry.teardownAll();
    this._sim.stop();
    this._pipeline.destroy();
    this._bus.clear();
  }
}
```

---

### Langkah 10 — Refactor plugins

Refactor keempat plugin dengan urutan berikut. Untuk setiap plugin:
- Ganti parameter `setup(data, view)` → `setup(ctx, data)`
- Ganti akses `view.updateNodes(fn)` → `ctx.bus.emit('highlight:nodes', ...)`
- Ganti `view.zoomTo(...)` → `ctx.bus.emit('zoom:to', ...)`
- Simpan `ctx` sebagai field untuk dipakai di method lain
- Simpan unsubscribe functions dari `ctx.bus.on(...)` dan panggil di `teardown()`

**Urutan refactor:**

1. **`zoom.plugin.ts`** (BARU — pisah dari `zoom-drag.plugin.ts`)
   - Tangani `d3.zoom`, attach ke SVG dari `ctx.svg`
   - Listen `'zoom:to'` dan `'zoom:fit'` dari bus
   - Priority: 0

2. **`drag.plugin.ts`** (BARU — pisah dari `zoom-drag.plugin.ts`)
   - Tangani `d3.drag` pada `ctx.nodeSelection`
   - Emit `'simulation:reheat'` saat drag start
   - Emit `'simulation:cool'` saat drag end
   - Priority: 1

3. **`hover.plugin.ts`** (MODIFIKASI)
   - Ganti `view.nodeSelection` → `ctx.nodeSelection`
   - Emit `'node:hover'` via bus (opsional, untuk extensibility)
   - Priority: 2

4. **`search.plugin.ts`** (MODIFIKASI)
   - Ganti `view.updateNodes(fn)` → `ctx.bus.emit('highlight:nodes', ...)`
   - Ganti `view.zoomTo(...)` → `ctx.bus.emit('zoom:to', ...)`
   - Priority: 4

5. **`selection.plugin.ts`** (MODIFIKASI)
   - Ganti `view.updateNodes(fn)` → `ctx.bus.emit('highlight:nodes', ...)`
   - Priority: 3

---

### Langkah 11 — DebugPlugin (gantikan GraphDebugger)

**File baru:** `src/lib/graph/plugins/debug.plugin.ts`

```ts
import type { GraphPlugin }  from '../types/plugin';
import type { GraphContext }  from '../core/GraphContext';
import type { GraphData }     from '../types/graph-data';

export interface DebugPluginConfig {
  enabled:    boolean;
  logMemory?: boolean;
}

export class DebugPlugin implements GraphPlugin {
  readonly name     = 'debug';
  readonly priority = 999;

  private _config: DebugPluginConfig;
  private _unsub:  (() => void)[] = [];

  constructor(config: DebugPluginConfig) {
    this._config = config;
  }

  setup(ctx: GraphContext, _data: GraphData): void {
    if (!this._config.enabled) return;

    this._unsub.push(
      ctx.bus.on('render:complete', ({ elapsed, nodeCount, edgeCount }) => {
        console.group('%c[GraphEngine] render:complete', 'color:#60A5FA;font-weight:bold');
        console.log(`⏱  Time    ${elapsed.toFixed(2)} ms`);
        console.log(`📦 Nodes   ${nodeCount}`);
        console.log(`🔗 Edges   ${edgeCount}`);
        if (this._config.logMemory) {
          const heap = (performance as any).memory?.usedJSHeapSize;
          if (heap) console.log(`🧠 Heap    ${(heap / 1024 / 1024).toFixed(2)} MB`);
        }
        console.groupEnd();
      }),
      ctx.bus.on('simulation:settled', () => {
        console.log('%c[GraphEngine] simulation settled', 'color:#34D399');
      }),
    );
  }

  teardown(): void {
    this._unsub.forEach(fn => fn());
    this._unsub = [];
  }
}
```

---

### Langkah 12 — Update useGraphD3.ts

Update composable agar menggunakan `GraphEngine` dan mengekspos API yang sama ke Vue component (untuk backward compatibility).

```ts
// composables/useGraphD3.ts
import { watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { Ref }        from 'vue';
import type { CodeGraph }  from '@/types/analysis/code-graph';
import type { D3Node }     from '@/lib/graph/types/node-edge';
import type { GraphPlugin } from '@/lib/graph/types/plugin';
import { GraphEngine }     from '@/lib/graph/core/GraphEngine';
import { ZoomPlugin }      from '@/lib/graph/plugins/zoom.plugin';
import { DragPlugin }      from '@/lib/graph/plugins/drag.plugin';
import { HoverPlugin }     from '@/lib/graph/plugins/hover.plugin';
import { SearchPlugin }    from '@/lib/graph/plugins/search.plugin';
import { DebugPlugin }     from '@/lib/graph/plugins/debug.plugin';
import { buildGraphData }  from '@/lib/graph/utils/graph-data';

export interface UseGraphEngineOptions {
  plugins?: GraphPlugin[];
  debug?:   boolean;
}

export function useGraphD3(
  containerRef: Ref<HTMLElement | null>,
  dataRef:      Ref<CodeGraph | null>,
  options:      UseGraphEngineOptions = {},
) {
  let engine: GraphEngine | null = null;

  const searchPlugin = new SearchPlugin();

  function init(raw: CodeGraph): void {
    const data = buildGraphData(raw);
    if (!containerRef.value) return;

    if (!engine) {
      engine = new GraphEngine({ container: containerRef.value });

      engine
        .use(new ZoomPlugin(),                             0)
        .use(new DragPlugin(),                             1)
        .use(new HoverPlugin(),                            2)
        .use(searchPlugin,                                 4)
        .use(new DebugPlugin({
          enabled:    options.debug ?? import.meta.env.DEV,
          logMemory:  true,
        }),                                              999);

      options.plugins?.forEach(p => engine!.use(p));
      engine.render(data);
    } else {
      engine.update(data);
    }
  }

  watch(dataRef, newData => { if (newData) init(newData); });

  onMounted(async () => {
    if (dataRef.value) { await nextTick(); init(dataRef.value); }
  });

  onUnmounted(() => {
    engine?.destroy();
    engine = null;
  });

  return {
    getEngine:    ()                              => engine,
    search:       (query: string): D3Node[]      => searchPlugin.search(query),
    focusNode:    (node: D3Node, scale?: number) => searchPlugin.focusNode(node, scale),
    focusResults: (results: D3Node[], padding?: number) => searchPlugin.focusResults(results, padding),
    clearSearch:  ()                             => searchPlugin.clearSearch(),
  };
}
```

---

### Langkah 13 — Cleanup & validasi akhir

1. **Hapus** `src/lib/graph/graph.debug.ts` (digantikan `DebugPlugin`)
2. **Hapus** `src/lib/graph/plugins/zoom-drag.plugin.ts` (digantikan `zoom.plugin.ts` + `drag.plugin.ts`)
3. **Rename** `src/lib/graph/graph.main.ts` → `src/lib/graph/graph.main.legacy.ts` (simpan sebagai referensi, hapus setelah dipastikan tidak ada consumer)
4. **Update** `src/lib/graph/types/_index.ts` untuk mengekspor semua type baru
5. **Jalankan** `tsc --noEmit` — harus 0 error
6. **Jalankan** `vite build` — harus berhasil
7. **Verifikasi manual** di browser: render graph, drag node, hover, search, zoom

---

## Konvensi Kode

Ikuti konvensi ini **konsisten di seluruh kode yang ditulis**.

### Penamaan

| Kategori | Konvensi | Contoh |
|---|---|---|
| Class | PascalCase | `GraphEngine`, `EventBus`, `NodePass` |
| Interface | PascalCase | `ILayout`, `GraphPlugin`, `GraphEvents` |
| Type alias | PascalCase | `Unsubscribe`, `Listener`, `Dimensions` |
| Private field | `_camelCase` (underscore prefix) | `_bus`, `_sim`, `_listeners` |
| Public field/getter | `camelCase` (tanpa underscore) | `bus`, `sim`, `nodeSelection` |
| Method | `camelCase` | `setupAll()`, `teardownAll()` |
| Private method | `_camelCase` | `_listenBusEvents()`, `_buildView()` |
| Constant | `SCREAMING_SNAKE_CASE` | `DIM_OPACITY`, `HIGHLIGHT_COLOR` |
| Event name (bus) | `'domain:action'` (kebab-case dengan colon separator) | `'zoom:to'`, `'highlight:nodes'`, `'simulation:reheat'` |
| File | `kebab-case.suffix.ts` | `graph-engine.ts`, `node.pass.ts`, `zoom.plugin.ts` |
| Folder | `kebab-case` | `core/`, `render/`, `passes/` |

### Struktur class

```ts
// Urutan member dalam class:
// 1. static fields/constants
// 2. private fields (_prefix)
// 3. constructor
// 4. public getters
// 5. public methods
// 6. private methods (_prefix)
```

### Import ordering

```ts
// 1. external libraries (d3, vue, etc.)
// 2. type imports (import type ...)
// 3. internal absolute imports (@/...)
// 4. internal relative imports (../...)
```

### Event handler pattern

```ts
// Pattern standar untuk listen bus di constructor/setup:
private _listenBusEvents(): void {
  this._unsub = [
    this._bus.on('event:name', payload => this._handleEventName(payload)),
    // ...
  ];
}

// Teardown wajib unsubscribe semua:
teardown(): void {
  this._unsub.forEach(fn => fn());
  this._unsub = [];
}
```

### Tidak boleh dilakukan

- ❌ Jangan gunakan `as unknown as X` — selalu temukan cara type-safe
- ❌ Jangan panggil `bus.emit()` di dalam D3 tick handler
- ❌ Jangan mutasi `GraphData.nodes` atau `GraphData.edges` langsung dari plugin
- ❌ Jangan simpan referensi ke `GraphView` lama — selalu gunakan `GraphContext`
- ❌ Jangan panggil `getComputedTextLength()` — gunakan `TextMeasurer`
- ❌ Jangan deklarasikan `d3.forceSimulation` di luar `SimulationController`

---

## Checklist Selesai

Tandai setiap item sebelum menyatakan refactor selesai:

- [ ] `tsc --noEmit` — 0 error, 0 warning baru
- [ ] `vite build` — berhasil
- [ ] Graph merender dengan benar di browser
- [ ] Drag node berfungsi
- [ ] Hover tooltip muncul
- [ ] Search highlight dan focus berfungsi
- [ ] Zoom pan berfungsi
- [ ] `update()` dipanggil dengan data baru — tidak ada stale ref error di console
- [ ] `destroy()` dipanggil saat unmount — tidak ada memory leak (cek via Chrome DevTools heap snapshot)
- [ ] DebugPlugin log muncul di console saat `DEV` mode
- [ ] Tidak ada import dari `graph.main.ts` yang tersisa (kecuali `graph.main.legacy.ts`)
- [ ] Tidak ada import dari `zoom-drag.plugin.ts` yang tersisa
- [ ] Tidak ada import dari `graph.debug.ts` yang tersisa
