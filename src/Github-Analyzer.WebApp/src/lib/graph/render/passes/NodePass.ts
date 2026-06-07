import * as d3 from 'd3';
import type { EventBus } from '@graph/core/EventBus';
import type { TextMeasurer } from '../measure/TextMeasurer';
import type { 
  D3Node, GraphConfig, NodeCardConfig, 
  NodeTypeStyle, NodeSelection
} from '@graph.types';

import { NODE_TYPE_KEYS } from '@graph/config';
import { getLucideIconBody } from '@graph/utils/icon';
import { truncateLabel } from '@graph/utils/label';

const DEFAULT_CARD: NodeCardConfig = {
  height:          24,
  iconSize:        14,

  paddingLeft:      8,
  paddingRight:     8,
  gap:              4,

  labelFontFamily: '\'Segoe UI\', sans-serif',
  labelLetterSpacing: 0, 
  labelFontSize:   12,

  cornerRadius:    12,
  approxCharWidth:  7.5,
  arrowGap:         4,
};

export class NodePass 
{
  private _selection: NodeSelection | null = null;
  private _measurer:  TextMeasurer;
  private _bus:       EventBus;

  constructor(measurer: TextMeasurer, bus: EventBus) 
  {
    this._measurer = measurer;
    this._bus = bus;
  }

  run(
    viewport: d3.Selection<SVGGElement, unknown, null, undefined>,
    nodes:    D3Node[],
    config:   GraphConfig,
  ): NodeSelection 
  {
    let container = viewport.select<SVGGElement>('g.nodes');
    if (container.empty()) 
    {
      container = viewport.append('g').attr('class', 'nodes');
    }

    this._selection = this._applyUpdatePattern(container, nodes, config);
    return this._selection;
  }

  apply(nodes: D3Node[], config: GraphConfig): void 
  {
    if (!this._selection) return;
    const firstNode = this._selection.node();

    if (!firstNode) return;
    const parentEl = firstNode.parentNode as SVGGElement | null;

    if (!parentEl) return;
    const parentSel = d3.select<SVGGElement, unknown>(parentEl);
    this._selection = this._applyUpdatePattern(parentSel, nodes, config);
  }

