<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useProjectApi } from '../../composables/useProjectApi'
import { useRepoInfo } from '../../composables/useRepoInfo'

const router = useRouter()
const { createProject } = useProjectApi()
const {
  branches, commits,
  isFetchingBranches, isFetchingCommits,
  fetchError, hasBranches, hasCommits,
  fetchBranches, fetchCommits,
  reset: resetRepoInfo,
} = useRepoInfo()

// ─── State ────────────────────────────────────────────────────────────────
const state = reactive({
  repoUrl: '',
  branch: '' as string,
  commitHash: '' as string,
})
const creating = ref(false)
const submitError = ref<string | null>(null)

// ─── Options ──────────────────────────────────────────────────────────────
const branchOptions = computed(() =>
  branches.value.map(b => ({ label: b.name, value: b.name }))
)
const commitOptions = computed(() =>
  commits.value.map(c => ({
    label: `${c.hash.slice(0, 7)}  ${c.message.slice(0, 55)}${c.message.length > 55 ? '…' : ''}`,
    value: c.hash,
  }))
)

// ─── Watchers ─────────────────────────────────────────────────────────────
let debounceTimer: ReturnType<typeof setTimeout> | null = null

watch(() => state.repoUrl, (url) => {
  if (debounceTimer) clearTimeout(debounceTimer)
  submitError.value = null
  state.branch = ''
  state.commitHash = ''
  resetRepoInfo()
  if (!url.trim()) return
  debounceTimer = setTimeout(async () => {
    await fetchBranches(url)
    if (branches.value.length > 0) state.branch = branches.value[0].name
  }, 700)
})

watch(() => state.branch, (branch) => {
  state.commitHash = ''
  if (branch && state.repoUrl) fetchCommits(state.repoUrl, branch)
})

