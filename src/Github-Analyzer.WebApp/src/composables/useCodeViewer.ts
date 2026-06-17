import { ref, shallowRef, watch, onUnmounted, computed } from 'vue';
import { useProjectApi } from '@/composables/useProjectApi';
import { useThemeStore } from '@/stores/theme.store';
import type { EditorView } from '@codemirror/view';

// Global cache for dynamically imported CodeMirror modules
let CMView      : typeof import('@codemirror/view') | null = null;
let CMState     : typeof import('@codemirror/state') | null = null;
let CMHighlight : typeof import('@/lib/codemirror/highlight') | null = null;
let CMConfig    : typeof import('@/lib/codemirror/config') | null = null;
let CMTheme     : typeof import('@/lib/codemirror/theme') | null = null;
let CMLanguages : typeof import('@/lib/codemirror/languages') | null = null;

let cmLoadPromise: Promise<void> | null = null;

const loadCodeMirror = (): Promise<void> => 
{
  if (CMView) return Promise.resolve();
  if (cmLoadPromise) return cmLoadPromise;
  
  cmLoadPromise = Promise.all([
    import('@codemirror/view'),
    import('@codemirror/state'),
    import('@/lib/codemirror/highlight'),
    import('@/lib/codemirror/config'),
    import('@/lib/codemirror/theme'),
    import('@/lib/codemirror/languages')
  ]).then(([view, state, highlight, config, theme, languages]) => 
  {
    CMView = view;
    CMState = state;
    CMHighlight = highlight;
    CMConfig = config;
    CMTheme = theme;
    CMLanguages = languages;
  });
  
  return cmLoadPromise;
};

export interface CodeViewerTab {
  id: string; // usually relativePath
  label: string; // file name
  content: string;
  languageExt: any;
  startLine?: number;
  endLine?: number;
}

