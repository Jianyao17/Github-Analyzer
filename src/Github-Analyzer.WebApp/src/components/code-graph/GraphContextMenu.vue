<script setup lang="ts">
import type { D3Node } from '@graph.types';
import type { CodeGraph } from '@/types/analysis/code-graph';
import { NODE_TYPE_KEYS, defaultGraphConfig } from '@graph/config';
import { ref, onMounted, onUnmounted, computed, watch } from 'vue';
import * as d3 from 'd3';

const props = defineProps<{
  show: boolean;
  x: number;
  y: number;
  node: D3Node | null;
  isPinned: boolean;
  graphContainer: HTMLElement | null;
  data: CodeGraph;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'toggle-pin'): void;
  (e: 'show-source-code', node: D3Node): void;
  (e: 'highlight-relations', node: D3Node): void;
  (e: 'focus-node', node: D3Node): void;
}>();

const menuRef = ref<HTMLElement | null>(null);

const menuX = ref(props.x);
const menuY = ref(props.y);

watch(() => props.x, (val) => { if (!props.isPinned) menuX.value = val; });
watch(() => props.y, (val) => { if (!props.isPinned) menuY.value = val; });

let isDragging = false;
let startDragX = 0;
let startDragY = 0;
let startMenuX = 0;
let startMenuY = 0;

function startDrag(e: MouseEvent) 
{
  if (e.button !== 0) return; // Only left click

  isDragging = true;
  
  startDragX = e.clientX;
  startDragY = e.clientY;
  
  startMenuX = menuX.value;
  startMenuY = menuY.value;
  document.addEventListener('mousemove', onDrag);
  document.addEventListener('mouseup', stopDrag);
}

function onDrag(e: MouseEvent) 
{
  if (!isDragging) return;

  menuX.value = startMenuX + (e.clientX - startDragX);
  menuY.value = startMenuY + (e.clientY - startDragY);
}

function stopDrag() 
{
  isDragging = false;

  document.removeEventListener('mousemove', onDrag);
  document.removeEventListener('mouseup', stopDrag);
}

// ─── Tracking Node Position ──────────────────────────────────────────────────
const nodeScreenX = ref(props.x);
const nodeScreenY = ref(props.y);
let rafId: number | null = null;

let cachedNodeEl: Element | null = null;

function trackNodePosition() 
{
  rafId = requestAnimationFrame(trackNodePosition);

  if (!props.node || !props.graphContainer || !props.show) return;
  
  if (cachedNodeEl && !cachedNodeEl.isConnected) 
  {
    cachedNodeEl = null;
  }

  if (!cachedNodeEl) 
  {
    const nodes = Array.from(props.graphContainer.querySelectorAll('g.node'));
    // In D3, data is often bound to __data__
    cachedNodeEl = nodes.find(el => (el as any).__data__?.id === props.node?.id) || null;
  }

  if (cachedNodeEl) 
  {
    const rect = cachedNodeEl.getBoundingClientRect();
    const containerRect = props.graphContainer.getBoundingClientRect();
    
    // Exact center of the node in screen pixels, relative to the container
    nodeScreenX.value = rect.left - containerRect.left + rect.width / 2;
    nodeScreenY.value = rect.top - containerRect.top + rect.height / 2;
  } 
  else 
  {
    // Fallback if DOM node is not found yet
    const svgEl = props.graphContainer.querySelector('svg');
    if (svgEl) 
    {
      const transform = d3.zoomTransform(svgEl);
      nodeScreenX.value = (props.node.x || 0) * transform.k + transform.x;
      nodeScreenY.value = (props.node.y || 0) * transform.k + transform.y;
    }
  }
}

onMounted(() => 
{
  trackNodePosition();
});

onUnmounted(() => 
{
  if (rafId) cancelAnimationFrame(rafId);
});

