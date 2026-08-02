using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OppSignal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDigestOptOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DigestOptedOutAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DigestOptedOutAt",
                table: "AspNetUsers");
        }
    }
}
