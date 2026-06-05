import { 
  ZoomPlugin, DragPlugin, HoverPlugin, 
  SearchPlugin, DebugPlugin, CollapsePlugin } from '@graph.plugins';
import { GraphEngine } from '@graph/core/GraphEngine';
import type { Plugin } from 'vue';

export const GRAPH_ENGINE_INJECTION_KEY = Symbol('GraphEngine');

/**
 * Creates a Vue plugin that initializes and provides the GraphEngine singleton.
 * 
 * @param options Plugin options.
 * @returns Vue Plugin
 */
export function createGraphEngine(debugEnabled: boolean = false): Plugin 
{
  return {
    install(app) 
    {
      const engine = new GraphEngine();
      engine
        .use(new ZoomPlugin(),     0)
        .use(new DragPlugin(),     1)
        .use(new CollapsePlugin(), 2)
        .use(new HoverPlugin(),    3)
        .use(new SearchPlugin(),   4)
        .use(new DebugPlugin({
          enabled: debugEnabled ?? import.meta.env.DEV,
          logMemory: true,
        }), 999);

      // Provide the engine to the entire Vue application
      app.provide(GRAPH_ENGINE_INJECTION_KEY, engine);
    }
  };
}
