// ─── Style Config Types ───────────────────────────────────────────────────────

export interface NodeTypeStyle
{
  /** Type color — used for the color dot and icon stroke. */
  color: string;

  /** Lucide icon name, e.g. 'folder', 'file-code', 'braces'. */
  icon: string;
}

export interface EdgeTypeStyle
{
  color:       string;
  strokeWidth: number;
  dashArray:   string; // e.g. 'none' | '4,4'
}

// ─── Node Card Config ─────────────────────────────────────────────────────────
// Controls the visual layout of the rectangular card node.
// All measurements are in SVG user units.

export interface NodeCardConfig
{
  /** Total card height. */
  height: number;

  /** Icon size (width = height). The Lucide SVG is scaled to this. */
  iconSize: number;

  /** Distance from the card's left edge to the icon. */
  paddingLeft: number;

  /** Distance from the label text to the card's right edge. */
  paddingRight: number;

  /** Space between the icon and the label text. */
  gap: number;

  /** Approximate width of a single character, used to compute dynamic card width. */
  approxCharWidth: number;

  /** Corner radius of the background rect. */
  cornerRadius: number;

  /** Label font family (e.g. 'JetBrains Mono, monospace'). */
  labelFontFamily: string;

  /** Label font size (px). */
  labelFontSize: number;

  /** Label letter spacing (px). Negative values condense the text. */
  labelLetterSpacing: number;

  /**
   * Extra gap (SVG units) between the edge line endpoint and the card border.
   * Used to avoid arrow overlap with the card rect.
   */
  arrowGap: number;
}

// ─── Graph Config ─────────────────────────────────────────────────────────────

export interface GraphConfig
{
  width?:  number;
  height?: number;

  nodeTypes: Record<string, NodeTypeStyle>;
  edgeTypes: Record<string, EdgeTypeStyle>;

  /**
   * Visual config for the rectangular card nodes.
   * If omitted, NodeRenderer falls back to sensible defaults.
   */
  nodeCard?: NodeCardConfig;

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

    /** Graph layout structure. Default: 'star-balloon' */
    layoutType?: 'star-balloon' | 'hierarchical';

    /** Orientation for hierarchical layout. Default: 'LR' */
    orientation?: 'LR' | 'RL' | 'TB' | 'BT';

    /** Gap (in pixels) between hierarchy levels. Only used if layoutType is 'hierarchical'. Default: 150 */
    levelGap?: number;

    /** Gap (in pixels) between sibling nodes in hierarchy. Default: 50 */
    nodeGap?: number;

    /** Angular spacing offset (in degrees) for multiple edges of different types between the same nodes. Default: 45 */
    arrowSpacingAngle?: number;
  };
}
