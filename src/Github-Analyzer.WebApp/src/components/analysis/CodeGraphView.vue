<script setup lang="ts">
import { ref, onMounted, watch, onUnmounted, nextTick } from 'vue'
import * as d3 from 'd3'

interface GraphNode {
  pathId: string
  label: string
  type: number | string
  x?: number
  y?: number
  id?: string
}

interface GraphEdge {
  from: string
  to: string
  type: number | string
  source?: string | GraphNode
  target?: string | GraphNode
  edgeCategory?: 'sourceRel' | 'useRel'
}

const props = defineProps<{
  nodes: GraphNode[]
  sourceRelEdges: GraphEdge[]
  useRelEdges: GraphEdge[]
}>()

const svgRef = ref<SVGSVGElement | null>(null)
const containerRef = ref<HTMLDivElement | null>(null)
let simulation: d3.Simulation<GraphNode, any> | null = null

async function initGraph() {
  if (!svgRef.value || !containerRef.value || !props.nodes.length) return

  await nextTick()

  if (simulation) {
    simulation.stop()
    simulation = null
  }

  d3.select(svgRef.value).selectAll('*').remove()

  const width = containerRef.value.clientWidth || 800
  const height = containerRef.value.clientHeight || 500
  const padding = 28

  const svg = d3.select(svgRef.value)
    .attr('viewBox', [-padding, -padding, width + (padding * 2), height + (padding * 2)])
    .attr('preserveAspectRatio', 'xMidYMid meet')
    .append('g')

  const zoom = d3.zoom<SVGSVGElement, unknown>()
    .scaleExtent([0.1, 4])
    .on('zoom', (event) => {
      svg.attr('transform', event.transform)
    })

  d3.select(svgRef.value).call(zoom)

  const mappedNodes = props.nodes.map(n => ({ ...n, id: n.pathId })) as GraphNode[]
  const combinedEdges = [
    ...(props.sourceRelEdges || []).map(e => ({ ...e, source: e.from, target: e.to, edgeCategory: 'sourceRel' })),
    ...(props.useRelEdges || []).map(e => ({ ...e, source: e.from, target: e.to, edgeCategory: 'useRel' }))
  ] as GraphEdge[]

  simulation = d3.forceSimulation<GraphNode>(mappedNodes)
    .force('link', d3.forceLink<GraphNode, any>(combinedEdges).id(d => d.id as string).distance(100))
    .force('charge', d3.forceManyBody().strength(-300))
    .force('center', d3.forceCenter(width / 2, height / 2))

  const link = svg.append('g')
    .attr('stroke', '#999')
    .attr('stroke-opacity', 0.6)
    .selectAll('line')
    .data(combinedEdges)
    .join('line')
    .attr('stroke-width', 2)
    .style('stroke-dasharray', (d: any) => d.edgeCategory === 'useRel' ? '5,5' : 'none')
    .style('stroke', (d: any) => d.edgeCategory === 'useRel' ? '#ff9800' : '#999')

  const node = svg.append('g')
    .attr('stroke', '#fff')
    .attr('stroke-width', 1.5)
    .selectAll('g')
    .data(mappedNodes)
    .join('g')
    .call(d3.drag<any, any>()
      .on('start', dragstarted)
      .on('drag', dragged)
      .on('end', dragended))

  node.append('circle')
    .attr('r', 8)
    .attr('fill', d => {
      // 0: FolderOrNamespace, 1: File, 2: Class, 3: Function
      switch (d.type) {
        case 0:
        case 'FolderOrNamespace': return '#ffc107'
        case 1:
        case 'File': return '#42b883'
        case 2:
        case 'Class': return '#61dafb'
        case 3:
        case 'Function': return '#35495e'
        default: return '#ccc'
      }
    })

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
      .attr('x1', (d: any) => d.source.x)
      .attr('y1', (d: any) => d.source.y)
      .attr('x2', (d: any) => d.target.x)
      .attr('y2', (d: any) => d.target.y)

    node.attr('transform', d => `translate(${d.x},${d.y})`)
  })

  function dragstarted(event: any, d: GraphNode) {
    if (!event.active) simulation?.alphaTarget(0.3).restart()
    d.x = event.x
    d.y = event.y
  }

  function dragged(event: any, d: GraphNode) {
    d.x = event.x
    d.y = event.y
  }

  function dragended(event: any, _d: GraphNode) {
    if (!event.active) simulation?.alphaTarget(0)
  }
}

onMounted(() => {
  initGraph()
})

watch(() => [props.nodes, props.sourceRelEdges, props.useRelEdges], initGraph, { deep: true })

onUnmounted(() => {
  simulation?.stop()
})
</script>

<template>
  <div ref="containerRef" class="absolute inset-0 overflow-hidden bg-elevated rounded-[--ui-radius]">
    <svg ref="svgRef" class="h-full w-full cursor-move"></svg>
    <div class="absolute top-2 right-2 flex gap-2">
      <div class="flex flex-col gap-1 bg-elevated/80 p-2 rounded shadow-sm border border-muted text-[10px]">
        <div class="flex items-center gap-1"><div class="h-2 w-2 rounded-full bg-[#ffc107]"></div> Folder/Namespace</div>
        <div class="flex items-center gap-1"><div class="h-2 w-2 rounded-full bg-[#42b883]"></div> File</div>
        <div class="flex items-center gap-1"><div class="h-2 w-2 rounded-full bg-[#61dafb]"></div> Class</div>
        <div class="flex items-center gap-1"><div class="h-2 w-2 rounded-full bg-[#35495e]"></div> Function/Method</div>
        <div class="h-px bg-muted my-1 w-full"></div>
        <div class="flex items-center gap-1"><div class="h-px w-4 bg-[#999]"></div> Declaration</div>
        <div class="flex items-center gap-1"><div class="h-px w-4 bg-[#ff9800]" style="border-top: 1px dashed #ff9800;"></div> Call Usage</div>
      </div>
    </div>
  </div>
</template>
