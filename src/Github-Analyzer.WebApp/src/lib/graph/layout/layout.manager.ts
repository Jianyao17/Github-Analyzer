import * as d3 from 'd3';
import type { D3Node, D3Edge, GraphConfig } from '../types/_index';
import type { IGraphLayout } from './layout.interface';
import { StarBalloonLayout } from './star-balloon.layout';
import { HierarchicalLayout } from './hierarchical.layout';

export class LayoutManager
{
  private sim: d3.Simulation<D3Node, D3Edge>;
  private layout: IGraphLayout | null = null;
  private config: GraphConfig;
  private nodes: D3Node[];
  private edges: D3Edge[];
  private width: number;
  private height: number;

  constructor(
    nodes: D3Node[],
    edges: D3Edge[],
    width: number,
    height: number,
    config: GraphConfig
  )
  {
    this.nodes = nodes;
    this.edges = edges;
    this.config = config;
    this.width = width;
    this.height = height;

    const alphaMin = config.simulation?.alphaMin ?? 0.05;
    const alphaDecay = config.simulation?.alphaDecay ?? 0.03;

    this.sim = d3
      .forceSimulation<D3Node, D3Edge>(this.nodes)
      .alphaMin(alphaMin)
      .alphaDecay(alphaDecay)
      .force('collide',
        d3.forceCollide<D3Node>()
          .radius((d) => (d._radius ?? 5) + 2)
      );

    this.applyLayout();
  }

  public getSimulation(): d3.Simulation<D3Node, D3Edge>
  {
    return this.sim;
  }

  public updateData(nodes: D3Node[], edges: D3Edge[]): void
  {
    this.nodes = nodes;
    this.edges = edges;
    this.sim.nodes(this.nodes);
    
    const linkForce = this.sim.force('link') as d3.ForceLink<D3Node, D3Edge> | null;
    if (linkForce) {
      linkForce.links(this.edges);
    }
  }

  public recalculate(): void
  {
    this.applyLayout();
    this.reheat(0.3);
  }

  public reheat(alpha = 0.3): void
  {
    this.sim.alphaTarget(alpha).restart();
  }

  public cool(): void
  {
    this.sim.alphaTarget(0);
  }

  public destroy(): void
  {
    if (this.layout) {
      this.layout.destroy(this.sim);
    }

    const linkForce = this.sim.force('link') as d3.ForceLink<D3Node, D3Edge> | null;
    if (linkForce) linkForce.links([]);

    this.sim.force('charge', null);
    this.sim.force('center', null);
    this.sim.force('collide', null);
    this.sim.force('link', null);
    this.sim.nodes([]);
    this.sim.stop();
  }

  private applyLayout(): void
  {
    if (this.layout) {
      this.layout.destroy(this.sim);
    }

    const type = this.config.simulation?.layoutType ?? 'star-balloon';
    
    if (type === 'hierarchical') {
      this.layout = new HierarchicalLayout();
    } else {
      this.layout = new StarBalloonLayout();
    }

    this.layout.apply(this.sim, this.nodes, this.edges, this.width, this.height, this.config);
  }
}
