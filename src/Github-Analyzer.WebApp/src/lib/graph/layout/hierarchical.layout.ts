import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig } from '../types/_index';
import type { IGraphLayout } from './layout.interface';
import { sortChildrenByProximity } from '../utils/proximity';

export class HierarchicalLayout implements IGraphLayout
{
  apply(
    sim: d3.Simulation<D3Node, D3Edge>,
    nodes: D3Node[],
    edges: D3Edge[],
    _width: number,
    _height: number,
    config: GraphConfig
  ): void
  {
    this.computeHierarchicalTargets(nodes, edges, config);

    // Apply link force with very low strength to maintain connections without fighting the exact positions too much
    sim.force('link',
      d3.forceLink<D3Node, D3Edge>(edges)
        .id((d) => d.id)
        .strength((d) => (d.type === 2 ? 0.0 : 0.01)) 
    );

    // Charge force keeps nodes from overlapping if they get knocked out of position
    sim.force('charge', d3.forceManyBody().strength(-50));

    // Bouncy target positioning forces
    sim.force('x', d3.forceX<D3Node>((d) => d.targetX ?? 0).strength(0.8));
    sim.force('y', d3.forceY<D3Node>((d) => d.targetY ?? 0).strength(0.8));
  }

  destroy(sim: d3.Simulation<D3Node, D3Edge>): void
  {
    sim.force('x', null);
    sim.force('y', null);
  }

  private computeHierarchicalTargets(nodes: D3Node[], edges: D3Edge[], config: GraphConfig): void
  {
    const levelGap = config.simulation?.levelGap ?? 150;
    const nodeGap = config.simulation?.nodeGap ?? 50;
    const orientation = config.simulation?.orientation ?? 'LR';

    // 1. Build adjacency list for structural edges ONLY (type 0, 1, 3)
    const adj = new Map<string, string[]>();
    const inDegree = new Map<string, number>();

    for (const n of nodes) {
      adj.set(n.id, []);
      inDegree.set(n.id, 0);
    }

    for (const e of edges) {
      if (e.type === 0 || e.type === 1 || e.type === 3) { // belongsTo, define, include
        const from = typeof e.source === 'object' ? (e.source as D3Node).id : e.source as string;
        const to = typeof e.target === 'object' ? (e.target as D3Node).id : e.target as string;
        
        if (adj.has(from) && inDegree.has(to)) {
          adj.get(from)!.push(to);
          inDegree.set(to, inDegree.get(to)! + 1);
        }
      }
    }

    // 2. Sort children by subtree proximity (use relations)
    const nodeMap = new Map<string, D3Node>(nodes.map(n => [n.id, n]));
    const useEdges = edges.filter(e => e.type === 2);

    for (const [parentId, children] of adj.entries()) {
      if (children.length > 1) {
        adj.set(parentId, sortChildrenByProximity(children, useEdges, adj, nodeMap));
      }
    }

    // 3. Find roots (inDegree == 0)
    let roots = Array.from(inDegree.entries()).filter(([_, deg]) => deg === 0).map(([id]) => id);

    // Sort roots by proximity too
    if (roots.length > 1) {
      roots = sortChildrenByProximity(roots, useEdges, adj, nodeMap);
    }

    // Fallback if there are cycles and no roots
    if (roots.length === 0 && nodes.length > 0) {
      roots = [nodes[0].id];
    }

    // 4. DFS Traversal to assign Row and Level
    const visited = new Set<string>();
    let currentRow = 0;

    const dfs = (id: string, currentLevel: number) => {
      if (visited.has(id)) return;
      visited.add(id);

      const node = nodeMap.get(id);
      if (node) {
        node._level = currentLevel;
        
        // Compute logical x and y
        let x = 0;
        let y = 0;

        if (orientation === 'LR') {
          x = currentLevel * levelGap;
          y = currentRow * nodeGap;
        } else if (orientation === 'RL') {
          x = -(currentLevel * levelGap);
          y = currentRow * nodeGap;
        } else if (orientation === 'TB') {
          x = currentRow * nodeGap;
          y = currentLevel * levelGap;
        } else if (orientation === 'BT') {
          x = currentRow * nodeGap;
          y = -(currentLevel * levelGap);
        }

        node.targetX = x;
        node.targetY = y;
        
        currentRow++;
      }

      const children = adj.get(id) ?? [];
      for (const childId of children) {
        dfs(childId, currentLevel + 1);
      }
    };

    for (const root of roots) {
      dfs(root, 0);
    }

    // Handline disconnected components or cycle leftovers
    for (const node of nodes) {
      if (!visited.has(node.id)) {
        dfs(node.id, 0);
      }
    }

    // Optional: Center the entire graph based on bounding box
    let minX = Infinity, maxX = -Infinity, minY = Infinity, maxY = -Infinity;
    for (const node of nodes) {
      if (node.targetX! < minX) minX = node.targetX!;
      if (node.targetX! > maxX) maxX = node.targetX!;
      if (node.targetY! < minY) minY = node.targetY!;
      if (node.targetY! > maxY) maxY = node.targetY!;
    }

    const cx = (minX + maxX) / 2;
    const cy = (minY + maxY) / 2;

    for (const node of nodes) {
      // Offset so the whole graph is centered around 0,0
      node.targetX = (node.targetX ?? 0) - cx;
      node.targetY = (node.targetY ?? 0) - cy;
    }
  }
}
