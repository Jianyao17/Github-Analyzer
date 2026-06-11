import { StateEffect, StateField } from '@codemirror/state';
import { Decoration, type DecorationSet, EditorView } from '@codemirror/view';

// ─── Types ────────────────────────────────────────────────────────────────────
export interface HighlightRange 
{
  fromLine: number;
  toLine?: number;
}

// ─── State Effect ─────────────────────────────────────────────────────────────
export const setHighlightEffect = StateEffect.define<HighlightRange | null>();

// ─── Line Decoration ──────────────────────────────────────────────────────────
const highlightLineDeco = Decoration.line({ class: 'cm-highlighted-line' });

// ─── State Field ──────────────────────────────────────────────────────────────
export const highlightField = StateField.define<DecorationSet>({
  create() 
  {
    return Decoration.none;
  },

  update(decorations, tr) 
  {
    for (const effect of tr.effects) 
    {
      if (effect.is(setHighlightEffect)) 
      {
        if (effect.value === null) 
        {
          return Decoration.none;
        }

        const { fromLine, toLine } = effect.value;
        const endLine = toLine ?? fromLine;
        const doc = tr.state.doc;
        const maxLine = doc.lines;

        const start = Math.max(1, Math.min(fromLine, maxLine));
        const end = Math.max(start, Math.min(endLine, maxLine));

        const ranges: ReturnType<typeof highlightLineDeco.range>[] = [];

        for (let line = start; line <= end; line++) 
        {
          const lineObj = doc.line(line);
          ranges.push(highlightLineDeco.range(lineObj.from));
        }

        return Decoration.set(ranges, true);
      }
    }

    if (tr.docChanged) 
    {
      return decorations.map(tr.changes);
    }

    return decorations;
  },

  provide: (field) => EditorView.decorations.from(field)
});

// ─── Extension Bundle ─────────────────────────────────────────────────────────
export function highlightExtension() 
{
  return [highlightField];
}

// ─── Dispatch Helpers ─────────────────────────────────────────────────────────

export function dispatchHighlight(
  view: EditorView, 
  startLine: number, 
  endLine?: number
): void 
{
  const end = endLine ?? startLine;
  const doc = view.state.doc;
  const maxLine = doc.lines;

  const clampedStart = Math.max(1, Math.min(startLine, maxLine));
  const clampedEnd = Math.max(clampedStart, Math.min(end, maxLine));

  const scrollTarget = doc.line(clampedStart).from;

  // Single transaction with both highlight effect and scroll
  view.dispatch({
    effects: [
      setHighlightEffect.of({ 
        fromLine: clampedStart, 
        toLine: clampedEnd 
      }),
      EditorView.scrollIntoView(scrollTarget, 
        { 
          y: 'center',
          yMargin: 50,
        })
    ]
  });
}

export function clearHighlight(view: EditorView): void 
{
  view.dispatch({
    effects: setHighlightEffect.of(null)
  });
}
