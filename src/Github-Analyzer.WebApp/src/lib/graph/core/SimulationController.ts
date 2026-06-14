import * as d3 from 'd3';
import { forceRectCollide } from './ForceRectCollide';
import type { D3Node, D3Edge } from '../types/node-edge';
import type { GraphConfig } from '../types/config';
import type { Dimensions } from '../types/events';
import type { EventBus } from './EventBus';

/**
 * Manages the D3 force simulation physics for the graph engine.
 * Handles node collisions, links, charge (repulsion), and centering forces.
 * Listens to events like drag or layout changes to reheat/restart the simulation.
 */
export class SimulationController 
{
  private _sim:    d3.Simulation<D3Node, D3Edge> | null = null;
  private _bus:    EventBus;
  private _config: GraphConfig;

  constructor(bus: EventBus, config: GraphConfig) 
  {
    this._bus    = bus;
    this._config = config;
    this._listenBusEvents();
  }

  get sim(): d3.Simulation<D3Node, D3Edge> | null 
  {
    return this._sim;
  }

  /**
   * Starts or restarts the D3 simulation with a new set of nodes and edges.
   * 
   * @param nodes The array of D3 node objects.
   * @param edges The array of D3 edge objects (type 2 edges are excluded from physics).
   * @param dims  The dimensions of the viewport for the centering force.
   * @param onTick The callback function triggered on every simulation tick.
   */
  start(
    nodes:    D3Node[],
    edges:    D3Edge[],
    dims:     Dimensions,
    onTick:   () => void,
  ): void 
  {
    this.stop();

    const { 
      linkDistance   = 60, 
      chargeStrength = -150, 
      alphaMin       = 0.005, 
      alphaDecay     = 0.025 
    } = this._config.simulation ?? {};

    this._sim = d3
      .forceSimulation<D3Node, D3Edge>(nodes)
      .alphaMin(alphaMin)
      .alphaDecay(alphaDecay)
      .force('link',
        d3.forceLink<D3Node, D3Edge>(edges.filter(e => e.type !== 2))
          .id(d => d.id).distance(linkDistance),
      )
      .force('charge',  d3.forceManyBody().strength(chargeStrength))
      .force('center',  d3.forceCenter(dims.width / 2, dims.height / 2))
      .force('collide', forceRectCollide(16, 2))
      .on('end',  () => this._bus.emit('simulation:settled', undefined as never))
      .on('tick', onTick); // Direct call, not via bus
  }

  /**
   * Stops the active simulation gracefully.
   * Cleans up forces and detaches nodes to prevent memory leaks.
   */
  stop(): void 
  {
    if (!this._sim) return;
    (this._sim.force('link') as d3.ForceLink<D3Node, D3Edge> | null)?.links([]);
    this._sim.force('charge',  null);
    this._sim.force('center',  null);
    this._sim.force('collide', null);
    this._sim.force('link',    null);
    this._sim.nodes([]);
    this._sim.stop();
    this._sim = null;
  }

  private _listenBusEvents(): void 
  {
    this._bus.on('simulation:reheat', (payload) => 
    {
      const alpha = payload?.alpha ?? 0.3;
      this._sim?.alphaTarget(alpha).restart();
    });
    
    this._bus.on('simulation:cool', () => 
    {
      this._sim?.alphaTarget(0);
    });

    this._bus.on('render:snap-positions', ({ positions }) => 
    {
      if (!this._sim) return;
      
      this._sim.force('layoutX', 
        d3.forceX<D3Node>().x(d => positions.get(d.id)?.x ?? 0)
          .strength(0.5));

      this._sim.force('layoutY', 
        d3.forceY<D3Node>().y(d => positions.get(d.id)?.y ?? 0)
          .strength(0.5));

      this._sim.force('charge', null);
      this._sim.force('center', null);

      // Use custom link distance to maintain relative distances between nodes
      const linkForce = this._sim.force('link') as d3.ForceLink<D3Node, D3Edge> | undefined;
      if (linkForce) 
      {
        linkForce.distance(link => 
        {
          const sourceId = String(typeof link.source === 'object' ? link.source.id : link.source);
          const targetId = String(typeof link.target === 'object' ? link.target.id : link.target);
          const p1 = positions.get(sourceId);
          const p2 = positions.get(targetId);
          if (p1 && p2) 
          {
            // Calculate the distance between the source and target nodes
            return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2));
          }
          return 60;
        });
      }

      const nodes = this._sim.nodes();
      for (const node of nodes) 
      {
        const target = positions.get(node.id);
        if (target) 
        {
          node.x  = target.x;
          node.y  = target.y;
          node.fx = null;
          node.fy = null;
        }
      }
      this._sim.alphaTarget(0.1).restart();
    });

    this._bus.on('render:tween-positions', ({ positions }) => 
    {
      if (!this._sim) return;

      this._sim.force('layoutX', 
        d3.forceX<D3Node>().x(d => positions.get(d.id)?.x ?? 0)
          .strength(0.5));

      this._sim.force('layoutY', 
        d3.forceY<D3Node>().y(d => positions.get(d.id)?.y ?? 0)
          .strength(0.5));

      this._sim.force('charge', null);
      this._sim.force('center', null);

      // Use custom link distance to maintain relative distances between nodes
      const linkForce = this._sim.force('link') as d3.ForceLink<D3Node, D3Edge> | undefined;
      if (linkForce) 
      {
        linkForce.distance(link => 
        {
          const sourceId = String(typeof link.source === 'object' ? link.source.id : link.source);
          const targetId = String(typeof link.target === 'object' ? link.target.id : link.target);
          const p1 = positions.get(sourceId);
          const p2 = positions.get(targetId);
          if (p1 && p2) 
          {
            // Calculate the distance between the source and target nodes
            return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2));
          }
          return 60;
        });
      }

      const nodes = this._sim.nodes();
      const interpolators = nodes.map(d => 
      {
        const target = positions.get(d.id);
        if (!target) return null;
      
        return {
          node: d,
          ix: d3.interpolateNumber(d.x ?? 0, target.x),
          iy: d3.interpolateNumber(d.y ?? 0, target.y)
        };
      })
        .filter(Boolean) as 
      { 
        node: D3Node, 
        ix: (t: number) => number, 
        iy: (t: number) => number 
      }[];

      if (interpolators.length === 0) return;

      d3.transition()
        .duration(750)
        .ease(d3.easeCubicOut)
        .tween('layout', () => (t: number) => 
        {
          for (const { node, ix, iy } of interpolators) 
          {
            node.x = ix(t);
            node.y = iy(t);
            // Fix positions during tween so simulation forces don't interfere
            node.fx = node.x;
            node.fy = node.y;
          }
        })
        .on('end', () => 
        {
          // Release fixed positions after tween. The layoutX and layoutY anchors will hold them elastically!
          for (const { node } of interpolators) 
          {
            node.fx = null;
            node.fy = null;
          }
          this._sim?.alphaTarget(0.1).restart();
        });
    });
  }
}
