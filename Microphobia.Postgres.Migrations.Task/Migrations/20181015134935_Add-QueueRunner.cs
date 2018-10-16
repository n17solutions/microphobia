using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace N17Solutions.Microphobia.Postgres.Migrations.Task.Migrations
{
    public partial class AddQueueRunner : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                schema: "microphobia",
                table: "TaskInfo",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QueueRunner",
                schema: "microphobia",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    DateLastUpdated = table.Column<DateTime>(nullable: false),
                    ResourceId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    IsRunning = table.Column<bool>(nullable: false),
                    LastTaskProcessed = table.Column<DateTime>(nullable: true),
                    DateRegistered = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueRunner", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueueRunner",
                schema: "microphobia");

            migrationBuilder.DropColumn(
                name: "Tags",
                schema: "microphobia",
                table: "TaskInfo");
        }
    }
}
