using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Djambi.Api.Db.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnTimeLimitSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "RulesetKindId",
                table: "Games",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "TurnTimeLimitSeconds",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "EventKinds",
                columns: new[] { "EventKindId", "Name" },
                values: new object[] { (byte)10, "TurnSkipped" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventKinds",
                keyColumn: "EventKindId",
                keyValue: (byte)10);

            migrationBuilder.DropColumn(
                name: "RulesetKindId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TurnTimeLimitSeconds",
                table: "Games");
        }
    }
}
