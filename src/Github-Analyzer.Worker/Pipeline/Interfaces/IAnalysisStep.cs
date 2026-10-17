using GithubAnalyzer.Shared.Enums;

namespace GithubAnalyzer.Worker.Pipeline;

/// <summary>
/// Kontrak untuk sebuah tahapan analisis dalam pipeline.
/// </summary>
public interface IAnalysisStep
{
    /// <summary>
    /// Tipe analisis yang ditangani oleh tahapan ini.
    /// </summary>
    AnalysisType AnalysisType { get; }

    /// <summary>
    /// Menentukan apakah tahapan ini harus dijalankan berdasarkan opsi job yang diminta.
    /// </summary>
    bool ShouldExecute(PipelineContext context);

    /// <summary>
    /// Menjalankan tahapan analisis.
    /// </summary>
    Task ExecuteAsync(PipelineContext context, CancellationToken ct = default);
}
