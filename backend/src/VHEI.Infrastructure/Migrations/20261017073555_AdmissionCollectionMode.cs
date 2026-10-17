using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VHEI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdmissionCollectionMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectionMode",
                table: "AdmissionFeePlans",
                type: "text",
                nullable: false,
                defaultValue: "Offline");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectionMode",
                table: "AdmissionFeePlans");
        }
    }
}
