using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalLocationDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationDescription",
                table: "HospitalProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NearbyLandmark",
                table: "HospitalProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_City",
                table: "HospitalProfiles",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_Governorate",
                table: "HospitalProfiles",
                column: "Governorate");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_Latitude_Longitude",
                table: "HospitalProfiles",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HospitalProfiles_City",
                table: "HospitalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_HospitalProfiles_Governorate",
                table: "HospitalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_HospitalProfiles_Latitude_Longitude",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "LocationDescription",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "NearbyLandmark",
                table: "HospitalProfiles");
        }
    }
}
