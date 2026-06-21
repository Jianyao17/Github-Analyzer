<script setup lang="ts">
import { ref, watch, nextTick, onMounted, onUnmounted } from 'vue';
import { NODE_TYPE_KEYS, defaultGraphConfig } from '@/lib/graph/config';
import type { D3Node } from '@graph.types';

// ─── Constants ────────────────────────────────────────────────────────────────
const MAX_RESULTS = 50;

// ─── Props ────────────────────────────────────────────────────────────────────
const props = defineProps<{
  /** Runs the search and returns matching nodes. */
  search:       (query: string) => D3Node[];

  /** Smooth zoom to a single node. */
  focusNode:    (node: D3Node, scale?: number) => void;

  /** Show tooltip when hovering or selecting a node. */
  focusHover:   (node: D3Node | null) => void;

  /** Fit the viewport around all results. */
  focusResults: (results: D3Node[], padding?: number) => void;

  /** Clears the graph's highlight state. */
  clearSearch:  () => void;

  /** Total node count shown in the empty-query hint. */
  totalNodes?:  number;
}>();

// ─── State ────────────────────────────────────────────────────────────────────
const isOpen        = ref(false);
const searchQuery   = ref('');
const searchResults = ref<D3Node[]>([]);
const resultsRef    = ref<HTMLElement | null>(null);
const inputRef      = ref<{ inputRef?: HTMLInputElement } | null>(null);
const activeIndex   = ref(-1);

// ─── Open / Close ─────────────────────────────────────────────────────────────
async function open(): Promise<void>
{
  isOpen.value = true;
  await nextTick();
  inputRef.value?.inputRef?.focus();
}

function close(): void
{
  isOpen.value        = false;
  searchQuery.value   = '';
  // (watch on searchQuery will automatically clear results and graph state)
}

// ─── Search Logic ─────────────────────────────────────────────────────────────
watch(searchQuery, (newVal) =>
{
  activeIndex.value = -1;
  const q = newVal.trim();
  
  if (!q)
  {
    searchResults.value = [];
    props.clearSearch();
  }
  else
  {
    searchResults.value = props.search(q);
  }
});

watch(activeIndex, (idx) => 
{
  if (idx >= 0 && searchResults.value[idx]) 
  {
    props.focusHover(searchResults.value[idx]);
  } 
  else 
  {
    props.focusHover(null);
  }
});

function selectNode(node: D3Node): void
{
  props.focusNode(node, 2.5);
  close();
}

function fitAll(): void
{
  props.focusResults(searchResults.value);
  close();
}

// ─── Keyboard navigation ──────────────────────────────────────────────────────
/** Scroll the highlighted result item into view after index changes. */
function scrollActiveIntoView(): void
{
  nextTick(() =>
  {
    const container = resultsRef.value;
    if (!container) return;
    const item = container.children[activeIndex.value] as HTMLElement | undefined;
    item?.scrollIntoView({ block: 'nearest' });
  });
}

function handleKeydown(e: KeyboardEvent): void
{
  const len = Math.min(searchResults.value.length, MAX_RESULTS);

  if (e.key === 'Escape') { close(); return; }

  if (e.key === 'ArrowDown')
  {
    e.preventDefault();
    activeIndex.value = (activeIndex.value + 1) % len;
    scrollActiveIntoView();
    return;
  }

  if (e.key === 'ArrowUp')
  {
    e.preventDefault();
    activeIndex.value = (activeIndex.value - 1 + len) % len;
    scrollActiveIntoView();
    return;
  }

  if (e.key === 'Enter' && activeIndex.value >= 0)
  {
    e.preventDefault();
    const node = searchResults.value[activeIndex.value];
    if (node) selectNode(node);
  }
}

// ─── Global shortcut: Ctrl+K ──────────────────────────────────────────────────
function onGlobalKeydown(e: KeyboardEvent): void
{
  if ((e.ctrlKey || e.metaKey) && e.key === 'k')
  {
    e.preventDefault();

    if (isOpen.value) close(); 
    else open();
  }
}

onMounted(() => window.addEventListener('keydown', onGlobalKeydown));
onUnmounted(() => window.removeEventListener('keydown', onGlobalKeydown));

// ─── Node type helpers ────────────────────────────────────────────────────────
function getNodeColor(type: number): string
{
  const key = NODE_TYPE_KEYS[type] ?? 'default';
  return defaultGraphConfig.nodeTypes[key]?.color ?? '#9CA3AF';
}

function getNodeIcon(type: number): string
{
  const key = NODE_TYPE_KEYS[type] ?? 'default';
  return defaultGraphConfig.nodeTypes[key]?.icon ?? 'circle';
}

function getNodeTypeLabel(type: number): string
{
  const key = NODE_TYPE_KEYS[type] ?? 'default';
  return key.charAt(0).toUpperCase() + key.slice(1);
}

// ─── Expose for parent ────────────────────────────────────────────────────────
defineExpose({ open, close });
</script>

