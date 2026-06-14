using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GithubAnalyzer.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Add_RepoAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StatisticAnalyses_CommitHash",
                schema: "Repo",
                table: "StatisticAnalyses",
                column: "CommitHash");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticAnalyses_GeneratedAtUtc",
                schema: "Repo",
                table: "StatisticAnalyses",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CodeGraphAnalyses_CommitHash",
                schema: "Repo",
                table: "CodeGraphAnalyses",
                column: "CommitHash");

            migrationBuilder.CreateIndex(
                name: "IX_CodeGraphAnalyses_GeneratedAtUtc",
                schema: "Repo",
                table: "CodeGraphAnalyses",
                column: "GeneratedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StatisticAnalyses_CommitHash",
                schema: "Repo",
                table: "StatisticAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_StatisticAnalyses_GeneratedAtUtc",
                schema: "Repo",
                table: "StatisticAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_CodeGraphAnalyses_CommitHash",
                schema: "Repo",
                table: "CodeGraphAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_CodeGraphAnalyses_GeneratedAtUtc",
                schema: "Repo",
                table: "CodeGraphAnalyses");
        }
    }
}
