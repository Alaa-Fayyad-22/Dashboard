using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversalDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointToSiteConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "SiteConnections",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "SiteConnections");
        }
    }
}
