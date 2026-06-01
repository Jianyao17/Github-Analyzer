import type { Ref } from 'vue';
import { watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { CodeGraph } from '@/types/analysis/code-graph';
import type { GraphPlugin, D3Node } from '@graph.types';

import { GraphD3 } from '@graph/graph.main';
import { GraphDebugger } from '@graph/graph.debug';
import { ZoomDragPlugin } from '@graph/plugins/zoom-drag.plugin';
import { SearchPlugin } from '@graph/plugins/search.plugin';
import { HoverPlugin } from '@graph/plugins/hover.plugin';
import { buildGraphData } from '@graph/utils/graph-data';

export interface UseGraphD3Options
{
  /** Extra plugins untuk didaftarkan di samping plugin default. */
  plugins?: GraphPlugin[];

  /** Aktifkan DEV-only performance profiling. Default: import.meta.env.DEV. */
  debug?: boolean;
}

/**
 * useGraphD3 — thin Vue composable wrapper untuk GraphD3 library.
 *
 * Default plugins: ZoomDrag, Hover, Search.
 * DEV performance profiling di-attach otomatis via GraphDebugger.
 *
 * Returns search API untuk dihubungkan ke Vue search UI:
 * ```ts
 * const { search, focusNode, focusResults, clearSearch } =
 *   useGraphD3(containerRef, graphData);
 *
 * const results = search('UserService');
 * if (results[0]) focusNode(results[0]);
 * ```
 */
export function useGraphD3(
  containerRef: Ref<HTMLElement | null>,
  dataRef:      Ref<CodeGraph | null>,
  options:      UseGraphD3Options = {},
)
{
  let graph: GraphD3 | null = null;

  // Plugin instances dibuat sekali dan dipakai ulang lintas update() cycle.
  // SearchPlugin tidak lagi memerlukan ZoomDragPlugin reference.
  const searchPlugin = new SearchPlugin();

  function initGraph(raw: CodeGraph): void
  {
    const data = buildGraphData(raw);
    if (!containerRef.value) return;

    if (!graph)
    {
      // ── First render ────────────────────────────────────────────────────────
      graph = new GraphD3({
        container: containerRef.value,
        data,
      });

      graph
        .use(new ZoomDragPlugin())
        .use(searchPlugin)
        .use(new HoverPlugin());

      // Daftarkan extra plugins dari caller
      options.plugins?.forEach((p) => graph!.use(p));

      // DEV performance profiling — zero cost in production
      new GraphDebugger({
        enabled:   options.debug ?? import.meta.env.DEV,
        logMemory: true,
      }).attachTo(graph);

      graph.render();
    }
    else
    {
      // ── Subsequent renders — plugins dipertahankan ──────────────────────────
      graph.update(data);
    }
  }

  // Re-render saat data berubah
  watch(dataRef, (newData) =>
  {
    if (newData) initGraph(newData);
  });

  // Render saat mount jika data sudah tersedia
  // (nextTick memastikan container punya dimensi nyata, bukan 0×0)
  onMounted(async () =>
  {
    if (dataRef.value)
    {
      await nextTick();
      initGraph(dataRef.value);
    }
  });

  // Cleanup saat unmount
  onUnmounted(() =>
  {
    graph?.destroy();
    graph = null;
  });

  return {
    /** Instance GraphD3 — tersedia setelah render(). */
    getGraph: () => graph,

    // ── Search API ────────────────────────────────────────────────────────────
    /** Cari node by label atau pathId. Highlight hasil di graph. */
    search: (query: string): D3Node[] => searchPlugin.search(query),

    /** Pan & zoom secara smooth ke satu node. */
    focusNode: (node: D3Node, scale?: number): void => searchPlugin.focusNode(node, scale),

    /** Fit semua result nodes di dalam viewport. */
    focusResults: (results: D3Node[], padding?: number): void =>
      searchPlugin.focusResults(results, padding),

    /** Reset semua search highlighting. */
    clearSearch: (): void => searchPlugin.clearSearch(),
  };
}
