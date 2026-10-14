using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TimetableAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Faculty",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Enrollments_InstitutionId_Id_StudentId",
                table: "Enrollments",
                columns: new[] { "InstitutionId", "Id", "StudentId" });

            migrationBuilder.CreateTable(
                name: "Classrooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                    table.UniqueConstraint("AK_Classrooms_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_Classrooms_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.UniqueConstraint("AK_Subjects_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.UniqueConstraint("AK_Subjects_InstitutionId_Id_ProgrammeId_AcademicSessionId_Sem~", x => new { x.InstitutionId, x.Id, x.ProgrammeId, x.AcademicSessionId, x.Semester });
                    table.ForeignKey(
                        name: "FK_Subjects_AcademicSessions_InstitutionId_AcademicSessionId",
                        columns: x => new { x.InstitutionId, x.AcademicSessionId },
                        principalTable: "AcademicSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_Programmes_InstitutionId_ProgrammeId",
                        columns: x => new { x.InstitutionId, x.ProgrammeId },
                        principalTable: "Programmes",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    AttendanceSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSessions", x => x.Id);
                    table.UniqueConstraint("AK_ClassSessions_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_ClassSession_Time", "\"EndsAt\" > \"StartsAt\"");
                    table.ForeignKey(
                        name: "FK_ClassSessions_Classrooms_InstitutionId_ClassroomId",
                        columns: x => new { x.InstitutionId, x.ClassroomId },
                        principalTable: "Classrooms",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessions_Faculty_InstitutionId_FacultyId",
                        columns: x => new { x.InstitutionId, x.FacultyId },
                        principalTable: "Faculty",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessions_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSessions_Subjects_InstitutionId_SubjectId_ProgrammeId_~",
                        columns: x => new { x.InstitutionId, x.SubjectId, x.ProgrammeId, x.AcademicSessionId, x.Semester },
                        principalTable: "Subjects",
                        principalColumns: new[] { "InstitutionId", "Id", "ProgrammeId", "AcademicSessionId", "Semester" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassRosters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassRosters", x => x.Id);
                    table.UniqueConstraint("AK_ClassRosters_InstitutionId_ClassSessionId_StudentId", x => new { x.InstitutionId, x.ClassSessionId, x.StudentId });
                    table.UniqueConstraint("AK_ClassRosters_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_ClassRosters_ClassSessions_InstitutionId_ClassSessionId",
                        columns: x => new { x.InstitutionId, x.ClassSessionId },
                        principalTable: "ClassSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassRosters_Enrollments_InstitutionId_EnrollmentId_Student~",
                        columns: x => new { x.InstitutionId, x.EnrollmentId, x.StudentId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "InstitutionId", "Id", "StudentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassRosters_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.UniqueConstraint("AK_AttendanceRecords_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Attendance_Status", "\"Status\" IN ('Present','Absent','Excused')");
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_ClassRosters_InstitutionId_ClassSessionId~",
                        columns: x => new { x.InstitutionId, x.ClassSessionId, x.StudentId },
                        principalTable: "ClassRosters",
                        principalColumns: new[] { "InstitutionId", "ClassSessionId", "StudentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalStatus = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewNote = table.Column<string>(type: "text", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCorrections", x => x.Id);
                    table.UniqueConstraint("AK_AttendanceCorrections_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Correction_Separation", "\"ReviewedBy\" IS NULL OR \"ReviewedBy\" <> \"RequestedBy\"");
                    table.CheckConstraint("CK_Correction_Status", "\"Status\" IN ('Pending','Approved','Rejected')");
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_AttendanceRecords_InstitutionId_Atten~",
                        columns: x => new { x.InstitutionId, x.AttendanceRecordId },
                        principalTable: "AttendanceRecords",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_InstitutionId_UserId",
                table: "Faculty",
                columns: new[] { "InstitutionId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_UserId",
                table: "Faculty",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_InstitutionId_AttendanceRecordId",
                table: "AttendanceCorrections",
                columns: new[] { "InstitutionId", "AttendanceRecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_InstitutionId_ClassSessionId_StudentId",
                table: "AttendanceRecords",
                columns: new[] { "InstitutionId", "ClassSessionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_InstitutionId_Name",
                table: "Classrooms",
                columns: new[] { "InstitutionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassRosters_InstitutionId_EnrollmentId_StudentId",
                table: "ClassRosters",
                columns: new[] { "InstitutionId", "EnrollmentId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_InstitutionId_ClassroomId",
                table: "ClassSessions",
                columns: new[] { "InstitutionId", "ClassroomId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_InstitutionId_FacultyId",
                table: "ClassSessions",
                columns: new[] { "InstitutionId", "FacultyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSessions_InstitutionId_SubjectId_ProgrammeId_AcademicS~",
                table: "ClassSessions",
                columns: new[] { "InstitutionId", "SubjectId", "ProgrammeId", "AcademicSessionId", "Semester" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_InstitutionId_AcademicSessionId",
                table: "Subjects",
                columns: new[] { "InstitutionId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_InstitutionId_ProgrammeId_AcademicSessionId_Code",
                table: "Subjects",
                columns: new[] { "InstitutionId", "ProgrammeId", "AcademicSessionId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Faculty_AspNetUsers_UserId",
                table: "Faculty",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
                ALTER TABLE "ClassSessions" ADD CONSTRAINT "EX_Class_Room" EXCLUDE USING gist ("InstitutionId" WITH =, "ClassroomId" WITH =, tstzrange("StartsAt", "EndsAt", '[)') WITH &&) WHERE (NOT "IsCancelled");
                ALTER TABLE "ClassSessions" ADD CONSTRAINT "EX_Class_Faculty" EXCLUDE USING gist ("InstitutionId" WITH =, "FacultyId" WITH =, tstzrange("StartsAt", "EndsAt", '[)') WITH &&) WHERE (NOT "IsCancelled");
                ALTER TABLE "ClassSessions" ADD CONSTRAINT "EX_Class_Group" EXCLUDE USING gist ("InstitutionId" WITH =, "ProgrammeId" WITH =, "AcademicSessionId" WITH =, "Semester" WITH =, "Section" WITH =, tstzrange("StartsAt", "EndsAt", '[)') WITH &&) WHERE (NOT "IsCancelled");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faculty_AspNetUsers_UserId",
                table: "Faculty");

            migrationBuilder.DropTable(
                name: "AttendanceCorrections");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "ClassRosters");

            migrationBuilder.DropTable(
                name: "ClassSessions");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Faculty_InstitutionId_UserId",
                table: "Faculty");

            migrationBuilder.DropIndex(
                name: "IX_Faculty_UserId",
                table: "Faculty");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Enrollments_InstitutionId_Id_StudentId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Faculty");
        }
    }
}
