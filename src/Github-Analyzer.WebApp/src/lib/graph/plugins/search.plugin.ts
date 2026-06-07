import * as d3 from 'd3';
import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

const DIM_OPACITY      = 0.24;

export type SearchResultNode = D3Node & {
  _isHidden?: boolean;
  _visibleParentId?: string;
};

export class SearchPlugin implements GraphPlugin 
{
  readonly name = 'search';
  readonly priority = 4;

  private _ctx:      GraphContext | null = null;
  private _data:     GraphData | null = null;
  private onResults?: (results: SearchResultNode[]) => void;

  private _tooltip: d3.Selection<HTMLDivElement, unknown, null, undefined> | null = null;

  constructor(onResults?: (results: SearchResultNode[]) => void) 
  {
    this.onResults = onResults;
  }

  setup(ctx: GraphContext, data: GraphData): void 
  {
    this._ctx = ctx;
    this._data = data;

    // Create a container for HTML tooltips over the graph
    const container = d3.select<HTMLElement, unknown>(ctx.svg?.node()?.parentNode as HTMLElement);
    this._tooltip = container.append('div')
      .style('position', 'absolute')
      .style('pointer-events', 'none')
      .style('opacity', 0)
      .style('background', '#1e293b')
      .style('color', '#fff')
      .style('padding', '6px 12px')
      .style('border-radius', '6px')
      .style('font-size', '12px')
      .style('box-shadow', '0 4px 6px -1px rgba(0, 0, 0, 0.1)')
      .style('border', '1px solid #334155')
      .style('z-index', 10);
  }

  teardown(): void 
  {
    this.clearSearch();
    this._tooltip?.remove();
    this._ctx = null;
    this._data = null;
  }

  search(query: string): SearchResultNode[] 
  {
    const trimmed = query.trim().toLowerCase();

    if (!trimmed) 
    {
      this.clearSearch();
      return [];
    }

    const allNodes = this._data?.nodes ?? [];
    const visibleNodeIds = new Set((this._ctx?.nodes ?? []).map(n => n.id));

    const rawResults = allNodes.filter(
      (n) => n.label.toLowerCase().includes(trimmed)
          || n.pathId.toLowerCase().includes(trimmed),
    );

    rawResults.sort((a, b) => 
    {
      const aLabelIdx = a.label.toLowerCase().indexOf(trimmed);
      const bLabelIdx = b.label.toLowerCase().indexOf(trimmed);
      
      const aHasLabel = aLabelIdx !== -1;
      const bHasLabel = bLabelIdx !== -1;

      // 1. Label match > Path match
      if (aHasLabel && !bHasLabel) return -1;
      if (!aHasLabel && bHasLabel) return 1;

      // 2. Node type order (0=Dir, 1=Namespace, 2=File, 3=Class, 4=Function)
      if (a.type !== b.type) return a.type - b.type;

      // 3. Within the same type, prioritize early match
      if (aHasLabel && bHasLabel) 
      {
        if (aLabelIdx !== bLabelIdx) return aLabelIdx - bLabelIdx;
        if (a.label.length !== b.label.length) return a.label.length - b.label.length;
      }
      else if (!aHasLabel && !bHasLabel) 
      {
        const aPathIdx = a.pathId.toLowerCase().indexOf(trimmed);
        const bPathIdx = b.pathId.toLowerCase().indexOf(trimmed);
        if (aPathIdx !== bPathIdx) return aPathIdx - bPathIdx;
      }

      // 4. Alphabetical fallback
      return a.label.localeCompare(b.label);
    });

    const results = rawResults.map(n => 
    {
      const isHidden = !visibleNodeIds.has(n.pathId);
      let visibleParentId: string | undefined = undefined;

      if (isHidden) 
      {
        visibleParentId = this._findNearestVisibleAncestor(n.pathId, visibleNodeIds);
      }

      return { ...n, id: n.pathId, _isHidden: isHidden, _visibleParentId: visibleParentId } as SearchResultNode;
    });

    this.applyHighlight(results);
    this.onResults?.(results);

    return results;
  }

  focusNode(node: SearchResultNode, scale = 2): void 
  {
    this.clearTooltip();

    // Dynamically check if the node is currently hidden in the live context
    const isCurrentlyHidden = !this._ctx?.nodes.some(n => n.id === node.id);

    if (isCurrentlyHidden) 
    {
      // Ask CollapsePlugin to expand the path
      this._ctx?.bus.emit('collapse:expand-path', { targetId: node.id });
      
      // Wait briefly for DOM to sync and layout to calculate the new starting positions
      setTimeout(() => 
      {
        const expandedNode = this._ctx?.nodes.find(n => n.id === node.id);
        if (expandedNode) 
        {
          const tx = expandedNode.targetX ?? expandedNode.x ?? 0;
          const ty = expandedNode.targetY ?? expandedNode.y ?? 0;
          this._ctx?.bus.emit('zoom:to', { x: tx, y: ty, scale });
        }
      }, 50);
    } 
    else 
    {
      // Find the live node to get the correct coordinates
      const liveNode = this._ctx?.nodes.find(n => n.id === node.id);
      if (liveNode) 
      {
        const tx = liveNode.targetX ?? liveNode.x ?? 0;
        const ty = liveNode.targetY ?? liveNode.y ?? 0;
        this._ctx?.bus.emit('zoom:to', { x: tx, y: ty, scale });
      }
    }
  }

  focusResults(results: SearchResultNode[], padding = 60): void 
  {
    if (!results.length || !this._ctx) return;
    this._ctx.bus.emit('zoom:fit', { padding });
  }

