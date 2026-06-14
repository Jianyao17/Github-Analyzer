import type { GraphPlugin } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';
import type { GraphData } from '@graph.types';
import type { D3Node } from '@graph.types';
import * as d3 from 'd3';

export type ContextMenuOptions = {
  onContextMenu: (x: number, y: number, node: D3Node, isKeyboard?: boolean) => void;
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

    ctx.bus.on('context-menu:open', ({ node, x, y, isKeyboard }: 
      { node: D3Node; x?: number; y?: number; isKeyboard?: boolean }) => 
    {
      if (x === undefined || y === undefined) 
      {
        const svgNode = ctx.svg?.node();
        if (svgNode) 
        {
          const transform = d3.zoomTransform(svgNode);
          x = transform.applyX(node.x ?? 0);
          y = transform.applyY(node.y ?? 0);
        } 
        else 
        {
          x = node.x ?? 0;
          y = node.y ?? 0;
        }
      }
      this.options.onContextMenu(x, y, node, isKeyboard);
    });
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

    let longPressTimer: any;
    
    ctx.nodeSelection.on('touchstart.plugin', (event: TouchEvent, d: any) => 
    {
      if (event.touches.length !== 1) return;
      
      const touch = event.touches[0];
      // d3.pointer works with touch events too
      const [x, y] = d3.pointer(touch, container);
      
      longPressTimer = setTimeout(() => 
      {
        this.options.onContextMenu(x, y, d as D3Node);
      }, 500); // 500ms long press
    })
      .on('touchmove.plugin touchend.plugin touchcancel.plugin', () => 
      {
        if (longPressTimer) clearTimeout(longPressTimer);
      });
  }

  teardown(): void 
  {
    if (this.ctx?.nodeSelection) 
    {
      this.ctx.nodeSelection.on('contextmenu.plugin', null);
      this.ctx.nodeSelection.on('touchstart.plugin', null);
      this.ctx.nodeSelection.on('touchmove.plugin touchend.plugin touchcancel.plugin', null);
    }
  }
}
