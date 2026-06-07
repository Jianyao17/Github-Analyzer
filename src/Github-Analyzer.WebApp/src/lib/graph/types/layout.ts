import type { D3Node, D3Edge } from './node-edge';
import type { GraphConfig }    from './config';
import type { Dimensions }     from './events';

export interface LayoutResult 
{
  positions: Map<string, { x: number; y: number }> | null;
  animationHint: 'instant' | 'tween' | 'simulate';
}

/**
 * Common interface for all graph layout algorithms.
 */
export interface IGraphLayout 
{
  readonly name: string;
  
  /**
   * Calculates layout constraints/targets and returns the target positions.
   */
  apply(
    nodes: D3Node[],
    edges: D3Edge[],
    dims: Dimensions,
    config: GraphConfig
  ): LayoutResult;
}
