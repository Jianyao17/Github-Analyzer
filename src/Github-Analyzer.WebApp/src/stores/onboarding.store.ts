import { ref } from 'vue';
import { defineStore } from 'pinia';
import { useSidebar } from '../composables/useSidebar';
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
  interaction?: {
    actionName?: 'zoom' | 'collapse' | 'context-menu' | 'hover';
    onBeforeStep?: (plugin: any) => void | Promise<void>;
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
  const activeEngineCallback = ref<(() => any) | null>(null);

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

  const setupGraphEventListener = async (stepIndex: number, attempt = 0) => 
  {
    const step = currentSteps.value[stepIndex];
    if (!step) return;

    const engine = activeEngineCallback.value?.();
    if (!engine) 
    {
      if (attempt < 20) 
      {
        setTimeout(() => setupGraphEventListener(stepIndex, attempt + 1), 100);
      }
      return;
    }

    const plugin = engine.getOnboardingPlugin?.() ?? engine.getPlugin?.('onboarding');
    if (!plugin) 
    {
      if (attempt < 20) 
      {
        setTimeout(() => setupGraphEventListener(stepIndex, attempt + 1), 100);
      }
      return;
    }

    plugin.setTourActive?.(true);
    plugin.cancelWait?.();

    // Call onBeforeStep if defined
    if (step.interaction?.onBeforeStep) 
    {
      await step.interaction.onBeforeStep(plugin);
    }

    plugin.refreshOverlaySoon?.();

    if (step.interaction?.actionName) 
    {
      plugin.waitForAction(step.interaction.actionName, () => 
      {
        setTimeout(() => 
        {
          if (wrapperRef.value) 
          {
            wrapperRef.value.goToStep(stepIndex + 1);
          }
        }, 800);
      });
    }
    else 
    {
      plugin.cancelWait?.();
    }
  };

  const deactivateGraphTour = () => 
  {
    const engine = activeEngineCallback.value?.();
    const plugin = engine?.getOnboardingPlugin?.() ?? engine?.getPlugin?.('onboarding');
    plugin?.cancelWait?.();
    plugin?.setTourActive?.(false);
  };

  const triggerNewAnalysisTour = () => 
  {
    if (!hasSeenNewAnalysis.value) 
    {
      const { isMobile } = useSidebar();
      
      const commonSteps: OnboardingStep[] = 
      [
        {
          attachTo: { element: '#repo-url' },
          content: { 
            icon: 'i-lucide-link', 
            title: 'URL Repositori', 
            description: 'Masukkan URL lengkap dari public repositori GitHub (misal: https://github.com/vuejs/core).' 
          }
        },
        {
          attachTo: { element: '#branch-commit' },
          content: { 
            icon: 'i-lucide-git-branch', 
            title: 'Pilih Branch & Commit', 
            description: 'Pilih spesifik branch atau hash commit yang ingin dianalisa. Kami akan mengambil data terbaru jika commit tidak dipilih.' 
          }
        },
        {
          attachTo: { element: isMobile.value ? '#onboarding-submit-btn-mobile' : '#onboarding-submit-btn' },
          content: { 
            icon: 'i-lucide-play', 
            title: 'Mulai Analisa', 
            description: 'Jika sudah, klik tombol ini untuk memproses repositori. Proses ini akan memakan waktu tergantung besar repositori.' 
          }
        }
      ];

      if (isMobile.value) 
      {
        startTour([
          {
            attachTo: { element: '#mobile-menu-btn' },
            content: { 
              icon: 'i-lucide-menu', 
              title: 'Akses Menu', 
              description: 'Gunakan tombol ini kapan saja untuk membuka sidebar dan melihat daftar repositori Anda.' 
            }
          },
          ...commonSteps
        ]);
      }
      else 
      {
        startTour([
          {
            attachTo: { element: '#sidebar-logo' },
            content: { 
              icon: 'i-lucide-menu', 
              title: 'Selamat Datang!', 
              description: 'Ini adalah Sidebar utama Github Analyzer. Anda bisa mengecilkan panel ini menggunakan tombol di sini.' 
            }
          },
          {
            attachTo: { element: '#new-analysis-btn' },
            content: { 
              icon: 'i-lucide-plus-circle', 
              title: 'Memulai Analisa', 
              description: 'Gunakan tombol ini kapan saja untuk kembali ke halaman ini dan memulai analisa repositori baru.' 
            }
          },
          {
            attachTo: { element: '#projects-list' },
            content: { 
              icon: 'i-lucide-history', 
              title: 'Riwayat Analisa', 
              description: 'Semua repositori yang pernah dianalisa akan tersimpan dan tampil di daftar ini.' 
            }
          },
          ...commonSteps
        ]);
      }
      
      setSeenNewAnalysis();
    }
  };

  const triggerOverviewTour = () => 
  {
    if (!hasSeenOverview.value) 
    {
      const { isMobile } = useSidebar();
      const doStart = () => 
      {
        startTour([
          {
            attachTo: { element: '#repo-header' },
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
            attachTo: { element: '#git-stats' },
            content: { 
              icon: 'i-lucide-git-commit-horizontal', 
              title: 'Statistik Git', 
              description: 'Melihat jumlah commit, kontributor, branch, dan total ukuran file dari repositori.' 
            }
          },
          {
            attachTo: { element: '#code-lines' },
            content: { 
              icon: 'i-lucide-code-2', 
              title: 'Analisis Baris Kode', 
              description: 'Rincian jumlah baris kode asli, baris komentar, dan baris kosong dari seluruh file.' 
            }
          }
        ]);
        setSeenOverview();
      };

      if (isMobile.value) 
      {
        const checkClosed = setInterval(() => 
        {
          if (document.getElementById('mobile-menu-btn')) 
          {
            clearInterval(checkClosed);
            setTimeout(doStart, 300); // Wait for the transition to finish fully
          }
        }, 150);
      }
      else 
      {
        doStart();
      }
    }
  };

  const triggerCodeGraphTour = (getEngine?: () => any) => 
  {
    if (getEngine) activeEngineCallback.value = getEngine;
    
    if (!hasSeenCodeGraph.value) 
    {
      const { isMobile } = useSidebar();
      const doStart = () => 
      {
        startTour([
          {
            attachTo: { element: '#code-graph-canvas' },
            content: { 
              icon: 'i-lucide-network', 
              title: 'Selamat Datang di Graph View', 
              description: 'Ini adalah visualisasi interaktif dari struktur codebase suatu repository. Mari kita pelajari cara menggunakannya.' 
            }
          },
          {
            attachTo: { element: '#graph-settings-menu-content' },
            options: { popper: { placement: 'left-end' } },
            content: { 
              icon: 'i-lucide-settings', 
              title: 'Pengaturan Graph', 
              description: 'Atur tata letak, mode tampilan (directory/namespace), dan kedalaman collapse di sini.' 
            },
            interaction: {
              onBeforeStep: async () => 
              {
                if (!document.getElementById('graph-settings-menu-content')) 
                {
                  const btn = document.querySelector('#graph-settings button') as HTMLButtonElement;
                  if (btn) btn.click();
                  await new Promise(resolve => setTimeout(resolve, 300));
                }
              }
            }
          },
          {
            attachTo: { element: '#graph-legend' },
            options: { popper: { placement: 'left-end' } },
            content: { 
              icon: 'i-lucide-palette', 
              title: 'Keterangan Warna', 
              description: 'Setiap warna mewakili tipe node berbeda. Klik untuk melihat rincian jumlah node dan relasi.' 
            },
            interaction: {
              onBeforeStep: async () => 
              {
                const content = document.getElementById('graph-legend-content');
                if (content && content.style.display === 'none') 
                {
                  const btn = document.querySelector('#graph-legend button') as HTMLButtonElement;
                  if (btn) btn.click();
                  await new Promise(resolve => setTimeout(resolve, 300));
                }
              }
            }
          },
          {
            attachTo: { element: '#graph-search' },
            options: { popper: { placement: 'bottom-start' } },
            content: { 
              icon: 'i-lucide-search', 
              title: 'Buka Pencarian', 
              description: 'Klik tombol ini atau gunakan shortcut Ctrl+K untuk membuka modal pencarian.' 
            },
            interaction: {
              onBeforeStep: async () => 
              {
                const modalInput = document.getElementById('graph-search-input-container');
                if (!modalInput) 
                {
                  document.getElementById('graph-search')?.click();
                  await new Promise(resolve => setTimeout(resolve, 300));
                }
              }
            }
          },
          {
            attachTo: { element: '#graph-search-input-container' },
            options: { popper: { placement: 'bottom' } },
            content: {
              icon: 'i-lucide-keyboard',
              title: 'Ketik Kata Kunci',
              description: 'Ketik nama file, class, atau fungsi yang ingin dicari. Kami mengisinya untuk Anda sebagai contoh.'
            },
            interaction: {
              onBeforeStep: async (plugin: any) => 
              {
                // Pastikan modal terbuka walau ditutup manual oleh user
                if (!document.getElementById('graph-search-input-container')) 
                {
                  document.getElementById('graph-search')?.click();
                  await new Promise(resolve => setTimeout(resolve, 300));
                }

                const input = document.querySelector('#graph-search-input-container input') as HTMLInputElement;
                if (input && !input.value) 
                {
                  const nodes = plugin?._data?.nodes || plugin?._ctx?.nodes || [];
                  const functionNodes = nodes.filter((n: any) => n.type === 4);
                  let targetLabel = 'a';
                  if (functionNodes.length > 0) 
                  {
                    const randomIdx = Math.floor(Math.random() * functionNodes.length);
                    targetLabel = functionNodes[randomIdx].label;
                  }
                  else if (nodes.length > 0) 
                  {
                    targetLabel = nodes[0].label;
                  }
                  
                  // Use native setter to trigger reactivity
                  const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')?.set;
                  nativeInputValueSetter?.call(input, targetLabel);
                  input.dispatchEvent(new Event('input', { bubbles: true }));
                  
                  await new Promise(resolve => setTimeout(resolve, 300));
                }
              }
            }
          },
          {
            attachTo: { element: '#graph-search-first-result' },
            options: { popper: { placement: 'right' } },
            content: {
              icon: 'i-lucide-mouse-pointer-click',
              title: 'Pilih Hasil Pencarian',
              description: 'Klik salah satu hasil pencarian ini. Graph akan secara otomatis memusatkan tampilan pada node tersebut.'
            },
            interaction: {
              onBeforeStep: async () => 
              {
                // Pastikan modal terbuka
                if (!document.getElementById('graph-search-first-result')) 
                {
                  document.getElementById('graph-search')?.click();
                  await new Promise(resolve => setTimeout(resolve, 300));
                }

                const btn = document.getElementById('graph-search-first-result');
                if (btn) 
                {
                  btn.addEventListener('click', () => 
                  {
                    setTimeout(() => 
                    {
                      // Index 6 adalah step Zoom (setelah search sequence)
                      if (wrapperRef.value) wrapperRef.value.goToStep(6);
                    }, 500);
                  }, { once: true });
                }
              }
            }
          },
          {
            attachTo: { element: '#code-graph-canvas' },
            content: { 
              icon: 'i-lucide-move', 
              title: 'Zoom & Navigasi', 
              description: 'Scroll untuk zoom in/out, klik dan drag untuk menggeser. Coba zoom ke salah satu node sekarang!' 
            },
            interaction: {
              actionName: 'zoom',
              onBeforeStep: (plugin: any) => 
              {
                const closeBtn = document.querySelector('#graph-search-input-container button:last-child') as HTMLButtonElement;
                if (closeBtn) closeBtn.click();
                
                plugin._ctx?.bus.emit('zoom:fit', { padding: 60 });
              }
            }
          },
          {
            attachTo: { element: '#graph-target-directory-node' },
            options: { popper: { placement: 'right' } },
            content: { 
              icon: 'i-lucide-info', 
              title: 'Hover Info', 
              description: 'Arahkan kursor ke node mana saja untuk melihat info singkat: nama, path, dan tipe node. Coba arahkan kursor ke node!' 
            },
            interaction: {
              actionName: 'hover'
            }
          },
          {
            attachTo: { element: '#graph-target-directory-node' },
            options: { popper: { placement: 'right' } },
            content: { 
              icon: 'i-lucide-folder-tree', 
              title: 'Collapse / Expand', 
              description: 'Klik node folder atau class untuk collapse atau expand anak-anaknya. Node yang menyala adalah node yang bisa di-expand. Coba klik salah satu node yang menyala!' 
            },
            interaction: {
              actionName: 'collapse',
              onBeforeStep: (plugin: any) => plugin.highlightExpandableNodes()
            }
          },
          {
            attachTo: { element: '#graph-target-file-node' },
            options: { popper: { placement: 'left' } },
            content: { 
              icon: 'i-lucide-mouse-pointer-click', 
              title: 'Context Menu', 
              description: 'Klik kanan pada node mana saja untuk membuka menu konteks. Di sini Anda bisa melihat relasi, highlight koneksi, atau copy path. Coba klik kanan salah satu node!' 
            },
            interaction: {
              actionName: 'context-menu',
              onBeforeStep: async (plugin: any) => 
              {
                await plugin.ensureFileNodeVisible();
                plugin.clearHighlight();
              }
            }
          },
          {
            attachTo: { element: '#graph-context-menu-btn' },
            options: { popper: { placement: 'left' } },
            content: { 
              icon: 'i-lucide-code', 
              title: 'Lihat Source Code', 
              description: 'Pilih \'Show Source Code\' dari context menu. Panel kode akan terbuka di samping graph.' 
            },
            interaction: {
              onBeforeStep: async (plugin: any) => 
              {
                // Cek apakah context menu sudah terbuka
                let btn = document.getElementById('graph-context-menu-btn');
                
                // Jika user menekan lanjut tanpa membuka context menu secara manual
                if (!btn) 
                {
                  plugin.openContextMenuOnTarget();
                  // Tunggu menu dirender
                  await new Promise(resolve => setTimeout(resolve, 300));
                  btn = document.getElementById('graph-context-menu-btn');
                }

                // Pasang event listener untuk auto-advance
                if (btn) 
                {
                  btn.addEventListener('click', () => 
                  {
                    setTimeout(() => 
                    {
                      if (wrapperRef.value) 
                      {
                        wrapperRef.value.goToStep(11);
                      }
                    }, 500);
                  }, { once: true });
                }
              }
            }
          },
          {
            attachTo: { element: '#code-viewer' },
            options: { popper: { placement: 'left' } },
            content: { 
              icon: 'i-lucide-file-code', 
              title: 'Code Viewer', 
              description: 'Di sini Anda dapat membaca baris kode asli dari repositori. Anda bisa ganti tema warna dan melakukan pencarian disini' 
            },
            interaction: {
              onBeforeStep: async () => 
              {
                const viewer = document.getElementById('code-viewer');
                if (!viewer) 
                {
                  const btn = document.getElementById('graph-context-menu-btn');
                  if (btn) 
                  {
                    btn.click();
                    await new Promise(resolve => setTimeout(resolve, 300));
                  }
                }
              }
            }
          }
        ]);
        setSeenCodeGraph();
      };

      if (isMobile.value) 
      {
        const checkClosed = setInterval(() => 
        {
          if (document.getElementById('mobile-menu-btn')) 
          {
            clearInterval(checkClosed);
            setTimeout(doStart, 300);
          }
        }, 150);
      }
      else 
      {
        doStart();
      }
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
    setupGraphEventListener,
    deactivateGraphTour,
    activeEngineCallback
  };
});
