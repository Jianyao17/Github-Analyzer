import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig } from '../graph.types';

/**
 * Creates a configured D3 force simulation.
 * _radius is annotated on each node by NodeRenderer and used by the collision force.
 */
export function createSimulation(
  nodes: D3Node[],
  edges: D3Edge[],
  width: number,
  height: number,
  config: GraphConfig,
): d3.Simulation<D3Node, D3Edge> 
{
  // Default values for linkDistance and chargeStrength
  const {
    linkDistance   = 50,
    chargeStrength = -100,
    // alphaMin: D3 default is 0.001 (~300 ticks).
    // 0.05 stops the simulation as soon as nodes are visually stable.
    alphaMin   = 0.05,
    // alphaDecay: D3 default is ~0.0228.
    // A slightly higher value makes it settle a bit faster.
    alphaDecay = 0.03,
  } = config.simulation ?? {};

  return d3
    .forceSimulation<D3Node, D3Edge>(nodes)
    .alphaMin(alphaMin)
    .alphaDecay(alphaDecay)
    .force('link',
      d3.forceLink<D3Node, D3Edge>(edges)
        .id((d) => d.id)
        .distance(linkDistance),
    )
    .force('charge', d3.forceManyBody().strength(chargeStrength))
    .force('center', d3.forceCenter(width / 2, height / 2))
    .force('collide',
      d3.forceCollide<D3Node>()
        .radius((d) => (d._radius ?? 5) + 2),
    );
}

export function stopSimulation(
  sim: d3.Simulation<D3Node, D3Edge>): void 
{
  sim.stop();
}

/** Heat up simulation — used when dragging starts. */
export function restartSimulation(
  sim: d3.Simulation<D3Node, D3Edge>, alpha = 0.3): void 
{
  sim.alphaTarget(alpha).restart();
}

/** Cool simulation — used when dragging ends. */
export function coolSimulation(sim: d3.Simulation<D3Node, D3Edge>): void 
{
  sim.alphaTarget(0);
}
