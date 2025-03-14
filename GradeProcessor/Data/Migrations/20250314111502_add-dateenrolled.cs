using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeProcessor.Data.Migrations
{
    /// <inheritdoc />
    public partial class adddateenrolled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateEnrolled",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateEnrolled",
                table: "Students");
        }
    }
}
