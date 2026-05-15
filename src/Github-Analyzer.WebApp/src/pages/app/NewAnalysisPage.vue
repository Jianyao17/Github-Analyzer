<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useProjectApi } from '../../composables/useProjectApi';
import { useRepoInfo } from '../../composables/useRepoInfo';

const router = useRouter();
const { createProject } = useProjectApi();
const {
  branches, commits,
  isFetchingBranches,
  fetchError, hasBranches, hasCommits,
  fetchBranches, fetchCommits,
  reset: resetRepoInfo,
} = useRepoInfo();

// ─── State ────────────────────────────────────────────────────────────────
const state = reactive({
  repoUrl: '',
  branch: '' as string,
  commitHash: '' as string,
});
const creating = ref(false);
const submitError = ref<string | null>(null);

// ─── Options ──────────────────────────────────────────────────────────────
const branchOptions = computed(() =>
  branches.value.map(b => ({ label: b.name, value: b.name }))
);
const commitOptions = computed(() =>
  commits.value.map(c => ({
    label: `${c.hash.slice(0, 7)}  ${c.message.slice(0, 55)}${c.message.length > 55 ? '…' : ''}`,
    value: c.hash,
  }))
);

// ─── Watchers ─────────────────────────────────────────────────────────────
let debounceTimer: ReturnType<typeof setTimeout> | null = null;

watch(() => state.repoUrl, (url) => 
{
  if (debounceTimer) clearTimeout(debounceTimer);
  submitError.value = null;
  state.branch = '';
  state.commitHash = '';
  resetRepoInfo();
  if (!url.trim()) return;
  debounceTimer = setTimeout(async () => 
  {
    await fetchBranches(url);
    if (branches.value.length > 0) state.branch = branches.value[0].name;
  }, 700);
});

watch(() => state.branch, (branch) => 
{
  state.commitHash = '';
  if (branch && state.repoUrl) fetchCommits(state.repoUrl, branch);
});

// ─── Submit ───────────────────────────────────────────────────────────────
async function onSubmit() 
{
  creating.value = true;
  submitError.value = null;
  try 
  {
    const project = await createProject({
      repoUrl: state.repoUrl,
      branch: state.branch || 'main',
      commitHash: state.commitHash || undefined,
    });
    router.push({ name: 'app.project-detail', params: { id: project.id } });
  }
  catch (err: any) 
  {
    submitError.value = err?.message ?? 'Gagal membuat project.';
  }
  finally 
  {
    creating.value = false;
  }
}
</script>

