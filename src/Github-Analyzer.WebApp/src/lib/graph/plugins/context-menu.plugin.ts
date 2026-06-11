import type { GraphPlugin } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';
import type { GraphData } from '@graph.types';
import type { D3Node } from '@graph.types';
import * as d3 from 'd3';


export type ContextMenuOptions = {
  onShowSourceCode: (node: D3Node) => void;
};

export class ContextMenuPlugin implements GraphPlugin 
{
  readonly name = 'ContextMenuPlugin';
  
  private ctx!: GraphContext;
  private menuDiv: d3.Selection<HTMLDivElement, unknown, null, undefined> | null = null;
  private options: ContextMenuOptions;
  
  constructor(options: ContextMenuOptions) 
  {
    this.options = options;
  }

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    this.ctx = ctx;
    
    // Create context menu container
    const container = ctx.svg?.node()?.parentNode as HTMLElement;
    if (!container) return;

    this.menuDiv = d3.select(container)
      .append('div')
      .attr('class', 'graph-context-menu absolute z-50 hidden min-w-[160px] rounded-md bg-white p-1 shadow-lg ring-1 ring-black/5 dark:bg-gray-800 dark:ring-white/10 focus:outline-none')
      .style('pointer-events', 'auto');
      
    const btn = this.menuDiv.append('button')
      .attr('class', 'flex w-full items-center gap-2 rounded px-3 py-2 text-sm text-left hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-200 transition-colors')
      .text('Show Source Code');
      
    // Hide menu on outside click
    d3.select(document.body).on('click.contextmenu', () => 
    {
      this.hideMenu();
    });

    this._bindEvents(ctx, container, btn);
    ctx.bus.on('render:complete', () => this._bindEvents(ctx, container, btn));
    ctx.bus.on('view:refresh-requested', () => this.hideMenu());
  }

  private _bindEvents(ctx: GraphContext, container: HTMLElement, btn: d3.Selection<HTMLButtonElement, unknown, null, undefined>): void 
  {
    if (!ctx.nodeSelection) return;

    ctx.nodeSelection.on('contextmenu.plugin', (event: MouseEvent, d: any) => 
    {
      // Allow only File, Class, Function (exclude Directory, Namespace)
      if (d.type === 0 || d.type === 1) 
      { // 0=Directory, 1=Namespace
        this.hideMenu();
        return;
      }

      event.preventDefault();
      event.stopPropagation();
      
      const [x, y] = d3.pointer(event, container);
      
      this.menuDiv!
        .style('left', `${x + 5}px`)
        .style('top', `${y + 5}px`)
        .classed('hidden', false);
        
      btn.on('click', (e) => 
      {
        e.stopPropagation();
        this.hideMenu();
        this.options.onShowSourceCode(d as D3Node);
      });
    });
  }
  
  private hideMenu() 
  {
    if (this.menuDiv) 
    {
      this.menuDiv.classed('hidden', true);
    }
  }

  teardown(): void 
  {
    if (this.ctx?.nodeSelection) 
    {
      this.ctx.nodeSelection.on('contextmenu.plugin', null);
    }
    d3.select(document.body).on('click.contextmenu', null);
    if (this.menuDiv) 
    {
      this.menuDiv.remove();
      this.menuDiv = null;
    }
  }
}
