import { ref, shallowRef, watch, onUnmounted } from 'vue';
import { getProjectSourceContentApi } from '@/api/project.api';
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

const loadCodeMirror = async () => 
{
  if (CMView) return;
  if (cmLoadPromise) return cmLoadPromise;
  
  cmLoadPromise = (async () => 
  {
    const [view, state, highlight, config, theme, languages] = 
      await Promise.all([
        import('@codemirror/view'),
        import('@codemirror/state'),
        import('@/lib/codemirror/highlight'),
        import('@/lib/codemirror/config'),
        import('@/lib/codemirror/theme'),
        import('@/lib/codemirror/languages')
      ]);
    
    CMView = view;
    CMState = state;
    CMHighlight = highlight;
    CMConfig = config;
    CMTheme = theme;
    CMLanguages = languages;
  })();
  
  await cmLoadPromise;
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
  const themeStore = useThemeStore();
  const viewerTheme = ref<'light'|'dark'>(themeStore.theme === 'dark' ? 'dark' : 'light');
  
  const tabs = shallowRef<CodeViewerTab[]>([]);
  const activeTabId = ref<string | null>(null);
  const editorView = shallowRef<EditorView | null>(null);
  const isLoading = ref(false);
  const isSearchOpen = ref(false);

  const isDark = () => 
    viewerTheme.value === 'dark';

  const openFile = async (relativePath: string, startLine?: number, endLine?: number) => 
  {
    let tab = tabs.value.find(t => t.id === relativePath);
    
    if (!tab) 
    {
      isLoading.value = true;
      try 
      {
        await loadCodeMirror();
        const res = await getProjectSourceContentApi(projectId, relativePath);
        const fileName = relativePath.split('/').pop() || relativePath;
        
        const ext = fileName.split('.').pop() || '';
        const langExt = await CMLanguages!.loadLanguageExtension(ext);
        
        tab = {
          id: relativePath,
          label: fileName,
          content: res.content,
          languageExt: langExt,
          startLine,
          endLine
        };
        tabs.value = [...tabs.value, tab];
      }
      catch (e) 
      {
        console.error('Failed to load file content', e);
      }
      finally 
      {
        isLoading.value = false;
      }
    }
    else 
    {
      // Update lines — always overwrite to prevent stale values from previous node
      tab.startLine = startLine;
      tab.endLine = endLine;
    }

    if (tab) 
    {
      activeTabId.value = tab.id;
      renderEditor(tab);
    }
  };

  const closeTab = (id: string) => 
  {
    const idx = tabs.value.findIndex(t => t.id === id);
    if (idx !== -1) 
    {
      const newTabs = [...tabs.value];
      newTabs.splice(idx, 1);
      tabs.value = newTabs;
      if (activeTabId.value === id) 
      {
        const nextTab = tabs.value[idx] || tabs.value[idx - 1];
        if (nextTab) 
        {
          activeTabId.value = nextTab.id;
          renderEditor(nextTab);
        }
        else 
        {
          activeTabId.value = null;
          destroyEditor();
        }
      }
    }
  };

  const initEditor = async (container: HTMLElement) => 
  {
    if (editorView.value) return;
    
    isLoading.value = true;
    try 
    {
      await loadCodeMirror();
      editorView.value = new CMView!.EditorView({
        parent: container
      });

      if (activeTabId.value) 
      {
        const activeTab = tabs.value.find(t => t.id === activeTabId.value);
        if (activeTab) renderEditor(activeTab);
      }
    } 
    finally 
    {
      isLoading.value = false;
    }
  };

  const destroyEditor = () => 
  {
    if (editorView.value && CMState && CMConfig) 
    {
      const emptyState = CMState.EditorState.create({
        extensions: CMConfig.getBaseExtensions(isDark()),
        doc: '',
      });
      editorView.value.setState(emptyState);
    }
  };

  const renderEditor = (tab: CodeViewerTab) => 
  {
    if (!editorView.value) return;

    // Check if this tab's document is already loaded in the editor
    const currentDoc = editorView.value.state.doc.toString();
    const needsRebuild = currentDoc !== tab.content;

    if (needsRebuild) 
    {
      const extensions: any[] = 
      [
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
              // Return focus to editor
              if (editorView.value) editorView.value.focus();
              return true;
            }
            return false;
          }
        }
      ]));

      const state = CMState!.EditorState.create({
        doc: tab.content,
        extensions
      });

      editorView.value.setState(state);
    }

    // Apply highlight via StateEffect dispatch
    if (tab.startLine != null && tab.startLine > 0) 
    {
      highlightLines(tab.startLine, tab.endLine);
    }
    else 
    {
      clearHighlightLines();
    }
  };

  /**
   * Highlight a range of lines and auto-scroll to them.
   * Replaces any existing highlight — no stacking.
   */
  const highlightLines = (startLine: number, endLine?: number): void => 
  {
    if (!editorView.value || !CMHighlight) return;

    try 
    {
      CMHighlight.dispatchHighlight(editorView.value, startLine, endLine);
    }
    catch (e) 
    {
      console.error('Failed to highlight lines', e);
    }
  };

  /**
   * Clear all highlights from the editor.
   */
  const clearHighlightLines = (): void => 
  {
    if (!editorView.value || !CMHighlight) return;
    CMHighlight.clearHighlight(editorView.value);
  };

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
    destroyEditor();
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
