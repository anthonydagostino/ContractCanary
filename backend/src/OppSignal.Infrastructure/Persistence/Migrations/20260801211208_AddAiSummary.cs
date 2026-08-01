using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OppSignal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiAttempts",
                table: "notices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AiFitNote",
                table: "notices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiGeneratedAt",
                table: "notices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiKeyPoints",
                table: "notices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModel",
                table: "notices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "notices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notices_AiGeneratedAt",
                table: "notices",
                column: "AiGeneratedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notices_AiGeneratedAt",
                table: "notices");

            migrationBuilder.DropColumn(
                name: "AiAttempts",
                table: "notices");

            migrationBuilder.DropColumn(
                name: "AiFitNote",
                table: "notices");

            migrationBuilder.DropColumn(
                name: "AiGeneratedAt",
                table: "notices");

            migrationBuilder.DropColumn(
                name: "AiKeyPoints",
                table: "notices");

            migrationBuilder.DropColumn(
                name: "AiModel",
                table: "notices");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "notices");
        }
    }
}
