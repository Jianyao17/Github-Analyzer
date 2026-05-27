import * as d3 from 'd3';
import type { D3Node, GraphConfig, INodeRenderer } from '../graph.types';
import { NODE_TYPE_KEYS } from '../graph.config';

export class NodeRenderer implements INodeRenderer 
{
  // Selection of all node groups
  private selection: d3.Selection<SVGGElement, D3Node, SVGGElement, unknown> | null = null;

  /**
   * Renders one <g> per node containing: <circle>, <text> label, <title> tooltip.
   * Also annotates d._radius on each datum for the simulation collision force.
   */
  render(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    nodes: D3Node[],
    config: GraphConfig,
  ): void 
  {
    // Select all node groups and bind nodes data
    this.selection = viewport
      .append('g')
      .attr('class', 'nodes')
      .selectAll<SVGGElement, D3Node>('g')
      .data(nodes, (d) => d.id)
      .enter()
      .append('g')
      .attr('class', 'node')
      .attr('cursor', 'grab');

    // Render each node
    this.selection.each(function (d) 
    {
      const typeKey = NODE_TYPE_KEYS[d.type] ?? 'default';
      const style = config.nodeTypes[typeKey] ?? config.nodeTypes['default'];

      // Annotate radius so the collision force can read it
      d._radius = style.radius;

      const g = d3.select<SVGGElement, D3Node>(this);

      g.append('circle')
        .attr('r', style.radius)
        .attr('fill', style.color)
        .attr('stroke', '#fff')
        .attr('stroke-width', 1.5);

      g.append('text')
        .attr('dx', style.radius + 4)
        .attr('dy', '0.35em')
        .attr('font-size', '10px')
        .attr('fill', 'currentColor')
        .attr('pointer-events', 'none')
        .text(d.label);

      // Native browser tooltip (lightweight fallback)
      g.append('title').text(`${d.label}\n${d.pathId}`);
    });
  }

  /** Called on every simulation tick to reposition nodes. */
  updatePositions(): void 
  {
    this.selection?.attr('transform', (d) => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }

  /** Returns the D3 selection for node groups, used by plugins. */
  getSelection(): d3.Selection<SVGGElement, D3Node, SVGGElement, unknown> | null 
  {
    return this.selection;
  }

  /** Reset internal state — called by D3Renderer.destroy(). */
  clear(): void 
  {
    this.selection = null;
  }
}
