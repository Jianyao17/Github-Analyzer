import { shallowRef } from 'vue';
import { 
  watch, onMounted, 
  onUnmounted, nextTick,
  reactive, ref
} from 'vue';

import type { Ref } from 'vue';
import type { D3Node } from '@graph.types';
import type { CodeGraph }  from '@/types/analysis/code-graph';
import type { GraphEngine as GraphEngineType } from '@graph/core/GraphEngine';
import type { SearchPlugin as SearchPluginType } from '@graph.plugins';

// Global cache for dynamically imported D3/Graph modules
let GraphEngineClass        : typeof import('@graph/core/GraphEngine').GraphEngine | null = null;
let graphDataModule         : typeof import('@graph/utils/graph-data') | null = null;
let layoutHierarchicalModule: typeof import('@graph/layout/hierarchical.layout') | null = null;
let layoutStarBalloonModule : typeof import('@graph/layout/star-balloon.layout') | null = null;
let graphPluginsModule      : typeof import('@graph.plugins') | null = null;

let d3LoadPromise: Promise<void> | null = null;

const loadGraphModules = async () => 
{
  if (GraphEngineClass) return;
  if (d3LoadPromise) return d3LoadPromise;
  
  d3LoadPromise = (async () => 
  {
    const [
      { GraphEngine },
      dataUtils,
      hierarchical,
      starBalloon,
      plugins
    ] = await Promise.all([
      import('@graph/core/GraphEngine'),
      import('@graph/utils/graph-data'),
      import('@graph/layout/hierarchical.layout'),
      import('@graph/layout/star-balloon.layout'),
      import('@graph.plugins')
    ]);

    GraphEngineClass = GraphEngine;
    graphDataModule = dataUtils;
    layoutHierarchicalModule = hierarchical;
    layoutStarBalloonModule = starBalloon;
    graphPluginsModule = plugins;
  })();
  
  await d3LoadPromise;
};

// Options for GraphD3 composable
export interface GraphD3Options 
{
  mode?:          'directory'  | 'namespace';
  layout?:        'hierarchical' | 'star-balloon';
  orientation?:   'LR' | 'TB' | 'RL' | 'BT';
  collapseDepth?: number;
  onContextMenu?: (x: number, y: number, node: D3Node, isKeyboard?: boolean) => void;
}