const sciFiPath = computed(() => 
{
  const nx = nodeScreenX.value;
  const ny = nodeScreenY.value;
  
  // We'll anchor the line to the middle-left (or middle-right) of the header
  // Let's use menuX and menuY + 20 (approx middle of the header)
  const mx = menuX.value + 10; 
  const my = menuY.value + 20;

  const dy = ny - my;
  const dxTotal = mx - nx;
  
  if (Math.abs(dxTotal) < Math.abs(dy)) 
  {
    // Diagonal to X-level, then vertical
    const dirY = my > ny ? 1 : -1;
    const dy2 = Math.abs(dxTotal) * dirY;
    return `M ${nx},${ny} L ${mx},${ny + dy2} L ${mx},${my}`;
  } 
  else 
  {
    // Diagonal to Y-level, then horizontal
    const dirX = mx > nx ? 1 : -1;
    const dx = Math.abs(dy) * dirX;
    return `M ${nx},${ny} L ${nx + dx},${my} L ${mx},${my}`;
  }
});

// ─── Computeds ──────────────────────────────────────────────────────────────
const iconName = computed(() => 
{
  if (!props.node) return 'i-lucide-circle';
  
  const typeKey = NODE_TYPE_KEYS[props.node.type] ?? 'default';
  const style = defaultGraphConfig.nodeTypes[typeKey] ?? defaultGraphConfig.nodeTypes['default'];
  
  return `i-lucide-${style.icon}`;
});

const iconColor = computed(() => 
{
  if (!props.node) return '#9CA3AF';
  const typeKey = NODE_TYPE_KEYS[props.node.type] ?? 'default';
  const style = defaultGraphConfig.nodeTypes[typeKey] ?? defaultGraphConfig.nodeTypes['default'];
  return style.color;
});

const shortPath = computed(() => 
{
  const path = props.node?.pathId || '';
  return path.length > 50 ? '...' + path.slice(-47) : path;
});

const incomingCount = computed(() => 
{
  if (!props.node || !props.data) return 0;
  return props.data.sourceRelEdges.filter(e => e.to === props.node!.id).length +
         props.data.useRelEdges.filter(e => e.to === props.node!.id).length;
});

const outgoingCount = computed(() => 
{
  if (!props.node || !props.data) return 0;
  return props.data.sourceRelEdges.filter(e => e.from === props.node!.id).length +
         props.data.useRelEdges.filter(e => e.from === props.node!.id).length;
});

const incomingNodes = computed(() => 
{
  if (!props.node || !props.data) return [];
  const edges = [
    ...props.data.sourceRelEdges.filter(e => e.to === props.node!.id),
    ...props.data.useRelEdges.filter(e => e.to === props.node!.id)
  ];

  return edges
    .map(e => props.data.nodes.find(n => 
      (n as D3Node).id === e.from || 
      (n as D3Node).pathId === e.from
    ))
    .filter(Boolean) as D3Node[];
});

const outgoingNodes = computed(() => 
{
  if (!props.node || !props.data) return [];
  const edges = [
    ...props.data.sourceRelEdges.filter(e => e.from === props.node!.id),
    ...props.data.useRelEdges.filter(e => e.from === props.node!.id)
  ];

  return edges
    .map(e => props.data.nodes.find(n => 
      (n as D3Node).id === e.to || 
      (n as D3Node).pathId === e.to
    ))
    .filter(Boolean) as D3Node[];
});

const isRelationsExpanded = ref(false);
const isRelationsCopied = ref(false);

async function handleCopyRelations() 
{
  if (!props.node) return;
  const inText = incomingNodes.value.map(n => `- ${n.pathId}`).join('\n');
  const outText = outgoingNodes.value.map(n => `- ${n.pathId}`).join('\n');
  const text = `Node: ${props.node.pathId}\n\nIncoming (${incomingCount.value}):\n${inText || 'None'}\n\nOutgoing (${outgoingCount.value}):\n${outText || 'None'}`;
  
  await navigator.clipboard.writeText(text);
  isRelationsCopied.value = true;
  setTimeout(() => isRelationsCopied.value = false, 2000);
}

// ─── Actions ──────────────────────────────────────────────────────────────────
function close() 
{
  emit('close');
}

const isCopied = ref(false);

async function handleCopyPath() 
{
  if (props.node?.pathId) 
  {
    await navigator.clipboard.writeText(props.node.pathId);
    isCopied.value = true;
    setTimeout(() => 
    {
      isCopied.value = false;
    }, 2000);
  }
}

const showPathTooltip = ref(false);
const pathTooltipX = ref(0);
const pathTooltipY = ref(0);

