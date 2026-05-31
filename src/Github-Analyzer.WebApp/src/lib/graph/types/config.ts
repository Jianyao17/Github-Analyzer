// ─── Style Config Types ───────────────────────────────────────────────────────

export interface NodeTypeStyle
{
  color:  string;
  radius: number;
}

export interface EdgeTypeStyle
{
  color:       string;
  strokeWidth: number;
  dashArray:   string; // e.g. 'none' | '4,4'
}

export interface GraphConfig
{
  width?:  number;
  height?: number;
  nodeTypes: Record<string, NodeTypeStyle>;
  edgeTypes: Record<string, EdgeTypeStyle>;
  simulation?:
  {
    /**
     * Desired average distance between connected nodes.
     * Higher values = more spread out graph.
     */
    linkDistance?: number;

    /**
     * Strength of the force attracting or repelling nodes.
     * Negative = repulsion (default).
     * Higher absolute values = stronger forces.
     */
    chargeStrength?: number;

    /**
     * Simulation stops when alpha drops below this value.
     * Default D3 value: 0.001 (runs ~300 ticks).
     * Raise to ~0.05 to stop as soon as the graph is visually stable.
     */
    alphaMin?: number;

    /**
     * Rate at which alpha decays each tick.
     * Default D3 value: ~0.0228 (targets 300 ticks).
     * Raise to decay faster and settle in fewer ticks.
     */
    alphaDecay?: number;
  };
}
