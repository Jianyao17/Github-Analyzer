<script setup lang="ts">
import { useRouter } from 'vue-router';
import { ref, computed, watch, onMounted } from 'vue';
import { useOnboardingStore } from '../../stores/onboarding.store';
import { useProjectApi } from '../../composables/useProjectApi';
import { useRepoInfo } from '../../composables/useRepoInfo';

const store = useOnboardingStore();
const router = useRouter();
const { createProject } = useProjectApi();
const {
  branches,
  commits,
  isFetchingBranches,
  fetchError,
  hasBranches,
  hasCommits,
  fetchBranches,
  fetchCommits,
  reset: resetRepoInfo,
} = useRepoInfo();

// ─── State ────────────────────────────────────────────────────────────────
const repoUrl = ref('');
const branch = ref('');
const commitHash = ref('');
const creating = ref(false);
const submitError = ref<string | null>(null);

// ─── Helpers ──────────────────────────────────────────────────────────────
function formatCommitLabel(hash: string, message: string): string
{
  const shortHash = hash.slice(0, 7);
  const shortMessage = message.length > 55
    ? `${message.slice(0, 55)}...`
    : message;

  return `${shortHash}  ${shortMessage}`;
}

function resetSelections(): void
{
  branch.value = '';
  commitHash.value = '';
  resetRepoInfo();
}

function applyDefaultBranch(): void
{
  const firstBranch = branches.value[0]?.name;
  if (firstBranch) branch.value = firstBranch;
}

// ─── Derived Options ──────────────────────────────────────────────────────
const branchOptions = computed(() =>
  branches.value.map(b => ({ label: b.name, value: b.name }))
);
const commitOptions = computed(() =>
  commits.value.map(c => ({
    label: formatCommitLabel(c.hash, c.message),
    value: c.hash,
  }))
);

// ─── Watchers ─────────────────────────────────────────────────────────────
watch(repoUrl, (url, _prev, onCleanup) => 
{
  submitError.value = null;
  resetSelections();

  const trimmedUrl = url.trim();
  if (!trimmedUrl) return;

  const timer = setTimeout(async () => 
  {
    await fetchBranches(trimmedUrl);
    applyDefaultBranch();
  }, 700);

  onCleanup(() => clearTimeout(timer));
});

watch(branch, (selectedBranch) => 
{
  commitHash.value = '';

  if (selectedBranch && repoUrl.value)
  {
    fetchCommits(repoUrl.value, selectedBranch);
  }
});

