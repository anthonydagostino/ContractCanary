using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OppSignal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelineStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "saved_notices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "saved_notices");
        }
    }
}
