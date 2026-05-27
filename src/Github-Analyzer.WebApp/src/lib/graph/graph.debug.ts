import type { GraphD3 } from './graph.main';

// ─── Config ───────────────────────────────────────────────────────────────────

export interface GraphDebugConfig
{
  /** Master switch. Set to false and none of this runs. */
  enabled: boolean;

  /** Log render() / update() elapsed time. Default: true */
  logRenderTime?: boolean;

  /** Log node and edge counts after render. Default: true */
  logNodeCount?: boolean;

  /** Log simulation tick count and settle time. Default: true */
  logSimulation?: boolean;

  /**
   * Estimate average FPS during simulation.
   * D3 simulation ticks are synced with d3.timer → RAF, so tick rate ≈ frame rate.
   * Default: true
   */
  logFps?: boolean;

  /**
   * Log JS heap usage before and after render.
   * Only available in Chromium browsers (performance.memory).
   * Default: false
   */
  logMemory?: boolean;
}

type ResolvedConfig = Required<GraphDebugConfig>;

const CONFIG_DEFAULTS: Omit<ResolvedConfig, 'enabled'> =
{
  logRenderTime: true,
  logNodeCount:  true,
  logSimulation: true,
  logFps:        true,
  logMemory:     false,
};

// ─── GraphDebugger ────────────────────────────────────────────────────────────

/**
 * GraphDebugger — non-invasive performance instrumentation for GraphD3.
 *
 * Works by wrapping render() and update() on the graph instance.
 * Must be attached BEFORE graph.render() is called.
 *
 * Usage:
 * ```ts
 * const graph = new GraphD3({ container, data });
 * graph.use(new ZoomDragPlugin()).use(new HoverPlugin());
 *
 * new GraphDebugger({ enabled: import.meta.env.DEV }).attachTo(graph);
 *
 * graph.render();
 * ```
 */
export class GraphDebugger
{
  private readonly config: ResolvedConfig;

  constructor(config: GraphDebugConfig)
  {
    this.config = { ...CONFIG_DEFAULTS, ...config };
  }

  /**
   * Wraps render() and update() on the given graph instance.
   * If enabled is false, this is a no-op — zero overhead in production.
   */
  attachTo(graph: GraphD3): void
  {
    if (!this.config.enabled) return;

    this.wrapMethod(graph, 'render', () =>
    {
      this.observeSimulation(graph, 'render()');
    });

    this.wrapMethod(graph, 'update', () =>
    {
      this.observeSimulation(graph, 'update()');
    });
  }

  // ─── Private ──────────────────────────────────────────────────────────────

  /**
   * Wraps a single method on `graph` with timing instrumentation.
   * `afterHook` is called after the original method completes.
   */
  private wrapMethod(
    graph: GraphD3,
    methodName: 'render' | 'update',
    afterHook: () => void,
  ): void
  {
    const original = (graph[methodName] as (...args: unknown[]) => void).bind(graph);

    // Double-cast via `unknown` is required here: GraphD3 has no index signature,
    // so TypeScript won't allow a direct cast to Record<string,unknown>.
    // This is the TypeScript-sanctioned pattern for intentional method wrapping.
    (graph as unknown as Record<string, unknown>)[methodName] = (...args: unknown[]) =>
    {
      const heapBefore = this.readHeap();
      const t0         = performance.now();

      original(...args);

      const elapsed   = performance.now() - t0;
      const heapAfter = this.readHeap();

      this.printLifecycleStats(methodName + '()', elapsed, graph, heapBefore, heapAfter);
      afterHook();
    };
  }

  /** Logs render/update stats to the console. */
  private printLifecycleStats(
    label:      string,
    elapsed:    number,
    graph:      GraphD3,
    heapBefore: number | null,
    heapAfter:  number | null,
  ): void
  {
    const nodeCount = graph.simulation?.nodes().length ?? 0;
    const linkForce = graph.simulation?.force('link') as any;
    const edgeCount = (linkForce?.links?.() as any[] | undefined)?.length ?? 0;

    console.group(`%c[GraphD3] ${label}`, 'color:#60A5FA;font-weight:bold');

    if (this.config.logRenderTime)
      console.log(`⏱  Time       ${elapsed.toFixed(2)} ms`);

    if (this.config.logNodeCount)
    {
      console.log(`📦 Nodes      ${nodeCount}`);
      console.log(`🔗 Edges      ${edgeCount}`);
    }

    if (this.config.logMemory && heapBefore !== null && heapAfter !== null)
    {
      const delta = heapAfter - heapBefore;
      const sign  = delta >= 0 ? '+' : '';
      console.log(`🧠 Heap       ${this.formatBytes(heapAfter)}  (${sign}${this.formatBytes(delta)})`);
    }

    console.groupEnd();
  }

  /**
   * Attaches `.debug` listeners to the simulation to measure:
   * - Total tick count and wall-clock settle time
   * - Estimated FPS (based on tick rate — D3 timer syncs with RAF)
   *
   * Listeners are cleaned up automatically when simulation ends.
   */
  private observeSimulation(graph: GraphD3, renderLabel: string): void
  {
    const sim = graph.simulation;
    if (!sim) return;
    if (!this.config.logSimulation && !this.config.logFps) return;

    let tickCount = 0;
    const simStart    = performance.now();
    const tickTimings: number[] = [];

    sim.on('tick.debug', () =>
    {
      tickCount++;
      if (this.config.logFps) tickTimings.push(performance.now());
    });

    sim.on('end.debug', () =>
    {
      const elapsed = performance.now() - simStart;

      console.group(`%c[GraphD3] simulation  (after ${renderLabel})`, 'color:#34D399;font-weight:bold');

      if (this.config.logSimulation)
      {
        const avgInterval = tickCount > 0 ? (elapsed / tickCount).toFixed(1) : '—';
        console.log(`⏱  Settled     ${tickCount} ticks in ${elapsed.toFixed(0)} ms`);
        console.log(`⚙  Avg tick    ${avgInterval} ms / tick`);
      }

      if (this.config.logFps && tickTimings.length >= 2)
      {
        const fps = this.estimateFps(tickTimings);
        const bar = this.fpsBar(fps);
        console.log(`🎞  Est. FPS    ${fps.toFixed(1)}  ${bar}`);
      }

      console.groupEnd();

      // Remove our own listeners — don't interfere with the main tick handler
      sim.on('tick.debug', null);
      sim.on('end.debug', null);
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────

  /**
   * Estimates average FPS from an array of per-tick timestamps.
   * Since D3's timer syncs ticks with requestAnimationFrame,
   * the tick rate closely mirrors the actual rendering frame rate.
   */
  private estimateFps(timestamps: number[]): number
  {
    const duration = timestamps[timestamps.length - 1] - timestamps[0];
    if (duration <= 0) return 0;
    return ((timestamps.length - 1) / duration) * 1000;
  }

  /** Returns a simple ASCII bar for quick visual FPS assessment. */
  private fpsBar(fps: number): string
  {
    if (fps >= 55) return '████████ 60';
    if (fps >= 45) return '██████░░ ~50';
    if (fps >= 30) return '████░░░░ ~30';
    return              '██░░░░░░ <30 ⚠';
  }

  /** Reads Chrome's non-standard usedJSHeapSize. Returns null in other browsers. */
  private readHeap(): number | null
  {
    return (performance as any).memory?.usedJSHeapSize ?? null;
  }

  private formatBytes(bytes: number): string
  {
    const abs = Math.abs(bytes);
    if (abs < 1024)        return `${bytes} B`;
    if (abs < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return                        `${(bytes / 1024 / 1024).toFixed(2)} MB`;
  }
}
