import { ref, computed } from 'vue';
import type { FetchRepoInfoResponse, RepoBranch, RepoCommit } from '../types/repo-info';
import type { ApiResponse } from '../types/api-response';
import apiClient from '../api/axios';

/**
 * Composable untuk fetch info branch dan commit dari GitHub repository.
 * Menggunakan endpoint GET /api/v{version}/projects/github/info
 *
 * @param version - Versi API yang digunakan, e.g. '1', '2'. Default: '1'.
 *
 * @example
 * const { fetchBranches, fetchCommits } = useRepoInfo()     // menggunakan v1
 * const { fetchBranches }               = useRepoInfo('2')  // menggunakan v2
 */
export const useRepoInfo = (version = '1') => 
{
  const client = apiClient.withVersion(version);

  const branches = ref<RepoBranch[]>([]);
  const commits = ref<RepoCommit[]>([]);

  const isFetchingBranches = ref(false);
  const isFetchingCommits = ref(false);
  const fetchError = ref<string | null>(null);

  /** Apakah ada branch yang berhasil di-fetch */
  const hasBranches = computed(() => branches.value.length > 0);
  /** Apakah ada commit yang berhasil di-fetch */
  const hasCommits = computed(() => commits.value.length > 0);

  /**
   * Fetch branches dari repository.
   * Juga mereset commits ke array kosong.
   */
  const fetchBranches = async (repoUrl: string): Promise<void> => 
  {
    if (!repoUrl.trim()) return;

    isFetchingBranches.value = true;
    fetchError.value = null;
    branches.value = [];
    commits.value = [];

    try 
    {
      const response = await client.get<ApiResponse<FetchRepoInfoResponse>>(
        `/projects/github/info?repoUrl=${encodeURIComponent(repoUrl)}`
      );
      branches.value = response.data.data?.branches ?? [];
    }
    catch (err: any) 
    {
      fetchError.value = err?.message ?? 'Gagal mengambil daftar branch.';
    }
    finally 
    {
      isFetchingBranches.value = false;
    }
  };

  /**
   * Fetch commits dari branch yang dipilih.
   * Requires repoUrl dan branchName yang valid.
   */
  const fetchCommits = async (repoUrl: string, branchName: string): Promise<void> => 
  {
    if (!repoUrl.trim() || !branchName.trim()) return;

    isFetchingCommits.value = true;
    fetchError.value = null;
    commits.value = [];

    try 
    {
      const response = await client.get<ApiResponse<FetchRepoInfoResponse>>(
        `/projects/github/info?repoUrl=${encodeURIComponent(repoUrl)}&branch=${encodeURIComponent(branchName)}`
      );
      commits.value = response.data.data?.commits ?? [];
    }
    catch (err: any) 
    {
      fetchError.value = err?.message ?? 'Gagal mengambil daftar commit.';
    }
    finally 
    {
      isFetchingCommits.value = false;
    }
  };

  /** Reset semua state ke kondisi awal */
  const reset = () => 
  {
    branches.value = [];
    commits.value = [];
    fetchError.value = null;
    isFetchingBranches.value = false;
    isFetchingCommits.value = false;
  };

  return {
    branches,
    commits,
    isFetchingBranches,
    isFetchingCommits,
    fetchError,
    hasBranches,
    hasCommits,
    fetchBranches,
    fetchCommits,
    reset,
  };
};
