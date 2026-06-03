import type { D3Node, D3Edge, GraphConfig, Dimensions, IGraphLayout, LayoutResult } from '@graph.types';
import { sortChildrenByProximity } from '../utils/proximity';

export interface StarBalloonLayoutOptions 
{
  levelGap?: number;
  minArcSpace?: number;
  concentricGap?: number;
  gapMultipliers?: Record<number, number>;
}

export class StarBalloonLayout implements IGraphLayout 
{
  readonly name = 'star-balloon';
  private _options: StarBalloonLayoutOptions;

  constructor(options: StarBalloonLayoutOptions = {}) {
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
    this.computeRadialTargets(nodes, edges, config, positions);
    
    return {
      positions,
      animationHint: 'tween',
    };
  }

  private computeRadialTargets(nodes: D3Node[], edges: D3Edge[], config: GraphConfig, positions: Map<string, { x: number; y: number }>): void 
  {
    const levelGap = this._options.levelGap ?? config.simulation?.levelGap ?? 150;
    
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
    if (roots.length === 0 && nodes.length > 0) roots = [nodes[0].id];

    if (roots.length > 1) 
    {
      roots = sortChildrenByProximity(roots, useEdges, adj, nodeMap);
    }

    const leavesCount = new Map<string, number>();
    const countLeaves = (id: string): number => 
    {
      const children = adj.get(id) ?? [];
      if (children.length === 0) 
      {
        leavesCount.set(id, 1);
        return 1;
      }
      let count = 0;
      for (const child of children) 
      {
        count += countLeaves(child);
      }
      leavesCount.set(id, count);
      return count;
    };

    let totalLeaves = 0;
    for (const root of roots) 
    {
      totalLeaves += countLeaves(root);
    }

    const getGapMultiplier = (type: number): number => 
    {
      if (this._options.gapMultipliers && 
          type in this._options.gapMultipliers) 
      {
        return this._options.gapMultipliers[type];
      }

      switch(type) 
      {
        case 0: case 1: return 2.5; // Directory / Namespace
        case 2: return 1.5;         // File
        case 3: return 1.0;         // Class
        case 4: return 0.8;         // Function
        default: return 1.0;
      }
    };

    const minArcSpace = this._options.minArcSpace ?? 60;
    const concentricGap = this._options.concentricGap ?? 40;
    const visited = new Set<string>();

    const dfsAssign = (id: string, startAngle: number, endAngle: number, currentRadius: number) => 
    {
      if (visited.has(id)) return;
      visited.add(id);

      const node = nodeMap.get(id);
      if (node) 
      {
        const midAngle = (startAngle + endAngle) / 2;
        positions.set(id, {
          x: Math.cos(midAngle) * currentRadius,
          y: Math.sin(midAngle) * currentRadius
        });
      }

      const children = adj.get(id) ?? [];
      if (children.length === 0) return;

      const myLeaves = leavesCount.get(id) || 1;
      const angleRange = endAngle - startAngle;

      const parentType = node?.type ?? 99;
      const gap = levelGap * getGapMultiplier(parentType);
      const baseChildRadius = currentRadius === 0 ? gap : currentRadius + gap;

      const arcLength = baseChildRadius * angleRange;
      const rings = Math.max(1, Math.ceil((children.length * minArcSpace) / arcLength));

      let currentAngle = startAngle;
      
      for (let i = 0; i < children.length; i++) 
      {
        const childId = children[i];
        const childLeaves = leavesCount.get(childId) || 1;
        const portion = (childLeaves / myLeaves) * angleRange;
        
        const extraRadius = (i % rings) * concentricGap;
        const finalChildRadius = baseChildRadius + extraRadius;

        dfsAssign(childId, currentAngle, currentAngle + portion, finalChildRadius);
        currentAngle += portion;
      }
    };

    let currentRootAngle = 0;
    for (const root of roots) 
    {
      const rootLeaves = leavesCount.get(root) || 1;
      const portion = totalLeaves > 0 ? (rootLeaves / totalLeaves) * Math.PI * 2 : Math.PI * 2;
      dfsAssign(root, currentRootAngle, currentRootAngle + portion, 0);
      currentRootAngle += portion;
    }

    for (const node of nodes) 
    {
      if (!visited.has(node.id)) 
      {
        positions.set(node.id, {
          x: (Math.random() - 0.5) * levelGap,
          y: (Math.random() - 0.5) * levelGap
        });
      }
    }
  }
}
