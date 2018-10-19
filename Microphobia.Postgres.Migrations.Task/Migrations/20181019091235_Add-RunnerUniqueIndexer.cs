using Microsoft.EntityFrameworkCore.Migrations;

namespace N17Solutions.Microphobia.Postgres.Migrations.Task.Migrations
{
    public partial class AddRunnerUniqueIndexer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UniqueIndexer",
                schema: "microphobia",
                table: "QueueRunner",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UniqueIndexer",
                schema: "microphobia",
                table: "QueueRunner");
        }
    }
}
