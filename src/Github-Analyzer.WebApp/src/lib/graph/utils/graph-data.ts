import type { GraphData, GraphDataIndexes, GraphDataMetadata } from '../types/graph-data';
import type { GraphNode, GraphEdge } from '../types/node-edge';
import type { CodeGraph } from '@/types/analysis/code-graph';

// ─── buildGraphData ───────────────────────────────────────────────────────────

/**
 * Mengkonversi raw CodeGraph dari backend menjadi GraphData yang sudah
 * dilengkapi dengan pre-built indexes dan metadata.
 *
 * Selalu gunakan helper ini — jangan buat GraphData secara manual.
 *
 * @param raw    Raw CodeGraph data dari API response.
 * @param extra  Metadata tambahan (branch, commitHash, dll).
 *
 * @example
 * const graphData = buildGraphData(JSON.parse(analysis.graphJson), {
 *   branch:     analysis.branch,
 *   commitHash: analysis.commitHash,
 * });
 */
export function buildGraphData(
  raw:   CodeGraph,
  extra: Partial<GraphDataMetadata> = {},
): GraphData
{
  const allEdges = [...raw.sourceRelEdges, ...raw.useRelEdges];

  // ── Build indexes ─────────────────────────────────────────────────────────
  const nodeIndex     = new Map<string, GraphNode>();
  const edgesBySource = new Map<string, GraphEdge[]>();
  const edgesByTarget = new Map<string, GraphEdge[]>();
  const nodesByType   = new Map<number, GraphNode[]>();

  for (const node of raw.nodes)
  {
    nodeIndex.set(node.pathId, node);
    const typeGroup = nodesByType.get(node.type);

    if (typeGroup) typeGroup.push(node);
    else nodesByType.set(node.type, [node]);
  }

  for (const edge of allEdges)
  {
    const bySource = edgesBySource.get(edge.from);

    if (bySource) bySource.push(edge);
    else edgesBySource.set(edge.from, [edge]);

    const byTarget = edgesByTarget.get(edge.to);
    
    if (byTarget) byTarget.push(edge);
    else edgesByTarget.set(edge.to, [edge]);
  }

  const indexes: GraphDataIndexes =
  {
    nodeIndex,
    edgesBySource,
    edgesByTarget,
    nodesByType,
  };

  const metadata: GraphDataMetadata =
  {
    nodeCount: raw.nodes.length,
    edgeCount: allEdges.length,
    ...extra,
  };

  return {
    nodes: raw.nodes,
    sourceRelEdges: raw.sourceRelEdges,
    useRelEdges: raw.useRelEdges,
    indexes,
    metadata,
  };
}
