import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig } from '../types/_index';
import type { IGraphLayout } from './layout.interface';
import { sortChildrenByProximity } from '../utils/proximity';

/**
 * Star-Balloon layout (formerly organic).
 * Uses a deterministic radial tree algorithm to prevent subtrees from crossing,
 * ordered by subtree proximity, and hooked into d3.force to maintain bouncy physics.
 */
export class StarBalloonLayout implements IGraphLayout
{
  apply(
    sim: d3.Simulation<D3Node, D3Edge>,
    _nodes: D3Node[],
    edges: D3Edge[],
    _width: number,
    _height: number,
    config: GraphConfig
  ): void
  {
    this.computeRadialTargets(_nodes, edges, config);

    // Apply very weak link force to keep connections springy
    sim.force('link',
      d3.forceLink<D3Node, D3Edge>(edges)
        .id((d) => d.id)
        .strength((d) => (d.type === 2 ? 0.0 : 0.01)) 
    );

    // Remove charge force so it doesn't break the radial formation
    sim.force('charge', null);
    
    // Remove center force, we manually center the targets
    sim.force('center', null);

    // Apply bouncy target forces
    sim.force('x', d3.forceX<D3Node>((d) => d.targetX ?? 0).strength(0.8));
    sim.force('y', d3.forceY<D3Node>((d) => d.targetY ?? 0).strength(0.8));
  }

  destroy(sim: d3.Simulation<D3Node, D3Edge>): void
  {
    sim.force('x', null);
    sim.force('y', null);
  }

  private computeRadialTargets(nodes: D3Node[], edges: D3Edge[], config: GraphConfig): void
  {
    const levelGap = config.simulation?.linkDistance ?? 150;
    
    const adj = new Map<string, string[]>();
    const inDegree = new Map<string, number>();

    for (const n of nodes) {
      adj.set(n.id, []);
      inDegree.set(n.id, 0);
    }

    // Only use structural edges for hierarchy
    for (const e of edges) {
      if (e.type === 0 || e.type === 1 || e.type === 3) {
        const from = typeof e.source === 'object' ? (e.source as D3Node).id : e.source as string;
        const to = typeof e.target === 'object' ? (e.target as D3Node).id : e.target as string;
        
        if (adj.has(from) && inDegree.has(to)) {
          adj.get(from)!.push(to);
          inDegree.set(to, inDegree.get(to)! + 1);
        }
      }
    }

    const nodeMap = new Map<string, D3Node>(nodes.map(n => [n.id, n]));
    const useEdges = edges.filter(e => e.type === 2);

    // Sort children by subtree proximity
    for (const [parentId, children] of adj.entries()) {
      if (children.length > 1) {
        adj.set(parentId, sortChildrenByProximity(children, useEdges, adj, nodeMap));
      }
    }

    let roots = Array.from(inDegree.entries()).filter(([_, deg]) => deg === 0).map(([id]) => id);
    if (roots.length === 0 && nodes.length > 0) roots = [nodes[0].id];

    // Sort roots by proximity too
    if (roots.length > 1) {
       roots = sortChildrenByProximity(roots, useEdges, adj, nodeMap);
    }

    // Count leaves per node
    const leavesCount = new Map<string, number>();
    const countLeaves = (id: string): number => {
      const children = adj.get(id) ?? [];
      if (children.length === 0) {
        leavesCount.set(id, 1);
        return 1;
      }
      let count = 0;
      for (const child of children) {
        count += countLeaves(child);
      }
      leavesCount.set(id, count);
      return count;
    };

    let totalLeaves = 0;
    for (const root of roots) {
      totalLeaves += countLeaves(root);
    }

    // Assign angles and radii
    const visited = new Set<string>();

    const dfsAssign = (id: string, startAngle: number, endAngle: number, level: number) => {
      if (visited.has(id)) return;
      visited.add(id);

      const node = nodeMap.get(id);
      if (node) {
        // Find center of angle range
        const midAngle = (startAngle + endAngle) / 2;
        const radius = level * levelGap;
        
        node.targetX = Math.cos(midAngle) * radius;
        node.targetY = Math.sin(midAngle) * radius;
      }

      const children = adj.get(id) ?? [];
      let currentAngle = startAngle;
      
      const myLeaves = leavesCount.get(id) || 1;
      const angleRange = endAngle - startAngle;

      for (const childId of children) {
        const childLeaves = leavesCount.get(childId) || 1;
        // Proportion of the angle range
        const portion = (childLeaves / myLeaves) * angleRange;
        dfsAssign(childId, currentAngle, currentAngle + portion, level + 1);
        currentAngle += portion;
      }
    };

    let currentRootAngle = 0;
    for (const root of roots) {
      const rootLeaves = leavesCount.get(root) || 1;
      const portion = totalLeaves > 0 ? (rootLeaves / totalLeaves) * Math.PI * 2 : Math.PI * 2;
      dfsAssign(root, currentRootAngle, currentRootAngle + portion, 0);
      currentRootAngle += portion;
    }

    // Handle disconnected nodes
    for (const node of nodes) {
      if (!visited.has(node.id)) {
        node.targetX = (Math.random() - 0.5) * levelGap;
        node.targetY = (Math.random() - 0.5) * levelGap;
      }
    }
  }
}
