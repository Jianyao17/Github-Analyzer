import * as d3 from 'd3';
import type { Ref } from 'vue';
import { watch, onUnmounted } from 'vue';
import type { CodeGraph, GraphNode, GraphEdge } from '../types/code-graph';

export const useCodeGraphD3 = (
  containerRef: Ref<HTMLElement | null>,
  graphDataRef: Ref<CodeGraph | null>
) => {
  let simulation: d3.Simulation<d3.SimulationNodeDatum, undefined> | null = null;
  let svg: d3.Selection<SVGSVGElement, unknown, null, undefined> | null = null;

  const nodeColor = (type: number) => {
    switch (type) {
      case 0: return '#FBBF24'; // Directory - Yellow
      case 1: return '#A78BFA'; // Namespace - Purple
      case 2: return '#60A5FA'; // File - Blue
      case 3: return '#34D399'; // Class - Green
      case 4: return '#F87171'; // Function - Red
      default: return '#9CA3AF'; // Default - Gray
    }
  };

  const nodeRadius = (type: number) => {
    switch (type) {
      case 0: return 12; // Directory
      case 1: return 10; // Namespace
      case 2: return 8;  // File
      case 3: return 8;  // Class
      case 4: return 6;  // Function
      default: return 5;
    }
  };

  const edgeColor = (type: number) => {
    switch (type) {
      case 0: return '#9CA3AF'; // BelongsTo - Gray
      case 1: return '#6B7280'; // Define - Dark Gray
      case 2: return '#F87171'; // Call - Red
      case 3: return '#60A5FA'; // Include - Blue
      default: return '#D1D5DB';
    }
  };

  const renderGraph = () => {
    if (!containerRef.value || !graphDataRef.value) return;
    
    const container = containerRef.value;
    const { nodes, sourceRelEdges, useRelEdges } = graphDataRef.value;

    // Clear previous
    d3.select(container).selectAll('*').remove();

    const width = container.clientWidth || 800;
    const height = container.clientHeight || 600;

    svg = d3.select(container)
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%')
      .attr('viewBox', [0, 0, width, height]);

    // Setup zoom
    const g = svg.append('g');
    svg.call(d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => {
        g.attr('transform', event.transform);
      }));

    // Prepare data
    const d3Nodes = nodes.map(d => ({ ...d, id: d.pathId })) as (d3.SimulationNodeDatum & GraphNode & { id: string })[];
    const allEdges = [...sourceRelEdges, ...useRelEdges];
    const d3Links = allEdges.map(d => ({ ...d, source: d.from, target: d.to })) as (d3.SimulationLinkDatum<d3.SimulationNodeDatum> & GraphEdge)[];

    simulation = d3.forceSimulation<any, any>(d3Nodes)
      .force('link', d3.forceLink<any, any>(d3Links).id((d: any) => d.id).distance(50))
      .force('charge', d3.forceManyBody().strength(-100))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collide', d3.forceCollide().radius((d: any) => nodeRadius(d.type) + 2));

    // Define arrows
    svg.append('defs').selectAll('marker')
      .data(['arrow-0', 'arrow-1', 'arrow-2', 'arrow-3'])
      .enter().append('marker')
      .attr('id', d => d)
      .attr('viewBox', '0 -5 10 10')
      .attr('refX', 15)
      .attr('refY', 0)
      .attr('markerWidth', 6)
      .attr('markerHeight', 6)
      .attr('orient', 'auto')
      .append('path')
      .attr('fill', d => edgeColor(parseInt(d.split('-')[1])))
      .attr('d', 'M0,-5L10,0L0,5');

    // Draw links
    const link = g.append('g')
      .selectAll('line')
      .data(d3Links)
      .enter().append('line')
      .attr('stroke', d => edgeColor(d.type))
      .attr('stroke-opacity', 0.6)
      .attr('stroke-width', d => d.type === 2 ? 1.5 : 1) // Call edges are slightly thicker
      .attr('stroke-dasharray', d => d.type === 2 ? '4,4' : 'none') // Call edges dashed
      .attr('marker-end', d => `url(#arrow-${d.type})`);

    // Draw nodes
    const nodeGroup = g.append('g')
      .selectAll('g')
      .data(d3Nodes)
      .enter().append('g')
      .call(d3.drag<SVGGElement, any>()
        .on('start', dragstarted)
        .on('drag', dragged)
        .on('end', dragended) as any);

    nodeGroup.append('circle')
      .attr('r', d => nodeRadius(d.type))
      .attr('fill', d => nodeColor(d.type))
      .attr('stroke', '#fff')
      .attr('stroke-width', 1.5);

    nodeGroup.append('text')
      .attr('dx', 12)
      .attr('dy', '.35em')
      .attr('font-size', '10px')
      .attr('fill', 'currentColor')
      .text(d => d.label)
      .attr('class', 'dark:text-gray-300 text-gray-700 pointer-events-none');

    nodeGroup.append('title')
      .text(d => `${d.label} (${d.pathId})`);

    simulation.on('tick', () => {
      link
        .attr('x1', (d: any) => d.source.x)
        .attr('y1', (d: any) => d.source.y)
        .attr('x2', (d: any) => d.target.x)
        .attr('y2', (d: any) => d.target.y);

      nodeGroup.attr('transform', (d: any) => `translate(${d.x},${d.y})`);
    });

    function dragstarted(event: any, d: any) {
      if (!event.active) simulation?.alphaTarget(0.3).restart();
      d.fx = d.x;
      d.fy = d.y;
    }

    function dragged(event: any, d: any) {
      d.fx = event.x;
      d.fy = event.y;
    }

    function dragended(event: any, d: any) {
      if (!event.active) simulation?.alphaTarget(0);
      d.fx = null;
      d.fy = null;
    }
  };

  watch([containerRef, graphDataRef], renderGraph, { deep: true, flush: 'post' });

  onUnmounted(() => {
    if (simulation) {
      simulation.stop();
    }
  });

  return {
    renderGraph
  };
};
