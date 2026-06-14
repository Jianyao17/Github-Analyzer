import type { GraphContext } from '@graph/core/GraphContext';
import * as d3 from 'd3';
import type { GraphPlugin, GraphData, D3Node } from '@graph.types';
import { NODE_TYPE_KEYS, defaultGraphConfig } from '@graph/config';
import { getLucideIconBody } from '@graph/utils/icon';

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

    this._bindEvents(ctx);
    ctx.bus.on('render:complete', () => this._bindEvents(ctx));
    ctx.bus.on('view:refresh-requested', () => this.hide());
  }

  private _bindEvents(ctx: GraphContext): void 
  {
    if (!ctx.nodeSelection) return;

    ctx.nodeSelection
      .on('mouseenter.hover', (event: any, d: D3Node) => 
      {
        d3.select(event.currentTarget).style('cursor', 'pointer');
        this.show(d);
      })
      .on('mousemove.hover',  (event: any) => this.move(event))
      .on('mouseleave.hover', (event: any) => 
      {
        d3.select(event.currentTarget).style('cursor', null);
        this.hide();
      });
  }

  private createTooltip(_container: HTMLElement): void
  {
    this.tooltip = document.createElement('div');

    Object.assign(this.tooltip.style,
      {
        position:       'fixed',
        pointerEvents:  'none',
        opacity:        '0',
        visibility:     'hidden',
        transform:      'translateY(4px)',
        transition:     'opacity 0.2s cubic-bezier(0.4, 0, 0.2, 1), transform 0.2s cubic-bezier(0.4, 0, 0.2, 1), visibility 0.2s',
        background:     'rgba(24, 24, 27, 0.95)',
        color:          '#f4f4f5',
        padding:        '6px 10px',
        borderRadius:   '8px',
        fontSize:       '13px',
        lineHeight:     '1.4',
        whiteSpace:     'nowrap',
        backdropFilter: 'blur(8px)',
        WebkitBackdropFilter: 'blur(8px)',
        border:         '1px solid rgba(255, 255, 255, 0.1)',
        boxShadow:      '0 8px 16px -4px rgba(0, 0, 0, 0.2), 0 4px 6px -2px rgba(0, 0, 0, 0.1)',
        zIndex:         '1000',
      });

    // Append ke body agar tooltip tidak terpotong dan aman dari mount/unmount
    document.body.appendChild(this.tooltip);
  }

  private show(d: D3Node): void
  {
    if (!this.tooltip) return;

    const typeKey = NODE_TYPE_KEYS[d.type] ?? 'default';
    const style   = defaultGraphConfig.nodeTypes[typeKey] ?? defaultGraphConfig.nodeTypes['default'];
    const iconSvg = getLucideIconBody(style.icon);

    const shortPath = d.pathId.length > 50 ? '...' + d.pathId.slice(-47) : d.pathId;

    this.tooltip.innerHTML = `
      <div style="display: flex; flex-direction: column; gap: 3px;">
        <div style="display: flex; align-items: center; gap: 4px;">
          <svg width="14" height="14" viewBox="0 0 24 24" style="color: ${style.color};" 
            fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            ${iconSvg}
          </svg>
          <div style="font-weight: 600; color: #ffffff; letter-spacing: 0.2px;">${d.label}</div>
        </div>
        <div style="font-size: 10px; color: #a1a1aa; font-family: monospace;">${shortPath}</div>
      </div>
    `;
    this.tooltip.style.opacity = '1';
    this.tooltip.style.visibility = 'visible';
    this.tooltip.style.transform = 'translateY(0)';
  }

  private move(event: MouseEvent): void
  {
    if (!this.tooltip) return;

    // Posisi didekatkan sedikit namun tetap aman dari label
    let left = event.clientX + 12;
    let top  = event.clientY + 18;

    // Basic edge boundary check untuk menghindari tooltip terpotong layar
    if (left + 250 > window.innerWidth) left = event.clientX - 250;
    if (top + 80 > window.innerHeight) top = event.clientY - 80;

    this.tooltip.style.left = `${left}px`;
    this.tooltip.style.top  = `${top}px`;
  }

  private hide(): void
  {
    if (this.tooltip) 
    {
      this.tooltip.style.opacity = '0';
      this.tooltip.style.visibility = 'hidden';
      this.tooltip.style.transform = 'translateY(4px)';
    }
  }

  teardown(): void
  {
    this.tooltip?.remove();
    this.tooltip   = null;
    this.container = null;
  }
}
