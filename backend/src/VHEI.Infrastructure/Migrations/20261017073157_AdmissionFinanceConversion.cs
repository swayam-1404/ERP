using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdmissionFinanceConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Admission_Review",
                table: "AdmissionApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Admission_Status",
                table: "AdmissionApplications");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalReference",
                table: "AdmissionApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "AdmissionApplications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "AdmissionApplications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmissionFeePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAcademicYear = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: false),
                    AuthorityReference = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalNote = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionFeePlans", x => x.Id);
                    table.UniqueConstraint("AK_AdmissionFeePlans_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_FeePlan_Amount", "\"Amount\" > 0 AND \"Currency\" = 'INR'");
                    table.CheckConstraint("CK_FeePlan_Approval", "\"Status\" = 'Draft' OR (\"Status\" = 'Approved' AND \"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"CreatedBy\")");
                    table.ForeignKey(
                        name: "FK_AdmissionFeePlans_AcademicSessions_InstitutionId_AcademicSe~",
                        columns: x => new { x.InstitutionId, x.AcademicSessionId },
                        principalTable: "AcademicSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionFeePlans_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionFeePlans_Programmes_InstitutionId_ProgrammeId",
                        columns: x => new { x.InstitutionId, x.ProgrammeId },
                        principalTable: "Programmes",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    IsVoid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionCharges", x => x.Id);
                    table.UniqueConstraint("AK_AdmissionCharges_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Charge_Amount", "\"Amount\" > 0 AND \"Currency\" = 'INR'");
                    table.ForeignKey(
                        name: "FK_AdmissionCharges_AdmissionApplications_InstitutionId_Applic~",
                        columns: x => new { x.InstitutionId, x.ApplicationId },
                        principalTable: "AdmissionApplications",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionCharges_AdmissionFeePlans_InstitutionId_FeePlanId",
                        columns: x => new { x.InstitutionId, x.FeePlanId },
                        principalTable: "AdmissionFeePlans",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionCharges_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EvidenceReference = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: false),
                    AcknowledgementNumber = table.Column<string>(type: "text", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionPayments", x => x.Id);
                    table.UniqueConstraint("AK_AdmissionPayments_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Payment_Acknowledgement", "(\"Status\" = 'Confirmed') = (\"AcknowledgementNumber\" IS NOT NULL)");
                    table.CheckConstraint("CK_Payment_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_Payment_Review", "\"Status\" = 'Pending' OR (\"ReviewedBy\" IS NOT NULL AND \"ReviewedBy\" <> \"RecordedBy\")");
                    table.CheckConstraint("CK_Payment_Status", "\"Status\" IN ('Pending','Confirmed','Rejected')");
                    table.ForeignKey(
                        name: "FK_AdmissionPayments_AdmissionCharges_InstitutionId_ChargeId",
                        columns: x => new { x.InstitutionId, x.ChargeId },
                        principalTable: "AdmissionCharges",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionPayments_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_InstitutionId_StudentId",
                table: "AdmissionApplications",
                columns: new[] { "InstitutionId", "StudentId" },
                unique: true,
                filter: "\"StudentId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admission_Approval",
                table: "AdmissionApplications",
                sql: "\"Status\" NOT IN ('Approved','Enrolled') OR (\"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"SubmittedBy\" AND length(trim(\"ApprovalReference\")) >= 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admission_Enrolled",
                table: "AdmissionApplications",
                sql: "(\"Status\" = 'Enrolled') = (\"StudentId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admission_Review",
                table: "AdmissionApplications",
                sql: "\"Status\" NOT IN ('Reviewed','Approved','Enrolled','Rejected') OR (\"ReviewedBy\" IS NOT NULL AND \"SubmittedBy\" IS NOT NULL AND \"ReviewedBy\" <> \"SubmittedBy\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admission_Status",
                table: "AdmissionApplications",
                sql: "\"Status\" IN ('Draft','Submitted','Reviewed','Approved','Enrolled','Rejected','Cancelled')");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionCharges_InstitutionId_ApplicationId",
                table: "AdmissionCharges",
                columns: new[] { "InstitutionId", "ApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionCharges_InstitutionId_FeePlanId",
                table: "AdmissionCharges",
                columns: new[] { "InstitutionId", "FeePlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionFeePlans_InstitutionId_AcademicSessionId",
                table: "AdmissionFeePlans",
                columns: new[] { "InstitutionId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionFeePlans_InstitutionId_ProgrammeId",
                table: "AdmissionFeePlans",
                columns: new[] { "InstitutionId", "ProgrammeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPayments_InstitutionId_AcknowledgementNumber",
                table: "AdmissionPayments",
                columns: new[] { "InstitutionId", "AcknowledgementNumber" },
                unique: true,
                filter: "\"AcknowledgementNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPayments_InstitutionId_ChargeId",
                table: "AdmissionPayments",
                columns: new[] { "InstitutionId", "ChargeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPayments_InstitutionId_ExternalReference",
                table: "AdmissionPayments",
                columns: new[] { "InstitutionId", "ExternalReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionPayments_InstitutionId_IdempotencyKey",
                table: "AdmissionPayments",
                columns: new[] { "InstitutionId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionApplications_Students_InstitutionId_StudentId",
                table: "AdmissionApplications",
                columns: new[] { "InstitutionId", "StudentId" },
                principalTable: "Students",
                principalColumns: new[] { "InstitutionId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionApplications_Students_InstitutionId_StudentId",
                table: "AdmissionApplications");

            migrationBuilder.DropTable(
                name: "AdmissionPayments");

            migrationBuilder.DropTable(
                name: "AdmissionCharges");

            migrationBuilder.DropTable(
                name: "AdmissionFeePlans");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplications_InstitutionId_StudentId",
                table: "AdmissionApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Admission_Approval",
                table: "AdmissionApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Admission_Enrolled",
                table: "AdmissionApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Admission_Review",
                table: "AdmissionApplications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Admission_Status",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "ApprovalReference",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "AdmissionApplications");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admission_Review",
                table: "AdmissionApplications",
                sql: "\"Status\" NOT IN ('Reviewed','Rejected') OR (\"ReviewedBy\" IS NOT NULL AND \"SubmittedBy\" IS NOT NULL AND \"ReviewedBy\" <> \"SubmittedBy\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admission_Status",
                table: "AdmissionApplications",
                sql: "\"Status\" IN ('Draft','Submitted','Reviewed','Rejected','Cancelled')");
        }
    }
}
