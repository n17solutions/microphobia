﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace N17Solutions.Microphobia.Postgres.Migrations.Task.Migrations
{
    [DbContext(typeof(TaskContext))]
    partial class TaskContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("N17Solutions.Microphobia.Domain.Clients.QueueRunner", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("DateCreated");

                    b.Property<DateTime>("DateLastUpdated");

                    b.Property<DateTime>("DateRegistered");

                    b.Property<bool>("IsRunning");

                    b.Property<DateTime?>("LastTaskProcessed");

                    b.Property<string>("Name");

                    b.Property<Guid>("ResourceId");

                    b.Property<string>("Tag");

                    b.Property<int>("UniqueIndexer");

                    b.HasKey("Id");

                    b.ToTable("QueueRunner","microphobia");
                });

            modelBuilder.Entity("N17Solutions.Microphobia.Domain.Tasks.TaskInfo", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AssemblyName");

                    b.Property<string>("Data");

                    b.Property<DateTime>("DateCreated");

                    b.Property<DateTime>("DateLastUpdated");

                    b.Property<string>("FailureDetails");

                    b.Property<string>("MethodName");

                    b.Property<Guid>("ResourceId");

                    b.Property<string>("ReturnType");

                    b.Property<int>("Status");

                    b.Property<string>("Tags");

                    b.Property<string>("TypeName");

                    b.HasKey("Id");

                    b.ToTable("TaskInfo","microphobia");
                });
#pragma warning restore 612, 618
        }
    }
}
