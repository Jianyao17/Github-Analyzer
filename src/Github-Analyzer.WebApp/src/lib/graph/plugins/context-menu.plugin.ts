import type { GraphPlugin } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';
import type { GraphData } from '@graph.types';
import type { D3Node } from '@graph.types';
import * as d3 from 'd3';

export type ContextMenuOptions = {
  onContextMenu: (x: number, y: number, node: D3Node) => void;
};

export class ContextMenuPlugin implements GraphPlugin 
{
  readonly name = 'ContextMenuPlugin';
  
  private ctx!: GraphContext;
  private options: ContextMenuOptions;
  
  constructor(options: ContextMenuOptions) 
  {
    this.options = options;
  }

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    this.ctx = ctx;
    const container = ctx.svg?.node()?.parentNode as HTMLElement;
    if (!container) return;

    this._bindEvents(ctx, container);
    ctx.bus.on('render:complete', () => this._bindEvents(ctx, container));
  }

  private _bindEvents(ctx: GraphContext, container: HTMLElement): void 
  {
    if (!ctx.nodeSelection) return;

    ctx.nodeSelection.on('contextmenu.plugin', (event: MouseEvent, d: any) => 
    {
      event.preventDefault();
      event.stopPropagation();
      
      const [x, y] = d3.pointer(event, container);
      
      this.options.onContextMenu(x, y, d as D3Node);
    });
  }

  teardown(): void 
  {
    if (this.ctx?.nodeSelection) 
    {
      this.ctx.nodeSelection.on('contextmenu.plugin', null);
    }
  }
}
