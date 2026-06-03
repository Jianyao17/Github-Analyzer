import type { D3Node, D3Edge, GraphConfig, Dimensions, IGraphLayout, LayoutResult } from '@graph.types';
import { sortChildrenByProximity } from '@graph/utils/proximity';

export interface HierarchicalLayoutOptions {
  orientation?: 'LR' | 'RL' | 'TB' | 'BT';
  clusterPadding?: number;
  levelGap?: number;
  nodeGap?: number;
}

export class HierarchicalLayout implements IGraphLayout 
{
  readonly name = 'hierarchical';
  private _options: HierarchicalLayoutOptions;

  constructor(options: HierarchicalLayoutOptions = {}) 
  {
    this._options = options;
  }

  apply(
    nodes: D3Node[],
    edges: D3Edge[],
    _dims: Dimensions,
    config: GraphConfig
  ): LayoutResult 
  {
    const positions = new Map<string, { x: number; y: number }>();
    this.computeHierarchicalTargets(nodes, edges, config, positions);
    
    return {
      positions,
      animationHint: 'tween',
    };
  }

  private computeHierarchicalTargets(
    nodes: D3Node[], edges: D3Edge[], _config: GraphConfig, 
    positions: Map<string, { x: number; y: number }>): void 
  {
    const levelGap = this._options.levelGap ?? _config.simulation?.levelGap ?? 150;
    const nodeGap = this._options.nodeGap ?? _config.simulation?.nodeGap ?? 50;
    const orientation = this._options.orientation ?? 'LR';

    const adj = new Map<string, string[]>();
    const inDegree = new Map<string, number>();

    for (const n of nodes) 
    {
      adj.set(n.id, []);
      inDegree.set(n.id, 0);
    }

    for (const e of edges) 
    {
      if (e.type === 0 || e.type === 1 || e.type === 3) 
      {
        const from = typeof e.source === 'object' ? (e.source as D3Node).id : e.source as string;
        const to = typeof e.target === 'object' ? (e.target as D3Node).id : e.target as string;
        
        if (adj.has(from) && inDegree.has(to)) 
        {
          adj.get(from)!.push(to);
          inDegree.set(to, inDegree.get(to)! + 1);
        }
      }
    }

    const nodeMap = new Map<string, D3Node>(nodes.map(n => [n.id, n]));
    const useEdges = edges.filter(e => e.type === 2);

    for (const [parentId, children] of adj.entries()) 
    {
      if (children.length > 1) 
      {
        adj.set(parentId, sortChildrenByProximity(children, useEdges, adj, nodeMap));
      }
    }

    let roots = Array.from(inDegree.entries()).filter(([_, deg]) => deg === 0).map(([id]) => id);

    if (roots.length > 1) 
    {
      roots = sortChildrenByProximity(roots, useEdges, adj, nodeMap);
    }

    if (roots.length === 0 && nodes.length > 0) 
    {
      roots = [nodes[0].id];
    }

    const clusterPadding = this._options.clusterPadding ?? 40;
    const visited = new Set<string>();

    const nodeSizes = new Map<string, number>();
    const nodeLocalPos = new Map<string, number>();

    const alignTop = orientation === 'LR' || orientation === 'RL';

    // Phase 1: Bottom-up calculate subtree bounding boxes (widths for TB, heights for LR)
    const calcSize = (id: string) => 
    {
      if (visited.has(id)) return;
      visited.add(id);

      const children = adj.get(id) ?? [];
      for (const c of children) calcSize(c);

      if (children.length === 0) 
      {
        nodeSizes.set(id, nodeGap);
        nodeLocalPos.set(id, nodeGap / 2);
      } 
      else 
      {
        let totalSize = 0;
        let sum = 0;
        let firstChildAbsolute = 0;

        for (let i = 0; i < children.length; i++) 
        {
          const c = children[i];
          const s = nodeSizes.get(c) ?? nodeGap;
          const ls = nodeLocalPos.get(c) ?? (nodeGap / 2);
          
          if (i > 0) totalSize += clusterPadding; // Add padding between subtree clusters
          
          const childAbsolute = totalSize + ls;
          if (i === 0) firstChildAbsolute = childAbsolute;

          sum += childAbsolute;
          totalSize += s;
        }

        // For LR/RL, align parent with first child to create a Flat Top.
        // For TB/BT, place parent at the centroid of all children.
        const parentPos = alignTop ? firstChildAbsolute : (sum / children.length);

        nodeSizes.set(id, totalSize);
        nodeLocalPos.set(id, parentPos);
      }
    };

    for (const root of roots) calcSize(root);
    for (const node of nodes) if (!visited.has(node.id)) calcSize(node.id);

    visited.clear();

    // Phase 2: Top-down assign absolute coordinates
    const assignCoords = (id: string, startPos: number, depth: number) => 
    {
      if (visited.has(id)) return;
      visited.add(id);

      const ls = nodeLocalPos.get(id) ?? (nodeGap / 2);
      const absolutePos = startPos + ls;
      
      let x = 0, y = 0;
      if (orientation === 'TB') 
      {
        x = absolutePos;
        y = depth * levelGap;
      } 
      else if (orientation === 'BT') 
      {
        x = absolutePos;
        y = -(depth * levelGap);
      } 
      else if (orientation === 'LR') 
      {
        x = depth * levelGap;
        y = absolutePos;
      } 
      else if (orientation === 'RL') 
      {
        x = -(depth * levelGap);
        y = absolutePos;
      }

      positions.set(id, { x, y });

      const children = adj.get(id) ?? [];
      let currentChildStartPos = startPos;
      
      for (let i = 0; i < children.length; i++) 
      {
        const c = children[i];
        const s = nodeSizes.get(c) ?? nodeGap;
        
        assignCoords(c, currentChildStartPos, depth + 1);
        
        currentChildStartPos += s;
        if (i < children.length - 1) currentChildStartPos += clusterPadding;
      }
    };

    let currentRootStartPos = 0;
    for (let i = 0; i < roots.length; i++) 
    {
      const root = roots[i];
      assignCoords(root, currentRootStartPos, 0);
      currentRootStartPos += nodeSizes.get(root) ?? nodeGap;
      if (i < roots.length - 1) currentRootStartPos += clusterPadding;
    }
    
    // Assign any disconnected nodes
    for (const node of nodes) 
    {
      if (!visited.has(node.id)) 
      {
        assignCoords(node.id, currentRootStartPos, 0);
        currentRootStartPos += nodeSizes.get(node.id) ?? nodeGap;
        currentRootStartPos += clusterPadding;
      }
    }

    // Center the entire graph around (0,0)
    let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
    for (const pos of positions.values()) 
    {
      if (pos.x < minX) minX = pos.x;
      if (pos.x > maxX) maxX = pos.x;
      if (pos.y < minY) minY = pos.y;
      if (pos.y > maxY) maxY = pos.y;
    }

    const cx = (minX + maxX) / 2;
    const cy = (minY + maxY) / 2;

    for (const [id, pos] of positions.entries()) 
    {
      positions.set(id, { x: pos.x - cx, y: pos.y - cy });
    }
  }
}
