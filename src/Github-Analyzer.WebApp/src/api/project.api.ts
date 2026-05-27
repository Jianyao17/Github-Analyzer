import apiClient, { baseURL } from './_axios';
import type { ApiRequestConfig } from './_axios';
import type { ApiResponse, ApiVersion } from '../types/_api/api';
import type { StatisticAnalysis } from '../types/analysis/statistic-analysis';
import type { CodeGraphAnalysis } from '../types/analysis/code-graph';
import type {
  ProjectResponse,
  RepoInfoResponse,
  StreamTokenResponse,
  CreateProjectRequest,
} from '../types/_api/project';

export async function createProjectApi(payload: CreateProjectRequest, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<ProjectResponse>>('/projects/new', payload)
    .then(res => res.data);
}

export async function fetchProjectsApi(version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<ProjectResponse[]>>('/projects')
    .then(res => res.data);
}

export async function fetchProjectApi(id: string, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<ProjectResponse>>(`/projects/${id}`)
    .then(res => res.data);
}

export async function fetchRepoInfoApi(githubUrl: string, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<RepoInfoResponse>>(`/projects/github/info?url=${encodeURIComponent(githubUrl)}`)
    .then(res => res.data);
}

export async function getCodeGraphAnalysisApi(id: string, version: ApiVersion = '1', config?: ApiRequestConfig) 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<CodeGraphAnalysis>>(`/projects/${id}/analysis?type=codegraph`, config)
    .then(res => res.data);
}

export async function getStatisticAnalysisApi(id: string, version: ApiVersion = '1', config?: ApiRequestConfig) 
{
  return await apiClient.withVersion(version)
    .get<ApiResponse<StatisticAnalysis>>(`/projects/${id}/analysis?type=statistic`, config)
    .then(res => res.data);
}

export async function issueStreamTokenApi(projectId: string, version: ApiVersion = '1') 
{
  return await apiClient.withVersion(version)
    .post<ApiResponse<StreamTokenResponse>>(`/projects/${projectId}/queue/stream-token`)
    .then(res => res.data);
}

export function getProjectQueueEventUrl(
  projectId: string, jobType: string,
  streamToken: string, version: ApiVersion = '1'
) 
{
  return `${baseURL}/api/v${version}/projects/${projectId}/queue/event`
    + `?job_type=${encodeURIComponent(jobType)}&stream_token=${encodeURIComponent(streamToken)}`;
}