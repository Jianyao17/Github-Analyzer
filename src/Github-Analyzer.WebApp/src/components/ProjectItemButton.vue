<script setup lang="ts">
import { ref, nextTick, computed } from 'vue';
import type { ProjectResponse } from '../types/_api/project';

type ProjectItemButtonProps = {
  project: ProjectResponse
  isActive: boolean
  isCollapsed: boolean
  isMobile: boolean
};

const props = defineProps<ProjectItemButtonProps>();

const emit = defineEmits<{
  (e: 'rename', id: string, newTitle: string): void
  (e: 'delete', id: string): void
}>();

const isEditing = ref(false);
const editTitle = ref('');
const inputRef = ref<any>(null); // NInput ref

function startEdit() 
{
  editTitle.value = props.project.title;
  isEditing.value = true;
  nextTick(() => 
  {
    // Focus inner input if possible
    const el = inputRef.value?.$el?.querySelector('input') || inputRef.value?.$el;
    if (el && typeof el.focus === 'function') el.focus();
  });
}

function saveEdit() 
{
  const newTitle = editTitle.value.trim();
  if (newTitle && newTitle !== props.project.title) 
  {
    emit('rename', props.project.id, newTitle);
  }
  isEditing.value = false;
}

function cancelEdit() 
{
  isEditing.value = false;
}

const dropdownOptions = computed(() => [
  [{
    label: 'Rename',
    icon: 'i-lucide-pencil',
    onSelect: startEdit
  }],
  [{
    label: 'Delete',
    icon: 'i-lucide-trash-2',
    color: 'error',
    onSelect: () => emit('delete', props.project.id)
  }]
]);

const formattedDate = computed(() => 
{
  if (!props.project.createdAtUtc) return '';
  try 
  {
    const date = new Date(props.project.createdAtUtc);
    return new Intl.DateTimeFormat('id-ID', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    }).format(date);
  }
  catch 
  {
    return '';
  }
});
</script>

<template>
  <div class="group relative flex w-full min-w-0 items-center">
    <NButton
      class="w-full min-w-0 py-2 transition-colors"
      :to="isEditing ? undefined : { name: 'app.project-detail', params: { id: project.id } }"
      :color="isActive ? 'primary' : 'gray'"
      :variant="isActive ? 'soft' : 'ghost'"
      :class="[
        (isCollapsed && !isMobile) 
          ? 'justify-center px-2' 
          : 'justify-start ' + (!isEditing ? 'pr-8' : 'pr-2'),
          
        isActive ? `
          bg-primary-50 font-bold text-primary-600
          dark:bg-primary-950 dark:text-primary-400
        ` : `
          transition-colors duration-150
          hover:!bg-gray-100
          dark:hover:!bg-gray-800/60
        `
      ]"
    >
      <div class="flex w-full min-w-0 items-center overflow-hidden"
        :class="(isCollapsed && !isMobile) ? 'justify-center' : 'gap-2'"
      >
        <NIcon name="i-lucide-github"
          class="h-5 w-5 shrink-0 justify-center"
        />
        <div v-if="!isCollapsed || isMobile"
          class="
            flex min-w-0 flex-1 flex-col items-start overflow-hidden text-left
          "
        >
          <!-- Edit Mode -->
          <div v-if="isEditing"
            class="flex w-full items-center gap-1"
            @click.stop.prevent
          >
            <NInput
              ref="inputRef"
              v-model="editTitle"
              size="sm"
              class="flex-1"
              placeholder="Project Name"
              @keyup.enter="saveEdit"
              @keyup.esc="cancelEdit"
            />
            <NButton icon="i-lucide-check"
              color="green"
              variant="ghost"
              size="xs"
              class="px-1"
              @click.stop.prevent="saveEdit"
            />
            <NButton icon="i-lucide-x"
              color="red"
              variant="ghost"
              size="xs"
              class="px-1"
              @click.stop.prevent="cancelEdit"
            />
          </div>
          
          <!-- Normal Mode -->
          <template v-else>
            <span class="block w-full truncate"
              :class="isActive ? `font-bold` : `font-medium`"
            >
              {{ project.title }}
            </span>
            <span class="
              mt-0.5 block w-full truncate text-xs font-normal transition-colors
              duration-150
            "
              :class="isActive ? `
                text-primary-500/80
                group-hover:text-primary-600
                dark:text-primary-400/80
                dark:group-hover:text-primary-300
              ` : `
                text-gray-400
                group-hover:text-gray-600
                dark:text-gray-500
                dark:group-hover:text-gray-300
              `"
            >{{ formattedDate }}</span>
          </template>
        </div>
      </div>
    </NButton>

    <!-- 3-dot menu -->
    <div v-if="!isEditing && (!isCollapsed || isMobile)" 
      class="
        absolute top-1/2 right-1 -translate-y-1/2 opacity-0 transition-opacity
        group-hover:opacity-100
      "
      :class="{ 'opacity-100': isActive }"
    >
      <NDropdownMenu 
        :items="dropdownOptions" 
        :content="{ align: 'end', side: 'bottom' }"
        :ui="{ content: 'z-[100] w-42', item: 'p-2 text-sm gap-1.5' }"
      >
        <NButton icon="i-lucide-more-vertical"
          color="gray"
          variant="ghost"
          size="sm"
          class="
            px-1.5 transition-colors duration-150
            hover:bg-gray-200
            dark:hover:bg-gray-800
          "
          @click.stop.prevent
        />
      </NDropdownMenu>
    </div>
  </div>
</template>
