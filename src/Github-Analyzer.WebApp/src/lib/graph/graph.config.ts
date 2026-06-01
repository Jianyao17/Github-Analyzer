import type { GraphConfig } from './types/_index';

// ─── Default Graph Config ─────────────────────────────────────────────────────
// Node/edge styles defined as config objects — no switch-case in renderers.
// Add new node/edge types here without touching renderer code.

export const defaultGraphConfig: GraphConfig =
{
  // ── Node types ──────────────────────────────────────────────────────────────
  // icon: Lucide icon name (see https://lucide.dev/icons)
  nodeTypes: {
    directory: { color: '#FBBF24', icon: 'folder'    }, // Yellow
    namespace: { color: '#A78BFA', icon: 'layers'    }, // Purple
    file:      { color: '#60A5FA', icon: 'file-code' }, // Blue
    class:     { color: '#34D399', icon: 'box'       }, // Green
    function:  { color: '#F87171', icon: 'braces'    }, // Red
    default:   { color: '#9CA3AF', icon: 'circle'    }, // Gray (fallback)
  },

  // ── Edge types ──────────────────────────────────────────────────────────────
  edgeTypes: {
    belongsTo: { color: '#9CA3AF', strokeWidth: 1,   dashArray: 'none' },
    define:    { color: '#6B7280', strokeWidth: 1,   dashArray: 'none' },
    call:      { color: '#F87171', strokeWidth: 1.5, dashArray: '4,4'  },
    include:   { color: '#60A5FA', strokeWidth: 1,   dashArray: 'none' },
    default:   { color: '#D1D5DB', strokeWidth: 1,   dashArray: 'none' }, // fallback
  },

  // ── Node card layout ────────────────────────────────────────────────────────
  // VS Code explorer-inspired compact card. All values in SVG units.
  nodeCard: {
    height:          24,  // total card height (~24px like VS Code items)
    iconSize:        14,  // icon rendered at this size (24×24 Lucide → scaled)
    paddingLeft:      8,  // gap from card left edge to icon left edge
    paddingRight:     8,  // gap from label text to card right edge
    gap:              4,  // space between icon right edge and label text

    labelFontFamily: '\'Segoe UI\', sans-serif',
    labelLetterSpacing: 0, // px 
    labelFontSize:   12,  // px

    arrowGap:         4,  // space between arrow tip and card border
    cornerRadius:    12,  // rect border-radius (more rounded)
    approxCharWidth:  7.5,// approx width of a JetBrains Mono character at 12px
  },

  // ── Simulation ──────────────────────────────────────────────────────────────
  simulation: {
    linkDistance:   60,
    chargeStrength: -150,
    // Stop when visually stable (D3 default: 0.001 ≈ 300 ticks)
    alphaMin:   0.005,
    // Decay rate — higher = fewer total ticks (D3 default: ~0.0228)
    alphaDecay: 0.025,
  },
};

// ─── Type Key Mappings ────────────────────────────────────────────────────────
// Maps backend numeric type codes → config keys above.
// Matches the type enum from the backend (GraphNode.type, GraphEdge.type).

export const NODE_TYPE_KEYS: Record<number, string> =
{
  0: 'directory',
  1: 'namespace',
  2: 'file',
  3: 'class',
  4: 'function',
};

export const EDGE_TYPE_KEYS: Record<number, string> =
{
  0: 'belongsTo',
  1: 'define',
  2: 'call',
  3: 'include',
};
