using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GithubAnalyzer.WebApi.Database.Migrations
{
    /// <inheritdoc />
    public partial class Update_ProjectQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectQueues_ProjectId_Status_Priority_JobType",
                schema: "Repo",
                table: "ProjectQueues");

            migrationBuilder.DropColumn(
                name: "JobType",
                schema: "Repo",
                table: "ProjectQueues");

            migrationBuilder.AddColumn<string>(
                name: "Options",
                schema: "Repo",
                table: "ProjectQueues",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserId",
                schema: "Repo",
                table: "Projects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectQueues_ProjectId_Status_Priority",
                schema: "Repo",
                table: "ProjectQueues",
                columns: new[] { "ProjectId", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectQueues_StartedAtUtc",
                schema: "Repo",
                table: "ProjectQueues",
                column: "StartedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_UserId",
                schema: "Repo",
                table: "Projects",
                column: "UserId",
                principalSchema: "Auth",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_UserId",
                schema: "Repo",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_UserId",
                schema: "Repo",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectQueues_ProjectId_Status_Priority",
                schema: "Repo",
                table: "ProjectQueues");

            migrationBuilder.DropIndex(
                name: "IX_ProjectQueues_StartedAtUtc",
                schema: "Repo",
                table: "ProjectQueues");

            migrationBuilder.DropColumn(
                name: "Options",
                schema: "Repo",
                table: "ProjectQueues");

            migrationBuilder.AddColumn<string>(
                name: "JobType",
                schema: "Repo",
                table: "ProjectQueues",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectQueues_ProjectId_Status_Priority_JobType",
                schema: "Repo",
                table: "ProjectQueues",
                columns: new[] { "ProjectId", "Status", "Priority", "JobType" });
        }
    }
}
