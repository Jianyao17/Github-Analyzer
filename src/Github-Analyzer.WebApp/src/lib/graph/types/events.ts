import type { D3Node, D3Edge } from './node-edge';
import type { IGraphLayout } from './layout';

export interface GraphEvents 
{
  // View mutations (dari plugin → RenderPipeline)
  'view:nodes-changed'    : { nodes: D3Node[]; edges: D3Edge[] };
  'view:refresh-requested': void;
  'view:filter-changed'   : void;
  'view:reset'            : void;
  'view:transform'        : any;

  // Highlight (dari SearchPlugin / CollapsePlugin → NodePass)
  'highlight:nodes' : { ids: Set<string>; dimOpacity: number };
  'highlight:focus' : { nodeId: string | null; relatedNodeIds?: Set<string> };
  'highlight:single': { nodeId: string | null };
  'highlight:clear' : void;

  // Collapse (dari SearchPlugin -> CollapsePlugin)
  'collapse:collapse-all' : void;
  'collapse:expand-all'   : void;
  'collapse:expand-path'  : { targetId: string };
  'collapse:set-depth'    : { depth: number };
  'collapse:max-depth'    : { maxDepth: number };

  // Node interaction (dari DragPlugin / HoverPlugin → consumer)
  'node:click': { node: D3Node; event: MouseEvent };
  'node:hover': { node: D3Node | null };
  
  // Context Menu (dari ContextMenuPlugin → consumer)
  'context-menu:open': { node: D3Node; x?: number; y?: number; isKeyboard?: boolean };

  // Zoom (dari SearchPlugin → ZoomPlugin)
  'zoom:to'  : { x: number; y: number; scale?: number; duration?: number };
  'zoom:fit' : { padding?: number; duration?: number };

  // Simulation (dari CollapsePlugin / LayoutManager → SimulationController)
  'simulation:reheat' : { alpha?: number };
  'simulation:settled': void;
  'simulation:cool'   : void;

  // Layout (dari LayoutManager → SimulationController)
  'layout:change': { layout: IGraphLayout };

  // Lifecycle (dari GraphEngine → DebugPlugin)
  'render:complete'        : { elapsed: number; nodeCount: number; edgeCount: number };
  'render:tween-positions' : { positions: Map<string, { x: number; y: number }> };
  'render:snap-positions'  : { positions: Map<string, { x: number; y: number }> };
}

export interface Dimensions 
{
  width:  number;
  height: number;
}
