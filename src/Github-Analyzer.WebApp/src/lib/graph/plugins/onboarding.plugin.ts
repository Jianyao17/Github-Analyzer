import type { GraphPlugin, GraphData } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

type OnboardingAction = 'zoom' | 'collapse' | 'context-menu' | 'hover';

export class OnboardingPlugin implements GraphPlugin 
{
  readonly name = 'onboarding';
  readonly priority = 100;

  private _unsub: (() => void)[] = [];
  private _ctx: GraphContext | null = null;
  private _data: GraphData | null = null;
  private _activeAction: string | null = null;
  private _onActionDetected: (() => void) | null = null;
  private _domListenersAttached = false;
  private _isTourActive = false;
  private _refreshFrame: number | null = null;

  private _handleWindowInteraction = () => 
  {
    if (this._activeAction === 'zoom') 
    {
      this.triggerAction();
    }
    // We no longer block pan/zoom here so the user can freely explore.
  };

  setup(ctx: GraphContext, _data: GraphData): void 
  {
    this._ctx = ctx;
    this._data = _data;

    // Wait until SVG is mounted to attach listeners if not already attached
    this._unsub.push(
      ctx.bus.on('render:complete', () => 
      {
        this.attachDomListeners();
        this.refreshOverlaySoon();
      })
    );

    // Detect node click (for collapse/expand)
    this._unsub.push(
      ctx.bus.on('node:click', ({ node }) => 
      {
        if (this._activeAction === 'collapse') 
        {
          // Only trigger if clicking a directory/expandable node
          if (node.type === 0) 
          {
            this.triggerAction();
          }
        }
      }),
      ctx.bus.on('context-menu:open', () => 
      {
        if (this._activeAction === 'context-menu') 
        {
          this.triggerAction();
        }
      }),
      ctx.bus.on('node:hover', ({ node }) => 
      {
        if (this._activeAction === 'hover' && node) 
        {
          this.triggerAction();
        }
      }),
      // Force v-onboarding to recalculate Popper & SVG overlay coords when graph moves
      ctx.bus.on('view:transform', () => 
      {
        this.refreshOverlaySoon();
      })
    );
  }

  private attachDomListeners() 
  {
    if (this._domListenersAttached) return;

    window.addEventListener('wheel', this._handleWindowInteraction, { capture: true });
    window.addEventListener('mousedown', this._handleWindowInteraction, { capture: true });
    
    this._unsub.push(() => 
    {
      window.removeEventListener('wheel', this._handleWindowInteraction, { capture: true } as any);
      window.removeEventListener('mousedown', this._handleWindowInteraction, { capture: true } as any);
    });

    this._domListenersAttached = true;
  }

  /**
   * Called by the onboarding store to instruct the plugin to watch for an action.
   */
  waitForAction(action: OnboardingAction, callback: () => void) 
  {
    this._isTourActive = true;
    this._activeAction = action;
    this._onActionDetected = callback;
    this.refreshOverlaySoon();
  }

  /**
   * Cancels the active listener.
   */
  cancelWait() 
  {
    this._activeAction = null;
    this._onActionDetected = null;
  }

  /**
   * Marks whether a graph onboarding step is active. This keeps overlay updates
   * scoped to tours while still allowing non-interactive graph steps to refresh.
   */
  setTourActive(isActive: boolean) 
  {
    this._isTourActive = isActive;
    if (isActive) 
    {
      this.refreshOverlaySoon();
    }
  }

  /**
   * v-onboarding recalculates Popper and SVG overlay coordinates on resize.
   * D3 graph movement changes SVG transforms, so we proxy that into a resize.
   */
  refreshOverlaySoon() 
  {
    if (!this._isTourActive && this._activeAction === null) return;
    if (this._refreshFrame !== null) return;

    this._refreshFrame = window.requestAnimationFrame(() => 
    {
      this._refreshFrame = null;
      window.dispatchEvent(new Event('resize'));
    });
  }

  /**
   * Triggers the callback and cleans up.
   */
  private triggerAction() 
  {
    if (this._onActionDetected) 
    {
      this._onActionDetected();
      this.cancelWait();
      this.refreshOverlaySoon();
    }
  }

  /**
   * Highlights nodes that can be expanded/collapsed (type 0).
   */
  highlightExpandableNodes() 
  {
    if (!this._ctx) return;
    const expandableIds = this._ctx.nodes
      .filter(n => n.type === 0)
      .map(n => n.id);
    
    this._ctx.bus.emit('highlight:nodes', { 
      ids: new Set(expandableIds), 
      dimOpacity: 0.2 
    });
  }

  /**
   * Clears any active highlight.
   */
  clearHighlight() 
  {
    this._ctx?.bus.emit('highlight:clear', undefined);
  }

  /**
   * Ensures at least one file/class/function node is visible in the graph.
   * If none are visible, it will programmatically expand the path to the first one found.
   */
  async ensureFileNodeVisible() 
  {
    if (!this._ctx || !this._data) return;

    // Check if any file/class/function is already visible (type >= 2)
    let targetNode = this._ctx.nodes.find(n => n.type >= 2);
    
    if (!targetNode) 
    {
      // Find a file node in the entire graph data
      const fileNode = this._data.nodes.find(n => n.type >= 2);
      if (fileNode) 
      {
        // Expand the path to it
        this._ctx.bus.emit('collapse:expand-path', { targetId: fileNode.pathId });
        // Wait for layout and render
        await new Promise(resolve => setTimeout(resolve, 300));
        
        targetNode = this._ctx.nodes.find(n => n.type >= 2);
      }
    }

    if (targetNode) 
    {
      this._ctx.bus.emit('zoom:to', {
        x: targetNode.targetX ?? targetNode.x ?? 0,
        y: targetNode.targetY ?? targetNode.y ?? 0
      });
      await new Promise(resolve => setTimeout(resolve, 300));
    }
  }

  /**
   * Programmatically opens the context menu on a target node.
   */
  openContextMenuOnTarget() 
  {
    if (!this._ctx) return;
    
    // Find a file/function node (type > 1) if possible
    let targetNode = this._ctx.nodes.find(n => n.type > 1);
    
    if (!targetNode) 
    {
      // Fallback to the first available node
      targetNode = this._ctx.nodes[0];
    }

    if (targetNode) 
    {
      // Emit event to open the context menu. CodeGraphView listens to this.
      this._ctx.bus.emit('context-menu:open', { 
        node: targetNode, 
        isKeyboard: true 
      });
    }
  }

  teardown(): void 
  {
    this._unsub.forEach(fn => fn());
    this._unsub = [];
    if (this._refreshFrame !== null) 
    {
      window.cancelAnimationFrame(this._refreshFrame);
      this._refreshFrame = null;
    }
    this._isTourActive = false;
    this._ctx = null;
    this._data = null;
  }
}
