import type { CodeGraph, CodeGraphAnalysis } from '../types/code-graph';
import type { StatisticAnalysis } from '../types/statistic-analysis';
import type { ApiResponse } from '../types/api-response';
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
  hasStatistic: boolean;
  hasCodeGraph: boolean;
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

export interface StreamTokenResponse {
  token: string;
  expiresAt: string; // ISO 8601 UTC
}

/**
 * Options untuk streamQueueProgress.
 */
export interface StreamProgressOptions {
  onComplete?: () => void;
  onError?: (err: any) => void;
  /**
   * Token yang sudah di-fetch sebelumnya via issueStreamToken().
   * Jika tidak disediakan, token akan di-fetch otomatis (1 request per stream).
   * Gunakan ini untuk berbagi 1 token antara beberapa stream dalam 1 project.
   */
  token?: string;
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
      `/projects/${id}/analysis?type=codegraph`,
      { suppressToast: true }
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
        `/projects/${id}/analysis?type=statistic`,
        { suppressToast: true }
      );
      return response.data.data;
    }
    catch 
    {
      return null;
    }
  };

  /**
   * Menerbitkan ephemeral stream token (berlaku 5 menit) untuk mengakses
   * SSE queue progress endpoint. Menggunakan JWT standar via axios interceptor.
   */
  const issueStreamToken = async (projectId: string): Promise<StreamTokenResponse> =>
  {
    const response = await client.post<ApiResponse<StreamTokenResponse>>(
      `/projects/${projectId}/queue/stream-token`
    );
    if (!response.data.data)
    {
      throw new Error('Failed to issue stream token.');
    }
    return response.data.data;
  };

  /**
   * Membuka koneksi SSE untuk memonitor progress queue job.
   *
   * Jika `options.token` tidak disediakan, token baru akan di-fetch otomatis
   * via `issueStreamToken()`. Untuk berbagi 1 token antara beberapa stream
   * dalam 1 project, fetch token terlebih dahulu lalu teruskan via `options.token`.
   *
   * @returns Promise yang resolve ke cleanup function untuk menutup EventSource.
   */
  const streamQueueProgress = async (
    projectId: string,
    jobType: string,
    onUpdate: (event: ProgressEvent) => void,
    options?: StreamProgressOptions
  ): Promise<() => void> =>
  {
    const { onComplete, onError, token: preIssuedToken } = options ?? {};

    // Gunakan token yang sudah ada, atau fetch token baru jika tidak disediakan
    const streamToken = preIssuedToken ?? (await issueStreamToken(projectId)).token;

    // Buka SSE connection dengan stream token sebagai query param
    // EventSource tidak bisa mengirim Authorization header, sehingga
    // ephemeral token (scope sempit, 5 menit) digunakan sebagai pengganti.
    const url = `${client.baseURL}/api/v${client.version}/projects/${projectId}/queue/event`
      + `?job_type=${encodeURIComponent(jobType)}&stream_token=${encodeURIComponent(streamToken)}`;

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
    issueStreamToken,
    streamQueueProgress
  };
};
