using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecurringTimetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TimetableSeriesId",
                table: "ClassSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TimetableSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FirstDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DaysOfWeek = table.Column<int[]>(type: "integer[]", nullable: false),
                    ExcludedDates = table.Column<DateOnly[]>(type: "date[]", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableSeries", x => x.Id);
                    table.UniqueConstraint("AK_TimetableSeries_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_TimetableSeries_Classrooms_InstitutionId_ClassroomId",
                        columns: x => new { x.InstitutionId, x.ClassroomId },
                        principalTable: "Classrooms",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableSeries_Faculty_InstitutionId_FacultyId",
                        columns: x => new { x.InstitutionId, x.FacultyId },
                        principalTable: "Faculty",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableSeries_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimetableSeries_Subjects_InstitutionId_SubjectId",
                        columns: x => new { x.InstitutionId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_InstitutionId_TimetableSeriesId",
                table: "ClassSessions",
                columns: new[] { "InstitutionId", "TimetableSeriesId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSeries_InstitutionId_ClassroomId",
                table: "TimetableSeries",
                columns: new[] { "InstitutionId", "ClassroomId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSeries_InstitutionId_CreatedAt",
                table: "TimetableSeries",
                columns: new[] { "InstitutionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSeries_InstitutionId_FacultyId",
                table: "TimetableSeries",
                columns: new[] { "InstitutionId", "FacultyId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSeries_InstitutionId_SubjectId",
                table: "TimetableSeries",
                columns: new[] { "InstitutionId", "SubjectId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSessions_TimetableSeries_InstitutionId_TimetableSeries~",
                table: "ClassSessions",
                columns: new[] { "InstitutionId", "TimetableSeriesId" },
                principalTable: "TimetableSeries",
                principalColumns: new[] { "InstitutionId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassSessions_TimetableSeries_InstitutionId_TimetableSeries~",
                table: "ClassSessions");

            migrationBuilder.DropTable(
                name: "TimetableSeries");

            migrationBuilder.DropIndex(
                name: "IX_ClassSessions_InstitutionId_TimetableSeriesId",
                table: "ClassSessions");

            migrationBuilder.DropColumn(
                name: "TimetableSeriesId",
                table: "ClassSessions");
        }
    }
}
