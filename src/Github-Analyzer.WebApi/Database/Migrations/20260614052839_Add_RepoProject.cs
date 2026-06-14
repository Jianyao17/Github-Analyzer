using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GithubAnalyzer.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Add_RepoProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Repo");

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "Repo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RepositoryName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocalPath = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthorName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BranchName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastCommitHash = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastCommitAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeGraphAnalyses",
                schema: "Repo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Branch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CommitHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GraphJson = table.Column<JsonDocument>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_CodeGraphAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeGraphAnalyses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Repo",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CodeGraphAnalyses_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectQueues",
                schema: "Repo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectQueues_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Repo",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatisticAnalyses",
                schema: "Repo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_StatisticAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatisticAnalyses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Repo",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatisticAnalyses_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeGraphAnalyses_ProjectId",
                schema: "Repo",
                table: "CodeGraphAnalyses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeGraphAnalyses_UserId",
                schema: "Repo",
                table: "CodeGraphAnalyses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectQueues_CompletedAtUtc",
                schema: "Repo",
                table: "ProjectQueues",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectQueues_ProjectId_Status_Priority_JobType",
                schema: "Repo",
                table: "ProjectQueues",
                columns: new[] { "ProjectId", "Status", "Priority", "JobType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectQueues_ScheduledAtUtc",
                schema: "Repo",
                table: "ProjectQueues",
                column: "ScheduledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticAnalyses_ProjectId",
                schema: "Repo",
                table: "StatisticAnalyses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticAnalyses_UserId",
                schema: "Repo",
                table: "StatisticAnalyses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeGraphAnalyses",
                schema: "Repo");

            migrationBuilder.DropTable(
                name: "ProjectQueues",
                schema: "Repo");

            migrationBuilder.DropTable(
                name: "StatisticAnalyses",
                schema: "Repo");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "Repo");
        }
    }
}
