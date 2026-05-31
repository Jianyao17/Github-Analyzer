import type { GraphPlugin, GraphData, GraphView, D3Node } from '@graph.types';

/**
 * SelectionPlugin — klik node untuk select/highlight.
 *
 * - Node yang dipilih tetap full opacity
 * - Semua node lain di-dim
 * - Klik node yang sama lagi untuk deselect
 *
 * Callback menerima D3Node yang dipilih, atau null saat deselect.
 */
export class SelectionPlugin implements GraphPlugin
{
  readonly name = 'selection';

  private selectedId: string | null                      = null;
  private readonly onSelectCallback?: (node: D3Node | null) => void;

  constructor(onSelect?: (node: D3Node | null) => void)
  {
    this.onSelectCallback = onSelect;
  }

  setup(_data: GraphData, view: GraphView): void
  {
    if (!view.nodeSelection) return;

    view.nodeSelection.on('click.selection', (_event, d) =>
    {
      // Toggle: klik node yang sudah dipilih untuk deselect
      this.selectedId = this.selectedId === d.id ? null : d.id;
      const selected  = this.selectedId;

      // Highlight node yang dipilih, dim semua node lain
      view.updateNodes((sel) =>
        sel.select('circle')
          .attr('opacity', (n) => (!selected || n.id === selected ? 1 : 0.25))
      );

      this.onSelectCallback?.(selected ? d : null);
    });
  }

  teardown(): void
  {
    this.selectedId = null;
  }
}
