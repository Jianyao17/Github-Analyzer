import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig, IGraphRenderer, INodeRenderer } from '../graph.types';
import { NodeRenderer } from './node.renderer';
import { EdgeRenderer } from './edge.renderer';

/**
 * D3Renderer — orchestrates SVG setup and delegates rendering to sub-renderers.
 *
 * Responsibilities:
 *   - Create and own the root <svg> and <g class="viewport">
 *   - Render edges first (z-order: edges below nodes)
 *   - Proxy tick updates to sub-renderers
 *
 * Does NOT contain: business logic, selection state, or interaction handling.
 */
export class D3Renderer implements IGraphRenderer 
{
  private svg: d3.Selection<SVGSVGElement, unknown, null, undefined> | null = null;
  private viewport: d3.Selection<SVGGElement, unknown, null, undefined> | null = null;

  // Sub-renderers — exposed as readonly so plugins can access their selections
  readonly nodeRenderer: NodeRenderer & INodeRenderer = new NodeRenderer();
  readonly edgeRenderer: EdgeRenderer = new EdgeRenderer();

  /** Creates the root SVG and viewport group inside the given container. */
  init(container: HTMLElement): void 
  {
    // Clear any pre-existing SVG (safe to call on re-render after destroy)
    d3.select(container).selectAll('svg').remove();

    const width = container.clientWidth || 800;
    const height = container.clientHeight || 600;

    this.svg = d3
      .select(container)
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%')
      .attr('viewBox', `0 0 ${width} ${height}`);

    // All rendered content lives inside viewport so zoom transform applies cleanly
    this.viewport = this.svg.append('g').attr('class', 'viewport');
  }

  /**
   * Renders edges then nodes (z-order: edges are drawn below nodes).
   * Must be called after init().
   */
  render(nodes: D3Node[], edges: D3Edge[], config: GraphConfig): void 
  {
    if (!this.svg || !this.viewport) return;

    this.edgeRenderer.render(this.viewport, this.svg, edges, config);
    this.nodeRenderer.render(this.viewport, nodes, config);
  }

  /** Proxies position updates to sub-renderers. Called on every simulation tick. */
  onTick(): void 
  {
    this.edgeRenderer.updatePositions();
    this.nodeRenderer.updatePositions();
  }

  /** Returns the root SVG selection, used by plugins to add custom elements. */
  getSvg(): d3.Selection<SVGSVGElement, unknown, null, undefined> | null 
  {
    return this.svg;
  }

  /** Returns the viewport group selection, used by plugins to add custom elements. */
  getViewport(): d3.Selection<SVGGElement, unknown, null, undefined> | null 
  {
    return this.viewport;
  }

  /**
   * Removes the SVG from the DOM and resets internal references.
   * The D3Renderer instance itself is NOT destroyed — it can be reused
   * by calling init() + render() again (e.g. after GraphD3.update()).
   */
  destroy(): void 
  {
    this.svg?.remove();
    this.svg = null;
    this.viewport = null;
    this.nodeRenderer.clear();
    this.edgeRenderer.clear();
  }
}
