using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryToOutboxMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "OutboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Error",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "OutboxMessages");
        }
    }
}
