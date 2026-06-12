import { EditorState } from '@codemirror/state';
import { defaultKeymap } from '@codemirror/commands';
import { indentationMarkers } from '@replit/codemirror-indentation-markers';
import { getThemeExtension, foldGutterMarkerDOM } from './theme';
import {
  EditorView,
  lineNumbers,
  highlightActiveLineGutter,
  highlightSpecialChars,
  drawSelection,
  dropCursor,
  crosshairCursor,
  rectangularSelection,
  highlightActiveLine,
  keymap,
} from '@codemirror/view';

import {
  bracketMatching,
  foldGutter,
  foldKeymap
} from '@codemirror/language';

import {
  search,
  highlightSelectionMatches
} from '@codemirror/search';



export const getBaseExtensions = (isDark: boolean) => 
  [
  // 1. Core Visual Features
    lineNumbers(),
    foldGutter({ 
      markerDOM: foldGutterMarkerDOM 
    }),
    dropCursor(),
    crosshairCursor(),
    highlightActiveLineGutter(),
    highlightSpecialChars(),
    indentationMarkers(),
  
    // 2. State & Data
    EditorState.readOnly.of(true), // Content is readonly
    EditorView.editable.of(false), // Prevent virtual keyboard on mobile

    // 3. Selection & Search
    drawSelection(),
    rectangularSelection(),
    highlightSelectionMatches(),
    highlightActiveLine(),
    bracketMatching(),
    search({
      createPanel: () => 
      {
        const dom = document.createElement('div');
        dom.style.display = 'none'; // Hide the default search panel entirely
        return { dom };
      }
    }),

    // 5. Scroll Margins to avoid floating elements
    EditorView.scrollMargins.of(() => ({ top: 60 })),

    // 6. Keymaps (Exclude history because read-only)
    keymap.of([
      ...defaultKeymap,
      ...foldKeymap
    ]),

    // 7. Highlighting & Theme
    getThemeExtension(isDark)
  ];