// ─── Submit ───────────────────────────────────────────────────────────────
async function onSubmit() 
{
  creating.value = true;
  submitError.value = null;

  try 
  {
    const project = await createProject({
      repoUrl: repoUrl.value,
      branch: branch.value || 'main',
      commitHash: commitHash.value || undefined,
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

onMounted(() => 
{
  store.triggerNewAnalysisTour();
});
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
        mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl
        bg-[var(--ui-primary)]/10 ring-1 ring-[var(--ui-primary)]/30
      "
      >
        <NIcon name="i-lucide-git-branch-plus"
          class="h-6 w-6 text-[var(--ui-primary)]"
        />
      </div>
      <h1 class="text-2xl font-bold text-[var(--ui-text-highlighted)]">Mulai Analisa Baru</h1>
      <p class="mt-1 text-sm text-[var(--ui-text-muted)]">
        Masukkan URL repositori, lalu pilih branch dan commit yang ingin dianalisa.
      </p>
    </div>

    <!-- ── Custom Form Card ───────────────────────────────────────────── -->
    <form @submit.prevent="onSubmit"
      class="
        mx-auto flex w-full max-w-3xl flex-col gap-3 rounded-2xl border
        border-[var(--ui-border)] bg-[var(--ui-bg)] p-6
        sm:p-7
      "
    >
      <!-- URL Input -->
      <div id="onboarding-repo-url"
        class="w-full"
      >
        <label class="sr-only">URL Repositori</label>
        <div class="
          relative flex w-full items-stretch overflow-hidden rounded-xl border
          border-[var(--ui-border)] bg-[var(--ui-bg-elevated)] transition-colors
          focus-within:border-transparent focus-within:bg-[var(--ui-bg)]
          focus-within:ring-2 focus-within:ring-[var(--ui-primary)]
        "
        >
          <div class="pointer-events-none flex items-center pl-4">
            <NIcon name="i-lucide-link"
              class="h-5 w-5 text-[var(--ui-text-muted)]"
            />
          </div>
          <input
            v-model="repoUrl"
            type="text"
            required
            placeholder="https://github.com/username/repo"
            :disabled="creating"
            class="
              w-full min-w-0 flex-1 bg-transparent py-2.5 pr-2 pl-3 text-xs
              text-[var(--ui-text)] placeholder-[var(--ui-text-muted)]
              focus:outline-none
              disabled:opacity-50
              sm:py-3 sm:text-sm
            "
          />
          <button
            id="onboarding-submit-btn"
            type="submit"
            :disabled="creating || !repoUrl"
            aria-label="Mulai analisa"
            class="
              relative hidden items-center justify-center gap-2
              bg-[var(--ui-primary)] px-2 text-xs font-semibold
              text-[var(--ui-bg)] transition-colors
              hover:bg-[var(--ui-primary)]/90
              focus:bg-[var(--ui-primary)]/90 focus:outline-none
              disabled:cursor-not-allowed disabled:opacity-50
              sm:flex sm:px-6 sm:text-base
            "
          >
            <span :class="{ 'opacity-0': creating }">Mulai Analisa</span>
            <NIcon name="i-lucide-play"
              class="
                h-4 w-4
                sm:h-5 sm:w-5
              "
              :class="{ 'opacity-0': creating }"
            />

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
        <div class="mt-1.5 min-h-5 px-1">
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
                flex items-center gap-1.5 text-xs text-[var(--ui-primary)]
                sm:text-sm
              "
            >
              <NIcon name="i-lucide-loader-2"
                class="h-4 w-4 animate-spin"
              /> Mengambil info repositori…
            </span>
            <span v-else-if="hasBranches"
              class="
                flex items-center gap-1.5 text-xs text-[var(--ui-primary)]
                sm:text-sm
              "
            >
              <NIcon name="i-lucide-check-circle-2"
                class="h-4 w-4"
              /> {{ branches.length }} branch tersedia
            </span>
            <span v-else
              class="
                text-xs text-[var(--ui-text-muted)]
                sm:text-sm
              "
            >
              Masukkan URL repositori publik GitHub
            </span>
          </transition>
        </div>
      </div>

      <!-- Branch & Commit Selectors -->
      <div id="onboarding-branch-commit"
        class="
          flex w-full flex-col gap-3
          sm:flex-row sm:gap-5
        "
      >
        <NTooltip text="Pilih branch target analisa." :popper="{ placement: 'top' }">
          <div class="
            relative w-full
            sm:w-1/2
          "
          >
            <div class="
              pointer-events-none absolute inset-y-0 left-0 flex items-center
              pl-3.5
            "
            >
              <NIcon name="i-lucide-git-branch"
                class="h-4 w-4 text-[var(--ui-text-muted)]"
              />
            </div>
            <select
              v-model="branch"
              :disabled="creating || !hasBranches"
              class="
                w-full cursor-pointer appearance-none rounded-xl border
                border-[var(--ui-border)] bg-[var(--ui-bg-elevated)] py-2.5 pr-8
                pl-10 text-xs text-[var(--ui-text)] transition-colors
                focus:ring-2 focus:ring-[var(--ui-primary)] focus:outline-none
                disabled:cursor-not-allowed disabled:opacity-50
                sm:text-sm
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
              pr-3 text-[var(--ui-text-muted)]
            "
            >
              <NIcon name="i-lucide-chevron-down"
                class="h-4 w-4"
              />
            </div>
          </div>
        </NTooltip>

        <NTooltip text="Pilih commit spesifik, kosongkan untuk commit terbaru." :popper="{ placement: 'top' }">
          <div class="
            relative w-full
            sm:w-1/2
          "
          >
            <div class="
              pointer-events-none absolute inset-y-0 left-0 flex items-center
              pl-3.5
            "
            >
              <NIcon name="i-lucide-git-commit"
                class="h-4 w-4 text-[var(--ui-text-muted)]"
              />
            </div>
            <select
              v-model="commitHash"
              :disabled="creating || !hasCommits"
              class="
                w-full cursor-pointer appearance-none rounded-xl border
                border-[var(--ui-border)] bg-[var(--ui-bg-elevated)] py-2.5 pr-8
                pl-10 text-xs text-[var(--ui-text)] transition-colors
                focus:ring-2 focus:ring-[var(--ui-primary)] focus:outline-none
                disabled:cursor-not-allowed disabled:opacity-50
                sm:text-sm
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
              pr-3 text-[var(--ui-text-muted)]
            "
            >
              <NIcon name="i-lucide-chevron-down"
                class="h-4 w-4"
              />
            </div>
          </div>
        </NTooltip>
      </div>

      <!-- Submit Button (Mobile Only) -->
      <button
        id="onboarding-submit-btn-mobile"
        type="submit"
        :disabled="creating || !repoUrl"
        aria-label="Mulai analisa"
        class="
          relative flex w-full items-center justify-center gap-2 rounded-xl
          bg-[var(--ui-primary)] px-4 py-3 text-sm font-semibold
          text-[var(--ui-bg)] transition-colors
          hover:bg-[var(--ui-primary)]/90
          focus:bg-[var(--ui-primary)]/90 focus:outline-none
          disabled:cursor-not-allowed disabled:opacity-50
          sm:hidden
        "
      >
        <span :class="{ 'opacity-0': creating }">Mulai Analisa</span>
        <NIcon name="i-lucide-play"
          class="
            h-4 w-4
            sm:h-5 sm:w-5
          "
          :class="{ 'opacity-0': creating }"
        />

        <NIcon
          v-if="creating"
          name="i-lucide-loader-2"
          class="
            absolute inset-0 m-auto h-5 w-5 animate-spin
            sm:h-6 sm:w-6
          "
        />
      </button>

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

    <p class="mt-6 text-center text-xs text-[var(--ui-text-muted)]">
      Analyzer dapat membuat kesalahan. Periksa informasi penting sebelum digunakan.
    </p>
  </div>
</template>