export function useCodeViewer(projectId: string) 
{
  const { getProjectSourceContent } = useProjectApi();
  const themeStore = useThemeStore();
  const viewerTheme = ref<'light'|'dark'>(themeStore.theme === 'dark' ? 'dark' : 'light');
  
  const tabs = shallowRef<CodeViewerTab[]>([]);
  const activeTabId = ref<string | null>(null);
  const editorView = shallowRef<EditorView | null>(null);
  const isLoading = ref(false);
  const isSearchOpen = ref(false);

  const isDark = () => viewerTheme.value === 'dark';
  const activeTab = computed(() => tabs.value.find(t => t.id === activeTabId.value));
  
  const openFile = async (relativePath: string, startLine?: number, endLine?: number) => 
  {
    let tab = tabs.value.find(t => t.id === relativePath);

    if (!tab) 
    {
      const fileName = relativePath.split('/').pop() || relativePath;
      tab = {
        id: relativePath,
        label: fileName,
        content: '',
        languageExt: null,
        startLine,
        endLine
      };
      tabs.value = [...tabs.value, tab];
    }
    else 
    {
      tab.startLine = startLine;
      tab.endLine = endLine;
    }

    activeTabId.value = tab.id;

    try 
    {
      isLoading.value = true;
      
      const [_, sourceData] = await Promise.all([
        loadCodeMirror()
          .then(async () => 
          {
            if (!tab!.languageExt) 
            {
              const ext = tab!.label.split('.').pop() || '';
              tab!.languageExt = await CMLanguages!.loadLanguageExtension(ext);
            }
          }),
        tab.content 
          ? Promise.resolve(null) 
          : getProjectSourceContent(projectId, relativePath)
      ]);

      if (sourceData) 
      {
        tab.content = sourceData.content;
      }
    }
    catch (e) 
    {
      console.error('Failed to load file resources', e);
    }
    finally 
    {
      isLoading.value = false;
    }
    
    if (activeTabId.value === tab.id) 
    {
      renderEditor(tab);
    }
  };

  const closeTab = (id: string) => 
  {
    const idx = tabs.value.findIndex(t => t.id === id);
    if (idx === -1) return;

    const newTabs = [...tabs.value];
    newTabs.splice(idx, 1);
    tabs.value = newTabs;
    
    if (activeTabId.value === id) 
    {
      const nextTab = newTabs[idx] || newTabs[idx - 1];
      activeTabId.value = nextTab ? nextTab.id : null;
      
      if (nextTab) 
      {
        renderEditor(nextTab);
      }
      else 
      {
        clearEditor();
      }
    }
  };

  const initEditor = async (container: HTMLElement) => 
  {
    if (editorView.value) return;
    
    try 
    {
      await loadCodeMirror();
      editorView.value = new CMView!.EditorView({
        parent: container
      });

      if (activeTab.value) 
      {
        renderEditor(activeTab.value);
      }
    }
    catch (e) 
    {
      console.error('Failed to init editor', e);
    }
  };

  const clearEditor = () => 
  {
    if (editorView.value && CMState && CMConfig) 
    {
      editorView.value.setState(CMState.EditorState.create({
        extensions: CMConfig.getBaseExtensions(isDark()),
        doc: '',
      }));
    }
  };

  // Kept for backward compatibility if used externally
  const destroyEditor = () => clearEditor();

  const renderEditor = (tab: CodeViewerTab) => 
  {
    if (!editorView.value || !CMState) return;

    const currentDoc = editorView.value.state.doc.toString();
    const needsRebuild = currentDoc !== tab.content;

    if (needsRebuild) 
    {
      const extensions = [
        ...CMConfig!.getBaseExtensions(isDark()),
        ...CMHighlight!.highlightExtension()
      ];

      if (tab.languageExt) 
      {
        extensions.push(tab.languageExt);
      }
      
      // Add custom search shortcut override
      extensions.push(CMView!.keymap.of([
        {
          key: 'Mod-f',
          preventDefault: true,
          run: () => 
          {
            isSearchOpen.value = true;
            return true;
          }
        },
        {
          key: 'Escape',
          run: () => 
          {
            if (isSearchOpen.value) 
            {
              isSearchOpen.value = false;
              editorView.value?.focus();
              return true;
            }
            return false;
          }
        }
      ]));

      editorView.value.setState(CMState.EditorState.create({
        doc: tab.content,
        extensions
      }));
    }

    if (tab.startLine != null && tab.startLine > 0) 
    {
      highlightLines(tab.startLine, tab.endLine);
    }
    else 
    {
      clearHighlightLines();
    }
  };

  const highlightLines = (startLine: number, endLine?: number): void => 
  {
    if (editorView.value && CMHighlight) 
    {
      try 
      {
        CMHighlight.dispatchHighlight(editorView.value, startLine, endLine);
      }
      catch (e) 
      {
        console.error('Failed to highlight lines', e);
      }
    }
  };

  const clearHighlightLines = (): void => 
  {
    if (editorView.value && CMHighlight) 
    {
      CMHighlight.clearHighlight(editorView.value);
    }
  };

  // Sync theme from store if changed externally
  watch(() => themeStore.theme, (newTheme) => 
  {
    viewerTheme.value = newTheme === 'dark' ? 'dark' : 'light';
  });

  // Dynamic theme switching using Compartment
  watch(viewerTheme, () => 
  {
    if (editorView.value && CMTheme) 
    {
      editorView.value.dispatch({
        effects: CMTheme.themeCompartment.reconfigure(isDark() ? CMTheme.darkTheme : CMTheme.lightTheme)
      });
    }
  });

  onUnmounted(() => 
  {
    if (editorView.value) 
    {
      editorView.value.destroy();
      editorView.value = null;
    }
  });

  return {
    viewerTheme,
    tabs,
    activeTabId,
    editorView,
    isLoading,
    isSearchOpen,
    openFile,
    closeTab,
    initEditor,
    destroyEditor,
    highlightLines,
    clearHighlightLines
  };
}
