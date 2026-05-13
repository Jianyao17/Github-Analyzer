/**
 * Represents a branch in a GitHub repository.
 * Maps to the backend record: RepoBranch(string Name, string CommitHash)
 */
export interface RepoBranch {
  name: string;
  commitHash: string;
}

/**
 * Represents a commit in a GitHub repository.
 * Maps to the backend record: RepoCommit(string Hash, string Message, string Author, DateTimeOffset Date)
 */
export interface RepoCommit {
  hash: string;
  message: string;
  author: string;
  date: string; // ISO 8601 string from DateTimeOffset
}

/**
 * Response shape from the GET /api/projects/github/info endpoint.
 * Maps to the backend record: FetchRepoInfoResponse
 */
export interface FetchRepoInfoResponse {
  branches: RepoBranch[];
  commits: RepoCommit[] | null;
}