/**
 * A composable to integrate the backend code graph data with D3.js Graph Engine.
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
  const engine = shallowRef<GraphEngineType | null>(null);
  const searchPlugin = shallowRef<SearchPluginType | null>(null);
  const isGraphLoading = ref(false);

  async function initEngine() 
  {
    isGraphLoading.value = true;
    try 
    {
      await loadGraphModules();

      // Modules are guaranteed to be loaded here
      const _engine = new GraphEngineClass!();
      _engine
        .use(new graphPluginsModule!.ZoomPlugin(),       0)
        .use(new graphPluginsModule!.DragPlugin(),       1)
        .use(new graphPluginsModule!.CollapsePlugin(),   2)
        .use(new graphPluginsModule!.HoverPlugin(),      3)
        .use(new graphPluginsModule!.SearchPlugin(),     4)
        .use(new graphPluginsModule!.NavigationPlugin(), 6)
        .use(new graphPluginsModule!.DebugPlugin({
          enabled: import.meta.env.DEV,
          logMemory: true,
        }), 999);

      if (initialOptions.onContextMenu) 
      {
        _engine.use(new graphPluginsModule!.ContextMenuPlugin({
          onContextMenu: initialOptions.onContextMenu
        }), 5);
      }

      engine.value = _engine;
      searchPlugin.value = _engine.getPlugin<SearchPluginType>('search') ?? null;

      _engine.ctx.bus.on('collapse:max-depth', ({ maxDepth }: { maxDepth: number }) => 
      {
        maxCollapseDepth.value = maxDepth;
        if (settings.collapseDepth > maxDepth && maxDepth > 0) 
        {
          settings.collapseDepth = maxDepth;
        }
      });
    }
    finally 
    {
      isGraphLoading.value = false;
    }
  }

  // Reactive settings object
  const settings = reactive<Required<GraphD3Options>>(
    {
      mode:          initialOptions.mode          || 'directory',
      layout:        initialOptions.layout        || 'star-balloon',
      orientation:   initialOptions.orientation   || 'LR',
      collapseDepth: initialOptions.collapseDepth || 2,
      onContextMenu: initialOptions.onContextMenu || (() => {})
    });

  /**
   * Applies the current layout based on `settings.layout`.
   */
  function applyLayout() 
  {
    if (!engine.value) return;
    if (settings.layout === 'hierarchical' && layoutHierarchicalModule) 
    {
      engine.value.useLayout(new layoutHierarchicalModule.HierarchicalLayout(
        { orientation: settings.orientation }));
    } 
    else if (layoutStarBalloonModule)
    {
      engine.value.useLayout(new layoutStarBalloonModule.StarBalloonLayout());
    }
  }

  /**
   * Re-initializes the graph with new data and layout.
   */
  function init(data: CodeGraph) 
  {
    if (!engine.value || !graphDataModule) return;
    
    const d3Data = graphDataModule.buildGraphData(data);
    
    // Use engine configuration logic
    if (settings.mode === 'directory') 
    {
      engine.value.setNodeFilter(node => node.type !== 1);
    } 
    else if (settings.mode === 'namespace') 
    {
      engine.value.setNodeFilter(node => node.type !== 0 && node.type !== 2);
    }

    if (engine.value.isMounted()) 
    {
      engine.value.update(d3Data);
    }
    else 
    {
      engine.value.render(d3Data);
    }
    applyLayout();

    // Fit zoom synchronously after initial layout to prevent visual shifting
    engine.value.ctx.bus.emit('zoom:fit', { padding: 60, duration: 0 });
  }

  // Watch for layout or orientation changes
  watch(
    () => [settings.layout, settings.orientation],
    () => 
    {
      if (engine.value?.isMounted()) 
      {
        applyLayout();
        // Force zoom-fit after layout change
        engine.value.ctx.bus.emit('zoom:fit', { padding: 60 });
      }
    }
  );

  // Watch for mode changes to dynamically filter nodes
  watch(() => settings.mode, (mode) => 
  {
    if (mode === 'directory') 
    {
      engine.value?.setNodeFilter(node => node.type !== 1); // 1 is Namespace
    } 
    else if (mode === 'namespace') 
    {
      engine.value?.setNodeFilter(node => node.type !== 0 && node.type !== 2); // 0 is Directory, 2 is File
    } 
    else 
    {
      engine.value?.setNodeFilter(null);
    }
  }, { immediate: true });

  watch(() => settings.collapseDepth, (depth) => 
  {
    if (engine.value?.isMounted()) 
    {
      engine.value.ctx.bus.emit('collapse:set-depth', { depth });
    }
  });

  const maxCollapseDepth = ref(4);

  function expandAll() 
  {
    if (settings.collapseDepth === maxCollapseDepth.value) 
    {
      engine.value?.ctx.bus.emit('collapse:set-depth', { depth: maxCollapseDepth.value });
    }
    else 
    {
      settings.collapseDepth = maxCollapseDepth.value;
    }
  }

  function collapseAll() 
  {
    const defaultDepth = initialOptions.collapseDepth || 2;
    if (settings.collapseDepth === defaultDepth) 
    {
      engine.value?.ctx.bus.emit('collapse:set-depth', { depth: defaultDepth });
    }
    else 
    {
      settings.collapseDepth = defaultDepth;
    }
  }

  watch(dataRef, newData => { if (newData) init(newData); });

  onMounted(async () => 
  {
    await initEngine();
    
    if (containerRef.value && engine.value) 
    {
      engine.value.mount(containerRef.value);
    }
    if (dataRef.value) { await nextTick(); init(dataRef.value); }
  });

  onUnmounted(() => 
  {
    engine.value?.unmount();
  });

  return {
    settings,
    maxCollapseDepth,
    isGraphLoading,
    expandAll,
    collapseAll,
    getEngine:    () => engine.value,
    highlightNode:(nodeId: string | null) => engine.value?.highlightNode(nodeId),
    search:       (query: string): D3Node[] => searchPlugin.value?.search(query) ?? [],
    focusHover:   (node: D3Node | null) => searchPlugin.value?.focusHover?.(node as any),
    focusNode:    (node: D3Node, scale?: number) => searchPlugin.value?.focusNode(node as any, scale),
    focusResults: (results: D3Node[], padding?: number) => searchPlugin.value?.focusResults(results as any, padding),
    clearSearch:  () => searchPlugin.value?.clearSearch(),
  };
}
