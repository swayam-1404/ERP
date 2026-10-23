using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdmissionIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyReference = table.Column<string>(type: "text", nullable: false),
                    EligibilityNote = table.Column<string>(type: "text", nullable: false),
                    DocumentReference = table.Column<string>(type: "text", nullable: false),
                    ReviewNote = table.Column<string>(type: "text", nullable: false),
                    IsDemo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplications", x => x.Id);
                    table.UniqueConstraint("AK_AdmissionApplications_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Admission_Review", "\"Status\" NOT IN ('Reviewed','Rejected') OR (\"ReviewedBy\" IS NOT NULL AND \"SubmittedBy\" IS NOT NULL AND \"ReviewedBy\" <> \"SubmittedBy\")");
                    table.CheckConstraint("CK_Admission_Status", "\"Status\" IN ('Draft','Submitted','Reviewed','Rejected','Cancelled')");
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_AcademicSessions_InstitutionId_Academ~",
                        columns: x => new { x.InstitutionId, x.AcademicSessionId },
                        principalTable: "AcademicSessions",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_Programmes_InstitutionId_ProgrammeId",
                        columns: x => new { x.InstitutionId, x.ProgrammeId },
                        principalTable: "Programmes",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionEvents", x => x.Id);
                    table.UniqueConstraint("AK_AdmissionEvents_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.ForeignKey(
                        name: "FK_AdmissionEvents_AdmissionApplications_InstitutionId_Applica~",
                        columns: x => new { x.InstitutionId, x.ApplicationId },
                        principalTable: "AdmissionApplications",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionEvents_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_InstitutionId_AcademicSessionId",
                table: "AdmissionApplications",
                columns: new[] { "InstitutionId", "AcademicSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_InstitutionId_ApplicationNumber",
                table: "AdmissionApplications",
                columns: new[] { "InstitutionId", "ApplicationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_InstitutionId_ProgrammeId",
                table: "AdmissionApplications",
                columns: new[] { "InstitutionId", "ProgrammeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_InstitutionId_Status_CreatedAt",
                table: "AdmissionApplications",
                columns: new[] { "InstitutionId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionEvents_InstitutionId_ApplicationId",
                table: "AdmissionEvents",
                columns: new[] { "InstitutionId", "ApplicationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionEvents");

            migrationBuilder.DropTable(
                name: "AdmissionApplications");
        }
    }
}
