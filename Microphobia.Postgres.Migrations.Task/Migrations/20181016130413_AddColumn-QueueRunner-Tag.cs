using Microsoft.EntityFrameworkCore.Migrations;

namespace N17Solutions.Microphobia.Postgres.Migrations.Task.Migrations
{
    public partial class AddColumnQueueRunnerTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tag",
                schema: "microphobia",
                table: "QueueRunner",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tag",
                schema: "microphobia",
                table: "QueueRunner");
        }
    }
}
