import type { D3Node, D3Edge, GraphConfig, Dimensions, IGraphLayout, LayoutResult } from '@graph.types';
import { sortChildrenByProximity } from '@graph/utils/proximity';

export interface StarBalloonLayoutOptions 
{
  levelGap?: number;
  minArcSpace?: number;
  concentricGap?: number;
  gapMultipliers?: Record<number, number>;
  clusterPadding?: number;
}

export class StarBalloonLayout implements IGraphLayout 
{
  readonly name = 'star-balloon';
  private _options: StarBalloonLayoutOptions;

  constructor(options: StarBalloonLayoutOptions = {}) 
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
    this.computeRadialTargets(nodes, edges, config, positions);
    
    return {
      positions,
      animationHint: 'tween',
    };
  }

  private computeRadialTargets(
    nodes: D3Node[], edges: D3Edge[], config: GraphConfig, 
    positions: Map<string, { x: number; y: number }>): void 
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

    const depths = new Map<string, number>();
    const calcDepth = (id: string, d: number) => 
    {
      depths.set(id, Math.max(depths.get(id) ?? 0, d));
      const children = adj.get(id) ?? [];
      for (const c of children) calcDepth(c, d + 1);
    };
    for (const root of roots) calcDepth(root, 0);

    let maxDepth = 1; // Prevent division by zero
    for (const d of depths.values()) 
    {
      if (d > maxDepth) maxDepth = d;
    }

    // Calculate complexity factor based on graph size.
    // Small graphs (< 200 nodes) don't need massive spacing. Large graphs (> 1000 nodes) need more core space.
    // Caps at 1.0 for graphs with 1500+ nodes.
    const complexityFactor = Math.min(1.0, nodes.length / 1500);
    
    // Maximum extra multiplier for the innermost nodes. 
    // Small graphs will have close to 0 extra expansion. Large graphs get up to 0.5 (1.5x gap).
    const maxExpansion = 0.5 * complexityFactor;

    const getGapMultiplier = (type: number, depth: number, maxDepth: number): number => 
    {
      let baseMultiplier = 1.0;
      if (this._options.gapMultipliers && type in this._options.gapMultipliers) 
      {
        baseMultiplier = this._options.gapMultipliers[type];
      }
      else
      {
        switch(type) 
        {
          case 0: baseMultiplier = 2.0; break; // Directory
          case 1: baseMultiplier = 2.0; break; // Namespace
          case 2: baseMultiplier = 1.5; break; // File
          case 3: baseMultiplier = 1.0; break; // Class
          case 4: baseMultiplier = 0.8; break; // Function
          default: baseMultiplier = 1.0; break;
        }
      }

      // Flexible scaling: uses the complexity factor so small graphs stay compact.
      const depthRatio = maxDepth > 0 ? ((maxDepth - depth) / maxDepth) : 0;
      const depthFactor = 1 + (maxExpansion * depthRatio);
      return baseMultiplier * depthFactor;
    };

    const minArcSpace = this._options.minArcSpace ?? 60;
    const concentricGap = this._options.concentricGap ?? 40;
    const clusterPadding = this._options.clusterPadding ?? 0.05;
    const visited = new Set<string>();

    const dfsAssign = (id: string, startAngle: number, endAngle: number, currentRadius: number, depth: number) => 
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
      const gap = levelGap * getGapMultiplier(parentType, depth, maxDepth);
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

        // Apply angular padding to prevent sibling clusters from touching.
        // Cap the padding to 40% of the portion so we don't invert or collapse the wedge.
        const angularPadding = Math.min(clusterPadding, portion * 0.4);
        const safeStartAngle = currentAngle + angularPadding;
        const safeEndAngle = (currentAngle + portion) - angularPadding;

        dfsAssign(childId, safeStartAngle, safeEndAngle, finalChildRadius, depth + 1);
        currentAngle += portion;
      }
    };

    let currentRootAngle = 0;
    for (const root of roots) 
    {
      const rootLeaves = leavesCount.get(root) || 1;
      const portion = totalLeaves > 0 ? (rootLeaves / totalLeaves) * Math.PI * 2 : Math.PI * 2;
      dfsAssign(root, currentRootAngle, currentRootAngle + portion, 0, 0);
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
