using Microsoft.EntityFrameworkCore.Migrations;

namespace N17Solutions.Microphobia.Postgres.Migrations.Task.Migrations
{
    public partial class RemoveIsAsyncColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAsync",
                schema: "microphobia",
                table: "TaskInfo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAsync",
                schema: "microphobia",
                table: "TaskInfo",
                nullable: false,
                defaultValue: false);
        }
    }
}
