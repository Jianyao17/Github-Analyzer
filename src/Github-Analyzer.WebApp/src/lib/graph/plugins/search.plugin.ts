import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

const DIM_OPACITY      = 0.24;

export class SearchPlugin implements GraphPlugin 
{
  readonly name = 'search';
  readonly priority = 4;

  private _ctx:      GraphContext | null = null;
  private onResults?: (results: D3Node[]) => void;

  constructor(onResults?: (results: D3Node[]) => void) 
  {
    this.onResults = onResults;
  }

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    this._ctx = ctx;
  }

  teardown(): void 
  {
    this.clearSearch();
    this._ctx = null;
  }

  search(query: string): D3Node[] 
  {
    const trimmed = query.trim().toLowerCase();

    if (!trimmed) 
    {
      this.clearSearch();
      return [];
    }

    const nodes   = this._ctx?.nodes ?? [];
    const results = nodes.filter(
      (n: D3Node) => n.label.toLowerCase().includes(trimmed)
          || n.pathId.toLowerCase().includes(trimmed),
    );

    this.applyHighlight(results);
    this.onResults?.(results);

    return results;
  }

  focusNode(node: D3Node, scale = 2): void 
  {
    this._ctx?.bus.emit('zoom:to', { x: node.x ?? 0, y: node.y ?? 0, scale });
  }

  focusResults(results: D3Node[], padding = 60): void 
  {
    if (!results.length || !this._ctx) return;
    this._ctx.bus.emit('zoom:fit', { padding });
  }

  clearSearch(): void 
  {
    this._ctx?.bus.emit('highlight:clear', undefined as never);
  }

  private applyHighlight(results: D3Node[]): void 
  {
    if (!this._ctx) return;

    if (results.length === 0) 
    {
      this._ctx.bus.emit('highlight:clear', undefined as never);
      return;
    }

    const matchIds = new Set(results.map((n) => n.id));
    this._ctx.bus.emit('highlight:nodes', { ids: matchIds, dimOpacity: DIM_OPACITY });
  }
}
