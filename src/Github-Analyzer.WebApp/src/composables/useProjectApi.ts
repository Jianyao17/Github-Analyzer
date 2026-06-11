import type { ApiVersion } from '../types/_api/api';
import type { CodeGraph } from '../types/analysis/code-graph';
import type { StatisticAnalysis } from '../types/analysis/statistic-analysis';
import type {
  CreateProjectRequest,
  StreamTokenResponse
} from '../types/_api/project';

import {
  createProjectApi,
  fetchProjectsApi,
  fetchProjectApi,
  fetchRepoInfoApi,
  getCodeGraphAnalysisApi,
  getStatisticAnalysisApi,
  getProjectQueueEventUrl,
  issueStreamTokenApi,
  renameProjectApi,
  deleteProjectApi,
} from '../api/project.api';


export interface ProgressEvent {
  jobType: string;
  status: string;
  progress: number;
  message: string;
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
export const useProjectApi = (version: ApiVersion = '1') => 
{
  const createProject = async (payload: CreateProjectRequest) => 
  {
    const response = await createProjectApi(payload, version);
    if (!response.data) 
    {
      throw new Error('Failed to create project.');
    }
    return response.data;
  };

  const fetchProjects = async () => 
  {
    const response = await fetchProjectsApi(version);
    return response.data ?? [];
  };

  const fetchProject = async (id: string) => 
  {
    const response = await fetchProjectApi(id, version);
    return response.data;
  };

  const renameProject = async (id: string, title: string) => 
  {
    const response = await renameProjectApi(id, title, version);
    return response.data;
  };

  const deleteProject = async (id: string) => 
  {
    const response = await deleteProjectApi(id, version);
    return response.data;
  };

  const fetchRepoInfo = async (githubUrl: string) => 
  {
    const response = await fetchRepoInfoApi(githubUrl, version);
    return response.data;
  };

  const getCodeGraphAnalysis = async (id: string) => 
  {
    const response = await getCodeGraphAnalysisApi(id, version, { suppressToast: true });
    const payload = response.data;
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

        (payload as any).graphData = 
        {
          nodes: rawNodes.map((n: any) => 
            ({
              pathId: n.pathId || n.PathId,
              label: n.label || n.Label,
              type: n.type !== undefined ? n.type : n.Type,
              startLine: n.startLine ?? n.StartLine,
              endLine: n.endLine ?? n.EndLine
            })),
          sourceRelEdges: rawSourceEdges.map((e: any) => 
            ({
              from: e.from || e.From,
              to: e.to || e.To,
              type: e.type !== undefined ? e.type : e.Type
            })),
          useRelEdges: rawUseEdges.map((e: any) => 
            ({
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
      const response = await getStatisticAnalysisApi(id, version, { suppressToast: true });
      return response.data;
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
    const response = await issueStreamTokenApi(projectId, version);
    if (!response.data) 
    {
      throw new Error('Failed to issue stream token.');
    }
    return response.data;
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
    const url = getProjectQueueEventUrl(projectId, jobType, streamToken, version);

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
    streamQueueProgress,
    renameProject,
    deleteProject
  };
};
