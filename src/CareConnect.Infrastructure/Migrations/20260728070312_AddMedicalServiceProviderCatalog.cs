using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalServiceProviderCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalServiceProviderProfiles_AspNetUsers_UserId",
                table: "MedicalServiceProviderProfiles");

            // The legacy profile stored an uncontrolled free-text ServiceType. Convert the
            // known Development value and safely map any other non-enum value to Other before
            // the application begins reading this column through the enum converter.
            migrationBuilder.Sql(
                """
                UPDATE [MedicalServiceProviderProfiles]
                SET [ServiceType] = 'MedicalCenter'
                WHERE [ServiceType] = 'General medical services';

                UPDATE [MedicalServiceProviderProfiles]
                SET [ServiceType] = 'Other'
                WHERE [ServiceType] IS NOT NULL
                  AND [ServiceType] NOT IN (
                      'MedicalCenter',
                      'Laboratory',
                      'RadiologyCenter',
                      'PhysiotherapyCenter',
                      'HomeCareProvider',
                      'NursingCenter',
                      'Pharmacy',
                      'Other');
                """);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MedicalServiceProviderProfiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "MedicalServiceProviderProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "MedicalServiceProviderProfiles",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "MedicalServiceProviderProfiles",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "MedicalServiceProviderProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MedicalServiceProviderProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalServiceCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalServiceProviderWorkingHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceProviderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServiceProviderWorkingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalServiceProviderWorkingHours_MedicalServiceProviderProfiles_MedicalServiceProviderProfileId",
                        column: x => x.MedicalServiceProviderProfileId,
                        principalTable: "MedicalServiceProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicalServiceOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceProviderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    PreparationInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServiceOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalServiceOfferings_MedicalServiceCategories_MedicalServiceCategoryId",
                        column: x => x.MedicalServiceCategoryId,
                        principalTable: "MedicalServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceOfferings_MedicalServiceProviderProfiles_MedicalServiceProviderProfileId",
                        column: x => x.MedicalServiceProviderProfileId,
                        principalTable: "MedicalServiceProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderProfiles_City",
                table: "MedicalServiceProviderProfiles",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderProfiles_Governorate",
                table: "MedicalServiceProviderProfiles",
                column: "Governorate");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderProfiles_Latitude_Longitude",
                table: "MedicalServiceProviderProfiles",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceCategories_IsActive",
                table: "MedicalServiceCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceCategories_Name",
                table: "MedicalServiceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceOfferings_IsActive",
                table: "MedicalServiceOfferings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceOfferings_MedicalServiceCategoryId",
                table: "MedicalServiceOfferings",
                column: "MedicalServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceOfferings_MedicalServiceProviderProfileId",
                table: "MedicalServiceOfferings",
                column: "MedicalServiceProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceOfferings_Provider_Name_Unique",
                table: "MedicalServiceOfferings",
                columns: new[] { "MedicalServiceProviderProfileId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderWorkingHours_Provider_Day_Unique",
                table: "MedicalServiceProviderWorkingHours",
                columns: new[] { "MedicalServiceProviderProfileId", "DayOfWeek" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalServiceProviderProfiles_AspNetUsers_UserId",
                table: "MedicalServiceProviderProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalServiceProviderProfiles_AspNetUsers_UserId",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropTable(
                name: "MedicalServiceOfferings");

            migrationBuilder.DropTable(
                name: "MedicalServiceProviderWorkingHours");

            migrationBuilder.DropTable(
                name: "MedicalServiceCategories");

            migrationBuilder.DropIndex(
                name: "IX_MedicalServiceProviderProfiles_City",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_MedicalServiceProviderProfiles_Governorate",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropIndex(
                name: "IX_MedicalServiceProviderProfiles_Latitude_Longitude",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MedicalServiceProviderProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalServiceProviderProfiles_AspNetUsers_UserId",
                table: "MedicalServiceProviderProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
