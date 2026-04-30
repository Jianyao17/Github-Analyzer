<script setup lang="ts">
import { ref, onMounted, watch, onUnmounted, nextTick } from 'vue'
import * as d3 from 'd3'

interface Node {
  id: string
  label: string
  type: string
  x?: number
  y?: number
}

interface Edge {
  source: string | Node
  target: string | Node
  type: string
}

const props = defineProps<{
  nodes: Node[]
  edges: Edge[]
}>()

const svgRef = ref<SVGSVGElement | null>(null)
const containerRef = ref<HTMLDivElement | null>(null)
let simulation: d3.Simulation<Node, Edge> | null = null

async function initGraph() {
  if (!svgRef.value || !containerRef.value || !props.nodes.length) return

  await nextTick()

  if (simulation) {
    simulation.stop()
    simulation = null
  }

  // Clear previous SVG content
  d3.select(svgRef.value).selectAll('*').remove()

  const width = containerRef.value.clientWidth || 800
  const height = containerRef.value.clientHeight || 500
  const padding = 28

  const svg = d3.select(svgRef.value)
    .attr('viewBox', [-padding, -padding, width + (padding * 2), height + (padding * 2)])
    .attr('preserveAspectRatio', 'xMidYMid meet')
    .append('g')

  // Zoom behavior
  const zoom = d3.zoom<SVGSVGElement, unknown>()
    .scaleExtent([0.1, 4])
    .on('zoom', (event) => {
      svg.attr('transform', event.transform)
    })

  d3.select(svgRef.value).call(zoom)

  // Simulation setup
  simulation = d3.forceSimulation<Node>(props.nodes)
    .force('link', d3.forceLink<Node, Edge>(props.edges).id(d => d.id).distance(100))
    .force('charge', d3.forceManyBody().strength(-300))
    .force('center', d3.forceCenter(width / 2, height / 2))

  // Link lines
  const link = svg.append('g')
    .attr('stroke', '#999')
    .attr('stroke-opacity', 0.6)
    .selectAll('line')
    .data(props.edges)
    .join('line')
    .attr('stroke-width', 2)

  // Node groups
  const node = svg.append('g')
    .attr('stroke', '#fff')
    .attr('stroke-width', 1.5)
    .selectAll('g')
    .data(props.nodes)
    .join('g')
    .call(d3.drag<any, Node>()
      .on('start', dragstarted)
      .on('drag', dragged)
      .on('end', dragended))

  // Node circles
  node.append('circle')
    .attr('r', 8)
    .attr('fill', d => {
      switch (d.type) {
        case 'File': return '#42b883'
        case 'Method': return '#35495e'
        case 'Class': return '#61dafb'
        default: return '#ccc'
      }
    })

  // Node labels
  node.append('text')
    .attr('x', 12)
    .attr('y', 4)
    .text(d => d.label)
    .attr('stroke', 'none')
    .attr('fill', 'currentColor')
    .style('font-size', '10px')
    .style('pointer-events', 'none')

  simulation.on('tick', () => {
    link
      .attr('x1', d => (d.source as Node).x!)
      .attr('y1', d => (d.source as Node).y!)
      .attr('x2', d => (d.target as Node).x!)
      .attr('y2', d => (d.target as Node).y!)

    node.attr('transform', d => `translate(${d.x},${d.y})`)
  })

  function dragstarted(event: any, d: Node) {
    if (!event.active) simulation?.alphaTarget(0.3).restart()
    d.x = event.x
    d.y = event.y
  }

  function dragged(event: any, d: Node) {
    d.x = event.x
    d.y = event.y
  }

  function dragended(event: any, _d: Node) {
    if (!event.active) simulation?.alphaTarget(0)
  }
}

onMounted(() => {
  initGraph()
})

watch(() => [props.nodes, props.edges], initGraph)

onUnmounted(() => {
  simulation?.stop()
})
</script>

<template>
  <div ref="containerRef" class="absolute inset-0 overflow-hidden bg-elevated rounded-[--ui-radius]">
    <svg ref="svgRef" class="h-full w-full cursor-move"></svg>
    <div class="absolute top-2 right-2 flex gap-2">
      <div class="flex items-center gap-1 text-[10px]">
        <div class="h-2 w-2 rounded-full bg-[#42b883]"></div> File
      </div>
      <div class="flex items-center gap-1 text-[10px]">
        <div class="h-2 w-2 rounded-full bg-[#35495e]"></div> Method
      </div>
    </div>
  </div>
</template>
