import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import type { GraphContext } from '@graph/core/GraphContext';

/**
 * HoverPlugin — menampilkan floating tooltip saat hover pada node.
 *
 * Tooltip adalah <div> biasa yang di-append ke parent container
 * agar tidak terpotong oleh SVG boundary.
 */
export class HoverPlugin implements GraphPlugin
{
  readonly name = 'hover';
  readonly priority = 2;

  private tooltip:   HTMLDivElement | null = null;
  private container: HTMLElement    | null = null;

  setup(ctx: GraphContext, _data: GraphData): void
  {
    const svg = ctx.svg?.node();
    if (!svg) return;

    // Dapatkan container dari parent SVG element
    this.container = svg.parentElement as HTMLElement | null;
    if (!this.container) return;

    this.createTooltip(this.container);

    if (!ctx.nodeSelection) return;

    ctx.nodeSelection
      .on('mouseenter.hover', (_event: any, d: D3Node) => this.show(d))
      .on('mousemove.hover',  (event: any)      => this.move(event))
      .on('mouseleave.hover', ()           => this.hide());
  }

  private createTooltip(_container: HTMLElement): void
  {
    this.tooltip = document.createElement('div');

    Object.assign(this.tooltip.style,
      {
        position:       'fixed',
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

    // Append ke body agar tooltip tidak terpotong dan aman dari mount/unmount
    document.body.appendChild(this.tooltip);
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
    if (!this.tooltip) return;

    this.tooltip.style.left = `${event.clientX + 14}px`;
    this.tooltip.style.top  = `${event.clientY - 10}px`;
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
