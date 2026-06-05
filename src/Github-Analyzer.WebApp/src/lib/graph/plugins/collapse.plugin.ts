import type { GraphPlugin }  from '@graph/types/plugin';
import type { GraphData }    from '@graph/types/graph-data';
import type { GraphContext } from '@graph/core/GraphContext';
import type { D3Node }       from '@graph/types/node-edge';

/**
 * Plugin to handle node collapsing and expanding.
 * Acts as a middleware to filter out hidden nodes and edges before rendering.
 */
export class CollapsePlugin implements GraphPlugin 
{
  readonly name = 'CollapsePlugin';
  readonly priority = 2; // Run early to filter nodes before other plugins process them

  // State
  private _collapsedNodes = new Set<string>();
  private _expandedNodes  = new Set<string>();
  private _isInitialized  = false;
  
  private _ctx: GraphContext | null = null;
  private _data: GraphData | null = null;

  // Configuration
  private _initialDepth = 2;
  
  private _unsubs: (() => void)[] = [];

  setup(ctx: GraphContext, data: GraphData): void 
  {
    this._ctx = ctx;
    this._data = data;
    
    this._unsubs.push(
      ctx.bus.on('node:click', this._handleNodeClick.bind(this)),
      ctx.bus.on('collapse:expand-path', ({ targetId }: { targetId: string }) => 
      {
        this.expandPathToTarget(targetId);
      }),
      ctx.bus.on('collapse:expand-all', () => 
      {
        if (!this._data) return;
        this._initialDepth = 99; // Set to very deep
        this._collapsedNodes.clear();
        this._expandedNodes.clear();
        this._isInitialized = false;
        ctx.bus.emit('view:refresh-requested', undefined as never);
      }),
      ctx.bus.on('collapse:collapse-all', () => 
      {
        if (!this._data) return;
        this._collapsedNodes.clear();
        this._expandedNodes.clear();
        this._isInitialized = false;
        ctx.bus.emit('view:refresh-requested', undefined as never);
      }),
      ctx.bus.on('collapse:set-depth', ({ depth }: { depth: number }) => 
      {
        if (!this._data) return;
        this._initialDepth = depth;
        this._collapsedNodes.clear();
        this._expandedNodes.clear();
        this._isInitialized = false;
        ctx.bus.emit('view:refresh-requested', undefined as never);
      })
    );
  }

  transform(ctx: GraphContext, data: GraphData): void 
  {
    this._ctx = ctx;
    this._data = data;

    if (!this._isInitialized) 
    {
      this._initializeState(data);
      this._isInitialized = true;
    }

    const { visibleNodeIds, hiddenChildCounts } = this._computeVisibleState(data);

    // Filter ctx.nodes and ctx.edges
    ctx.nodes = ctx.nodes.filter(n => visibleNodeIds.has(n.id));
    ctx.edges = ctx.edges.filter(e => 
    {
      const sourceId = String(typeof e.source === 'object' ? e.source.id : e.source);
      const targetId = String(typeof e.target === 'object' ? e.target.id : e.target);
      return visibleNodeIds.has(sourceId) && visibleNodeIds.has(targetId);
    });

    // Inject hidden child count for NodePass to render the `>` badge
    for (const node of ctx.nodes) 
    {
      (node as any)._hiddenChildCount = hiddenChildCounts.get(node.id) || 0;
    }
  }

  teardown(): void 
  {
    this._unsubs.forEach(u => u());
    this._unsubs = [];
    
    this._collapsedNodes.clear();
    this._expandedNodes.clear();
    this._isInitialized = false;
  }

  /**
   * Expands the specific path to the target node.
   * Useful for search plugin integration.
   */
  expandPathToTarget(targetId: string): void 
  {
    if (!this._data || !this._ctx) return;

    // Traverse all structural ancestors and expand them
    const queue: string[] = [targetId];
    const visited = new Set<string>([targetId]);

    while (queue.length > 0) 
    {
      const currentId = queue.shift()!;
      
      if (currentId !== targetId) 
      {
        this._expandedNodes.add(currentId);
        this._collapsedNodes.delete(currentId);
      }

      const edges = this._data.indexes.edgesByTarget.get(currentId) || [];
      for (const edge of edges) 
      {
        if (edge.type !== 2 && !visited.has(edge.from)) 
        {
          visited.add(edge.from);
          queue.push(edge.from);
        }
      }
    }
    
    // Force an engine refresh to apply the new state
    this._ctx.bus.emit('view:refresh-requested', undefined as never);
  }

