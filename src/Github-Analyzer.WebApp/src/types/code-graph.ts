// Representasi dari GraphNode dan GraphEdge dari backend
export interface GraphNode {
  pathId: string;
  label: string;
  type: number; // 0=Directory, 1=Namespace, 2=File, 3=Class, 4=Function
}

export interface GraphEdge {
  from: string;
  to: string;
  type: number; // 0=BelongsTo, 1=Define, 2=Call, 3=Include
}

export interface CodeGraph {
  nodes: GraphNode[];
  sourceRelEdges: GraphEdge[];
  useRelEdges: GraphEdge[];
}

export interface CodeGraphAnalysis {
  id: string;
  projectId: string;
  branch: string | null;
  commitHash: string | null;
  generatedAtUtc: string | null;
  graphJson: string; // Serialized CodeGraph
  nodeCount: number;
  edgeCount: number;
}
