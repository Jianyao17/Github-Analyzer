import { watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { Ref } from 'vue';
import type { GraphData, D3Node, GraphPlugin } from '@graph/graph.types';

import { GraphD3 } from '@graph/graph.main';
import { GraphDebugger } from '@graph/graph.debug';
import { ZoomDragPlugin } from '@graph/plugins/zoom-drag.plugin';
import { HoverPlugin } from '@graph/plugins/hover.plugin';
import { SearchPlugin } from '@graph/plugins/search.plugin';

export interface UseGraphD3Options
{
  /** Extra plugins to register in addition to the defaults. */
  plugins?: GraphPlugin[];

  /** Enable DEV-only performance profiling. Defaults to import.meta.env.DEV. */
  debug?: boolean;
}

/**
 * useGraphD3 — thin Vue composable wrapper around the GraphD3 library.
 *
 * Default plugins: ZoomDrag, Hover, Search.
 * DEV performance profiling is attached automatically via GraphDebugger.
 *
 * Returns a search API for wiring to Vue search UI:
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
  dataRef:      Ref<GraphData | null>,
  options:      UseGraphD3Options = {},
)
{
  let graph: GraphD3 | null = null;

  // Plugin instances are created once and reused across update() cycles.
  // SearchPlugin holds a reference to ZoomDragPlugin for programmatic zoom.
  const zoomPlugin   = new ZoomDragPlugin();
  const searchPlugin = new SearchPlugin(zoomPlugin);

  function initGraph(data: GraphData): void
  {
    if (!containerRef.value) return;

    if (!graph)
    {
      // ── First render ────────────────────────────────────────────────────────
      graph = new GraphD3({
        container: containerRef.value,
        data,
      });

      graph
        .use(zoomPlugin)
        .use(searchPlugin)
        .use(new HoverPlugin());

      // Register any extra plugins the caller provided
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
      // ── Subsequent renders — plugins are preserved ──────────────────────────
      graph.update(data);
    }
  }

  // Re-render whenever data changes
  watch(dataRef, (newData) =>
  {
    if (newData) initGraph(newData);
  });

  // Render on mount if data is already available
  // (nextTick ensures the container has real dimensions, not 0×0)
  onMounted(async () =>
  {
    if (dataRef.value)
    {
      await nextTick();
      initGraph(dataRef.value);
    }
  });

  // Clean up on unmount
  onUnmounted(() =>
  {
    graph?.destroy();
    graph = null;
  });

  return {
    /** The underlying GraphD3 instance — available after render(). */
    getGraph: () => graph,

    // ── Search API ────────────────────────────────────────────────────────────
    /** Search nodes by label or pathId. Highlights matches in the graph. */
    search: (query: string): D3Node[] => searchPlugin.search(query),

    /** Smoothly pan & zoom to a single node. */
    focusNode: (node: D3Node, scale?: number): void => searchPlugin.focusNode(node, scale),

    /** Fit all result nodes inside the viewport. */
    focusResults: (results: D3Node[], padding?: number): void =>
      searchPlugin.focusResults(results, padding),

    /** Reset all search highlighting. */
    clearSearch: (): void => searchPlugin.clearSearch(),
  };
}
