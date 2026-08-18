using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentGradeApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeNotificationSent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GradeNotificationSent",
                table: "StudentSubjects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GradeNotificationSent",
                table: "StudentSubjects");
        }
    }
}
