import { 
  watch, onMounted, 
  onUnmounted, nextTick,
  inject, reactive
} from 'vue';

import type { Ref } from 'vue';
import type { CodeGraph }  from '@/types/analysis/code-graph';
import type { D3Node } from '@graph.types';
import type { SearchPlugin } from '@graph.plugins';

import { GraphEngine } from '@graph/core/GraphEngine';
import { buildGraphData } from '@graph/utils/graph-data';
import { HierarchicalLayout } from '@graph/layout/hierarchical.layout';
import { StarBalloonLayout } from '@graph/layout/star-balloon.layout';
import { GRAPH_ENGINE_INJECTION_KEY } from '@/plugins/graph';

// Options for GraphD3 composable
export interface GraphD3Options 
{
  mode?:        'directory'  | 'namespace';
  layout?:      'hierarchical' | 'star-balloon';
  orientation?: 'LR' | 'TB' | 'RL' | 'BT';
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
  initialOptions: GraphD3Options = {},
) 
{
  // Get the engine from the injector
  const engine = inject<GraphEngine>(GRAPH_ENGINE_INJECTION_KEY);
  if (!engine) throw new Error('GraphEngine not provided! Make sure app.use(createGraphEngine()) is called.');

  // Get the search plugin
  const searchPlugin = engine.getPlugin<SearchPlugin>('search');

  // Reactive settings object
  const settings = reactive<Required<GraphD3Options>>(
    {
      mode:        initialOptions.mode        || 'directory',
      layout:      initialOptions.layout      || 'star-balloon',
      orientation: initialOptions.orientation || 'LR'
    });

  /**
   * Applies the current layout based on `settings.layout`.
   */
  function applyLayout() 
  {
    if (settings.layout === 'hierarchical') 
    {
      engine!.useLayout(new HierarchicalLayout({
        orientation: settings.orientation,
        clusterPadding: 100,
        levelGap: 250,
        nodeGap: 50,
      }));
    } 
    else if (settings.layout === 'star-balloon') 
    {
      engine!.useLayout(new StarBalloonLayout({
        minArcSpace: 200,   
        concentricGap: 100, 
        clusterPadding: 0.01,
        gapMultipliers: {
          0: 1.8, 
          1: 1.8, 
          2: 1.6, 
          3: 1.2, 
          4: 1.0  
        }
      }));
    }
  }

  /**
   * Initializes or updates the graph engine when the container and data are ready.
   */
  function init(raw: CodeGraph): void 
  {
    const data = buildGraphData(raw);
    if (!containerRef.value) return;

    engine!.reset();
    applyLayout();
    engine!.render(data);
  }

  // Watch for layout or orientation changes to dynamically re-render
  watch(
    () => [settings.layout, settings.orientation], 
    () => 
    {
      if (containerRef.value && dataRef.value && engine.isMounted()) 
      {
        applyLayout();
        engine.render(buildGraphData(dataRef.value));
      }
    }
  );

  // Watch for mode changes to dynamically filter nodes
  watch(() => settings.mode, (mode) => 
  {
    if (mode === 'directory') 
    {
      engine.setNodeFilter(node => node.type !== 1); // 1 is Namespace
    } 
    else if (mode === 'namespace') 
    {
      engine.setNodeFilter(node => node.type !== 0 && node.type !== 2); // 0 is Directory, 2 is File
    } 
    else 
    {
      engine.setNodeFilter(null);
    }
  }, { immediate: true });

  watch(dataRef, newData => { if (newData) init(newData); });

  onMounted(async () => 
  {
    if (containerRef.value) 
    {
      engine.mount(containerRef.value);
    }
    if (dataRef.value) { await nextTick(); init(dataRef.value); }
  });

  onUnmounted(() => 
  {
    engine.unmount();
  });

  return {
    settings,
    getEngine:    () => engine,
    search:       (query: string): D3Node[] => searchPlugin?.search(query) ?? [],
    focusNode:    (node: D3Node, scale?: number) => searchPlugin?.focusNode(node, scale),
    focusResults: (results: D3Node[], padding?: number) => searchPlugin?.focusResults(results, padding),
    clearSearch:  () => searchPlugin?.clearSearch(),
  };
}
