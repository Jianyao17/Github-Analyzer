import * as d3 from 'd3';
import type { GraphPlugin, GraphData, GraphView } from '@graph.types';

/**
 * ZoomDragPlugin — menambahkan drag behavior pada node.
 *
 * Pan/zoom pada SVG sudah ditangani secara built-in oleh GraphView.
 * Plugin ini hanya bertugas mengurus user interaction: drag node.
 *
 * - Drag: fix posisi node saat drag, lepas saat selesai
 * - Menggunakan view.reheat() dan view.cool() dari GraphView
 */
export class ZoomDragPlugin implements GraphPlugin
{
  readonly name = 'zoom-drag';

  setup(_data: GraphData, view: GraphView): void
  {
    if (!view.nodeSelection) return;

    view.nodeSelection
      .attr('cursor', 'grab')
      .call(
        d3.drag<SVGGElement, any>()
          .on('start', (event, d) =>
          {
            if (!event.active) view.reheat();
            d.fx = d.x;
            d.fy = d.y;
          })
          .on('drag', (event, d) =>
          {
            d.fx = event.x;
            d.fy = event.y;
          })
          .on('end', (event, d) =>
          {
            if (!event.active) view.cool();
            d.fx = null;
            d.fy = null;
          }),
      );
  }

  teardown?(): void {}
}
