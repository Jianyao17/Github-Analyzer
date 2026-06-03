import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

export class SelectionPlugin implements GraphPlugin 
{
  readonly name = 'selection';
  readonly priority = 3;

  private selectedId: string | null                      = null;
  private readonly onSelectCallback?: (node: D3Node | null) => void;

  constructor(onSelect?: (node: D3Node | null) => void) 
  {
    this.onSelectCallback = onSelect;
  }

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    if (!ctx.nodeSelection) return;

    ctx.nodeSelection.on('click.selection', (_event: any, d: D3Node) => 
    {
      this.selectedId = this.selectedId === d.id ? null : d.id;
      const selected  = this.selectedId;

      if (selected) 
      {
        ctx.bus.emit('highlight:nodes', { ids: new Set([selected]), dimOpacity: 0.25 });
      }
      else 
      {
        ctx.bus.emit('highlight:clear', undefined as never);
      }

      this.onSelectCallback?.(selected ? d : null);
    });
  }

  teardown(): void 
  {
    this.selectedId = null;
  }
}
