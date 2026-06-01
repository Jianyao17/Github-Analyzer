// ─── Truncation ───────────────────────────────────────────────────────────────

/**
 * Truncates a label to at most `maxWords` words.
 * Appends '...' (3 chars) when truncated.
 * If the label has fewer or equal words than `maxWords`, it is returned as-is.
 *
 * Word boundaries are determined by matching common programming naming
 * conventions (camelCase, PascalCase, snake_case, kebab-case, dots, paths).
 * Separators are naturally preserved.
 *
 * @param label     The original label string.
 * @param maxWords  Maximum words to keep. Default: 2.
 *
 * @example
 * truncateLabel('getUserServiceHandler')     // 'getUser...'
 * truncateLabel('UserService')               // 'UserService'
 * truncateLabel('com.example.UserService')   // 'com.example...'
 * truncateLabel('src/utils/label')           // 'src/utils...'
 * truncateLabel('get_user_data')             // 'get_user...'
 * truncateLabel('Utf8Valid_WithComments')    // 'Utf8Valid...'
 */
export function truncateLabel(label: string, maxWords = 3): string
{
  // Match programming words:
  // 1. [A-Z]?[a-z0-9]+  (camelCase/PascalCase word, e.g. "User", "get", "Utf8")
  // 2. [A-Z]+(?![a-z])  (Acronyms, e.g. "XML", "HTTP")
  const regex = /[A-Z]?[a-z0-9]+|[A-Z]+(?![a-z])/g;

  let match;
  let wordCount = 0;
  let cutIndex  = -1;

  while ((match = regex.exec(label)) !== null)
  {
    wordCount++;
    if (wordCount === maxWords)
    {
      cutIndex = match.index + match[0].length;
      break;
    }
  }

  // Jika jumlah kata kurang/sama dengan maxWords, atau regex gagal matching
  if (cutIndex === -1 || cutIndex === label.length)
  {
    return label;
  }

  return label.substring(0, cutIndex) + '...';
}
