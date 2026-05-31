import type * as d3 from 'd3';

// ─── Domain Node & Edge Types ─────────────────────────────────────────────────
// Representasi dari node dan edge yang diterima dari backend.
// Field wajib selalu ada; field opsional boleh ditambah tanpa merusak kode lain.

export interface GraphNode
{
  // ── Wajib (selalu ada, dari backend) ──────────────────────────────────────
  pathId: string;
  label:  string;
  type:   number; // 0=Directory, 1=Namespace, 2=File, 3=Class, 4=Function

  // ── Opsional / extensible ──────────────────────────────────────────────────
  // Tambahkan field baru di sini. Kode yang tidak memakai field baru
  // tidak perlu diupdate karena field ini opsional.
  metadata?: Record<string, unknown>;
}

export interface GraphEdge
{
  // ── Wajib ─────────────────────────────────────────────────────────────────
  from: string;
  to:   string;
  type: number; // 0=BelongsTo, 1=Define, 2=Call, 3=Include

  // ── Opsional / extensible ──────────────────────────────────────────────────
  metadata?: Record<string, unknown>;
}

// ─── D3 Augmented Types ───────────────────────────────────────────────────────
// Extends domain types dengan properti D3 simulation.

export type D3Node = d3.SimulationNodeDatum & GraphNode &
{
  id:       string;  // Alias untuk pathId, required by forceLink id accessor
  _radius?: number;  // Dianotasi saat render, digunakan oleh collision force
};

export type D3Edge = d3.SimulationLinkDatum<D3Node> & GraphEdge;
