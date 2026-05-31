import type { GraphData } from './graph-data';
import type { GraphView } from './graph-view';

// ─── GraphPlugin ──────────────────────────────────────────────────────────────

export interface GraphPlugin
{
  /**
   * Identifier unik — digunakan untuk mencegah duplikasi registrasi plugin.
   */
  readonly name: string;

  /**
   * Dipanggil setelah render() dan simulation siap.
   *
   * @param data  GraphData — referensi read-only ke data CodeGraph + indexes.
   *              Plugin boleh membaca ini tapi TIDAK boleh mengubahnya.
   * @param view  GraphView — state view yang mutable.
   *              Plugin memodifikasi tampilan dan behavior melalui ini.
   */
  setup(data: GraphData, view: GraphView): void;

  /**
   * Dipanggil saat destroy() dan sebelum setiap update().
   * Bersihkan event listeners dan referensi di sini.
   */
  teardown?(): void;
}
