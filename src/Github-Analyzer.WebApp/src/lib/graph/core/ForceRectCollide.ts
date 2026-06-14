import * as d3 from 'd3';
import type { D3Node } from '../types/node-edge';

/**
 * A custom D3 force for Axis-Aligned Bounding Box (AABB) collisions.
 * Uses _hw (half-width) and _hh (half-height) properties of nodes to perform rectangle collisions,
 * replacing the default circular `d3.forceCollide`.
 *
 * @param padding Extra padding space around the rectangle.
 * @param iterations Number of times to run the resolution per tick. Higher = more stable but slower.
 */
export function forceRectCollide(
  padding: number = 4,
  iterations: number = 2
) 
{
  let nodes: D3Node[];
  let strength = 1; // Collision strength/elasticity

  // Default fallback values if a node hasn't been rendered yet
  const fallbackHw = 40;
  const fallbackHh = 16;

  function force(alpha: number) 
  {
    const tree = d3.quadtree<D3Node>()
      .x(d => d.x ?? 0)
      .y(d => d.y ?? 0)
      .addAll(nodes);

    // Optimize: Precompute the maximum half-width and half-height for each quadrant
    // so we can prune the search tree effectively (O(N log N) instead of O(N^2)).
    tree.visitAfter((quad: any) => 
    {
      if (quad.data) 
      {
        quad.maxHw = (quad.data._hw ?? fallbackHw) + padding;
        quad.maxHh = (quad.data._hh ?? fallbackHh) + padding;
      } 
      else 
      {
        quad.maxHw = 0;
        quad.maxHh = 0;
        for (let i = 0; i < 4; ++i) 
        {
          if (quad[i]) 
          {
            if (quad[i].maxHw > quad.maxHw) quad.maxHw = quad[i].maxHw;
            if (quad[i].maxHh > quad.maxHh) quad.maxHh = quad[i].maxHh;
          }
        }
      }
    });

    for (let i = 0; i < iterations; ++i) 
    {
      for (const d of nodes) 
      {
        const hwA = (d._hw ?? fallbackHw) + padding;
        const hhA = (d._hh ?? fallbackHh) + padding;

        const nx1 = (d.x ?? 0) - hwA;
        const ny1 = (d.y ?? 0) - hhA;
        const nx2 = (d.x ?? 0) + hwA;
        const ny2 = (d.y ?? 0) + hhA;

        tree.visit((quad: any, x1, y1, x2, y2) => 
        {
          if (!quad.maxHw && !quad.maxHh) return true; // Empty quad

          const maxHwB = quad.maxHw;
          const maxHhB = quad.maxHh;

          // Prune: if this quadrant is completely outside the node's bounds + quadrant max bounds
          if (x1 > nx2 + maxHwB || x2 < nx1 - maxHwB || y1 > ny2 + maxHhB || y2 < ny1 - maxHhB) 
          {
            return true;
          }
          
          if (!quad.length) 
          { // Leaf node
            const d2 = quad.data;
            if (d2 && d2 !== d) 
            {
              let dx = (d.x ?? 0) - (d2.x ?? 0);
              let dy = (d.y ?? 0) - (d2.y ?? 0);

              if (dx === 0) dx = (Math.random() - 0.5) * 1e-6;
              if (dy === 0) dy = (Math.random() - 0.5) * 1e-6;

              const hwB = (d2._hw ?? fallbackHw) + padding;
              const hhB = (d2._hh ?? fallbackHh) + padding;

              const minDx = hwA + hwB;
              const minDy = hhA + hhB;

              const absDx = Math.abs(dx);
              const absDy = Math.abs(dy);

              if (absDx < minDx && absDy < minDy) 
              {
                const overlapX = minDx - absDx;
                const overlapY = minDy - absDy;

                if (overlapX < overlapY) 
                {
                  const repulseX = (overlapX * Math.sign(dx)) * strength * alpha;
                  d.x! += repulseX / 2;
                  d2.x! -= repulseX / 2;
                } 
                else 
                {
                  const repulseY = (overlapY * Math.sign(dy)) * strength * alpha;
                  d.y! += repulseY / 2;
                  d2.y! -= repulseY / 2;
                }
              }
            }
          }
          
          return false;
        });
      }
    }
  }

  force.initialize = function(_nodes: D3Node[]) 
  {
    nodes = _nodes;
  };

  force.iterations = function(_: number) 
  {
    return arguments.length ? ((iterations = +_), force) : iterations;
  };

  force.strength = function(_: number) 
  {
    return arguments.length ? ((strength = +_), force) : strength;
  };

  return force;
}
