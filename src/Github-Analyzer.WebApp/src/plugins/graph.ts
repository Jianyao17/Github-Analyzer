import type { Plugin } from 'vue';
import type { GraphPlugin } from '@graph.types';

import { 
  ZoomPlugin, DragPlugin, HoverPlugin, 
  SearchPlugin, DebugPlugin } from '@graph.plugins';
import { GraphEngine } from '@graph/core/GraphEngine';

export const GRAPH_ENGINE_INJECTION_KEY = Symbol('GraphEngine');
export interface UseGraphD3Options 
{
  enabled?: boolean
  plugins?: GraphPlugin[];
  layout?:  'hierarchical' | 'star-balloon';
}

/**
 * Creates a Vue plugin that initializes and provides the GraphEngine singleton.
 * 
 * @param options Plugin options.
 * @returns Vue Plugin
 */
export function createGraphEngine(options: UseGraphD3Options = {}): Plugin 
{
  return {
    install(app) 
    {
      const engine = new GraphEngine();
      engine
        .use(new ZoomPlugin(),   0)
        .use(new DragPlugin(),   1)
        .use(new HoverPlugin(),  2)
        .use(new SearchPlugin(), 4)
        .use(new DebugPlugin({
          enabled: options.enabled ?? import.meta.env.DEV,
          logMemory: true,
        }), 999);

      options.plugins?.forEach(p => engine.use(p));

      // Provide the engine to the entire Vue application
      app.provide(GRAPH_ENGINE_INJECTION_KEY, engine);
    }
  };
}
