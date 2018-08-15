using Microsoft.EntityFrameworkCore.Migrations;

namespace N17Solutions.Microphobia.Postgres.Migrations
{
    public partial class IsAsyncColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAsync",
                schema: "microphobia",
                table: "TaskInfo",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAsync",
                schema: "microphobia",
                table: "TaskInfo");
        }
    }
}
