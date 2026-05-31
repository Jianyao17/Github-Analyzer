import type { GraphNode, GraphEdge } from './node-edge';

// ─── GraphData Indexes ────────────────────────────────────────────────────────
// Pre-built lookup structures untuk akses data yang efisien.

export interface GraphDataIndexes
{
  /** pathId → GraphNode. Untuk lookup O(1) by ID. */
  nodeIndex: Map<string, GraphNode>;

  /**
   * pathId → GraphEdge[] keluar dari node itu.
   * Untuk traversal: "siapa yang node ini tunjuk?"
   */
  edgesBySource: Map<string, GraphEdge[]>;

  /**
   * pathId → GraphEdge[] masuk ke node itu.
   * Untuk reverse traversal: "siapa yang menunjuk node ini?"
   */
  edgesByTarget: Map<string, GraphEdge[]>;

  /** type number → GraphNode[]. Untuk filter / group by type. */
  nodesByType: Map<number, GraphNode[]>;
}

// ─── GraphData Metadata ───────────────────────────────────────────────────────

export interface GraphDataMetadata
{
  nodeCount: number;
  edgeCount: number;

  /** Extensible — tambahkan field baru tanpa mengubah type ini. */
  [key: string]: unknown;
}

// ─── GraphData ────────────────────────────────────────────────────────────────
// Data utama yang diterima dari backend dan digunakan sebagai referensi
// read-only oleh plugin. Plugin tidak boleh mengubah data ini.

export interface GraphData
{
  // ── Raw data (dari backend) ────────────────────────────────────────────────
  nodes:          GraphNode[];
  sourceRelEdges: GraphEdge[];
  useRelEdges:    GraphEdge[];

  // ── Pre-built indexes ─────────────────────────────────────────────────────
  readonly indexes:  GraphDataIndexes;

  // ── Metadata ──────────────────────────────────────────────────────────────
  readonly metadata: GraphDataMetadata;
}
