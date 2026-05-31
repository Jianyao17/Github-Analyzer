import type { GraphPlugin, GraphData, GraphView, D3Node } from '@graph.types';

/**
 * HoverPlugin — menampilkan floating tooltip saat hover pada node.
 *
 * Tooltip adalah <div> biasa yang di-append ke parent container
 * agar tidak terpotong oleh SVG boundary.
 */
export class HoverPlugin implements GraphPlugin
{
  readonly name = 'hover';

  private tooltip:   HTMLDivElement | null = null;
  private container: HTMLElement    | null = null;

  setup(_data: GraphData, view: GraphView): void
  {
    const svg = view.svg?.node();
    if (!svg) return;

    // Dapatkan container dari parent SVG element
    this.container = svg.parentElement as HTMLElement | null;
    if (!this.container) return;

    this.createTooltip(this.container);

    if (!view.nodeSelection) return;

    view.nodeSelection
      .on('mouseenter.hover', (_event, d) => this.show(d))
      .on('mousemove.hover',  (event)      => this.move(event))
      .on('mouseleave.hover', ()           => this.hide());
  }

  private createTooltip(container: HTMLElement): void
  {
    this.tooltip = document.createElement('div');

    Object.assign(this.tooltip.style,
      {
        position:       'absolute',
        pointerEvents:  'none',
        display:        'none',
        background:     'rgba(15, 15, 15, 0.85)',
        color:          '#f3f4f6',
        padding:        '5px 10px',
        borderRadius:   '6px',
        fontSize:       '12px',
        lineHeight:     '1.5',
        whiteSpace:     'nowrap',
        backdropFilter: 'blur(4px)',
        border:         '1px solid rgba(255,255,255,0.1)',
        zIndex:         '10',
      });

    // Append ke parent agar tooltip tidak terpotong oleh SVG overflow
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

  private move(event: MouseEvent): void
  {
    if (!this.tooltip || !this.container) return;

    const rect = this.container.getBoundingClientRect();
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
    this.tooltip   = null;
    this.container = null;
  }
}
