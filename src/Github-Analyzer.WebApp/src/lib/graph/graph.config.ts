import type { GraphConfig } from './types/_index';

// ─── Default Graph Config ─────────────────────────────────────────────────────
// Node/edge styles are defined as config objects — no switch-case.
// Add new node or edge types here without touching any renderer code.

export const defaultGraphConfig: GraphConfig = 
{
  nodeTypes: {
    directory:  { color: '#FBBF24', radius: 12 }, // Yellow
    namespace:  { color: '#A78BFA', radius: 10 }, // Purple
    file:       { color: '#60A5FA', radius: 8  }, // Blue
    class:      { color: '#34D399', radius: 8  }, // Green
    function:   { color: '#F87171', radius: 6  }, // Red
    default:    { color: '#9CA3AF', radius: 5  }, // Gray (fallback)
  },
  edgeTypes: {
    belongsTo: { color: '#9CA3AF', strokeWidth: 1,   dashArray: 'none' },
    define:    { color: '#6B7280', strokeWidth: 1,   dashArray: 'none' },
    call:      { color: '#F87171', strokeWidth: 1.5, dashArray: '4,4'  },
    include:   { color: '#60A5FA', strokeWidth: 1,   dashArray: 'none' },
    default:   { color: '#D1D5DB', strokeWidth: 1,   dashArray: 'none' }, // fallback
  },
  simulation: {
    linkDistance:  50,
    chargeStrength: -100,
    // Stop when visually stable instead of running all ~300 D3 default ticks.
    // D3 default: 0.001. Raise to stop earlier; lower for more precision.
    alphaMin:   0.005,
    // Decay rate per tick. Higher = fewer total ticks.
    // D3 default: ~0.0228 (300 ticks). 0.03 ≈ 150–200 ticks.
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
