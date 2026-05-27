import type { GraphPlugin, IGraphD3, D3Node } from '../graph.types';

/**
 * HoverPlugin — shows a floating tooltip on node hover.
 *
 * Tooltip is a plain <div> appended to the container's parent element
 * so it is not clipped by the SVG boundary.
 */
export class HoverPlugin implements GraphPlugin 
{
  readonly name = 'hover';

  private tooltip: HTMLDivElement | null = null;

  setup(graph: IGraphD3): void 
  {
    this.createTooltip(graph.container);

    const nodeSelection = graph.renderer.nodeRenderer.getSelection();
    if (!nodeSelection) return;

    nodeSelection
      .on('mouseenter.hover', (_event, d) => this.show(d))
      .on('mousemove.hover',  (event)     => this.move(event, graph.container))
      .on('mouseleave.hover', ()           => this.hide());
  }

  private createTooltip(container: HTMLElement): void 
  {
    this.tooltip = document.createElement('div');

    Object.assign(this.tooltip.style, 
      {
        position:        'absolute',
        pointerEvents:   'none',
        display:         'none',
        background:      'rgba(15, 15, 15, 0.85)',
        color:           '#f3f4f6',
        padding:         '5px 10px',
        borderRadius:    '6px',
        fontSize:        '12px',
        lineHeight:      '1.5',
        whiteSpace:      'nowrap',
        backdropFilter:  'blur(4px)',
        border:          '1px solid rgba(255,255,255,0.1)',
        zIndex:          '10',
      });

    // Append to parent so the tooltip isn't clipped by SVG overflow rules
    (container.parentElement ?? container).appendChild(this.tooltip);
  }

  private show(d: D3Node): void 
  {
    if (!this.tooltip) return;

    this.tooltip.innerHTML =
      `<strong>${d.label}</strong><br/>` +
      `<span style="opacity:0.6;font-size:10px">${d.pathId}</span>`;
    this.tooltip.style.display = 'block';
  }

  private move(event: MouseEvent, container: HTMLElement): void 
  {
    if (!this.tooltip) return;

    const rect = container.getBoundingClientRect();
    this.tooltip.style.left = `${event.clientX - rect.left + 14}px`;
    this.tooltip.style.top  = `${event.clientY - rect.top  - 10}px`;
  }

  private hide(): void 
  {
    if (this.tooltip) this.tooltip.style.display = 'none';
  }

  teardown(): void 
  {
    this.tooltip?.remove();
    this.tooltip = null;
  }
}
