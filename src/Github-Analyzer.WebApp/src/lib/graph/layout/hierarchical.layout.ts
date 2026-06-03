import type { D3Node, D3Edge, GraphConfig, Dimensions, IGraphLayout, LayoutResult } from '@graph.types';
import { sortChildrenByProximity } from '../utils/proximity';

export interface HierarchicalLayoutOptions {
  orientation?: 'LR' | 'RL' | 'TB' | 'BT';
  levelGap?: number;
  nodeGap?: number;
}

export class HierarchicalLayout implements IGraphLayout 
{
  readonly name = 'hierarchical';
  private _options: HierarchicalLayoutOptions;

  constructor(options: HierarchicalLayoutOptions = {}) {
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

    const visited = new Set<string>();
    let currentRow = 0;

    const dfs = (id: string, currentLevel: number) => 
    {
      if (visited.has(id)) return;
      visited.add(id);

      const node = nodeMap.get(id);
      if (node) 
      {
        let x = 0;
        let y = 0;

        if (orientation === 'LR') 
        {
          x = currentLevel * levelGap;
          y = currentRow * nodeGap;
        }
        else if (orientation === 'RL') 
        {
          x = -(currentLevel * levelGap);
          y = currentRow * nodeGap;
        }
        else if (orientation === 'TB') 
        {
          x = currentRow * nodeGap;
          y = currentLevel * levelGap;
        }
        else if (orientation === 'BT') 
        {
          x = currentRow * nodeGap;
          y = -(currentLevel * levelGap);
        }

        positions.set(node.id, { x, y });
        currentRow++;
      }

      const children = adj.get(id) ?? [];
      for (const childId of children) 
      {
        dfs(childId, currentLevel + 1);
      }
    };

    for (const root of roots) 
    {
      dfs(root, 0);
    }

    for (const node of nodes) 
    {
      if (!visited.has(node.id)) 
      {
        dfs(node.id, 0);
      }
    }

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