<template>
  <!--
    GraphSearchModal — contained within parent's `position: relative` element.
    No Teleport: uses `absolute inset-0 z-30` so it stays within CodeGraphView.
    The 40%-opacity backdrop lets the graph + amber node highlights show through.
  -->
  <Transition
    enter-active-class="transition duration-200 ease-out"
    enter-from-class="opacity-0"
    enter-to-class="opacity-100"
    leave-active-class="transition duration-150 ease-in"
    leave-from-class="opacity-100"
    leave-to-class="opacity-0"
  >
    <div
      v-if="isOpen"
      class="absolute inset-0 z-30"
      role="dialog"
      aria-modal="true"
      aria-label="Search graph nodes"
      @click="close"
    >
      <!-- ── Panel: click.stop prevents closing when clicking inside ────────── -->
      <div
        class="
          absolute inset-x-0 top-0 flex justify-center px-3 pt-12
          sm:pt-16
        "
        @click.stop
      >
        <div class="w-full max-w-lg">
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="-translate-y-3 opacity-0"
            enter-to-class="translate-y-0 opacity-100"
            appear
          >
            <div
              class="
                overflow-hidden rounded-xl border border-[var(--ui-border)]
                bg-[var(--ui-bg)] shadow-2xl shadow-gray-400/20
                dark:shadow-black/40
              "
            >
              <!-- ── Input row ─────────────────────────────────────────────── -->
              <div class="flex items-center gap-2 px-2 py-1"
                id="graph-search-input-container"
              >
                <NInput
                  ref="inputRef"
                  v-model="searchQuery"
                  icon="i-lucide-search"
                  size="lg"
                  variant="none"
                  placeholder="Search nodes by name or path..."
                  autocomplete="off"
                  :ui="{
                    root: 'flex-1',
                    base: 'text-gray-800 dark:text-gray-100 placeholder:text-gray-400',
                    leadingIcon: 'text-gray-400',
                  }"
                  @keydown="handleKeydown"
                />

                <!-- Result count badge -->
                <NBadge
                  v-if="searchResults.length && searchQuery.trim()"
                  :label="`${searchResults.length}`"
                  size="sm"
                  color="primary"
                  variant="subtle"
                  class="shrink-0"
                />

                <!-- Close button -->
                <NButton
                  icon="i-lucide-x"
                  size="xs"
                  variant="ghost"
                  color="neutral"
                  class="shrink-0"
                  @click="close"
                />
              </div>

              <!-- ── Results list ──────────────────────────────────────────── -->
              <template v-if="searchQuery.trim()">
                <div
                  v-if="searchResults.length > 0"
                  ref="resultsRef"
                  class="
                    max-h-[45vh] overflow-y-auto border-t
                    border-[var(--ui-border)]
                    sm:max-h-64
                  "
                >
                  <button
                    v-for="(node, i) in searchResults.slice(0, MAX_RESULTS)"
                    :id="i === 0 ? 'graph-search-first-result' : undefined"
                    :key="node.id"
                    :class="[
                      `
                        flex w-full items-center gap-3 px-4 py-2.5 text-left
                        transition-colors
                      `,
                      i === activeIndex
                        ? `bg-[var(--ui-bg-elevated)]`
                        : `hover:bg-[var(--ui-bg-muted)]`,
                    ]"
                    @click="selectNode(node)"
                    @mouseenter="activeIndex = i"
                    @mouseleave="activeIndex = -1"
                  >
                    <!-- LEFT PART: ICON -->
                    <NIcon
                      :name="'i-lucide-' + getNodeIcon(node.type)"
                      class="h-5 w-5 shrink-0"
                      :style="{ color: getNodeColor(node.type) }"
                    />

                    <!-- RIGHT PART -->
                    <div class="flex min-w-0 flex-1 flex-col gap-0.5">
                      <!-- TOP ROW -->
                      <div class="flex items-center gap-2">
                        <span class="
                          truncate text-sm font-medium text-[var(--ui-text)]
                        "
                        >
                          {{ node.label }}
                        </span>
                        <span class="
                          shrink-0 rounded-full px-2 py-0.5 text-[10px]
                          leading-none font-semibold
                        "
                          :style="{
                            backgroundColor: getNodeColor(node.type) + '26',
                            color: getNodeColor(node.type)
                          }"
                        >
                          {{ getNodeTypeLabel(node.type) }}
                        </span>
                      </div>

                      <!-- BOTTOM ROW -->
                      <span class="truncate text-xs text-[var(--ui-text-muted)]">
                        {{ node.pathId }}
                      </span>
                    </div>
                  </button>
                </div>

                <!-- No results -->
                <div
                  v-else
                  class="
                    border-t border-[var(--ui-border)] py-8 text-center text-sm
                    text-[var(--ui-text-muted)]
                  "
                >
                  No nodes found for
                  <span class="font-medium text-[var(--ui-text-highlighted)]">"{{ searchQuery }}"</span>
                </div>
              </template>

              <!-- ── Empty query hint ─────────────────────────────────────── -->
              <div
                v-else
                class="
                  border-t border-[var(--ui-border)] py-8 text-center text-sm
                  text-[var(--ui-text-muted)]
                "
              >
                <template v-if="totalNodes">
                  Search through {{ totalNodes }} nodes
                </template>
                <template v-else>
                  Type to search nodes
                </template>
              </div>

              <!-- ── Footer ──────────────────────────────────────────────── -->
              <div
                v-if="searchResults.length > 0"
                class="
                  flex items-center justify-between border-t
                  border-[var(--ui-border)] px-4 py-2
                "
              >
                <span class="text-xs text-[var(--ui-text-muted)]">
                  {{ searchResults.length }} result{{ searchResults.length !== 1 ? 's' : '' }}
                  <span v-if="searchResults.length > MAX_RESULTS"
                    class="opacity-60"
                  >&nbsp;(showing {{ MAX_RESULTS }})</span>
                </span>

                <div class="flex items-center gap-3">
                  <span class="
                    hidden text-xs text-[var(--ui-text-dimmed)]
                    sm:block
                  "
                  >
                    ↑↓ navigate · Enter select · Esc close
                  </span>
                  <NButton
                    v-if="searchResults.length > 1"
                    label="Fit All"
                    size="xs"
                    variant="ghost"
                    color="primary"
                    @click="fitAll"
                  />
                </div>
              </div>
            </div>
          </Transition>
        </div>
      </div>
    </div>
  </Transition>
</template>
