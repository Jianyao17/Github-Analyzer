import * as d3 from 'd3';
import type { GraphConfig } from '@graph.types';
import { EDGE_TYPE_KEYS } from '@graph/config';

export class MarkerPass 
{
  run(
    svg:    d3.Selection<SVGSVGElement, unknown, null, undefined>,
    config: GraphConfig,
  ): void 
  {
    const edgeTypeNums = Object.keys(EDGE_TYPE_KEYS).map(Number);

    let defs = svg.select<SVGDefsElement>('defs');
    if (defs.empty()) 
    {
      defs = svg.append('defs');
    }

    const bound = defs.selectAll('marker').data(edgeTypeNums);
    bound.exit().remove();

    bound
      .enter()
      .append('marker')
      .attr('id', (typeNum) => `graph-arrow-${typeNum}`)
      .attr('viewBox', '0 -5 10 10')
      .attr('refX',         10)
      .attr('refY',         0)
      .attr('markerWidth',  6)
      .attr('markerHeight', 6)
      .attr('orient',       'auto')
      .append('path')
      .attr('d',    'M0,-5L10,0L0,5')
      .attr('fill', (typeNum) => 
      {
        const key = EDGE_TYPE_KEYS[typeNum] ?? 'default';
        return (config.edgeTypes[key] ?? config.edgeTypes['default']).color;
      });
  }
}
