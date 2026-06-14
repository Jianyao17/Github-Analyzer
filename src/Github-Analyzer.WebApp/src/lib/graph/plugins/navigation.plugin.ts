import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

export class NavigationPlugin implements GraphPlugin 
{
  readonly name = 'NavigationPlugin';
  readonly priority = 6;
  
  private ctx!: GraphContext;
  private handleKeyDown = this._onKeyDown.bind(this);

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    this.ctx = ctx;
    document.addEventListener('keydown', this.handleKeyDown);
  }

  teardown(): void 
  {
    document.removeEventListener('keydown', this.handleKeyDown);
  }

  private _onKeyDown(e: KeyboardEvent): void 
  {
    // Ignore if user is typing in an input or textarea
    if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) 
    {
      return;
    }

    const key = e.key;
    const isNavKey = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'w', 'a', 's', 'd', 'W', 'A', 'S', 'D'].includes(key);
    const isContextMenu = (e.shiftKey && key === 'F10') || key === 'm' || key === 'c' || key === 'M' || key === 'C';
    const isCollapse = key === ' ' || key === 'Enter';

    if (!isNavKey && !isCollapse && !isContextMenu) return;

    e.preventDefault(); // Prevent default browser scrolling

    const currentId = this.ctx.focusedNodeId;
    let current = this.ctx.nodes.find(n => n.id === currentId);

    // Initial trigger: fallback to root node
    if (!current) 
    {
      if (this.ctx.nodes.length === 0) return;
      current = this.ctx.nodes.reduce((min, n) => n.pathId.length < min.pathId.length ? n : min, this.ctx.nodes[0]);
      this._focusNode(current);
      return;
    }

    if (isCollapse) 
    {
      this.ctx.bus.emit('node:click', { node: current, event: e as unknown as MouseEvent });
      return;
    }

    if (isContextMenu) 
    {
      this.ctx.bus.emit('context-menu:open', { node: current, isKeyboard: true });
      return;
    }

    if (isNavKey) 
    {
      const dir = this._getDirection(key);
      if (dir.x === 0 && dir.y === 0) return;
      this._navigate(current, dir);
    }
  }

  private _getDirection(key: string): { x: number, y: number } 
  {
    switch (key) 
    {
      case 'ArrowUp': case 'w': case 'W': return { x: 0, y: -1 };
      case 'ArrowDown': case 's': case 'S': return { x: 0, y: 1 };
      case 'ArrowLeft': case 'a': case 'A': return { x: -1, y: 0 };
      case 'ArrowRight': case 'd': case 'D': return { x: 1, y: 0 };
      default: return { x: 0, y: 0 };
    }
  }

  private _navigate(current: D3Node, dir: { x: number, y: number }): void 
  {
    if (typeof current.x !== 'number' || typeof current.y !== 'number') return;
    const cx = current.x;
    const cy = current.y;

    let bestNode: D3Node | null = null;
    let bestScore = Infinity;

    for (let i = 0; i < this.ctx.nodes.length; i++) 
    {
      const n = this.ctx.nodes[i];
      if (n === current || typeof n.x !== 'number' || typeof n.y !== 'number') continue;

      const dx = n.x - cx;
      const dy = n.y - cy;
      
      const dot = dx * dir.x + dy * dir.y;
      
      // Node is not in the requested direction
      if (dot <= 0.1) continue; 

      const distSq = dx * dx + dy * dy;
      const dist = Math.sqrt(distSq);

      // Score prioritizes close nodes directly in front
      const anglePenalty = dist / dot;
      const score = dist * (anglePenalty * anglePenalty);

      if (score < bestScore) 
      {
        bestScore = score;
        bestNode = n;
      }
    }

    if (bestNode) 
    {
      this._focusNode(bestNode);
    }
  }

  private _focusNode(node: D3Node): void 
  {
    this.ctx.focusedNodeId = node.id;
    const tx = node.targetX ?? node.x ?? 0;
    const ty = node.targetY ?? node.y ?? 0;
    this.ctx.bus.emit('zoom:to', { x: tx, y: ty, scale: 2 });
    
    // Highlight node with border only using the new event
    this.ctx.bus.emit('highlight:single', { nodeId: node.id });
  }
}