  updatePositions(): void 
  {
    this._selection?.attr('transform', (d) => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }

  applyHighlight(ids: Set<string>, dimOpacity: number): void 
  {
    if (!this._selection) return;
    const hasQuery = ids.size > 0;
    this._selection.each(function (d) 
    {
      const g       = d3.select<SVGGElement, D3Node>(this);
      const matched = ids.has(d.id);

      g.transition()
        .duration(200)
        .attr('opacity', hasQuery && !matched ? dimOpacity : 1);

      g.select<SVGRectElement>('rect')
        .transition()
        .duration(200)
        .attr('stroke',       matched ? '#FCD34D' : 'none')
        .attr('stroke-width', matched ? 3 : 0);
    });
  }

  clearHighlight(): void 
  {
    if (!this._selection) return;
    this._selection.each(function () 
    {
      const g = d3.select<SVGGElement, D3Node>(this);
      g.transition().duration(200).attr('opacity', 1);
      g.select<SVGRectElement>('rect')
        .transition().duration(200).attr('stroke', 'none').attr('stroke-width', 0);
    });
  }

  clear(): void 
  {
    this._selection = null;
  }

  private _applyUpdatePattern(
    container: d3.Selection<SVGGElement, unknown, null, undefined>,
    nodes:     D3Node[],
    config:    GraphConfig,
  ): NodeSelection 
  {
    const card = { ...DEFAULT_CARD, ...config.nodeCard };
    const bound = container
      .selectAll<SVGGElement, D3Node>('g.node')
      .data(nodes, (d) => d.id);

    bound.exit().remove();

    const entered = bound
      .enter()
      .append('g')
      .attr('class',  'node')
      .attr('cursor', 'grab')
      .on('click', (event, d) => this._bus.emit('node:click', { node: d, event }));

    entered.each((d, i, nodes) => 
    {
      const typeKey = NODE_TYPE_KEYS[d.type] ?? 'default';
      const style   = config.nodeTypes[typeKey] ?? config.nodeTypes['default'];
      const g       = d3.select<SVGGElement, D3Node>(nodes[i]);
      this._renderNodeCard(g, d, style, card);
    });

    const merged = entered.merge(bound);
    
    merged.each((d, i, nodes) => 
    {
      const g = d3.select<SVGGElement, D3Node>(nodes[i]);
      this._updateCollapseBadge(g, d);
    });

    return merged;
  }

  private _renderNodeCard(
    g:     d3.Selection<SVGGElement, D3Node, null, undefined>,
    d:     D3Node,
    style: NodeTypeStyle,
    card:  NodeCardConfig,
  ): void 
  {
    const shortLabel  = truncateLabel(d.label);
    const isTruncated = shortLabel !== d.label;
    
    const textEl = g.append('text')
      .attr('dominant-baseline', 'middle')
      .attr('font-family',       card.labelFontFamily)
      .attr('font-size',         card.labelFontSize)
      .attr('letter-spacing',    card.labelLetterSpacing)
      .attr('fill',              'currentColor')
      .text(shortLabel);

    const fontStr = `${card.labelFontSize}px ${card.labelFontFamily}`;
    const textWidth = this._measurer.measure(shortLabel, fontStr) + 4;

    const cardWidth = card.paddingLeft + card.iconSize + card.gap + textWidth + card.paddingRight;
    const hw = cardWidth / 2;
    const hh = card.height / 2;
    
    d._hw = hw;
    d._hh = hh;
    d._radius = Math.sqrt(hw * hw + hh * hh);

    const iconLeft = -hw + card.paddingLeft;
    const labelX   = iconLeft + card.iconSize + card.gap;

    textEl
      .attr('x', labelX)
      .attr('y', 0);

    g.insert('rect', 'text')
      .attr('x', -hw)
      .attr('y', -hh)
      .attr('width',  cardWidth)
      .attr('height', card.height)
      .attr('rx',     card.cornerRadius)
      .attr('fill',   style.color)
      .attr('fill-opacity', 0.2)
      .attr('stroke', 'none');

    g.insert('svg', 'text')
      .attr('x', iconLeft)
      .attr('y', -card.iconSize / 2)
      .attr('width',  card.iconSize)
      .attr('height', card.iconSize)
      .attr('viewBox', '0 0 24 24')
      .style('color',   style.color)
      .html(getLucideIconBody(style.icon));

    if (isTruncated) 
    {
      g.on('mouseenter.label', function() 
      {
        textEl.text(d.label);
      })
        .on('mouseleave.label', function() 
        {
          textEl.text(shortLabel);
        });
    }
  }

  private _updateCollapseBadge(
    g: d3.Selection<SVGGElement, D3Node, null, undefined>,
    d: D3Node & { _hiddenChildCount?: number }
  ): void 
  {
    const count = d._hiddenChildCount || 0;
    
    let badge = g.select<SVGGElement>('g.collapse-badge');
    
    if (count > 0) 
    {
      if (badge.empty()) 
      {
        badge = g.append('g')
          .attr('class', 'collapse-badge')
          .attr('transform', `translate(${(d._hw ?? 0) - 2}, -${(d._hh ?? 0) - 2})`); // Top-right offset
          
        badge.append('circle')
          .attr('r', 7)
          .attr('fill', '#3b82f6') // bright blue
          .attr('cx', 0)
          .attr('cy', 0);

        badge.append('text')
          .attr('fill', '#ffffff')
          .attr('font-size', '8px')
          .attr('font-family', 'sans-serif')
          .attr('font-weight', 'bold')
          .attr('dominant-baseline', 'middle')
          .attr('text-anchor', 'middle')
          .attr('y', 0.5); // optical alignment
      }
      
      const badgeText = `${count}`;
      badge.select('text').text(badgeText);
    } 
    else 
    {
      badge.remove();
    }
  }
}
