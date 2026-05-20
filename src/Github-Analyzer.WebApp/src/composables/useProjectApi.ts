import type { CodeGraph, CodeGraphAnalysis } from '../types/code-graph';
import type { StatisticAnalysis } from '../types/statistic-analysis';
import type { ApiResponse } from '../types/api-response';
import { useAuthStore } from '../stores/auth.store';
import apiClient from '../api/axios';

export interface CreateProjectRequest {
  repoUrl: string;
  branch: string;
  commitHash?: string;
}

export interface ProjectResponse {
  id: string;
  repositoryName: string;
  repositoryUrl: string;
  branchName: string | null;
  lastCommitHash: string | null;
  createdAtUtc: string;
}

export interface RepoInfoResponse {
  name: string;
  defaultBranch: string;
  description: string | null;
  url: string;
}

export interface ProgressEvent {
  jobType: string;
  status: string;
  progress: number;
  message: string;
}

/**
 * Composable untuk Project API.
 * @param version - Versi API yang digunakan, e.g. '1', '2'. Default: '1'.
 *
 * @example
 * const { fetchProjects, createProject } = useProjectApi()      // menggunakan v1
 * const { fetchProjects }               = useProjectApi('2')    // menggunakan v2
 */
export const useProjectApi = (version = '1') => 
{
  const client = apiClient.withVersion(version);

  const createProject = async (payload: CreateProjectRequest) => 
  {
    const response = await client.post<ApiResponse<ProjectResponse>>('/projects/new', payload);
    if (!response.data.data)
    {
      throw new Error('Failed to create project.');
    }
    return response.data.data;
  };

  const fetchProjects = async () => 
  {
    const response = await client.get<ApiResponse<ProjectResponse[]>>('/projects');
    return response.data.data ?? [];
  };

  const fetchProject = async (id: string) => 
  {
    const response = await client.get<ApiResponse<ProjectResponse>>(`/projects/${id}`);
    return response.data.data;
  };

  const fetchRepoInfo = async (githubUrl: string) => 
  {
    const response = await client.get<ApiResponse<RepoInfoResponse>>(
      `/projects/github/info?url=${encodeURIComponent(githubUrl)}`
    );
    return response.data.data;
  };

  const getCodeGraphAnalysis = async (id: string) => 
  {
    const response = await client.get<ApiResponse<CodeGraphAnalysis>>(
      `/projects/${id}/analysis/code-graph`
    );
    const payload = response.data.data;
    // Parse the graphJson automatically for convenience
    if (payload && payload.graphJson) 
    {
      try 
      {
        let parsed: any = payload.graphJson;
        if (typeof parsed === 'string') 
        {
          parsed = JSON.parse(parsed);
        }
        if (typeof parsed === 'string') 
        {
          parsed = JSON.parse(parsed);
        }
         
        const rawNodes = parsed.nodes || parsed.Nodes || [];
        const rawSourceEdges = parsed.sourceRelEdges || parsed.SourceRelEdges || [];
        const rawUseEdges = parsed.useRelEdges || parsed.UseRelEdges || [];

        (payload as any).graphData = {
          nodes: rawNodes.map((n: any) => ({
            pathId: n.pathId || n.PathId,
            label: n.label || n.Label,
            type: n.type !== undefined ? n.type : n.Type
          })),
          sourceRelEdges: rawSourceEdges.map((e: any) => ({
            from: e.from || e.From,
            to: e.to || e.To,
            type: e.type !== undefined ? e.type : e.Type
          })),
          useRelEdges: rawUseEdges.map((e: any) => ({
            from: e.from || e.From,
            to: e.to || e.To,
            type: e.type !== undefined ? e.type : e.Type
          }))
        } as CodeGraph;
      }
      catch (e) 
      {
        console.error('Failed to parse graphJson', e);
      }
    }
    return payload;
  };

  const getStatisticAnalysis = async (id: string): Promise<StatisticAnalysis | null> => 
  {
    try 
    {
      const response = await client.get<ApiResponse<StatisticAnalysis>>(
        `/projects/${id}/analysis/statistic`
      );
      return response.data.data;
    }
    catch 
    {
      return null;
    }
  };

  const streamQueueProgress = (
    projectId: string, 
    jobType: string, 
    onUpdate: (event: ProgressEvent) => void,
    onComplete?: () => void,
    onError?: (err: any) => void
  ) => 
  {
    const authStore = useAuthStore();
    const token = authStore.token;
    
    // Construct the SSE URL manually — EventSource doesn't go through axios.
    // Uses client.baseURL and client.version from the VersionedClient.
    const url = `${client.baseURL}/api/v${client.version}/projects/${projectId}/queue/event?job_type=${jobType}&token=${token}`;
    
    const eventSource = new EventSource(url);
    
    eventSource.onmessage = (e) => 
    {
      try 
      {
        const raw = JSON.parse(e.data);
        
        // Memetakan tipe data C# (PascalCase, Status Enum int) ke Typescript interface (camelCase)
        let statusStr = raw.Status ?? raw.status;
        if (typeof raw.Status === 'number') 
        {
          // Asumsi pemetaan Status Enum .NET:
          if (raw.Status === 3) statusStr = 'Completed';
          else if (raw.Status === 4) statusStr = 'Failed';
          else if (raw.Progress >= 100) statusStr = 'Completed';
          else statusStr = 'Processing';
        }
        else if (raw.Progress >= 100) 
        {
          statusStr = 'Completed';
        }

        const data: ProgressEvent = {
          jobType: raw.JobType ?? raw.jobType,
          status: statusStr,
          progress: raw.Progress ?? raw.progress ?? 0,
          message: raw.Message ?? raw.message ?? ''
        };

        onUpdate(data);
        if (data.status === 'Completed' || data.status === 'Failed') 
        {
          eventSource.close();
          if (onComplete) onComplete();
        }
      }
      catch (err) 
      {
        console.error('Failed to parse SSE data', err);
      }
    };

    eventSource.onerror = (err) => 
    {
      eventSource.close();
      if (onError) onError(err);
    };

    return () => 
    {
      eventSource.close();
    };
  };

  return {
    createProject,
    fetchProjects,
    fetchProject,
    fetchRepoInfo,
    getCodeGraphAnalysis,
    getStatisticAnalysis,
    streamQueueProgress
  };
};
