using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Djambi.Api.Db.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddBotAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BotAssignmentsJson",
                table: "Games",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotAssignmentsJson",
                table: "Games");
        }
    }
}
