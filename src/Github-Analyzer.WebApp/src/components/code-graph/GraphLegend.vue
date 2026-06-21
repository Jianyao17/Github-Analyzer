<script setup lang="ts">
import { ref, computed } from 'vue';
import type { CodeGraph } from '@/types/analysis/code-graph';
import { NODE_TYPE_KEYS, EDGE_TYPE_KEYS, defaultGraphConfig } from '@/lib/graph/config';

// ─── Props ────────────────────────────────────────────────────────────────────
const props = defineProps<{
  data: CodeGraph;
}>();

// ─── Legend config ────────────────────────────────────────────────────────────
const NODE_LEGEND = [
  { key: 'directory', label: 'Directory' },
  { key: 'namespace', label: 'Namespace' },
  { key: 'file',      label: 'File'      },
  { key: 'class',     label: 'Class'     },
  { key: 'function',  label: 'Function'  },
] as const;

const EDGE_LEGEND = [
  { key: 'belongsTo', label: 'Belongs To' },
  { key: 'define',    label: 'Define' },
  { key: 'call',      label: 'Call' },
  { key: 'include',   label: 'Include' },
] as const;

// ─── Computed stats ───────────────────────────────────────────────────────────
const nodeCountByType = computed(() =>
  props.data.nodes.reduce<Record<string, number>>((acc, node) =>
  {
    const key = NODE_TYPE_KEYS[node.type] ?? 'other';
    acc[key]  = (acc[key] ?? 0) + 1;
    return acc;
  }, {}));

const edgeCountByType = computed(() => 
{
  const allEdges = [...props.data.sourceRelEdges, ...props.data.useRelEdges];
  return allEdges.reduce<Record<string, number>>((acc, edge) => 
  {
    const key = EDGE_TYPE_KEYS[edge.type] ?? 'other';
    acc[key] = (acc[key] ?? 0) + 1;
    return acc;
  }, {});
});

const totalNodes  = computed(() => props.data.nodes.length);

function getNodeConfig(key: string) 
{
  // Need to cast to any because TS might complain about indexing Record with string
  return (defaultGraphConfig.nodeTypes as any)[key] ?? defaultGraphConfig.nodeTypes.default;
}

function getEdgeConfig(key: string) 
{
  return (defaultGraphConfig.edgeTypes as any)[key] ?? defaultGraphConfig.edgeTypes.default;
}

// ─── Collapse State ───────────────────────────────────────────────────────────
const isExpanded = ref(false);
</script>

<template>
  <!--
    GraphLegend — node type legend with per-type counts and relation counts.
    Collapsed by default on mobile, always expanded on sm+.
  -->
  <div
    class="
      pointer-events-auto w-48 rounded-lg border border-[var(--ui-border)]
      bg-[var(--ui-bg)] text-sm
      sm:w-52
    "
  >
    <!-- ── Header ────────────────────────────────────────────────────────────── -->
    <button
      class="
        flex w-full items-center justify-between px-3 py-2.5 text-left
        font-semibold text-[var(--ui-text-highlighted)]
      "
      @click="isExpanded = !isExpanded"
    >
      <span class="flex items-center gap-1.5">
        Nodes Legend
        <span class="text-xs font-normal text-[var(--ui-text-muted)]">({{ totalNodes }})</span>
      </span>

      <!-- Chevron — only visible on mobile -->
      <NIcon
        :name="isExpanded ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
        class="h-4 w-4 text-[var(--ui-text-muted)] transition-transform"
      />
    </button>

    <!-- ── Body ─────────────────────────────────────────────────────────────── -->
    <div v-show="isExpanded"
      id="graph-legend-content"
    >
      <div class="border-t border-[var(--ui-border)] px-3 pt-2 pb-3">

        <!-- Node types with counts -->
        <div class="space-y-1.5">
          <div
            v-for="item in NODE_LEGEND"
            :key="item.key"
            class="flex items-center justify-between gap-2"
          >
            <div class="flex min-w-0 items-center gap-2">
              <NIcon 
                :name="`i-lucide-${getNodeConfig(item.key).icon}`"
                class="h-4 w-4 shrink-0"
                :style="{ color: getNodeConfig(item.key).color }"
              />
              <span class="truncate text-[var(--ui-text-muted)]">{{ item.label }}</span>
            </div>
            <span class="
              shrink-0 text-xs text-[var(--ui-text-muted)] tabular-nums
            "
            >
              {{ nodeCountByType[item.key] ?? 0 }}
            </span>
          </div>
        </div>

        <!-- Relations section -->
        <div class="mt-3 border-t border-[var(--ui-border)] pt-3">
          <p class="
            mb-1.5 text-xs font-medium tracking-wide text-[var(--ui-text-muted)]
            uppercase
          "
          >
            Relations
          </p>
          <div class="space-y-1.5">
            <div
              v-for="item in EDGE_LEGEND"
              :key="item.key"
              class="flex items-center justify-between gap-2"
            >
              <div class="flex items-center gap-2">
                <svg width="24"
                  height="12"
                  class="shrink-0 overflow-visible"
                >
                  <defs>
                    <marker :id="`legend-arrow-${item.key}`"
                      viewBox="0 -5 10 10"
                      refX="8"
                      refY="0"
                      markerWidth="6"
                      markerHeight="6"
                      orient="auto"
                    >
                      <path d="M0,-5L10,0L0,5"
                        :fill="getEdgeConfig(item.key).color"
                      />
                    </marker>
                  </defs>
                  <line 
                    x1="0"
                    y1="6"
                    x2="22"
                    y2="6" 
                    :stroke="getEdgeConfig(item.key).color"
                    :stroke-width="getEdgeConfig(item.key).strokeWidth"
                    :stroke-dasharray="getEdgeConfig(item.key).dashArray !== 'none' ? getEdgeConfig(item.key).dashArray : undefined"
                    :marker-end="`url(#legend-arrow-${item.key})`"
                  />
                </svg>
                <span class="text-[var(--ui-text-muted)]">{{ item.label }}</span>
              </div>
              <span class="text-xs text-[var(--ui-text-muted)] tabular-nums">
                {{ edgeCountByType[item.key] ?? 0 }}
              </span>
            </div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
