/**
 * Safely resolves a color from a style map.
 * Falls back to the 'default' key, then to a hardcoded gray.
 */
export function getColor(
  map: Record<string, { color: string }>,
  key: string, fallbackKey = 'default',
): string 
{
  return (map[key] ?? map[fallbackKey])?.color ?? '#9CA3AF';
}
