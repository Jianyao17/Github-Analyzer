import type { GraphPlugin, GraphData } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

export interface DebugPluginConfig {
  enabled:    boolean;
  logMemory?: boolean;
}

export class DebugPlugin implements GraphPlugin 
{
  readonly name     = 'debug';
  readonly priority = 999;

  private _config: DebugPluginConfig;
  private _unsub:  (() => void)[] = [];

  constructor(config: DebugPluginConfig) 
  {
    this._config = config;
  }

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    if (!this._config.enabled) return;

    this._unsub.push(
      ctx.bus.on('render:complete', (payload: any) => 
      {
        const { elapsed, nodeCount, edgeCount } = payload;
        console.group('%c[GraphEngine] render:complete', 'color:#60A5FA;font-weight:bold');
        console.log(`⏱  Time    ${elapsed.toFixed(2)} ms`);
        console.log(`📦 Nodes   ${nodeCount}`);
        console.log(`🔗 Edges   ${edgeCount}`);
        if (this._config.logMemory) 
        {
          const heap = (performance as any).memory?.usedJSHeapSize;
          if (heap) console.log(`🧠 Heap    ${(heap / 1024 / 1024).toFixed(2)} MB`);
        }
        console.groupEnd();
      }),
      ctx.bus.on('simulation:settled', () => 
      {
        console.log('%c[GraphEngine] simulation settled', 'color:#34D399');
      }),
    );
  }

  teardown(): void 
  {
    this._unsub.forEach(fn => fn());
    this._unsub = [];
  }
}
