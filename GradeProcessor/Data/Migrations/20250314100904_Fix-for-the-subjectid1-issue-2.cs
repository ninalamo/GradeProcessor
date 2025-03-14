using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeProcessor.Data.Migrations;

/// <inheritdoc />
public partial class Fixforthesubjectid1issue2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Sections_SubjectId",
            table: "Sections",
            column: "SubjectId");

        migrationBuilder.AddForeignKey(
            name: "FK_Sections_Subjects_SubjectId",
            table: "Sections",
            column: "SubjectId",
            principalTable: "Subjects",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Sections_Subjects_SubjectId",
            table: "Sections");

        migrationBuilder.DropIndex(
            name: "IX_Sections_SubjectId",
            table: "Sections");
    }
}
