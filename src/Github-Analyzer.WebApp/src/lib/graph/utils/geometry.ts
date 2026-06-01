// ─── Graph Geometry Utilities ─────────────────────────────────────────────────

/**
 * Calculates the point on the boundary of an axis-aligned rectangle
 * that lies along the line from (sx, sy) to the rectangle's center (tx, ty).
 *
 * Used to shorten D3 edge lines so they terminate at the node card border
 * instead of at the node center — required for accurate arrow placement on
 * rectangular nodes.
 *
 * @param sx      Source node center X
 * @param sy      Source node center Y
 * @param tx      Target node center X
 * @param ty      Target node center Y
 * @param hw      Target rectangle half-width
 * @param hh      Target rectangle half-height
 * @param margin  Extra gap (in SVG units) between the boundary and the endpoint.
 *                Use for arrow clearance. Default: 0.
 *
 * @returns  The point on (or near) the target rect boundary, along the source → target line.
 */
export function getRectEdgeEndpoint(
  sx: number, sy: number,
  tx: number, ty: number,
  hw: number, hh: number,
  margin = 0,
): { x: number; y: number }
{
  const dx = tx - sx;
  const dy = ty - sy;

  // Guard: coincident nodes — return target center
  if (dx === 0 && dy === 0) return { x: tx, y: ty };

  // Scale factor to reach each axis boundary from target center toward source.
  // We add margin to push the endpoint slightly outside the rect boundary.
  const scaleX = (hw + margin) / Math.abs(dx);
  const scaleY = (hh + margin) / Math.abs(dy);

  // The smaller scale hits the nearest boundary face first
  const scale = Math.min(scaleX, scaleY);

  return {
    x: tx - dx * scale,
    y: ty - dy * scale,
  };
}
