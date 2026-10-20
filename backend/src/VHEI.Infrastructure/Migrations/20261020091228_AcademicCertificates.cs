using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AcademicCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GraduationPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredSubjectsJson = table.Column<string>(type: "text", nullable: false),
                    MinimumCredits = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalNote = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraduationPolicies", x => x.Id);
                    table.UniqueConstraint("AK_GraduationPolicies_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_GraduationPolicy_Approval", "\"Status\" = 'Draft' OR (\"Status\" = 'Approved' AND \"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"CreatedBy\")");
                    table.CheckConstraint("CK_GraduationPolicy_Credits", "\"MinimumCredits\" > 0");
                    table.ForeignKey(
                        name: "FK_GraduationPolicies_AcademicSessions_InstitutionId_AcademicS~",
                        columns: x => new { x.InstitutionId, x.AcademicSessionId },
                        principalTable: "AcademicSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraduationPolicies_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraduationPolicies_Programmes_InstitutionId_ProgrammeId",
                        columns: x => new { x.InstitutionId, x.ProgrammeId },
                        principalTable: "Programmes",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    GraduationPolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    ClearanceReference = table.Column<string>(type: "text", nullable: false),
                    AuthorityReference = table.Column<string>(type: "text", nullable: false),
                    IssuedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Number = table.Column<string>(type: "text", nullable: true),
                    VerificationToken = table.Column<string>(type: "text", nullable: true),
                    Pdf = table.Column<byte[]>(type: "bytea", nullable: true),
                    Sha256 = table.Column<string>(type: "text", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicCertificates", x => x.Id);
                    table.UniqueConstraint("AK_AcademicCertificates_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Certificate_Issue", "\"Status\" IN ('Requested','Cancelled') OR (\"Status\" IN ('Issued','Revoked') AND \"IssuedBy\" IS NOT NULL AND \"IssuedBy\" <> \"RequestedBy\" AND \"Pdf\" IS NOT NULL AND \"Number\" IS NOT NULL AND \"Sha256\" IS NOT NULL AND \"VerificationToken\" IS NOT NULL AND \"IssuedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_Certificate_Kind", "\"Kind\" IN ('Transcript','CourseCompletion') AND (\"Kind\" <> 'CourseCompletion' OR \"GraduationPolicyId\" IS NOT NULL)");
                    table.CheckConstraint("CK_Certificate_Revoke", "\"Status\" <> 'Revoked' OR (\"RevokedBy\" IS NOT NULL AND \"RevokedAt\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AcademicCertificates_GraduationPolicies_InstitutionId_Gradu~",
                        columns: x => new { x.InstitutionId, x.GraduationPolicyId },
                        principalTable: "GraduationPolicies",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicCertificates_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicCertificates_Students_InstitutionId_StudentId",
                        columns: x => new { x.InstitutionId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCertificates_InstitutionId_GraduationPolicyId",
                table: "AcademicCertificates",
                columns: new[] { "InstitutionId", "GraduationPolicyId" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCertificates_InstitutionId_StudentId",
                table: "AcademicCertificates",
                columns: new[] { "InstitutionId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCertificates_Number",
                table: "AcademicCertificates",
                column: "Number",
                unique: true,
                filter: "\"Number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCertificates_VerificationToken",
                table: "AcademicCertificates",
                column: "VerificationToken",
                unique: true,
                filter: "\"VerificationToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GraduationPolicies_InstitutionId_AcademicSessionId",
                table: "GraduationPolicies",
                columns: new[] { "InstitutionId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_GraduationPolicies_InstitutionId_ProgrammeId",
                table: "GraduationPolicies",
                columns: new[] { "InstitutionId", "ProgrammeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicCertificates");

            migrationBuilder.DropTable(
                name: "GraduationPolicies");
        }
    }
}
