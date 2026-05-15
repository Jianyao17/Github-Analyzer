// Hasil analisis statistik dari backend StatisticAnalysis entity
export interface StatisticAnalysis {
  id: string
  projectId: string
  branch: string | null
  commitHash: string | null
  generatedAtUtc: string | null

  // Structural
  totalFolders: number | null
  totalFiles: number | null
  sizeInBytes: number | null

  // Code lines
  totalLinesOfCode: number | null
  codeLines: number | null
  commentLines: number | null
  blankLines: number | null

  // Git (from GitHub API — may be null if API unreachable)
  totalCommits: number | null
  totalContributors: number | null
  totalBranches: number | null
}
