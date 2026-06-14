import { defineStore } from 'pinia';
import { ref } from 'vue';

import type { StepEntity } from 'v-onboarding';

export type OnboardingStep = Omit<StepEntity, 'content'> & 
{
  content: StepEntity['content'] & 
  {
    icon?: string;
    media?: {
      type: 'image' | 'video';
      url: string;
    };
  };
};

export interface VOnboardingInstance 
{
  start: () => void;
  finish: () => void;
  goToStep: (index: number) => void;
}

export const useOnboardingStore = defineStore('onboarding', () => 
{
  const wrapperRef = ref<VOnboardingInstance | null>(null); // Reference to VOnboardingWrapper instance
  const currentSteps = ref<OnboardingStep[]>([]); // To store steps for the current tour

  const hasSeenNewAnalysis = ref<boolean>(
    localStorage.getItem('onboarding_new_analysis') === 'true'
  );
  
  const hasSeenOverview = ref<boolean>(
    localStorage.getItem('onboarding_overview') === 'true'
  );
  
  const hasSeenCodeGraph = ref<boolean>(
    localStorage.getItem('onboarding_codegraph') === 'true'
  );

  const setSeenNewAnalysis = () => 
  {
    hasSeenNewAnalysis.value = true;
    localStorage.setItem('onboarding_new_analysis', 'true');
  };

  const setSeenOverview = () => 
  {
    hasSeenOverview.value = true;
    localStorage.setItem('onboarding_overview', 'true');
  };

  const setSeenCodeGraph = () => 
  {
    hasSeenCodeGraph.value = true;
    localStorage.setItem('onboarding_codegraph', 'true');
  };
  
  // Utility to reset all (for testing or replaying tours)
  const resetTours = () => 
  {
    hasSeenNewAnalysis.value = false;
    hasSeenOverview.value = false;
    hasSeenCodeGraph.value = false;
    localStorage.removeItem('onboarding_new_analysis');
    localStorage.removeItem('onboarding_overview');
    localStorage.removeItem('onboarding_codegraph');
  };

  const startTour = (steps: OnboardingStep[], delayMs: number = 800) => 
  {
    currentSteps.value = steps;
    // Delay to ensure DOM transitions (like sidebar closing) finish
    setTimeout(() => 
    {
      if (wrapperRef.value) 
      {
        wrapperRef.value.start();
      }
    }, delayMs);
  };

  const triggerNewAnalysisTour = () => 
  {
    if (!hasSeenNewAnalysis.value) 
    {
      startTour([
        {
          attachTo: { element: '#onboarding-sidebar-logo' },
          content: { 
            icon: 'i-lucide-menu', 
            title: 'Selamat Datang!', 
            description: 'Ini adalah Sidebar utama Github Analyzer. Anda bisa mengecilkan panel ini menggunakan tombol di sini.',
          }
        },
        {
          attachTo: { element: '#onboarding-new-analysis-btn' },
          content: { 
            icon: 'i-lucide-plus-circle', 
            title: 'Memulai Analisa', 
            description: 'Gunakan tombol ini kapan saja untuk kembali ke halaman ini dan memulai analisa repositori baru.' 
          }
        },
        {
          attachTo: { element: '#onboarding-projects-list' },
          content: { 
            icon: 'i-lucide-history', 
            title: 'Riwayat Analisa', 
            description: 'Semua repositori yang pernah dianalisa akan tersimpan dan tampil di daftar ini.' 
          }
        },
        {
          attachTo: { element: '#onboarding-repo-url' },
          content: { 
            icon: 'i-lucide-link', 
            title: 'URL Repositori', 
            description: 'Masukkan URL lengkap dari public repositori GitHub (misal: https://github.com/vuejs/core).' 
          }
        },
        {
          attachTo: { element: '#onboarding-branch-commit' },
          content: { 
            icon: 'i-lucide-git-branch', 
            title: 'Pilih Branch & Commit', 
            description: 'Pilih spesifik branch atau hash commit yang ingin dianalisa. Kami akan mengambil data terbaru jika commit tidak dipilih.' 
          }
        },
        {
          attachTo: { element: '#onboarding-submit-btn' },
          content: { 
            icon: 'i-lucide-play', 
            title: 'Mulai Analisa', 
            description: 'Jika sudah, klik tombol ini untuk memproses repositori. Proses ini akan memakan waktu tergantung besar repositori.' 
          }
        }
      ]);
      setSeenNewAnalysis();
    }
  };

  const triggerOverviewTour = () => 
  {
    if (!hasSeenOverview.value) 
    {
      startTour([
        {
          attachTo: { element: '#onboarding-repo-header' },
          content: { 
            icon: 'i-lucide-github', 
            title: 'Informasi Repositori', 
            description: 'Di sini Anda dapat melihat informasi repositori yang sedang dianalisa, beserta branch dan commit hash-nya.' 
          }
        },
        {
          attachTo: { element: '#tab-statistic' },
          content: { 
            icon: 'i-lucide-bar-chart-2', 
            title: 'Tab Statistik', 
            description: 'Tab ini menampilkan ringkasan statistik Git dan analisis jumlah baris kode dari repositori.' 
          }
        },
        {
          attachTo: { element: '#tab-codegraph' },
          content: { 
            icon: 'i-lucide-network', 
            title: 'Tab Code Graph', 
            description: 'Tab ini digunakan untuk melihat visualisasi struktur direktori dan keterhubungan file.' 
          }
        },
        {
          attachTo: { element: '#onboarding-git-stats' },
          content: { 
            icon: 'i-lucide-git-commit-horizontal', 
            title: 'Statistik Git', 
            description: 'Melihat jumlah commit, kontributor, branch, dan total ukuran file dari repositori.' 
          }
        },
        {
          attachTo: { element: '#onboarding-code-lines' },
          content: { 
            icon: 'i-lucide-code-2', 
            title: 'Analisis Baris Kode', 
            description: 'Rincian jumlah baris kode asli, baris komentar, dan baris kosong dari seluruh file.' 
          }
        }
      ]);
      setSeenOverview();
    }
  };

  const triggerCodeGraphTour = () => 
  {
    if (!hasSeenCodeGraph.value) 
    {
      startTour([
        {
          attachTo: { element: '#onboarding-code-graph-canvas' },
          content: { 
            icon: 'i-lucide-network', 
            title: 'Visualisasi Code Graph', 
            description: 'Grafik ini menunjukkan struktur direktori dan file dari repositori. Anda dapat melakukan zoom dan pan (geser) pada area ini.' 
          }
        },
        {
          attachTo: { element: '#onboarding-code-graph-canvas' },
          content: { 
            icon: 'i-lucide-mouse-pointer-click', 
            title: 'Melihat Source Code', 
            description: 'Klik node file (yang berbentuk lingkaran) untuk langsung melihat source code-nya di panel sebelah kanan.' 
          }
        }
      ]);
      setSeenCodeGraph();
    }
  };

  return {
    wrapperRef,
    currentSteps,
    hasSeenNewAnalysis,
    hasSeenOverview,
    hasSeenCodeGraph,
    triggerNewAnalysisTour,
    triggerOverviewTour,
    triggerCodeGraphTour,
    setSeenNewAnalysis,
    setSeenOverview,
    setSeenCodeGraph,
    resetTours,
    startTour,
  };
});
