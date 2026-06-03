import { 
  watch, onMounted, 
  onUnmounted, nextTick,
  inject
} from 'vue';

import type { Ref } from 'vue';
import type { CodeGraph }  from '@/types/analysis/code-graph';
import type { UseGraphD3Options } from '@/plugins/graph';
import type { SearchPlugin } from '@graph.plugins';
import type { D3Node } from '@graph.types';

import { GraphEngine } from '@graph/core/GraphEngine';
import { buildGraphData } from '@graph/utils/graph-data';
import { HierarchicalLayout } from '@graph/layout/hierarchical.layout';
import { StarBalloonLayout } from '@graph/layout/star-balloon.layout';
import { GRAPH_ENGINE_INJECTION_KEY } from '@/plugins/graph';

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
  const engine = inject<GraphEngine>(GRAPH_ENGINE_INJECTION_KEY);
  if (!engine) throw new Error('GraphEngine not provided! Make sure app.use(createGraphEngine()) is called.');

  const searchPlugin = engine.getPlugin<SearchPlugin>('search');

  /**
   * Initializes or updates the graph engine when the container and data are ready.
   * On subsequent calls, it performs a lightweight data update.
   */
  function init(raw: CodeGraph): void 
  {
    const data = buildGraphData(raw);
    if (!containerRef.value) return;

    engine!.reset();
    
    // Add any component-specific dynamic plugins if requested
    options.plugins?.forEach(p => engine!.use(p));
    
    if (options.layout === 'hierarchical') 
    {
      engine!.useLayout(new HierarchicalLayout({
        orientation: 'LR',
        clusterPadding: 100,
        levelGap: 250,
        nodeGap: 50,
      }));
    } 
    else if (options.layout === 'star-balloon') 
    {
      engine!.useLayout(new StarBalloonLayout({
        minArcSpace: 200,   // Sufficient arc length to fit wide node cards
        concentricGap: 100, // Sufficient radial distance to prevent horizontal overlap
        clusterPadding: 0.01,
        gapMultipliers: {
          0: 1.8, // Directory
          1: 1.8, // Namespace
          2: 1.6, // File
          3: 1.2, // Class
          4: 1.0  // Function
        }
      }));
    }

    engine!.render(data);
  }

  watch(dataRef, newData => { if (newData) init(newData); });

  onMounted(async () => 
  {
    if (containerRef.value) {
      engine.mount(containerRef.value);
    }
    if (dataRef.value) { await nextTick(); init(dataRef.value); }
  });

  onUnmounted(() => 
  {
    engine.unmount();
  });

  return {
    getEngine:    () => engine,
    search:       (query: string): D3Node[] => searchPlugin?.search(query) ?? [],
    focusNode:    (node: D3Node, scale?: number) => searchPlugin?.focusNode(node, scale),
    focusResults: (results: D3Node[], padding?: number) => searchPlugin?.focusResults(results, padding),
    clearSearch:  () => searchPlugin?.clearSearch(),
  };
}
