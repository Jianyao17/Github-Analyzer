import type { GraphContext } from '@graph/core/GraphContext';
import type { GraphData }    from './graph-data';

export interface GraphPlugin 
{
  readonly name: string;

  /**
   * Priority teardown (opsional, informational).
   * PluginRegistry menggunakan nilai saat registrasi, bukan dari sini.
   */
  readonly priority?: number;

  /**
   * Dipanggil setelah render selesai dan simulation berjalan.
   * @param ctx   GraphContext stabil — aman disimpan sebagai field plugin.
   * @param data  GraphData read-only — jangan mutasi.
   */
  setup(ctx: GraphContext, data: GraphData): void;

  /**
   * Hook middleware untuk transformasi data graph.
   * Digunakan untuk memfilter atau mengubah ctx.nodes dan ctx.edges sebelum layout dan render.
   * @param ctx  GraphContext yang berisi nodes dan edges yang bisa dimodifikasi.
   * @param data GraphData sumber yang read-only.
   */
  transform?(ctx: GraphContext, data: GraphData): void;

  /** Bersihkan semua event listener dan referensi eksternal. */
  teardown?(): void;
}