  focusHover(node: SearchResultNode | null): void 
  {
    if (!node) 
    {
      this.clearTooltip();
      return;
    }
    
    // Dynamically check visibility
    const isCurrentlyHidden = !this._ctx?.nodes.some(n => n.id === node.id);

    if (isCurrentlyHidden) 
    {
      // We might need a fresh visible parent ID if the graph changed
      const visibleNodeIds = new Set((this._ctx?.nodes ?? []).map(n => n.id));
      const freshVisibleParentId = this._findNearestVisibleAncestor(node.id, visibleNodeIds);
      
      this._showTooltipForHidden({ ...node, _visibleParentId: freshVisibleParentId });
    }
    else
    {
      this._showTooltipForVisible(node);
    }
  }

  clearSearch(): void 
  {
    this._ctx?.bus.emit('highlight:clear', undefined as never);
    this.clearTooltip();
  }

  private applyHighlight(results: SearchResultNode[]): void 
  {
    if (!this._ctx || results.length === 0) 
    {
      this.clearSearch();
      return;
    }

    const matchIds = new Set<string>();
    const hiddenResults = [];

    for (const res of results) 
    {
      if (res._isHidden && res._visibleParentId) 
      {
        matchIds.add(res._visibleParentId);
        hiddenResults.push(res);
      } 
      else 
      {
        matchIds.add(res.id);
      }
    }

    this._ctx.bus.emit('highlight:nodes', { ids: matchIds, dimOpacity: DIM_OPACITY });
  }

  private _showTooltipForVisible(node: SearchResultNode): void 
  {
    if (!this._ctx || !this._tooltip) return;

    const targetNode = this._ctx.nodes.find(n => n.id === node.id);
    if (!targetNode || typeof targetNode.x !== 'number' || typeof targetNode.y !== 'number') return;

    const svgNode = this._ctx.svg!.node()!;
    const rect = svgNode.getBoundingClientRect();
    const transform = d3.zoomTransform(svgNode);
    
    const svgX = transform.applyX(targetNode.x);
    const svgY = transform.applyY(targetNode.y) - 20; // Above the node

    this._tooltip
      .html(`
        <div style="text-align: center;">
          <div style="font-weight: 600; color: #fbbf24;">${node.label}</div>
          <div style="font-size: 10px; color: #94a3b8;">Exact Match</div>
        </div>
        <!-- Arrow border -->
        <div 
          style="position: absolute; bottom: -7px; left: 50%; 
          transform: translateX(-50%); border-width: 7px 7px 0 7px; 
          border-style: solid; border-color: #334155 transparent transparent transparent;">
        </div>
        <!-- Arrow fill -->
        <div 
          style="position: absolute; bottom: -6px; left: 50%; 
          transform: translateX(-50%); border-width: 6px 6px 0 6px; 
          border-style: solid; border-color: #1e293b transparent transparent transparent;">
        </div>
      `)
      .style('position', 'fixed')
      .style('left', `${rect.left + svgX}px`)
      .style('top', `${rect.top + svgY}px`)
      .style('transform', 'translate(-50%, -100%)')
      .transition()
      .duration(200)
      .style('opacity', 1);
  }


  private _showTooltipForHidden(node: SearchResultNode): void 
  {
    if (!this._ctx || !this._tooltip || !node._visibleParentId) return;

    const parentNode = this._ctx.nodes.find(n => n.id === node._visibleParentId);
    if (!parentNode || typeof parentNode.x !== 'number' || typeof parentNode.y !== 'number') return;

    const svgNode = this._ctx.svg!.node()!;
    const rect = svgNode.getBoundingClientRect();
    const transform = d3.zoomTransform(svgNode);
    
    const svgX = transform.applyX(parentNode.x);
    const svgY = transform.applyY(parentNode.y) - 20; // Above the node

    this._tooltip
      .html(`
        <div style="text-align: center;">
          <div style="font-weight: 600; color: #fbbf24;">${node.label}</div>
          <div style="font-size: 10px; color: #94a3b8;">is inside <strong>${parentNode.label}</strong></div>
        </div>
        <!-- Arrow border -->
        <div 
          style="position: absolute; bottom: -7px; left: 50%; 
          transform: translateX(-50%); border-width: 7px 7px 0 7px; 
          border-style: solid; border-color: #334155 transparent transparent transparent;">
        </div>
        <!-- Arrow fill -->
        <div 
          style="position: absolute; bottom: -6px; left: 50%; 
          transform: translateX(-50%); border-width: 6px 6px 0 6px; 
          border-style: solid; border-color: #1e293b transparent transparent transparent;">
        </div>
      `)
      .style('position', 'fixed')
      .style('left', `${rect.left + svgX}px`)
      .style('top', `${rect.top + svgY}px`)
      .style('transform', 'translate(-50%, -100%)')
      .transition()
      .duration(200)
      .style('opacity', 1);
  }

  private clearTooltip(): void 
  {
    this._tooltip?.transition().duration(200).style('opacity', 0);
  }

  private _findNearestVisibleAncestor(targetId: string, visibleNodeIds: Set<string>): string | undefined 
  {
    if (!this._data) return undefined;
    
    const queue: string[] = [targetId];
    const visited = new Set<string>([targetId]);

    while (queue.length > 0) 
    {
      const currentId = queue.shift()!;
      
      if (currentId !== targetId && visibleNodeIds.has(currentId)) 
      {
        return currentId;
      }

      const edges = this._data.indexes.edgesByTarget.get(currentId) || [];
      for (const e of edges) 
      {
        if (e.type !== 2 && !visited.has(e.from)) 
        {
          visited.add(e.from);
          queue.push(e.from);
        }
      }
    }

    return undefined;
  }
}
