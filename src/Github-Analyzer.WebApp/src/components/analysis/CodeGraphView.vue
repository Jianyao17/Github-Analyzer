<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, nextTick, shallowRef } from 'vue'
import * as d3 from 'd3'

interface Node extends d3.SimulationNodeDatum {
  id: string
  label: string
  type: number
  fx?: number | null
  fy?: number | null
}

interface Edge {
  from: string
  to: string
  type: number
}

interface D3Link extends d3.SimulationLinkDatum<Node> {
  source: string | Node
  target: string | Node
  category: 'source' | 'use'
}

const props = defineProps<{
  nodes: any[]
  sourceEdges: Edge[]
  useEdges: Edge[]
}>()

const containerRef = ref<HTMLDivElement | null>(null)
const svgRef = ref<SVGSVGElement | null>(null)
const simulation = shallowRef<d3.Simulation<Node, D3Link> | null>(null)

// Resize handling
let resizeObserver: ResizeObserver | null = null
const dimensions = ref({ width: 0, height: 0 })

function updateDimensions() {
  if (!containerRef.value) return
  dimensions.value = {
    width: containerRef.value.clientWidth,
    height: containerRef.value.clientHeight
  }
}

function initGraph() {
  if (!svgRef.value || !containerRef.value || props.nodes.length === 0) return
  if (dimensions.value.width === 0 || dimensions.value.height === 0) {
    updateDimensions()
  }

  const { width, height } = dimensions.value
  
  // 1. Prepare Data (Deep copy to avoid mutating props directly)
  const nodes: Node[] = props.nodes.map(n => ({
    ...n,
    id: n.pathId || n.id, // Support both formats
    x: width / 2 + (Math.random() - 0.5) * 200,
    y: height / 2 + (Math.random() - 0.5) * 200
  }))

  const links: D3Link[] = [
    ...props.sourceEdges.map(e => ({ source: e.from, target: e.to, category: 'source' as const })),
    ...props.useEdges.map(e => ({ source: e.from, target: e.to, category: 'use' as const }))
  ].filter(l => 
    nodes.some(n => n.id === l.source) && nodes.some(n => n.id === l.target)
  )

  // 2. Clear SVG
  const mainSvg = d3.select(svgRef.value)
  mainSvg.selectAll('*').remove()
  
  const g = mainSvg.append('g')

  // 3. Zoom
  const zoom = d3.zoom<SVGSVGElement, unknown>()
    .scaleExtent([0.05, 5])
    .on('zoom', (event) => g.attr('transform', event.transform))
  
  mainSvg.call(zoom as any)

  // 4. Force Simulation
  if (simulation.value) simulation.value.stop()

  const sim = d3.forceSimulation<Node>(nodes)
    .force('link', d3.forceLink<Node, D3Link>(links).id(d => d.id).distance(d => d.category === 'source' ? 100 : 200).strength(0.5))
    .force('charge', d3.forceManyBody().strength(-1000).distanceMax(height))
    .force('collide', d3.forceCollide().radius(50))
    .force('center', d3.forceCenter(width / 2, height / 2))
    .force('x', d3.forceX(width / 2).strength(0.05))
    .force('y', d3.forceY(height / 2).strength(0.05))
    .alphaDecay(0.02)

  simulation.value = sim

  // 5. Draw Links
  const link = g.append('g')
    .attr('class', 'links')
    .selectAll('line')
    .data(links)
    .join('line')
    .attr('stroke', d => d.category === 'source' ? '#6366f1' : '#f59e0b')
    .attr('stroke-opacity', 0.6)
    .attr('stroke-width', d => d.category === 'source' ? 2 : 1.5)
    .attr('stroke-dasharray', d => d.category === 'use' ? '5,5' : 'none')

  // 6. Draw Nodes
  const node = g.append('g')
    .attr('class', 'nodes')
    .selectAll('g')
    .data(nodes)
    .join('g')
    .call(d3.drag<any, Node>()
      .on('start', (e, d) => {
        if (!e.active) sim.alphaTarget(0.3).restart()
        d.fx = d.x; d.fy = d.y
      })
      .on('drag', (e, d) => {
        d.fx = e.x; d.fy = e.y
      })
      .on('end', (e, d) => {
        if (!e.active) sim.alphaTarget(0)
        d.fx = null; d.fy = null
      }))

  node.append('circle')
    .attr('r', d => [15, 12, 9, 7][d.type] || 8)
    .attr('fill', d => ['#6366f1', '#10b981', '#3b82f6', '#a855f7'][d.type] || '#94a3b8')
    .attr('stroke', '#fff')
    .attr('stroke-width', 2)

  node.append('text')
    .attr('x', d => ([15, 12, 9, 7][d.type] || 8) + 5)
    .attr('y', 5)
    .text(d => d.label)
    .attr('fill', 'currentColor')
    .style('font-size', '12px')
    .style('font-weight', d => d.type <= 1 ? 'bold' : 'normal')
    .style('pointer-events', 'none')
    .style('text-shadow', '0 1px 2px rgba(0,0,0,0.1)')

  // 7. Tick
  sim.on('tick', () => {
    link
      .attr('x1', d => (d.source as any).x)
      .attr('y1', d => (d.source as any).y)
      .attr('x2', d => (d.target as any).x)
      .attr('y2', d => (d.target as any).y)

    node.attr('transform', d => `translate(${d.x},${d.y})`)
  })

  // Fit to screen initial
  sim.alpha(1).restart()
}

