import { 
  watch, onMounted, 
  onUnmounted, nextTick 
} from 'vue';

import type { Ref } from 'vue';
import type { CodeGraph }  from '@/types/analysis/code-graph';
import type { D3Node, GraphPlugin } from '@graph.types';

import { GraphEngine }     from '@graph/core/GraphEngine';
import { ZoomPlugin }      from '@graph/plugins/zoom.plugin';
import { DragPlugin }      from '@graph/plugins/drag.plugin';
import { HoverPlugin }     from '@graph/plugins/hover.plugin';
import { SearchPlugin }    from '@graph/plugins/search.plugin';
import { DebugPlugin }     from '@graph/plugins/debug.plugin';
import { StarBalloonLayout } from '@graph/layout/star-balloon.layout';
import { buildGraphData }  from '@graph/utils/graph-data';

export interface UseGraphD3Options {
  plugins?: GraphPlugin[];
  debug?:   boolean;
}

/**
 * Vue composable for integrating the D3 graph engine into a Vue component.
 * Handles mounting, data reactivity, engine initialization, and plugin orchestration.
 * 
 * @param containerRef A Vue ref pointing to the HTML container element.
 * @param dataRef A Vue ref containing the raw backend code graph data.
 * @param options Additional initialization options (e.g., custom plugins).
 */
export function useGraphD3(
  containerRef: Ref<HTMLElement | null>,
  dataRef:      Ref<CodeGraph | null>,
  options:      UseGraphD3Options = {},
) 
{
  let engine: GraphEngine | null = null;

  const searchPlugin = new SearchPlugin();

  /**
   * Initializes or updates the graph engine when the container and data are ready.
   * Instantiates the engine on the first run, registers default plugins, and triggers rendering.
   * On subsequent calls, it performs a lightweight data update.
   */
  function init(raw: CodeGraph): void 
  {
    const data = buildGraphData(raw);
    if (!containerRef.value) return;

    if (!engine) 
    {
      engine = new GraphEngine({ container: containerRef.value });

      engine
        .useLayout(new StarBalloonLayout({
          minArcSpace: 60,
          concentricGap: 40,
          gapMultipliers: {
            0: 2.5, // Directory
            1: 2.5, // Namespace
            2: 1.5, // File
            3: 1.0, // Class
            4: 0.8  // Function
          }
        }))
        .use(new ZoomPlugin(),  0)
        .use(new DragPlugin(),  1)
        .use(new HoverPlugin(), 2)
        .use(searchPlugin,      4)
        .use(new DebugPlugin({
          enabled:    options.debug ?? import.meta.env.DEV,
          logMemory:  true,
        }), 999);

      options.plugins?.forEach(p => engine!.use(p));
      engine.render(data);
    }
    else 
    {
      engine.update(data);
    }
  }

  watch(dataRef, newData => { if (newData) init(newData); });

  onMounted(async () => 
  {
    if (dataRef.value) { await nextTick(); init(dataRef.value); }
  });

  onUnmounted(() => 
  {
    engine?.destroy();
    engine = null;
  });

  return {
    getEngine:    () => engine,
    search:       (query: string): D3Node[] => searchPlugin.search(query),
    focusNode:    (node: D3Node, scale?: number) => searchPlugin.focusNode(node, scale),
    focusResults: (results: D3Node[], padding?: number) => searchPlugin.focusResults(results, padding),
    clearSearch:  () => searchPlugin.clearSearch(),
  };
}
