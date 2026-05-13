import apiClient from '../api/axios';
import { useAuthStore } from '../stores/auth.store';
import type { CodeGraph, CodeGraphAnalysis } from '../types/code-graph';

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

export const useProjectApi = () => {
  const createProject = async (payload: CreateProjectRequest) => {
    const response = await apiClient.post<ProjectResponse>('/projects/new', payload);
    return response.data;
  };

  const fetchProjects = async () => {
    const response = await apiClient.get<ProjectResponse[]>('/projects');
    return response.data;
  };

  const fetchProject = async (id: string) => {
    const response = await apiClient.get<ProjectResponse>(`/projects/${id}`);
    return response.data;
  };

  const fetchRepoInfo = async (githubUrl: string) => {
    const response = await apiClient.get<RepoInfoResponse>(`/projects/github/info?url=${encodeURIComponent(githubUrl)}`);
    return response.data;
  };

  const getCodeGraphAnalysis = async (id: string) => {
    const response = await apiClient.get<CodeGraphAnalysis>(`/projects/${id}/analysis/code-graph`);
    // Parse the graphJson automatically for convenience
    if (response.data && response.data.graphJson) {
      try {
         let parsed: any = response.data.graphJson;
         if (typeof parsed === 'string') {
            parsed = JSON.parse(parsed);
         }
         if (typeof parsed === 'string') {
            parsed = JSON.parse(parsed);
         }
         
         const rawNodes = parsed.nodes || parsed.Nodes || [];
         const rawSourceEdges = parsed.sourceRelEdges || parsed.SourceRelEdges || [];
         const rawUseEdges = parsed.useRelEdges || parsed.UseRelEdges || [];

         (response.data as any).graphData = {
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
      } catch (e) {
         console.error('Failed to parse graphJson', e);
      }
    }
    return response.data;
  };

  const getStatisticAnalysis = async (id: string) => {
    const response = await apiClient.get<any>(`/projects/${id}/analysis/statistic`);
    return response.data;
  };

  const streamQueueProgress = (
    projectId: string, 
    jobType: string, 
    onUpdate: (event: ProgressEvent) => void,
    onComplete?: () => void,
    onError?: (err: any) => void
  ) => {
    const authStore = useAuthStore();
    const token = authStore.token;
    
    // Construct the URL using the api base URL. Since apiClient handles prefixes, we will manually do it here.
    const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5242';
    const url = `${apiBaseUrl}/api/projects/${projectId}/queue/event?job_type=${jobType}&token=${token}`;
    
    const eventSource = new EventSource(url);
    
    eventSource.onmessage = (e) => {
      try {
        const data = JSON.parse(e.data) as ProgressEvent;
        onUpdate(data);
        if (data.status === 'Completed' || data.status === 'Failed') {
          eventSource.close();
          if (onComplete) onComplete();
        }
      } catch (err) {
        console.error('Failed to parse SSE data', err);
      }
    };

    eventSource.onerror = (err) => {
      eventSource.close();
      if (onError) onError(err);
    };

    return () => {
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
