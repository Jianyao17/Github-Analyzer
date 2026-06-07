import type * as d3 from 'd3';
import type { D3Node, D3Edge } from './node-edge';

// ─── D3 Selection Type Aliases ────────────────────────────────────────────────
// Digunakan di GraphView dan plugin untuk keterbacaan.

export type SvgSelection      = d3.Selection<SVGSVGElement,   unknown, null,       undefined>;
export type ViewportSelection = d3.Selection<SVGGElement,     unknown, null,       undefined>;
export type NodeSelection     = d3.Selection<SVGGElement,     D3Node,  SVGGElement, unknown>;
export type EdgeSelection     = d3.Selection<SVGPathElement,  D3Edge,  SVGGElement, unknown>;

