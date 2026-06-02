import type { D3Node, D3Edge, GraphConfig } from '../types/_index';

/**
 * Common interface for all graph layout algorithms.
 */
export interface IGraphLayout
{
  /**
   * Calculates layout constraints/targets and updates the given simulation with specific forces.
   */
  apply(
    sim: d3.Simulation<D3Node, D3Edge>,
    nodes: D3Node[],
    edges: D3Edge[],
    width: number,
    height: number,
    config: GraphConfig
  ): void;

  /**
   * Cleans up any specific forces or state added by this layout.
   */
  destroy(sim: d3.Simulation<D3Node, D3Edge>): void;
}
