<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { EditorView } from '@codemirror/view';
import { 
  SearchQuery, setSearchQuery, getSearchQuery,
  findNext, findPrevious 
} from '@codemirror/search';

const props = defineProps<{
  view: EditorView | null;
  modelValue: boolean; // is search open
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', val: boolean): void;
}>();

const query = ref('');
const matchCase = ref(false);
const useRegex = ref(false);
const wholeWord = ref(false);
const inputRef = ref<HTMLInputElement | null>(null);

const matchCount = ref(0);
const currentMatchIndex = ref(0);

const updateMatchCount = () => 
{
  if (!props.view || !query.value) 
  {
    matchCount.value = 0;
    currentMatchIndex.value = 0;
    return;
  }
  const state = props.view.state;
  const sq = getSearchQuery(state);
  if (!sq || !sq.search) 
  {
    matchCount.value = 0;
    currentMatchIndex.value = 0;
    return;
  }
  
  const cursor = sq.getCursor(state);
  let count = 0;
  let currentIndex = 0;
  const mainSelection = state.selection.main;
  
  let current = cursor.next();
  while (!current.done) 
  {
    count++;
    const match = current.value;
    if (match.from <= mainSelection.to && match.to >= mainSelection.from) 
    {
      currentIndex = count;
    }
    current = cursor.next();
  }
  
  matchCount.value = count;
  currentMatchIndex.value = currentIndex;
};

const closeSearch = () => 
{
  emit('update:modelValue', false);
  if (props.view) 
  {
    props.view.focus(); // return focus to editor
  }
};

const executeSearch = () => 
{
  if (!props.view || !query.value) 
  {
    updateMatchCount();
    return;
  }
  const sq = new SearchQuery({
    search: query.value,
    caseSensitive: matchCase.value,
    regexp: useRegex.value,
    wholeWord: wholeWord.value
  });
  props.view.dispatch({
    effects: setSearchQuery.of(sq)
  });
  updateMatchCount();

  // Auto-jump to the first valid match if we are not currently focused on one
  if (currentMatchIndex.value === 0 && matchCount.value > 0) 
  {
    findNext(props.view);
    updateMatchCount();
  }
};

const onNext = () => 
{
  if (!props.view) return;
  findNext(props.view);
  updateMatchCount();
};

const onPrev = () => 
{
  if (!props.view) return;
  findPrevious(props.view);
  updateMatchCount();
};

// Sync query back from editor state
watch(() => props.view, (v) => 
{
  if (v) 
  {
    const q = getSearchQuery(v.state);
    if (q && q.search) 
    {
      query.value = q.search;
      matchCase.value = q.caseSensitive;
      useRegex.value = q.regexp;
      wholeWord.value = q.wholeWord;
    }
  }
}, { immediate: true });

let searchTimeout: ReturnType<typeof setTimeout>;

// Auto search on typing/toggling
watch([query, matchCase, useRegex, wholeWord], () => 
{
  clearTimeout(searchTimeout);
  searchTimeout = setTimeout(() => 
  {
    executeSearch();
  }, 250); // 250ms debounce
});

// Auto focus on open
watch(() => props.modelValue, (isOpen) => 
{
  if (isOpen) 
  {
    nextTick(() => 
    {
      inputRef.value?.focus();
      inputRef.value?.select();
    });
  }
});

// Global Escape to close if focused inside
const handleKeydown = (e: KeyboardEvent) => 
{
  if (e.key === 'Escape' && props.modelValue) 
  {
    closeSearch();
  }
};

onMounted(() => 
{
  document.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => 
{
  document.removeEventListener('keydown', handleKeydown);
});
</script>

<template>
  <div v-if="modelValue" 
    class="
      absolute top-2 right-6 z-30 flex items-center gap-2 rounded border
      border-[var(--ui-border)] bg-[var(--ui-bg)] p-1 shadow-sm
    "
  >
    <div class="relative flex items-center">
      <input
        ref="inputRef"
        v-model="query"
        type="text"
        placeholder="Find"
        class="
          w-72 rounded border border-transparent bg-[var(--ui-bg-elevated)] px-2
          py-1 pr-[130px] text-[13px] text-[var(--ui-text)] transition-colors
          outline-none
          focus:border-primary-500
        "
        @keydown.enter.exact.prevent="onNext"
        @keydown.shift.enter.prevent="onPrev"
      />
      <!-- Match count indicator -->
      <div v-if="query"
        class="
          pointer-events-none absolute right-[78px] flex items-center pr-1
          text-[11px] text-[var(--ui-text-muted)]
        "
      >
        <span v-if="matchCount > 0">{{ currentMatchIndex }} of {{ matchCount }}</span>
        <span v-else>No results</span>
      </div>
      <!-- Toggles inside input -->
      <div class="absolute right-1 flex items-center gap-0.5">
        <button
          class="
            flex h-6 w-6 items-center justify-center rounded-sm
            text-[var(--ui-text-muted)] transition-colors
            hover:bg-[var(--ui-bg-muted)] hover:text-[var(--ui-text)]
          "
          :class="{ 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-200': matchCase }"
          title="Match Case"
          @click="matchCase = !matchCase"
        >
          <span class="text-[11px] font-bold">Aa</span>
        </button>
        <button
          class="
            flex h-6 w-6 items-center justify-center rounded-sm
            text-[var(--ui-text-muted)] transition-colors
            hover:bg-[var(--ui-bg-muted)] hover:text-[var(--ui-text)]
          "
          :class="{ 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-200': wholeWord }"
          title="Match Whole Word"
          @click="wholeWord = !wholeWord"
        >
          <span class="text-[11px] font-bold underline underline-offset-2">ab</span>
        </button>
        <button
          class="
            flex h-6 w-6 items-center justify-center rounded-sm
            text-[var(--ui-text-muted)] transition-colors
            hover:bg-[var(--ui-bg-muted)] hover:text-[var(--ui-text)]
          "
          :class="{ 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-200': useRegex }"
          title="Use Regular Expression"
          @click="useRegex = !useRegex"
        >
          <span class="text-[11px] font-bold">.*</span>
        </button>
      </div>
    </div>

    <!-- Actions -->
    <div class="flex items-center gap-1 pl-1">
      <button
        class="
          flex h-7 w-7 items-center justify-center rounded-sm
          text-[var(--ui-text-muted)] transition-colors
          hover:bg-[var(--ui-bg-elevated)] hover:text-[var(--ui-text)]
        "
        title="Previous Match (Shift+Enter)"
        @click="onPrev"
      >
        <NIcon name="i-lucide-arrow-up"
          class="h-4 w-4"
        />
      </button>
      <button
        class="
          flex h-7 w-7 items-center justify-center rounded-sm
          text-[var(--ui-text-muted)] transition-colors
          hover:bg-[var(--ui-bg-elevated)] hover:text-[var(--ui-text)]
        "
        title="Next Match (Enter)"
        @click="onNext"
      >
        <NIcon name="i-lucide-arrow-down"
          class="h-4 w-4"
        />
      </button>
      <button
        class="
          ml-1 flex h-7 w-7 items-center justify-center rounded-sm
          text-[var(--ui-text-muted)] transition-colors
          hover:bg-[var(--ui-bg-elevated)] hover:text-[var(--ui-text)]
        "
        title="Close (Escape)"
        @click="closeSearch"
      >
        <NIcon name="i-lucide-x"
          class="h-4 w-4"
        />
      </button>
    </div>
  </div>
</template>
