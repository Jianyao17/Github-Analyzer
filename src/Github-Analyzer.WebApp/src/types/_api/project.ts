export interface CreateProjectRequest {
  repoUrl: string;
  branch: string;
  commitHash?: string;
}

export interface ProjectResponse {
  id: string;
  title: string;
  repositoryName: string;
  repositoryUrl: string;
  branchName: string | null;
  lastCommitHash: string | null;
  createdAtUtc: string;
  hasStatistic: boolean;
  hasCodeGraph: boolean;
}

export interface RepoInfoResponse {
  name: string;
  defaultBranch: string;
  description: string | null;
  url: string;
}

export interface StreamTokenResponse {
  token: string;
  expiresAt: string; // ISO 8601 UTC
}