function handlePathMouseMove(e: MouseEvent) 
{
  pathTooltipX.value = e.clientX;
  pathTooltipY.value = e.clientY + 20;
}

function onClickOutside(e: MouseEvent) 
{
  if (!props.show || props.isPinned) return;
  if (menuRef.value && !menuRef.value.contains(e.target as Node)) 
  {
    // Cek jika klik pada node SVG, jangan langsung tutup, biarkan onContextMenu yang handle
    // Namun event klik biasa pada canvas akan menutup menu.
    close();
  }
}

onMounted(() => 
{
  document.addEventListener('click', onClickOutside);
  document.addEventListener('contextmenu', onClickOutside);
});

onUnmounted(() => 
{
  document.removeEventListener('click', onClickOutside);
  document.removeEventListener('contextmenu', onClickOutside);
});
</script>

<template>
  <div v-if="show"
    class="pointer-events-none absolute inset-0"
  >
    <!-- Callout Line (z-0 goes behind graphContainer z-10) -->
    <svg class="pointer-events-none absolute inset-0 z-0 h-full w-full">
      <path 
        :d="sciFiPath"
        fill="none"
        stroke="var(--ui-border)" 
        stroke-width="2" 
      />
    </svg>

    <!-- Menu Card (z-50 goes on top of graphContainer z-10) -->
    <div
      ref="menuRef"
      class="
        pointer-events-auto absolute z-[1050] flex max-w-[320px] min-w-[240px]
        flex-col rounded-lg bg-[var(--ui-bg)]/95 shadow-xl ring-1
        ring-[var(--ui-border)] backdrop-blur-sm
        focus:outline-none
      "
      :style="{ left: menuX + 'px', top: menuY + 'px' }"
    >
      <!-- Header -->
      <div class="
        flex h-9 cursor-move items-center justify-between overflow-hidden
        rounded-t-lg border-b border-[var(--ui-border)]
        bg-[var(--ui-bg-elevated)]/50 py-0 pr-0 pl-3 select-none
      "
        @mousedown.stop="startDrag"
      >
        <div class="flex min-w-0 items-center gap-2">
          <NIcon :name="iconName"
            class="h-4 w-4 shrink-0"
            :style="{ color: iconColor }"
          />
          <span class="truncate text-sm font-medium text-[var(--ui-text)]"
            :title="node?.label"
          >
            {{ node?.label }}
          </span>
        </div>
        <div class="flex h-full shrink-0 items-stretch"
          @mousedown.stop
        >
          <button 
            class="
              relative flex h-full w-9 items-center justify-center
              overflow-hidden transition-all duration-300
            "
            :class="isPinned 
              ? `
                bg-[var(--ui-primary)]/10 text-[var(--ui-primary)]
                hover:bg-[var(--ui-primary)]/20
              ` 
              : `
                text-[var(--ui-text-muted)]
                hover:bg-gray-500/10 hover:text-[var(--ui-text)]
              `"
            @click.stop="$emit('toggle-pin')"
            :title="isPinned ? 'Unpin menu' : 'Pin menu'"
          >
            <NIcon name="i-lucide-pin"
              class="absolute h-4 w-4 transform transition-all duration-300"
              :class="isPinned ? 'scale-50 -rotate-90 opacity-0' : `
                scale-100 rotate-0 opacity-100
              `"
            />
            <NIcon name="i-lucide-pin-off"
              class="absolute h-4 w-4 transform transition-all duration-300"
              :class="isPinned ? 'scale-100 rotate-0 opacity-100' : `
                scale-150 rotate-90 opacity-0
              `"
            />
          </button>
          <button 
            class="
              flex h-full w-9 items-center justify-center
              text-[var(--ui-text-muted)] transition-colors
              hover:bg-red-500/10 hover:text-red-500
            "
            @click.stop="close"
            title="Close"
          >
            <NIcon name="i-lucide-x"
              class="h-4 w-4"
            />
          </button>
        </div>
      </div>

      <!-- Info Body -->
      <div class="
        flex flex-col gap-2 border-b border-[var(--ui-border)] px-3 py-2
      "
      >
        <div class="flex items-center justify-between gap-2">
          
          <!-- Path & Copy Button (Hover) -->
          <div class="flex min-w-0 flex-1">
            <button 
              @click.stop="handleCopyPath"
              @mouseenter="showPathTooltip = true"
              @mouseleave="showPathTooltip = false"
              @mousemove="handlePathMouseMove"
              class="
                group/path relative -ml-1 flex w-full min-w-0 items-center
                rounded-sm py-0.5 pl-1 text-left
              "
            >

              <span class="
                truncate font-mono text-xs text-[var(--ui-text-muted)]
                transition-colors
                group-hover/path:text-[var(--ui-text)]
              "
              >
                {{ shortPath }}
              </span>
              <div
                class="
                  absolute top-0 right-0 bottom-0 flex w-10 items-center
                  justify-end bg-gradient-to-l from-[var(--ui-bg)]
                  via-[var(--ui-bg)] to-transparent pr-1 opacity-0
                  transition-opacity duration-200
                  group-hover/path:opacity-100
                "
              >
                <NIcon name="i-lucide-copy"
                  class="
                    absolute right-1 h-3.5 w-3.5 transform
                    text-[var(--ui-text-muted)] transition-all duration-100
                    group-hover/path:text-[var(--ui-text)]
                  "
                  :class="isCopied ? 'scale-50 opacity-0' : `
                    scale-100 opacity-100
                  `"
                />
                <NIcon name="i-lucide-check"
                  class="
                    absolute right-1 h-3.5 w-3.5 transform text-green-500
                    transition-all duration-100
                  "
                  :class="isCopied ? 'scale-100 opacity-100' : `
                    scale-150 opacity-0
                  `"
                />
              </div>
            </button>
          </div>

          <!-- Lines Indicator -->
          <div v-if="node?.startLine"
            class="
              flex shrink-0 items-center gap-1 rounded
              bg-[var(--ui-bg-elevated)] px-1.5 py-0.5 font-mono text-[10px]
              text-[var(--ui-text-muted)]
            "
            title="Lines"
          >
            L{{ node.startLine }}<span v-if="node.endLine">-{{ node.endLine }}</span>
          </div>

        </div>
      </div>

      <!-- Relations Collapsable -->
      <div class="flex flex-col border-b border-[var(--ui-border)]">
        <button 
          class="
            flex w-full items-center justify-between px-3 py-2 text-xs
            transition-colors
            hover:bg-[var(--ui-bg-elevated)]
          "
          @click.stop="isRelationsExpanded = !isRelationsExpanded"
        >
          <div class="
            flex items-center gap-1.5 font-medium text-[var(--ui-text)]
          "
          >
            <NIcon name="i-lucide-chevron-right" 
              class="h-3.5 w-3.5 transition-transform duration-200" 
              :class="isRelationsExpanded ? 'rotate-90' : 'rotate-0'" 
            />
            <span>Relations</span>
          </div>
          <div class="flex items-center gap-3 text-[var(--ui-text-muted)]">
            <div class="flex items-center gap-1"
              title="Incoming Connections"
            >
              <NIcon name="i-lucide-arrow-down-to-line"
                class="h-3 w-3 text-green-500/80"
              />
              <span>{{ incomingCount }}</span>
            </div>
            <div class="flex items-center gap-1"
              title="Outgoing Connections"
            >
              <NIcon name="i-lucide-arrow-up-from-line"
                class="h-3 w-3 text-blue-500/80"
              />
              <span>{{ outgoingCount }}</span>
            </div>
          </div>
        </button>
        
        <div v-if="isRelationsExpanded"
          class="
            flex flex-col gap-2 bg-[var(--ui-bg-elevated)]/30 px-3 pt-1 pb-3
          "
        >
          
          <div v-if="incomingNodes.length"
            class="flex flex-col gap-1"
          >
            <span class="
              text-[10px] font-semibold tracking-wider
              text-[var(--ui-text-muted)] uppercase
            "
            >Incoming</span>
            <div class="flex max-h-[70px] flex-col gap-0.5 overflow-y-auto">
              <div v-for="n in incomingNodes.slice(0, 3)"
                :key="n.pathId"
                class="truncate text-xs text-[var(--ui-text)]"
                :title="n.pathId"
              >
                {{ n.label }}
              </div>
              <div v-if="incomingNodes.length > 3"
                class="text-[10px] text-[var(--ui-text-muted)] italic"
              >
                ... and {{ incomingNodes.length - 3 }} more
              </div>
            </div>
          </div>
          
          <div v-if="outgoingNodes.length"
            class="mt-1 flex flex-col gap-1"
          >
            <span class="
              text-[10px] font-semibold tracking-wider
              text-[var(--ui-text-muted)] uppercase
            "
            >Outgoing</span>
            <div class="flex max-h-[70px] flex-col gap-0.5 overflow-y-auto">
              <div v-for="n in outgoingNodes.slice(0, 3)"
                :key="n.pathId"
                class="truncate text-xs text-[var(--ui-text)]"
                :title="n.pathId"
              >
                {{ n.label }}
              </div>
              <div v-if="outgoingNodes.length > 3"
                class="text-[10px] text-[var(--ui-text-muted)] italic"
              >
                ... and {{ outgoingNodes.length - 3 }} more
              </div>
            </div>
          </div>

          <div v-if="!incomingNodes.length && !outgoingNodes.length"
            class="py-2 text-center text-xs text-[var(--ui-text-muted)] italic"
          >
            No relations found.
          </div>
          
          <!-- Relations Actions -->
          <div class="mt-1 flex gap-2">
            <button 
              class="
                flex flex-1 items-center justify-center gap-1.5 rounded
                bg-[var(--ui-primary)]/10 px-2 py-1.5 text-xs
                text-[var(--ui-primary)] transition-colors
                hover:bg-[var(--ui-primary)]/20
              "
              @click.stop="$emit('highlight-relations', node!)"
            >
              <NIcon name="i-lucide-network"
                class="h-3.5 w-3.5"
              />
              Highlight
            </button>
            <button 
              class="
                relative flex flex-1 items-center justify-center gap-1.5
                overflow-hidden rounded border border-[var(--ui-border)] px-2
                py-1.5 text-xs text-[var(--ui-text)] transition-colors
                hover:bg-[var(--ui-bg-elevated)]
              "
              @click.stop="handleCopyRelations"
            >
              <NIcon :name="isRelationsCopied ? 'i-lucide-check' : 'i-lucide-copy'" 
                class="h-3.5 w-3.5 transition-all" 
                :class="isRelationsCopied ? 'text-green-500' : `
                  text-[var(--ui-text-muted)]
                `"
              />
              <span>Copy</span>
            </button>
          </div>

        </div>
      </div>

      <!-- Actions -->
      <div class="flex flex-col p-1.5">
        <button
          class="
            flex w-full items-center gap-2.5 rounded px-2.5 py-2 text-left
            text-sm text-[var(--ui-text)] transition-colors
            hover:bg-[var(--ui-bg-elevated)]
          "
          @click.stop="$emit('focus-node', node!)"
        >
          <NIcon name="i-lucide-target"
            class="h-4 w-4 text-[var(--ui-text-muted)]"
          />
          Focus Node
        </button>
        <!-- Highlight Relations button has been moved to the Collapsable Relations section -->
        <button v-if="node && node.type > 1 && node.startLine !== undefined"
          class="
            flex w-full items-center gap-2.5 rounded px-2.5 py-2 text-left
            text-sm text-[var(--ui-text)] transition-colors
            hover:bg-[var(--ui-bg-elevated)]
          "
          @click.stop="$emit('show-source-code', node!)"
        >
          <NIcon name="i-lucide-code"
            class="h-4 w-4 text-[var(--ui-text-muted)]"
          />
          Show Source Code
        </button>
      </div>
    </div>
  </div>

  <Teleport to="body">
    <div v-if="showPathTooltip && node?.pathId"
      class="
        pointer-events-none fixed z-[9999] rounded bg-[var(--ui-bg-elevated)]
        px-2 py-1.5 text-xs font-medium whitespace-nowrap text-[var(--ui-text)]
        shadow-lg ring-1 ring-[var(--ui-border)] backdrop-blur-sm
      "
      :style="{ left: pathTooltipX + 'px', top: pathTooltipY + 'px' }"
    >
      {{ node.pathId }}
    </div>
  </Teleport>
</template>
