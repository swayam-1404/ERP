using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExaminationsResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAcademicYear = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: false),
                    BandsJson = table.Column<string>(type: "text", nullable: false),
                    FailGrade = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalNote = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingPolicies", x => x.Id);
                    table.UniqueConstraint("AK_GradingPolicies_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_GradingPolicy_Approval", "\"Status\" = 'Draft' OR (\"Status\" = 'Approved' AND \"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"CreatedBy\")");
                    table.ForeignKey(
                        name: "FK_GradingPolicies_AcademicSessions_InstitutionId_AcademicSess~",
                        columns: x => new { x.InstitutionId, x.AcademicSessionId },
                        principalTable: "AcademicSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradingPolicies_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradingPolicies_Programmes_InstitutionId_ProgrammeId",
                        columns: x => new { x.InstitutionId, x.ProgrammeId },
                        principalTable: "Programmes",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    ScheduledOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ConfigurationReference = table.Column<string>(type: "text", nullable: false),
                    Credits = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MaximumInternal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    MaximumExternal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PassInternal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PassExternal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PassTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAssessments", x => x.Id);
                    table.UniqueConstraint("AK_ExamAssessments_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.UniqueConstraint("AK_ExamAssessments_InstitutionId_Id_SubjectId", x => new { x.InstitutionId, x.Id, x.SubjectId });
                    table.CheckConstraint("CK_Assessment_Approval", "\"Status\" NOT IN ('Approved','Published') OR (\"ApprovedBy\" IS NOT NULL AND \"SubmittedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"SubmittedBy\")");
                    table.CheckConstraint("CK_Assessment_Limits", "\"Credits\" > 0 AND \"MaximumInternal\" >= 0 AND \"MaximumExternal\" > 0 AND \"PassInternal\" BETWEEN 0 AND \"MaximumInternal\" AND \"PassExternal\" BETWEEN 0 AND \"MaximumExternal\" AND \"PassTotal\" BETWEEN 0 AND (\"MaximumInternal\" + \"MaximumExternal\")");
                    table.CheckConstraint("CK_Assessment_Status", "\"Status\" IN ('Draft','Submitted','Approved','Published')");
                    table.ForeignKey(
                        name: "FK_ExamAssessments_Faculty_InstitutionId_FacultyId",
                        columns: x => new { x.InstitutionId, x.FacultyId },
                        principalTable: "Faculty",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAssessments_GradingPolicies_InstitutionId_GradingPolicy~",
                        columns: x => new { x.InstitutionId, x.GradingPolicyId },
                        principalTable: "GradingPolicies",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAssessments_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAssessments_Subjects_InstitutionId_SubjectId",
                        columns: x => new { x.InstitutionId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamMarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Attendance = table.Column<string>(type: "text", nullable: false),
                    InternalMark = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    ExternalMark = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Percentage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Grade = table.Column<string>(type: "text", nullable: true),
                    GradePoints = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    IsPass = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMarks", x => x.Id);
                    table.UniqueConstraint("AK_ExamMarks_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_ExamMark_Attendance", "\"Attendance\" IN ('Unentered','Present','Absent')");
                    table.ForeignKey(
                        name: "FK_ExamMarks_Enrollments_InstitutionId_EnrollmentId_StudentId",
                        columns: x => new { x.InstitutionId, x.EnrollmentId, x.StudentId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "InstitutionId", "Id", "StudentId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarks_ExamAssessments_InstitutionId_AssessmentId_Subjec~",
                        columns: x => new { x.InstitutionId, x.AssessmentId, x.SubjectId },
                        principalTable: "ExamAssessments",
                        principalColumns: new[] { "InstitutionId", "Id", "SubjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarks_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssessments_InstitutionId_FacultyId",
                table: "ExamAssessments",
                columns: new[] { "InstitutionId", "FacultyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssessments_InstitutionId_GradingPolicyId",
                table: "ExamAssessments",
                columns: new[] { "InstitutionId", "GradingPolicyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssessments_InstitutionId_SubjectId_Section",
                table: "ExamAssessments",
                columns: new[] { "InstitutionId", "SubjectId", "Section" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_InstitutionId_AssessmentId_StudentId",
                table: "ExamMarks",
                columns: new[] { "InstitutionId", "AssessmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_InstitutionId_AssessmentId_SubjectId",
                table: "ExamMarks",
                columns: new[] { "InstitutionId", "AssessmentId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_InstitutionId_EnrollmentId_StudentId",
                table: "ExamMarks",
                columns: new[] { "InstitutionId", "EnrollmentId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_InstitutionId_StudentId_SubjectId",
                table: "ExamMarks",
                columns: new[] { "InstitutionId", "StudentId", "SubjectId" },
                unique: true,
                filter: "\"IsPublished\"");

            migrationBuilder.CreateIndex(
                name: "IX_GradingPolicies_InstitutionId_AcademicSessionId",
                table: "GradingPolicies",
                columns: new[] { "InstitutionId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradingPolicies_InstitutionId_ProgrammeId",
                table: "GradingPolicies",
                columns: new[] { "InstitutionId", "ProgrammeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamMarks");

            migrationBuilder.DropTable(
                name: "ExamAssessments");

            migrationBuilder.DropTable(
                name: "GradingPolicies");
        }
    }
}
