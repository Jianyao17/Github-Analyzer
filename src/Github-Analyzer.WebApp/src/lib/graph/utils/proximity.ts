import type { D3Node, D3Edge } from '@graph.types';

/**
 * Returns a sorted array of children based on subtree proximity (use relations).
 * Uses a greedy chain algorithm to ensure that siblings with highly connected
 * subtrees are placed adjacently.
 */
export function sortChildrenByProximity(
  children: string[],
  useEdges: D3Edge[],
  adj: Map<string, string[]>,
  nodeMap: Map<string, D3Node>
): string[] 
{
  if (children.length <= 1) return children;

  // 1. Build descendants map for each child (Top-Down to Bottom-Up mapping)
  const descendantsMap = new Map<string, Set<string>>();
  const nodeToChild = new Map<string, string>(); // Reverse lookup for O(1) checks

  for (const child of children) 
  {
    const descendants = new Set<string>();
    const stack = [child];
    const visited = new Set<string>();
    
    while (stack.length > 0) 
    {
      const curr = stack.pop()!;
      if (!visited.has(curr)) 
      {
        visited.add(curr);
        descendants.add(curr);
        nodeToChild.set(curr, child);
        const childrenOfCurr = adj.get(curr) ?? [];
        stack.push(...childrenOfCurr);
      }
    }
    descendantsMap.set(child, descendants);
  }

  // 2. Build weight matrix between children based on cross-subtree 'use' edges
  const weightMatrix = new Map<string, Map<string, number>>();
  for (const child of children) 
  {
    weightMatrix.set(child, new Map<string, number>());
  }

  for (const edge of useEdges) 
  {
    const sourceId = typeof edge.source === 'object' ? (edge.source as D3Node).id : edge.source as string;
    const targetId = typeof edge.target === 'object' ? (edge.target as D3Node).id : edge.target as string;

    const childA = nodeToChild.get(sourceId);
    const childB = nodeToChild.get(targetId);

    // If source and target belong to different sibling subtrees, increment their connection weight
    if (childA && childB && childA !== childB) 
    {
      const mapA = weightMatrix.get(childA)!;
      mapA.set(childB, (mapA.get(childB) ?? 0) + 1);

      const mapB = weightMatrix.get(childB)!;
      mapB.set(childA, (mapB.get(childA) ?? 0) + 1);
    }
  }

  // Calculate total connectivity weight for each child
  const totalWeights = new Map<string, number>();
  for (const child of children) 
  {
    let sum = 0;
    for (const val of weightMatrix.get(child)!.values()) 
    {
      sum += val;
    }
    totalWeights.set(child, sum);
  }

  // 3. Greedy Chain Sorting
  const sorted: string[] = [];
  const unplaced = new Set<string>(children);

  // Find start node: highest total weight. If tie, sort by node type
  let startNode = children[0];
  let maxWeight = -1;
  for (const child of children) 
  {
    const w = totalWeights.get(child) ?? 0;
    if (w > maxWeight) 
    {
      maxWeight = w;
      startNode = child;
    }
    else if (w === maxWeight) 
    {
      // tie breaker by type
      const typeA = nodeMap.get(child)?.type ?? 99;
      const typeB = nodeMap.get(startNode)?.type ?? 99;
      if (typeA < typeB) 
      {
        startNode = child;
      }
    }
  }

  sorted.push(startNode);
  unplaced.delete(startNode);

  // Greedily append the most connected remaining node to the chain
  while (unplaced.size > 0) 
  {
    const lastPlaced = sorted[sorted.length - 1];
    const connections = weightMatrix.get(lastPlaced)!;

    let bestNext: string | null = null;
    let bestConnWeight = -1;

    for (const candidate of unplaced) 
    {
      const w = connections.get(candidate) ?? 0;
      
      if (w > bestConnWeight) 
      {
        bestConnWeight = w;
        bestNext = candidate;
      }
      else if (w === bestConnWeight) 
      {
        // tie breaker 1: overall total weight (start a new strong chain if completely disconnected)
        const totalCand = totalWeights.get(candidate) ?? 0;
        const totalBest = bestNext ? (totalWeights.get(bestNext) ?? 0) : -1;
         
        if (totalCand > totalBest) 
        {
          bestNext = candidate;
        }
        else if (totalCand === totalBest) 
        {
          // tie breaker 2: node type
          const typeCand = nodeMap.get(candidate)?.type ?? 99;
          const typeBest = bestNext ? (nodeMap.get(bestNext)?.type ?? 99) : 99;
          if (typeCand < typeBest) 
          {
            bestNext = candidate;
          }
        }
      }
    }

    if (bestNext) 
    {
      sorted.push(bestNext);
      unplaced.delete(bestNext);
    }
  }

  return sorted;
}
