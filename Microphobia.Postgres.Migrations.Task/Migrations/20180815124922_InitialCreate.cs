using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace N17Solutions.Microphobia.Postgres.Migrations.Task.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "microphobia");

            migrationBuilder.CreateTable(
                name: "TaskInfo",
                schema: "microphobia",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DateCreated = table.Column<DateTime>(nullable: false),
                    DateLastUpdated = table.Column<DateTime>(nullable: false),
                    ResourceId = table.Column<Guid>(nullable: false),
                    AssemblyName = table.Column<string>(nullable: true),
                    TypeName = table.Column<string>(nullable: true),
                    MethodName = table.Column<string>(nullable: true),
                    ReturnType = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    FailureDetails = table.Column<string>(nullable: true),
                    Data = table.Column<string>(nullable: true),
                    IsAsync = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskInfo", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskInfo",
                schema: "microphobia");
        }
    }
}
