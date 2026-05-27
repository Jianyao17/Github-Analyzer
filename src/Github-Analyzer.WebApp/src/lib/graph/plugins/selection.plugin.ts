import type { GraphPlugin, IGraphD3, D3Node } from '../graph.types';

/**
 * SelectionPlugin — click a node to select/highlight it.
 *
 * - Selected node stays at full opacity
 * - All other nodes are dimmed
 * - Clicking the same node again deselects it
 *
 * Callback receives the selected D3Node, or null on deselect.
 */
export class SelectionPlugin implements GraphPlugin 
{
  readonly name = 'selection';

  private selectedId: string | null = null;
  private readonly onSelectCallback?: (node: D3Node | null) => void;

  constructor(onSelect?: (node: D3Node | null) => void) 
  {
    this.onSelectCallback = onSelect;
  }

  setup(graph: IGraphD3): void 
  {
    const nodeSelection = graph.renderer.nodeRenderer.getSelection();
    if (!nodeSelection) return;

    nodeSelection.on('click.selection', (_event, d) => 
    {
      // Toggle: clicking the selected node again deselects it
      this.selectedId = this.selectedId === d.id ? null : d.id;
      const selected = this.selectedId;

      // Highlight selected node, dim all others
      nodeSelection.select('circle')
        .attr('opacity', (n) => (!selected || n.id === selected ? 1 : 0.25));

      this.onSelectCallback?.(selected ? d : null);
    });
  }

  teardown(): void 
  {
    this.selectedId = null;
  }
}
