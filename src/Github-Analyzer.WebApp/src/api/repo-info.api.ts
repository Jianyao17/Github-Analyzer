import apiClient from './_axios';
import type { ApiResponse, ApiVersion } from '../types/_api/api';
import type { FetchRepoInfoResponse } from '../types/_api/repo-info';

export async function getRepoBranchesApi(repoUrl: string, version: ApiVersion = '1') 
{
  const repoUrlEncoded = encodeURIComponent(repoUrl);

  return await apiClient.withVersion(version)
    .get<ApiResponse<FetchRepoInfoResponse>>(`/projects/github/info?repoUrl=${repoUrlEncoded}`)
    .then(res => res.data);
}

export async function getRepoCommitsApi(repoUrl: string, branchName: string, version: ApiVersion = '1') 
{
  const repoUrlEncoded = encodeURIComponent(repoUrl);
  const branchNameEncoded = encodeURIComponent(branchName);

  return await apiClient.withVersion(version)
    .get<ApiResponse<FetchRepoInfoResponse>>(`/projects/github/info?repoUrl=${repoUrlEncoded}&branch=${branchNameEncoded}`)
    .then(res => res.data);
}