// ─── Submit ───────────────────────────────────────────────────────────────
async function onSubmit() {
  creating.value = true
  submitError.value = null
  try {
    const project = await createProject({
      repoUrl: state.repoUrl,
      branch: state.branch || 'main',
      commitHash: state.commitHash || undefined,
    })
    router.push({ name: 'app.project-detail', params: { id: project.id } })
  } catch (err: any) {
    submitError.value = err?.message ?? 'Gagal membuat project.'
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <div class="flex flex-col items-center justify-center w-full min-h-[calc(100vh-4rem)] px-4 py-12">

    <!-- ── Header ─────────────────────────────────────────────────────── -->
    <div class="mb-8 text-center">
      <div class="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-primary-50 dark:bg-primary-950 ring-1 ring-primary-200 dark:ring-primary-800 mb-4">
        <UIcon name="i-lucide-git-branch-plus" class="w-6 h-6 text-primary-500" />
      </div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Mulai Analisa Baru</h1>
      <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
        Masukkan URL repositori, lalu pilih branch dan commit yang ingin dianalisa.
      </p>
    </div>

    <!-- ── Custom Form Card ───────────────────────────────────────────── -->
    <form @submit.prevent="onSubmit" class="w-full max-w-3xl bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-6 sm:p-7 flex flex-col gap-3">
      
      <!-- URL + Embedded Submit Button -->
      <div class="w-full">
        <label class="sr-only">URL Repositori</label>
        <div class="relative flex items-stretch w-full bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden focus-within:ring-2 focus-within:ring-green-500 focus-within:border-transparent focus-within:bg-white dark:focus-within:bg-gray-900 transition-all">
          <div class="flex items-center pl-4 pointer-events-none">
            <UIcon name="i-lucide-link" class="w-5 h-5 text-gray-400" />
          </div>
          <input
            v-model="state.repoUrl"
            type="text"
            required
            placeholder="https://github.com/username/repo"
            :disabled="creating"
            class="flex-1 w-full pl-3 pr-2 py-3.5 bg-transparent text-sm sm:text-base text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none disabled:opacity-50 min-w-0"
          />
          <button
            type="submit"
            :disabled="creating || !state.repoUrl"
            class="bg-green-500 hover:bg-green-600 focus:bg-green-600 focus:outline-none disabled:bg-gray-300 dark:disabled:bg-gray-700 text-white px-5 sm:px-8 flex items-center justify-center gap-2 font-semibold text-sm sm:text-base transition-colors disabled:cursor-not-allowed disabled:text-gray-500 relative"
          >
            <span class="hidden sm:inline" :class="{ 'opacity-0': creating }">Mulai Analisa</span>
            <span class="sm:hidden" :class="{ 'opacity-0': creating }">Mulai</span>
            <UIcon name="i-lucide-play" class="w-4 h-4 sm:w-5 sm:h-5" :class="{ 'opacity-0': creating }" />
            
            <!-- Loading Spinner (diletakkan terpusat absolut saat sedang proses) -->
            <UIcon 
              v-if="creating" 
              name="i-lucide-loader-2" 
              class="w-5 h-5 sm:w-6 sm:h-6 absolute inset-0 m-auto animate-spin" 
            />
          </button>
        </div>
        
        <!-- Contextual Help / Status -->
        <div class="mt-2.5 min-h-[1.25rem] px-1">
          <transition name="fade" mode="out-in">
            <span v-if="fetchError" class="text-red-500 flex items-center gap-1.5 text-xs sm:text-sm">
              <UIcon name="i-lucide-alert-circle" class="w-4 h-4" /> {{ fetchError }}
            </span>
            <span v-else-if="isFetchingBranches" class="text-green-500 flex items-center gap-1.5 text-xs sm:text-sm">
              <UIcon name="i-lucide-loader-2" class="w-4 h-4 animate-spin" /> Mengambil info repositori…
            </span>
            <span v-else-if="hasBranches" class="text-green-600 dark:text-green-400 flex items-center gap-1.5 text-xs sm:text-sm">
              <UIcon name="i-lucide-check-circle-2" class="w-4 h-4" /> {{ branches.length }} branch tersedia
            </span>
            <span v-else class="text-gray-400 text-xs sm:text-sm">
              Masukkan URL repositori publik GitHub
            </span>
          </transition>
        </div>
      </div>

      <!-- Branch & Commit Selectors -->
      <div class="flex flex-col sm:flex-row gap-5 w-full">
        <!-- Branch: 60% -->
        <div class="relative w-full sm:w-[60%]">
          <div class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none">
            <UIcon name="i-lucide-git-branch" class="w-4 h-4 text-gray-400" />
          </div>
          <select
            v-model="state.branch"
            :disabled="creating || !hasBranches"
            class="w-full pl-10 pr-8 py-2.5 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500 appearance-none transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
          >
            <option value="" disabled hidden>Pilih branch…</option>
            <option v-for="b in branchOptions" :key="b.value" :value="b.value">{{ b.label }}</option>
          </select>
          <div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none text-gray-400">
            <UIcon name="i-lucide-chevron-down" class="w-4 h-4" />
          </div>
        </div>

        <!-- Commit: 40% -->
        <div class="relative w-full sm:w-[40%]">
          <div class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none">
            <UIcon name="i-lucide-git-commit" class="w-4 h-4 text-gray-400" />
          </div>
          <select
            v-model="state.commitHash"
            :disabled="creating || !hasCommits"
            class="w-full pl-10 pr-8 py-2.5 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500 appearance-none transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
          >
            <option value="">Commit terbaru</option>
            <option v-for="c in commitOptions" :key="c.value" :value="c.value">{{ c.label }}</option>
          </select>
          <div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none text-gray-400">
            <UIcon name="i-lucide-chevron-down" class="w-4 h-4" />
          </div>
        </div>
      </div>

      <!-- Error Alert -->
      <div v-if="submitError" class="p-3 text-sm rounded-xl bg-red-50 dark:bg-red-950/50 border border-red-200 dark:border-red-900 text-red-600 dark:text-red-400 flex items-start gap-2.5">
        <UIcon name="i-lucide-alert-circle" class="w-4 h-4 shrink-0 mt-0.5" />
        <div class="flex-1 font-medium">{{ submitError }}</div>
        <button type="button" @click="submitError = null" class="text-red-500 hover:text-red-700 transition-colors">
          <UIcon name="i-lucide-x" class="w-4 h-4" />
        </button>
      </div>
    </form>

    <p class="mt-6 text-xs text-gray-400 text-center">
      Analyzer dapat membuat kesalahan. Periksa informasi penting sebelum digunakan.
    </p>

  </div>
</template>