<template>
  <div class="
    flex min-h-[calc(100vh-4rem)] w-full flex-col items-center justify-center
    px-4 py-12
  "
  >

    <!-- ── Header ─────────────────────────────────────────────────────── -->
    <div class="mb-8 text-center">
      <div class="
        bg-primary-50
        dark:bg-primary-950
        ring-primary-200
        dark:ring-primary-800
        mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl ring-1
      "
      >
        <NIcon name="i-lucide-git-branch-plus"
          class="text-primary-500 h-6 w-6"
        />
      </div>
      <h1 class="
        text-2xl font-bold text-gray-900
        dark:text-white
      "
      >Mulai Analisa Baru</h1>
      <p class="
        mt-1 text-sm text-gray-500
        dark:text-gray-400
      "
      >
        Masukkan URL repositori, lalu pilih branch dan commit yang ingin dianalisa.
      </p>
    </div>

    <!-- ── Custom Form Card ───────────────────────────────────────────── -->
    <form @submit.prevent="onSubmit"
      class="
        flex w-full max-w-3xl flex-col gap-3 rounded-2xl border border-gray-200
        bg-white p-6 shadow-sm
        sm:p-7
        dark:border-gray-800 dark:bg-gray-900
      "
    >
      
      <!-- URL + Embedded Submit Button -->
      <div class="w-full">
        <label class="sr-only">URL Repositori</label>
        <div class="
          relative flex w-full items-stretch overflow-hidden rounded-xl border
          border-gray-200 bg-gray-50 transition-all
          focus-within:border-transparent focus-within:bg-white
          focus-within:ring-2 focus-within:ring-green-500
          dark:border-gray-700 dark:bg-gray-800
          dark:focus-within:bg-gray-900
        "
        >
          <div class="pointer-events-none flex items-center pl-4">
            <NIcon name="i-lucide-link"
              class="h-5 w-5 text-gray-400"
            />
          </div>
          <input
            v-model="state.repoUrl"
            type="text"
            required
            placeholder="https://github.com/username/repo"
            :disabled="creating"
            class="
              w-full min-w-0 flex-1 bg-transparent py-3.5 pr-2 pl-3 text-sm
              text-gray-900 placeholder-gray-400
              focus:outline-none
              disabled:opacity-50
              sm:text-base
              dark:text-white
            "
          />
          <button
            type="submit"
            :disabled="creating || !state.repoUrl"
            class="
              relative flex items-center justify-center gap-2 bg-green-500 px-5
              text-sm font-semibold text-white transition-colors
              hover:bg-green-600
              focus:bg-green-600 focus:outline-none
              disabled:cursor-not-allowed disabled:bg-gray-300
              disabled:text-gray-500
              sm:px-8 sm:text-base
              dark:disabled:bg-gray-700
            "
          >
            <span class="
              hidden
              sm:inline
            "
              :class="{ 'opacity-0': creating }"
            >Mulai Analisa</span>
            <span class="sm:hidden"
              :class="{ 'opacity-0': creating }"
            >Mulai</span>
            <NIcon name="i-lucide-play"
              class="
                h-4 w-4
                sm:h-5 sm:w-5
              "
              :class="{ 'opacity-0': creating }"
            />
            
            <!-- Loading Spinner (diletakkan terpusat absolut saat sedang proses) -->
            <NIcon 
              v-if="creating" 
              name="i-lucide-loader-2" 
              class="
                absolute inset-0 m-auto h-5 w-5 animate-spin
                sm:h-6 sm:w-6
              " 
            />
          </button>
        </div>
        
        <!-- Contextual Help / Status -->
        <div class="mt-2.5 min-h-5 px-1">
          <transition name="fade"
            mode="out-in"
          >
            <span v-if="fetchError"
              class="
                flex items-center gap-1.5 text-xs text-red-500
                sm:text-sm
              "
            >
              <NIcon name="i-lucide-alert-circle"
                class="h-4 w-4"
              /> {{ fetchError }}
            </span>
            <span v-else-if="isFetchingBranches"
              class="
                flex items-center gap-1.5 text-xs text-green-500
                sm:text-sm
              "
            >
              <NIcon name="i-lucide-loader-2"
                class="h-4 w-4 animate-spin"
              /> Mengambil info repositori…
            </span>
            <span v-else-if="hasBranches"
              class="
                flex items-center gap-1.5 text-xs text-green-600
                sm:text-sm
                dark:text-green-400
              "
            >
              <NIcon name="i-lucide-check-circle-2"
                class="h-4 w-4"
              /> {{ branches.length }} branch tersedia
            </span>
            <span v-else
              class="
                text-xs text-gray-400
                sm:text-sm
              "
            >
              Masukkan URL repositori publik GitHub
            </span>
          </transition>
        </div>
      </div>

      <!-- Branch & Commit Selectors -->
      <div class="
        flex w-full flex-col gap-5
        sm:flex-row
      "
      >
        <!-- Branch: 60% -->
        <div class="
          relative w-full
          sm:w-[60%]
        "
        >
          <div class="
            pointer-events-none absolute inset-y-0 left-0 flex items-center
            pl-3.5
          "
          >
            <NIcon name="i-lucide-git-branch"
              class="h-4 w-4 text-gray-400"
            />
          </div>
          <select
            v-model="state.branch"
            :disabled="creating || !hasBranches"
            class="
              w-full cursor-pointer appearance-none rounded-xl border
              border-gray-200 bg-gray-50 py-2.5 pr-8 pl-10 text-sm text-gray-900
              transition-all
              focus:ring-2 focus:ring-green-500 focus:outline-none
              disabled:cursor-not-allowed disabled:opacity-50
              dark:border-gray-700 dark:bg-gray-800 dark:text-white
            "
          >
            <option value=""
              disabled
              hidden
            >Pilih branch…</option>
            <option v-for="b in branchOptions"
              :key="b.value"
              :value="b.value"
            >{{ b.label }}</option>
          </select>
          <div class="
            pointer-events-none absolute inset-y-0 right-0 flex items-center
            pr-3 text-gray-400
          "
          >
            <NIcon name="i-lucide-chevron-down"
              class="h-4 w-4"
            />
          </div>
        </div>

        <!-- Commit: 40% -->
        <div class="
          relative w-full
          sm:w-[40%]
        "
        >
          <div class="
            pointer-events-none absolute inset-y-0 left-0 flex items-center
            pl-3.5
          "
          >
            <NIcon name="i-lucide-git-commit"
              class="h-4 w-4 text-gray-400"
            />
          </div>
          <select
            v-model="state.commitHash"
            :disabled="creating || !hasCommits"
            class="
              w-full cursor-pointer appearance-none rounded-xl border
              border-gray-200 bg-gray-50 py-2.5 pr-8 pl-10 text-sm text-gray-900
              transition-all
              focus:ring-2 focus:ring-green-500 focus:outline-none
              disabled:cursor-not-allowed disabled:opacity-50
              dark:border-gray-700 dark:bg-gray-800 dark:text-white
            "
          >
            <option value="">Commit terbaru</option>
            <option v-for="c in commitOptions"
              :key="c.value"
              :value="c.value"
            >{{ c.label }}</option>
          </select>
          <div class="
            pointer-events-none absolute inset-y-0 right-0 flex items-center
            pr-3 text-gray-400
          "
          >
            <NIcon name="i-lucide-chevron-down"
              class="h-4 w-4"
            />
          </div>
        </div>
      </div>

      <!-- Error Alert -->
      <div v-if="submitError"
        class="
          flex items-start gap-2.5 rounded-xl border border-red-200 bg-red-50
          p-3 text-sm text-red-600
          dark:border-red-900 dark:bg-red-950/50 dark:text-red-400
        "
      >
        <NIcon name="i-lucide-alert-circle"
          class="mt-0.5 h-4 w-4 shrink-0"
        />
        <div class="flex-1 font-medium">{{ submitError }}</div>
        <button type="button"
          @click="submitError = null"
          class="
            text-red-500 transition-colors
            hover:text-red-700
          "
        >
          <NIcon name="i-lucide-x"
            class="h-4 w-4"
          />
        </button>
      </div>
    </form>

    <p class="mt-6 text-center text-xs text-gray-400">
      Analyzer dapat membuat kesalahan. Periksa informasi penting sebelum digunakan.
    </p>

  </div>
</template>