  private _handleNodeClick({ node }: { node: D3Node }): void 
  {
    const id = node.id;
    // Only toggle if the node has children
    const children = this._getChildren(id, this._data!);
    if (children.length === 0) return;

    if (this._collapsedNodes.has(id)) 
    {
      this._collapsedNodes.delete(id);
      this._expandedNodes.add(id);
    } 
    else 
    {
      this._collapsedNodes.add(id);
      this._expandedNodes.delete(id);
    }

    // Trigger engine refresh FIRST so layout updates node target positions
    this._ctx?.bus.emit('view:refresh-requested', undefined as never);

    // THEN focus on the node's new position
    this._ctx?.bus.emit('zoom:to', {
      x: node.targetX ?? node.x ?? 0,
      y: node.targetY ?? node.y ?? 0
    });
  }

  private _initializeState(data: GraphData): void 
  {
    if (!this._ctx) return;
    const allowedNodeIds = new Set(this._ctx.nodes.map(n => n.id));

    // Find roots (in-degree 0 in sourceRelEdges) among allowed nodes
    const inDegree = new Map<string, number>();
    
    for (const edge of data.sourceRelEdges) 
    {
      if (!allowedNodeIds.has(edge.from) || !allowedNodeIds.has(edge.to)) continue;
      const current = inDegree.get(edge.to) || 0;
      inDegree.set(edge.to, current + 1);
    }

    const roots = data.nodes.filter(n => allowedNodeIds.has(n.pathId) && (inDegree.get(n.pathId) || 0) === 0);
    
    // BFS to assign depths and collapse nodes at initialDepth
    const queue: { id: string, depth: number }[] = roots.map(r => ({ id: r.pathId, depth: 1 }));
    const visited = new Set<string>();
    let maxDepth = 1;

    while (queue.length > 0) 
    {
      const { id, depth } = queue.shift()!;
      if (visited.has(id)) continue;
      visited.add(id);

      if (depth > maxDepth) maxDepth = depth;

      // Only get children that are allowed
      const children = this._getChildren(id, data).filter(childId => allowedNodeIds.has(childId));
      
      if (children.length > 0) 
      {
        if (depth >= this._initialDepth) 
        {
          this._collapsedNodes.add(id);
        } 
        else 
        {
          this._expandedNodes.add(id);
        }
      }

      for (const child of children) 
      {
        queue.push({ id: child, depth: depth + 1 });
      }
    }

    // Emit the max depth found so the UI can update its stepper
    this._ctx.bus.emit('collapse:max-depth', { maxDepth });
  }

  private _computeVisibleState(data: GraphData): 
    { 
      visibleNodeIds:    Set<string>, 
      hiddenChildCounts: Map<string, number> 
    }   
  {
    const visibleNodeIds = new Set<string>();
    const hiddenChildCounts = new Map<string, number>();

    if (!this._ctx) return { visibleNodeIds, hiddenChildCounts };

    // The GraphEngine already applied nodeFilter and populated ctx.nodes.
    // We must only traverse through nodes that are globally allowed by the current view mode.
    const allowedNodeIds = new Set(this._ctx.nodes.map(n => n.id));

    // Find roots (nodes with 0 in-degree among allowed structural edges)
    const inDegree = new Map<string, number>();
    for (const edge of data.sourceRelEdges) 
    {
      if (!allowedNodeIds.has(edge.from) || !allowedNodeIds.has(edge.to)) continue;
      const current = inDegree.get(edge.to) || 0;
      inDegree.set(edge.to, current + 1);
    }
    const roots = data.nodes
      .filter(n => allowedNodeIds.has(n.pathId) && (inDegree.get(n.pathId) || 0) === 0)
      .map(n => n.pathId);

    // BFS from roots
    const queue: string[] = [...roots];
    
    while (queue.length > 0) 
    {
      const id = queue.shift()!;
      if (visibleNodeIds.has(id)) continue;
      
      visibleNodeIds.add(id);

      // Get children and filter out nodes that are not allowed by the current view mode
      const children = this._getChildren(id, data).filter(childId => allowedNodeIds.has(childId));

      if (this._collapsedNodes.has(id)) 
      {
        hiddenChildCounts.set(id, children.length);
        // Do NOT add children to queue
      } 
      else 
      {
        hiddenChildCounts.set(id, 0);
        for (const child of children) 
        {
          queue.push(child);
        }
      }
    }

    return { visibleNodeIds, hiddenChildCounts };
  }

  private _getChildren(nodeId: string, data: GraphData): string[] 
  {
    const edges = data.indexes.edgesBySource.get(nodeId) || [];
    // Filter only structural edges (e.g., BelongsTo, Define)
    // Assuming sourceRelEdges are the structural ones. Type 0 = BelongsTo, 1 = Define, 3 = Include
    return edges
      .filter(e => e.type !== 2) // exclude Call relations
      .map(e => e.to);
  }
}
