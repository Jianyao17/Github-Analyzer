export class TextMeasurer 
{
  private _canvas: HTMLCanvasElement;
  private _ctx:    CanvasRenderingContext2D;
  private _cache = new Map<string, number>();

  constructor() 
  {
    this._canvas = document.createElement('canvas');
    this._ctx    = this._canvas.getContext('2d')!;
  }

  /**
   * Mengukur lebar teks tanpa menyentuh DOM / memicu reflow.
   * Hasil di-cache per kombinasi (text, font).
   *
   * @param text  String yang diukur
   * @param font  CSS font string, contoh: '12px "Segoe UI", sans-serif'
   */
  measure(text: string, font: string): number 
  {
    const key = `${font}|${text}`;
    if (this._cache.has(key)) return this._cache.get(key)!;

    this._ctx.font = font;
    const width    = this._ctx.measureText(text).width;
    this._cache.set(key, width);
    return width;
  }

  /** Bersihkan cache jika font config berubah. */
  clearCache(): void 
  {
    this._cache.clear();
  }
}
