import { 
  watch, onMounted, 
  onUnmounted, nextTick,
  reactive, ref
} from 'vue';

import type { Ref } from 'vue';
import type { CodeGraph }  from '@/types/analysis/code-graph';
import type { D3Node } from '@graph.types';

import { GraphEngine } from '@graph/core/GraphEngine';
import { buildGraphData } from '@graph/utils/graph-data';
import { HierarchicalLayout } from '@graph/layout/hierarchical.layout';
import { StarBalloonLayout } from '@graph/layout/star-balloon.layout';
import { 
  ZoomPlugin, DragPlugin, HoverPlugin, 
  SearchPlugin, DebugPlugin, CollapsePlugin 
} from '@graph.plugins';

// Options for GraphD3 composable
export interface GraphD3Options 
{
  mode?:          'directory'  | 'namespace';
  layout?:        'hierarchical' | 'star-balloon';
  orientation?:   'LR' | 'TB' | 'RL' | 'BT';
  collapseDepth?: number;
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
  // Instantiate GraphEngine per-instance
  const engine = new GraphEngine();
  engine
    .use(new ZoomPlugin(),     0)
    .use(new DragPlugin(),     1)
    .use(new CollapsePlugin(), 2)
    .use(new HoverPlugin(),    3)
    .use(new SearchPlugin(),   4)
    .use(new DebugPlugin({
      enabled: import.meta.env.DEV,
      logMemory: true,
    }), 999);

  // Get the search plugin
  const searchPlugin = engine.getPlugin<SearchPlugin>('search');

  // Reactive settings object
  const settings = reactive<Required<GraphD3Options>>(
    {
      mode:          initialOptions.mode          || 'directory',
      layout:        initialOptions.layout        || 'star-balloon',
      orientation:   initialOptions.orientation   || 'LR',
      collapseDepth: initialOptions.collapseDepth || 2
    });

  /**
   * Applies the current layout based on `settings.layout`.
   */
  function applyLayout() 
  {
    if (settings.layout === 'hierarchical') 
    {
      engine?.useLayout(new HierarchicalLayout({ orientation: settings.orientation }));
    } 
    else 
    {
      engine?.useLayout(new StarBalloonLayout());
    }
  }

  /**
   * Re-initializes the graph with new data and layout.
   */
  function init(data: CodeGraph) 
  {
    const d3Data = buildGraphData(data);
    
    // Use engine configuration logic
    if (settings.mode === 'directory') 
    {
      engine?.setNodeFilter(node => node.type !== 1);
    } 
    else if (settings.mode === 'namespace') 
    {
      engine?.setNodeFilter(node => node.type !== 0 && node.type !== 2);
    }

    if (engine?.isMounted()) 
    {
      engine?.update(d3Data);
    }
    else 
    {
      engine?.render(d3Data);
    }
    applyLayout();

    // Fit zoom synchronously after initial layout to prevent visual shifting
    engine?.ctx.bus.emit('zoom:fit', { padding: 60, duration: 0 });
  }

  // Watch for layout or orientation changes
  watch(
    () => [settings.layout, settings.orientation],
    () => 
    {
      if (engine?.isMounted()) 
      {
        applyLayout();
        // Force zoom-fit after layout change
        engine?.ctx.bus.emit('zoom:fit', { padding: 60 });
      }
    }
  );

  // Watch for mode changes to dynamically filter nodes
  watch(() => settings.mode, (mode) => 
  {
    if (mode === 'directory') 
    {
      engine?.setNodeFilter(node => node.type !== 1); // 1 is Namespace
    } 
    else if (mode === 'namespace') 
    {
      engine?.setNodeFilter(node => node.type !== 0 && node.type !== 2); // 0 is Directory, 2 is File
    } 
    else 
    {
      engine?.setNodeFilter(null);
    }
  }, { immediate: true });

  watch(() => settings.collapseDepth, (depth) => 
  {
    if (engine?.isMounted()) 
    {
      engine?.ctx.bus.emit('collapse:set-depth', { depth });
    }
  });

  const maxCollapseDepth = ref(4);

  engine?.ctx.bus.on('collapse:max-depth', ({ maxDepth }: { maxDepth: number }) => 
  {
    maxCollapseDepth.value = maxDepth;
    // ensure current depth setting does not exceed new max depth
    if (settings.collapseDepth > maxDepth && maxDepth > 0) 
    {
      settings.collapseDepth = maxDepth;
    }
  });

  function expandAll() 
  {
    if (settings.collapseDepth === maxCollapseDepth.value) 
    {
      engine?.ctx.bus.emit('collapse:set-depth', { depth: maxCollapseDepth.value });
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
      engine?.ctx.bus.emit('collapse:set-depth', { depth: defaultDepth });
    }
    else 
    {
      settings.collapseDepth = defaultDepth;
    }
  }

  watch(dataRef, newData => { if (newData) init(newData); });

  onMounted(async () => 
  {
    if (containerRef.value) 
    {
      engine?.mount(containerRef.value);
    }
    if (dataRef.value) { await nextTick(); init(dataRef.value); }
  });

  onUnmounted(() => 
  {
    engine?.unmount();
  });

  return {
    settings,
    maxCollapseDepth,
    expandAll,
    collapseAll,
    getEngine:    () => engine,
    search:       (query: string): D3Node[] => searchPlugin?.search(query) ?? [],
    focusHover:   (node: D3Node | null) => searchPlugin?.focusHover?.(node as any),
    focusNode:    (node: D3Node, scale?: number) => searchPlugin?.focusNode(node as any, scale),
    focusResults: (results: D3Node[], padding?: number) => searchPlugin?.focusResults(results as any, padding),
    clearSearch:  () => searchPlugin?.clearSearch(),
  };
}
