using Microsoft.EntityFrameworkCore.Migrations;

namespace Server.Migrations
{
    public partial class itemEquip : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEquiped",
                table: "Item",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEquiped",
                table: "Item");
        }
    }
}
