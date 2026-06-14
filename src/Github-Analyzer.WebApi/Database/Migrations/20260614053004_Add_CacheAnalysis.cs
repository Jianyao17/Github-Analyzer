using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GithubAnalyzer.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Add_CacheAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Cache");

            migrationBuilder.CreateTable(
                name: "CodeGraphCaches",
                schema: "Cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LookupKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RepoUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Branch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CommitHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GraphJson = table.Column<string>(type: "jsonb", nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    EdgeCount = table.Column<int>(type: "integer", nullable: false),
                    AnalysisVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeGraphCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatisticCaches",
                schema: "Cache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LookupKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RepoUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Branch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CommitHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalFolders = table.Column<int>(type: "integer", nullable: true),
                    TotalFiles = table.Column<int>(type: "integer", nullable: true),
                    SizeInBytes = table.Column<int>(type: "integer", nullable: true),
                    TotalLinesOfCode = table.Column<long>(type: "bigint", nullable: true),
                    CodeLines = table.Column<long>(type: "bigint", nullable: true),
                    CommentLines = table.Column<long>(type: "bigint", nullable: true),
                    BlankLines = table.Column<long>(type: "bigint", nullable: true),
                    TotalCommits = table.Column<int>(type: "integer", nullable: true),
                    TotalContributors = table.Column<int>(type: "integer", nullable: true),
                    TotalBranches = table.Column<int>(type: "integer", nullable: true),
                    AnalysisVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticCaches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeGraphCaches_CommitHash",
                schema: "Cache",
                table: "CodeGraphCaches",
                column: "CommitHash");

            migrationBuilder.CreateIndex(
                name: "IX_CodeGraphCaches_LookupKey",
                schema: "Cache",
                table: "CodeGraphCaches",
                column: "LookupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatisticCaches_CommitHash",
                schema: "Cache",
                table: "StatisticCaches",
                column: "CommitHash");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticCaches_LookupKey",
                schema: "Cache",
                table: "StatisticCaches",
                column: "LookupKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeGraphCaches",
                schema: "Cache");

            migrationBuilder.DropTable(
                name: "StatisticCaches",
                schema: "Cache");
        }
    }
}
