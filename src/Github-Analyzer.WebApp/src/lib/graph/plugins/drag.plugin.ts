import * as d3 from 'd3';
import type { GraphPlugin, GraphData } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

export class DragPlugin implements GraphPlugin 
{
  readonly name = 'drag';
  readonly priority = 1;

  private _unsub?: () => void;

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    this._bindEvents(ctx);
    this._unsub = ctx.bus.on('render:complete', () => this._bindEvents(ctx));
  }

  private _bindEvents(ctx: GraphContext): void 
  {
    if (!ctx.nodeSelection) return;

    ctx.nodeSelection
      .attr('cursor', 'grab')
      .call(
        d3.drag<SVGGElement, any>()
          .on('start', (event, d) => 
          {
            if (!event.active) ctx.bus.emit('simulation:reheat', { alpha: 0.3 });
            d.fx = d.x;
            d.fy = d.y;
          })
          .on('drag', (event, d) => 
          {
            d.fx = event.x;
            d.fy = event.y;
          })
          .on('end', (event, d) => 
          {
            if (!event.active) ctx.bus.emit('simulation:cool', undefined as never);
            d.fx = null;
            d.fy = null;
          })
      );
  }

  teardown(): void 
  {
    if (this._unsub) 
    {
      this._unsub();
      this._unsub = undefined;
    }
  }
}
