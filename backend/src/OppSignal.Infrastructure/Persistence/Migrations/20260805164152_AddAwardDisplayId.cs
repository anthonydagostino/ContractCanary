using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OppSignal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAwardDisplayId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayAwardId",
                table: "awards",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayAwardId",
                table: "awards");
        }
    }
}
