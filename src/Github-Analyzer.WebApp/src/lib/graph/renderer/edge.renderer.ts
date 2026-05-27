import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig } from '../graph.types';
import { EDGE_TYPE_KEYS } from '../graph.config';

export class EdgeRenderer 
{
  // Selection of all edge lines
  private selection: d3.Selection<SVGLineElement, D3Edge, SVGGElement, unknown> | null = null;

  /**
   * Renders SVG arrow markers into <defs> and <line> elements into the viewport.
   * svg is needed separately to inject <defs> at the root SVG level.
   */
  render(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    svg: d3.Selection<SVGSVGElement, unknown, null, undefined>,
    edges: D3Edge[],
    config: GraphConfig,
  ): void 
  {
    this.renderArrowMarkers(svg, config);
    this.renderLines(viewport, edges, config);
  }

  private renderArrowMarkers(
    svg: d3.Selection<SVGSVGElement, unknown, null, undefined>,
    config: GraphConfig,
  ): void 
  {
    // Render arrow markers for each edge type
    const edgeTypeNums = Object.keys(EDGE_TYPE_KEYS).map(Number);

    svg
      .append('defs')
      .selectAll('marker')
      .data(edgeTypeNums)
      .enter()
      .append('marker')
      .attr('id', (typeNum) => `graph-arrow-${typeNum}`)
      .attr('viewBox', '0 -5 10 10')
      .attr('refX', 18)
      .attr('refY', 0)
      .attr('markerWidth', 6)
      .attr('markerHeight', 6)
      .attr('orient', 'auto')
      .append('path')
      .attr('d', 'M0,-5L10,0L0,5')
      .attr('fill', (typeNum) => 
      {
        const key = EDGE_TYPE_KEYS[typeNum] ?? 'default';
        return (config.edgeTypes[key] ?? config.edgeTypes['default']).color;
      });
  }

  private renderLines(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    edges: D3Edge[],
    config: GraphConfig,
  ): void 
  {
    // Select all edge lines and bind edges data
    this.selection = viewport
      .append('g')
      .attr('class', 'edges')
      .selectAll<SVGLineElement, D3Edge>('line')
      .data(edges)
      .enter()
      .append('line')
      .each(function (d) 
      {
        const key = EDGE_TYPE_KEYS[d.type] ?? 'default';
        const style = config.edgeTypes[key] ?? config.edgeTypes['default'];
        
        d3.select(this)
          .attr('stroke', style.color)
          .attr('stroke-opacity', 0.6)
          .attr('stroke-width', style.strokeWidth)
          .attr('stroke-dasharray', style.dashArray === 'none' ? null : style.dashArray)
          .attr('marker-end', `url(#graph-arrow-${d.type})`);
      });
  }

  /** Called on every simulation tick to reposition edge endpoints. */
  updatePositions(): void 
  {
    this.selection
      ?.attr('x1', (d) => (d.source as D3Node).x ?? 0)
      .attr('y1', (d) => (d.source as D3Node).y ?? 0)
      .attr('x2', (d) => (d.target as D3Node).x ?? 0)
      .attr('y2', (d) => (d.target as D3Node).y ?? 0);
  }

  /** Reset internal state — called by D3Renderer.destroy(). */
  clear(): void 
  {
    this.selection = null;
  }
}
