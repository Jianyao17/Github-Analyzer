<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useProjectApi } from '../../composables/useProjectApi'
import { useRouter } from 'vue-router'

const { createProject } = useProjectApi()
const router = useRouter()

const state = reactive({
  repoUrl: '',
  branch: 'main'
})
const creating = ref(false)



async function onSubmit() 
{
  creating.value = true
  try 
  {
    const project = await createProject(state)
    state.repoUrl = ''
    state.branch = 'main'
    // Navigate to the newly created project detail page
    router.push({ name: 'app.project-detail', params: { id: project.id } })
  } 
  catch (error) 
  {
    console.error('Failed to create project', error)
  } 
  finally 
  {
    creating.value = false
  }
}


</script>

<template>
  <div class="h-full flex flex-col items-center justify-center w-full min-h-[calc(100vh-4rem)]">
    
    <UCard class="w-full max-w-3xl rounded-3xl shadow-sm border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900" :ui="{ body: 'p-8 md:p-12 text-center' }">
      <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-2">
        Mulai Analisa Baru
      </h1>
      <p class="text-gray-500 dark:text-gray-400 mb-8">
        Tempelkan tautan repositori GitHub Anda di bawah ini.
      </p>

      <UForm :state="state" @submit="onSubmit" class="relative max-w-2xl mx-auto flex items-center">
        <UInput
          v-model="state.repoUrl"
          type="url"
          required
          placeholder="https://github.com/username/repository"
          size="xl"
          class="w-full shadow-sm text-lg"
          :ui="{ wrapper: 'w-full', base: 'py-4 pr-16 rounded-xl border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 focus:ring-primary-500' }"
          :disabled="creating"
        />
        <UButton
          type="submit"
          :disabled="creating || !state.repoUrl"
          :loading="creating"
          icon="i-lucide-send"
          color="gray"
          variant="solid"
          class="absolute right-2 top-1/2 -translate-y-1/2 p-2.5 bg-gray-800 hover:bg-gray-700 dark:bg-gray-700 dark:hover:bg-gray-600 text-white rounded-lg"
        />
      </UForm>
      
      <!-- Hidden branch input defaulting to main -->
      <input type="hidden" v-model="state.branch" value="main" />
    </UCard>

    <!-- Error alert if any -->
    <div v-if="false" class="mt-4 text-red-500 text-sm"></div>

    <div class="absolute bottom-6 text-xs text-gray-400 text-center">
      Analyzer ini dapat membuat kesalahan. Periksa informasi penting.
    </div>
  </div>
</template>
