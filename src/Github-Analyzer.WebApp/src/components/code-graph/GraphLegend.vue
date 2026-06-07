<script setup lang="ts">
import { ref, computed } from 'vue';
import type { CodeGraph } from '@/types/analysis/code-graph';
import { NODE_TYPE_KEYS, defaultGraphConfig } from '@/lib/graph/config';

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

// ─── Computed stats ───────────────────────────────────────────────────────────
const nodeCountByType = computed(() =>
  props.data.nodes.reduce<Record<string, number>>((acc, node) =>
  {
    const key = NODE_TYPE_KEYS[node.type] ?? 'other';
    acc[key]  = (acc[key] ?? 0) + 1;
    return acc;
  }, {}));

const totalNodes  = computed(() => props.data.nodes.length);
const sourceCount = computed(() => props.data.sourceRelEdges.length);
const useCount    = computed(() => props.data.useRelEdges.length);

function getColor(key: string): string
{
  return defaultGraphConfig.nodeTypes[key]?.color ?? '#9CA3AF';
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
      pointer-events-auto w-48 rounded-lg border border-gray-200 bg-white
      text-sm
      sm:w-52
      dark:border-gray-700 dark:bg-gray-900
    "
  >
    <!-- ── Header ────────────────────────────────────────────────────────────── -->
    <button
      class="
        flex w-full items-center justify-between px-3 py-2.5 text-left
        font-semibold text-gray-700
        dark:text-gray-300
      "
      @click="isExpanded = !isExpanded"
    >
      <span class="flex items-center gap-1.5">
        Nodes Legend
        <span class="text-xs font-normal text-gray-400">({{ totalNodes }})</span>
      </span>

      <!-- Chevron — only visible on mobile -->
      <NIcon
        :name="isExpanded ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
        class="h-4 w-4 text-gray-400 transition-transform"
      />
    </button>

    <!-- ── Body ─────────────────────────────────────────────────────────────── -->
    <div v-show="isExpanded">
      <div class="
        border-t border-gray-100 px-3 pt-2 pb-3
        dark:border-gray-800
      "
      >

        <!-- Node types with counts -->
        <div class="space-y-1.5">
          <div
            v-for="item in NODE_LEGEND"
            :key="item.key"
            class="flex items-center justify-between gap-2"
          >
            <div class="flex min-w-0 items-center gap-2">
              <span
                class="h-2.5 w-2.5 shrink-0 rounded-full"
                :style="{ backgroundColor: getColor(item.key) }"
              />
              <span class="
                truncate text-gray-600
                dark:text-gray-400
              "
              >{{ item.label }}</span>
            </div>
            <span class="
              shrink-0 text-xs text-gray-400 tabular-nums
              dark:text-gray-500
            "
            >
              {{ nodeCountByType[item.key] ?? 0 }}
            </span>
          </div>
        </div>

        <!-- Relations section -->
        <div class="
          mt-3 border-t border-gray-100 pt-3
          dark:border-gray-800
        "
        >
          <p class="
            mb-1.5 text-xs font-medium tracking-wide text-gray-400 uppercase
            dark:text-gray-500
          "
          >
            Relations
          </p>
          <div class="space-y-1.5">
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <span class="
                  h-2.5 w-2.5 shrink-0 rounded-full bg-gray-400
                  dark:bg-gray-500
                "
                />
                <span class="
                  text-gray-600
                  dark:text-gray-400
                "
                >Source</span>
              </div>
              <span class="
                text-xs text-gray-400 tabular-nums
                dark:text-gray-500
              "
              >{{ sourceCount }}</span>
            </div>
            <div class="flex items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <span class="
                  h-2.5 w-2.5 shrink-0 rounded-full bg-red-300
                  dark:bg-red-700
                "
                />
                <span class="
                  text-gray-600
                  dark:text-gray-400
                "
                >Use</span>
              </div>
              <span class="
                text-xs text-gray-400 tabular-nums
                dark:text-gray-500
              "
              >{{ useCount }}</span>
            </div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