onMounted(() => {
  updateDimensions()
  resizeObserver = new ResizeObserver(() => {
    updateDimensions()
    if (simulation.value) {
      simulation.value.force('center', d3.forceCenter(dimensions.value.width / 2, dimensions.value.height / 2))
      simulation.value.alpha(0.3).restart()
    }
  })
  if (containerRef.value) resizeObserver.observe(containerRef.value)
  initGraph()
})

onUnmounted(() => {
  resizeObserver?.disconnect()
  simulation.value?.stop()
})

watch(() => [props.nodes, props.sourceEdges, props.useEdges], () => {
  nextTick(initGraph)
}, { deep: true })
</script>

<template>
  <div ref="containerRef" class="absolute inset-0 overflow-hidden bg-elevated rounded-[--ui-radius] border border-muted">
    <svg ref="svgRef" class="h-full w-full cursor-grab active:cursor-grabbing"></svg>
    
    <!-- Legend -->
    <div class="absolute top-4 right-4 flex flex-col gap-3 p-4 bg-background/80 backdrop-blur-md rounded-lg border border-muted text-[11px] shadow-xl min-w-[180px]">
      <div class="space-y-2">
        <div class="flex items-center justify-between border-b border-muted/50 pb-1 mb-2">
          <div class="font-bold text-xs uppercase tracking-wider opacity-60">Nodes</div>
          <span class="font-mono bg-muted px-1.5 py-0.5 rounded text-[10px]">{{ nodes.length }}</span>
        </div>
        <div class="flex items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <div class="h-3 w-3 rounded-full bg-[#6366f1]"></div> <span>Namespace / Folder</span>
          </div>
          <span class="font-mono opacity-60">{{ nodes.filter(n => Number(n.type) === 0).length }}</span>
        </div>
        <div class="flex items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <div class="h-3 w-3 rounded-full bg-[#10b981]"></div> <span>File</span>
          </div>
          <span class="font-mono opacity-60">{{ nodes.filter(n => Number(n.type) === 1).length }}</span>
        </div>
        <div class="flex items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <div class="h-3 w-3 rounded-full bg-[#3b82f6]"></div> <span>Class / Interface</span>
          </div>
          <span class="font-mono opacity-60">{{ nodes.filter(n => Number(n.type) === 2).length }}</span>
        </div>
        <div class="flex items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <div class="h-3 w-3 rounded-full bg-[#a855f7]"></div> <span>Function / Method</span>
          </div>
          <span class="font-mono opacity-60">{{ nodes.filter(n => Number(n.type) === 3).length }}</span>
        </div>
      </div>
      
      <div class="space-y-2 mt-2">
        <div class="flex items-center justify-between border-b border-muted/50 pb-1 mb-2">
          <div class="font-bold text-xs uppercase tracking-wider opacity-60">Edges</div>
          <span class="font-mono bg-muted px-1.5 py-0.5 rounded text-[10px]">{{ sourceEdges.length + useEdges.length }}</span>
        </div>
        <div class="flex items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <div class="h-0.5 w-6 bg-[#6366f1]"></div> <span>Hierarchy</span>
          </div>
          <span class="font-mono opacity-60">{{ sourceEdges.length }}</span>
        </div>
        <div class="flex items-center justify-between gap-3">
          <div class="flex items-center gap-2">
            <div class="h-0.5 w-6 bg-[#f59e0b] border-t-2 border-dashed border-[#f59e0b]/50"></div> <span>Usage</span>
          </div>
          <span class="font-mono opacity-60">{{ useEdges.length }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.links line {
  transition: stroke-opacity 0.2s;
}
.nodes g:hover circle {
  stroke: #4f46e5;
  stroke-width: 3px;
}
</style>
