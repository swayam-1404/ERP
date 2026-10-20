using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StudentGuardianPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PortalAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    EvidenceReference = table.Column<string>(type: "text", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevocationReason = table.Column<string>(type: "text", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalAccesses", x => x.Id);
                    table.UniqueConstraint("AK_PortalAccesses_InstitutionId_Id", x => new { x.InstitutionId, x.Id });
                    table.CheckConstraint("CK_Portal_Kind", "\"Kind\" IN ('Student','Guardian')");
                    table.ForeignKey(
                        name: "FK_PortalAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortalAccesses_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortalAccesses_Students_InstitutionId_StudentId",
                        columns: x => new { x.InstitutionId, x.StudentId },
                        principalTable: "Students",
                        principalColumns: new[] { "InstitutionId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccesses_InstitutionId_StudentId",
                table: "PortalAccesses",
                columns: new[] { "InstitutionId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccesses_InstitutionId_UserId_StudentId_Kind",
                table: "PortalAccesses",
                columns: new[] { "InstitutionId", "UserId", "StudentId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortalAccesses_UserId",
                table: "PortalAccesses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortalAccesses");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");
        }
    }
}
