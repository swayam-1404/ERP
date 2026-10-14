using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StudentFacultyCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faculty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculty", x => x.Id);
                    table.UniqueConstraint("AK_Faculty_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_Faculty_Departments_InstitutionId_DepartmentId",
                        columns: x => new { x.InstitutionId, x.DepartmentId },
                        principalTable: "Departments",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Faculty_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Relationship = table.Column<string>(type: "text", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                    table.UniqueConstraint("AK_Guardians_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_Guardians_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GuardianId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.UniqueConstraint("AK_Students_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_Students_Guardians_InstitutionId_GuardianId",
                        columns: x => new { x.InstitutionId, x.GuardianId },
                        principalTable: "Guardians",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchYear = table.Column<int>(type: "integer", nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.UniqueConstraint("AK_Enrollments_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_Enrollments_AcademicSessions_InstitutionId_AcademicSessionId",
                        columns: x => new { x.InstitutionId, x.AcademicSessionId },
                        principalTable: "AcademicSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Programmes_InstitutionId_ProgrammeId",
                        columns: x => new { x.InstitutionId, x.ProgrammeId },
                        principalTable: "Programmes",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_InstitutionId_StudentId",
                        columns: x => new { x.InstitutionId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_InstitutionId_AcademicSessionId",
                table: "Enrollments",
                columns: new[] { "InstitutionId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_InstitutionId_ProgrammeId",
                table: "Enrollments",
                columns: new[] { "InstitutionId", "ProgrammeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_InstitutionId_StudentId_AcademicSessionId",
                table: "Enrollments",
                columns: new[] { "InstitutionId", "StudentId", "AcademicSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_InstitutionId_DepartmentId",
                table: "Faculty",
                columns: new[] { "InstitutionId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_InstitutionId_Email",
                table: "Faculty",
                columns: new[] { "InstitutionId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_GuardianId",
                table: "Students",
                columns: new[] { "InstitutionId", "GuardianId" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_Status_CreatedAt",
                table: "Students",
                columns: new[] { "InstitutionId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_InstitutionId_StudentNumber",
                table: "Students",
                columns: new[] { "InstitutionId", "StudentNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Faculty");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Guardians");
        }
    }
}
