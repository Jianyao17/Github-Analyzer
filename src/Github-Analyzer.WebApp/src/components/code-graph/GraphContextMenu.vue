<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import type { D3Node } from '@graph.types';

const props = defineProps<{
  show: boolean;
  x: number;
  y: number;
  node: D3Node | null;
}>();

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void;
  (e: 'show-source-code', node: D3Node): void;
}>();

const menuRef = ref<HTMLElement | null>(null);

function close() 
{
  emit('update:show', false);
}

function handleShowSource() 
{
  if (props.node) 
  {
    emit('show-source-code', props.node);
  }
  close();
}

function onClickOutside(e: MouseEvent) 
{
  if (props.show && menuRef.value && !menuRef.value.contains(e.target as Node)) 
  {
    close();
  }
}

onMounted(() => 
{
  document.addEventListener('click', onClickOutside);
  // Also hide if another context menu is triggered elsewhere
  document.addEventListener('contextmenu', onClickOutside);
});

onUnmounted(() => 
{
  document.removeEventListener('click', onClickOutside);
  document.removeEventListener('contextmenu', onClickOutside);
});
</script>

<template>
  <div
    v-if="show"
    ref="menuRef"
    class="
      absolute z-50 min-w-[160px] rounded-md bg-[var(--ui-bg)] p-1 shadow-lg
      ring-1 ring-[var(--ui-border)]
      focus:outline-none
    "
    :style="{ left: x + 5 + 'px', top: y + 5 + 'px' }"
  >
    <button
      class="
        flex w-full items-center gap-2 rounded px-3 py-2 text-left text-sm
        text-[var(--ui-text)] transition-colors
        hover:bg-[var(--ui-bg-elevated)]
      "
      @click.stop="handleShowSource"
    >
      <NIcon name="i-lucide-code"
        class="h-4 w-4 text-[var(--ui-text-muted)]"
      />
      Show Source Code
    </button>
  </div>
</template>
