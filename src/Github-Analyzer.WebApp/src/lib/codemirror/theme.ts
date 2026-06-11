import { EditorView } from '@codemirror/view';
import { githubDark, githubLight } from '@uiw/codemirror-theme-github';
import { Compartment, type Extension } from '@codemirror/state';
// ─── Custom Fold Gutter Marker ────────────────────────────────────────────────
export const foldGutterMarkerDOM = (isOpen: boolean): HTMLElement => 
{
  const el = document.createElement('div');
  el.innerHTML = isOpen
    ? '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"></polyline></svg>'
    : '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"></polyline></svg>';
  el.className = 'custom-fold-icon';
  return el;
};

// ─── Base Theme ───────────────────────────────────────────────────────────────
// Shared styles using CM6 baseTheme to avoid duplication
export const baseTheme = EditorView.baseTheme({
  '&': {
    height: '100%',
    fontSize: '14px',
    backgroundColor: 'transparent'
  },
  '.cm-content': {
    fontFamily: 'JetBrains Mono, Menlo, Monaco, Consolas, monospace',
  },
  '.cm-scroller': {
    overflow: 'auto'
  },
  '.cm-line.cm-highlighted-line': {
    transition: 'background-color 0.3s ease',
  },
  // Search Match Highlighting
  // Word highlight when iterating in search
  '&light .cm-searchMatch, &dark .cm-searchMatch': {
    backgroundColor: 'rgba(234, 92, 0, 0.33) !important',
  },
  '&light .cm-searchMatch.cm-searchMatch-selected, &dark .cm-searchMatch.cm-searchMatch-selected': {
    backgroundColor: 'rgba(255, 140, 0, 1) !important', 
    color: '#000000 !important',
  },
  // Ensure active line doesn't hide the selection layer (which is drawn behind text)
  '.cm-activeLine': {
    backgroundColor: 'rgba(128, 128, 128, 0.1) !important', // Semi-transparent instead of solid
  },
  // Word highlight when selecting text with mouse
  '.cm-selectionMatch': {
    backgroundColor: 'rgba(76, 175, 80, 0.4) !important',
  },
  // Fix for selection background being hidden
  '.cm-selectionBackground, .cm-content ::selection': {
    backgroundColor: 'rgba(51, 146, 255, 0.4) !important',
  },
  '.cm-gutterElement.cm-activeLineGutter': {},
  // Wider and neater gutters
  '.cm-gutters': {
    paddingRight: '4px',
  },
  '.cm-lineNumbers .cm-gutterElement': {
    padding: '0 12px 0 8px', // Wider padding for line numbers
  },
  '.cm-foldGutter': {
    width: '24px',
  },
  '.cm-foldGutter .cm-gutterElement': {
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: 'color 0.2s ease',
  },
  '&light .cm-foldGutter .cm-gutterElement:hover': {
    color: '#000000', // Black highlight on hover
  },
  '&dark .cm-foldGutter .cm-gutterElement:hover': {
    color: '#ffffff', // White highlight on hover
  },
  '.custom-fold-icon': {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
  }
});

// ─── Light Theme ──────────────────────────────────────────────────────────────
export const lightTheme: Extension = 
[
  githubLight,
  EditorView.theme({
    '.cm-line.cm-highlighted-line': {
      backgroundColor: 'rgba(46, 160, 67, 0.2) !important', // VS Code diff addition green
    }
  }, { dark: false })
];

// ─── Dark Theme ───────────────────────────────────────────────────────────────
export const darkTheme: Extension = 
[
  githubDark,
  EditorView.theme({
    '.cm-gutters': {
      backgroundColor: '#1a1a1a'
    },
    '.cm-line.cm-highlighted-line': {
      backgroundColor: 'rgba(46, 160, 67, 0.3) !important', // VS Code diff addition green
    }
  }, { dark: true })
];

// ─── Theme Compartment ────────────────────────────────────────────────────────
// Use a compartment for the active theme so it can be reconfigured dynamically
export const themeCompartment = new Compartment();

// ─── Theme Provider ───────────────────────────────────────────────────────────
/**
 * Returns the theme extension bundle for initial state creation.
 */
export const getThemeExtension = (isDark: boolean): Extension => 
  [
    baseTheme,
    themeCompartment.of(isDark ? darkTheme : lightTheme)
  ];
