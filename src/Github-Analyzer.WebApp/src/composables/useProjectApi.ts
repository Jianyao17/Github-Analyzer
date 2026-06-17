import { unref, type Ref } from 'vue';
import { useQuery, useMutation, useQueryCache } from '@pinia/colada';
import type { CreateProjectRequest, StreamTokenResponse } from '@/types/_api/project';
import type { CodeGraph } from '@/types/analysis/code-graph';
import type { ApiVersion } from '@/types/_api/api';

import {
  createProjectApi,
  fetchProjectsApi,
  fetchProjectApi,
  fetchRepoInfoApi,
  getCodeGraphAnalysisApi,
  getStatisticAnalysisApi,
  getProjectSourceContentApi,
  getProjectQueueEventUrl,
  issueStreamTokenApi,
  renameProjectApi,
  deleteProjectApi,
} from '@/api/project.api';


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
  const queryCache = useQueryCache();

  const useProjectsQuery = () => useQuery(
    {
      key: ['projects', version],
      query: async () => 
      {
        const response = await fetchProjectsApi(version);
        return response.data ?? [];
      },
      staleTime: 0
    });

  const useProjectQuery = (id: Ref<string> | string) => useQuery(
    {
      key: () => ['project', unref(id), version],
      query: async () => 
      {
        const response = await fetchProjectApi(unref(id), version);
        if (!response.data) throw new Error('Project not found');
        return response.data;
      },
      staleTime: 1000 * 60 * 5 // TTL 5 minutes
    });

  
  const useRepoInfoQuery = (githubUrl: string) => useQuery(
    {
      key: ['repo-info', githubUrl, version],
      query: async () => 
      {
        const response = await fetchRepoInfoApi(githubUrl, version);
        return response.data;
      },
      staleTime: 1000 * 60 * 1 // TTL 1 minutes
    });
  
  const useCodeGraphQuery = (id: Ref<string> | string) => useQuery({
    key: () => ['analysis', 'codegraph', unref(id), version],
    query: async () => 
    {
      const response = await getCodeGraphAnalysisApi(unref(id), version, { suppressToast: true });
      const payload = response.data;
      if (payload && payload.graphJson) 
      {
        try 
        {
          let parsed: any = payload.graphJson;
          if (typeof parsed === 'string') parsed = JSON.parse(parsed);
          if (typeof parsed === 'string') parsed = JSON.parse(parsed);

          const rawNodes = parsed.nodes || parsed.Nodes || [];
          const rawSourceEdges = parsed.sourceRelEdges || parsed.SourceRelEdges || [];
          const rawUseEdges = parsed.useRelEdges || parsed.UseRelEdges || [];

          return {
            nodes: rawNodes.map((n: any) => ({
              pathId: n.pathId || n.PathId,
              label: n.label || n.Label,
              type: n.type !== undefined ? n.type : n.Type,
              startLine: n.startLine ?? n.StartLine,
              endLine: n.endLine ?? n.EndLine
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
      return null;
    },
    staleTime: 1000 * 60 * 5 // TTL 5 minutes
  });

  const useStatisticQuery = (id: Ref<string> | string) => useQuery(
    {
      key: () => ['analysis', 'statistic', unref(id), version],
      query: async () => 
      {
        try 
        {
          const response = await getStatisticAnalysisApi(unref(id), version, { suppressToast: true });
          return response.data ?? null;
        }
        catch 
        {
          return null;
        }
      },
      staleTime: 1000 * 60 * 5 // TTL 5 minutes
    });

  const useCreateProjectMutation = () => useMutation(
    {
      mutation: async (payload: CreateProjectRequest) => 
      {
        const response = await createProjectApi(payload, version);
        if (!response.data) throw new Error('Failed to create project.');
        return response.data;
      },
      onSuccess: () => 
      {
        queryCache.invalidateQueries({ key: ['projects'] });
      }
    });

  const useRenameProjectMutation = () => useMutation(
    {
      mutation: async (params: { id: string, title: string }) => 
      {
        const response = await renameProjectApi(params.id, params.title, version);
        return response.data;
      },
      onSuccess: (_, params) => 
      {
        queryCache.invalidateQueries({ key: ['projects'] });
        queryCache.invalidateQueries({ key: ['project', params.id] });
      }
    });

  const useDeleteProjectMutation = () => useMutation(
    {
      mutation: async (id: string) => 
      {
        const response = await deleteProjectApi(id, version);
        return response.data;
      },
      onSuccess: () => 
      {
        queryCache.invalidateQueries({ key: ['projects'] });
      }
    });

  /**
   * Menerbitkan ephemeral stream token (berlaku 5 menit) untuk mengakses
   * SSE queue progress endpoint. Menggunakan JWT standar via axios interceptor.
   */
  const issueStreamToken = async (projectId: string): Promise<StreamTokenResponse> => 
  {
    const response = await issueStreamTokenApi(projectId, version);
    if (!response.data) throw new Error('Failed to issue stream token.');
    return response.data;
  };

  /**
   * Membuka koneksi SSE untuk memonitor progress queue job.
   */
  const streamQueueProgress = async (
    projectId: string,
    jobType: string,
    onUpdate: (event: ProgressEvent) => void,
    options?: StreamProgressOptions
  ): Promise<() => void> => 
  {
    const { onComplete, onError, token: preIssuedToken } = options ?? {};
    const streamToken = preIssuedToken ?? (await issueStreamToken(projectId)).token;
    const url = getProjectQueueEventUrl(projectId, jobType, streamToken, version);
    const eventSource = new EventSource(url);

    eventSource.onmessage = (e) => 
    {
      try 
      {
        const raw = JSON.parse(e.data);
        let statusStr = raw.Status ?? raw.status;
        if (typeof raw.Status === 'number') 
        {
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

    return () => eventSource.close();
  };

  const getProjectSourceContent = async (projectId: string, path: string): Promise<{path: string; content: string}> => 
  {
    const response = await getProjectSourceContentApi(projectId, path, version, { suppressToast: true });
    return response;
  };

  return {
    useProjectsQuery,
    useProjectQuery,
    useCodeGraphQuery,
    useStatisticQuery,
    useRepoInfoQuery,
    useCreateProjectMutation,
    useRenameProjectMutation,
    useDeleteProjectMutation,
    getProjectSourceContent,
    issueStreamToken,
    streamQueueProgress,
  };
};
