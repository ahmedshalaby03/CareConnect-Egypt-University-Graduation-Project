using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialtiesAndDoctorHospitalAffiliations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "DoctorProfiles");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ClosingTime",
                table: "HospitalProfiles",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "HospitalProfiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsProfileCompleted",
                table: "HospitalProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "HospitalProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OpeningTime",
                table: "HospitalProfiles",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HospitalProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "HospitalProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "DoctorProfiles",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "DoctorProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConsultationPrice",
                table: "DoctorProfiles",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DoctorProfiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Governorate",
                table: "DoctorProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfileCompleted",
                table: "DoctorProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "DoctorProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SpecialtyId",
                table: "DoctorProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DoctorProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorHospitalAffiliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorHospitalAffiliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorHospitalAffiliations_DoctorProfiles_DoctorProfileId",
                        column: x => x.DoctorProfileId,
                        principalTable: "DoctorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorHospitalAffiliations_HospitalProfiles_HospitalProfileId",
                        column: x => x.HospitalProfileId,
                        principalTable: "HospitalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HospitalSpecialties",
                columns: table => new
                {
                    HospitalProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialtyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalSpecialties", x => new { x.HospitalProfileId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_HospitalSpecialties_HospitalProfiles_HospitalProfileId",
                        column: x => x.HospitalProfileId,
                        principalTable: "HospitalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HospitalSpecialties_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalProfiles_Completed_Location",
                table: "HospitalProfiles",
                columns: new[] { "IsProfileCompleted", "Governorate", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_Completed_Location",
                table: "DoctorProfiles",
                columns: new[] { "IsProfileCompleted", "Governorate", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_SpecialtyId",
                table: "DoctorProfiles",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorHospitalAffiliations_Doctor_Hospital",
                table: "DoctorHospitalAffiliations",
                columns: new[] { "DoctorProfileId", "HospitalProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorHospitalAffiliations_DoctorProfileId",
                table: "DoctorHospitalAffiliations",
                column: "DoctorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorHospitalAffiliations_HospitalProfileId",
                table: "DoctorHospitalAffiliations",
                column: "HospitalProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorHospitalAffiliations_Status",
                table: "DoctorHospitalAffiliations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HospitalSpecialties_Hospital_Specialty_Unique",
                table: "HospitalSpecialties",
                columns: new[] { "HospitalProfileId", "SpecialtyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HospitalSpecialties_SpecialtyId",
                table: "HospitalSpecialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_ArabicName_Unique",
                table: "Specialties",
                column: "ArabicName",
                unique: true,
                filter: "[ArabicName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_IsActive",
                table: "Specialties",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Name_Unique",
                table: "Specialties",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorProfiles_Specialties_SpecialtyId",
                table: "DoctorProfiles",
                column: "SpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorProfiles_Specialties_SpecialtyId",
                table: "DoctorProfiles");

            migrationBuilder.DropTable(
                name: "DoctorHospitalAffiliations");

            migrationBuilder.DropTable(
                name: "HospitalSpecialties");

            migrationBuilder.DropTable(
                name: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_HospitalProfiles_Completed_Location",
                table: "HospitalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_DoctorProfiles_Completed_Location",
                table: "DoctorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_DoctorProfiles_SpecialtyId",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "IsProfileCompleted",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "HospitalProfiles");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "ConsultationPrice",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "Governorate",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "IsProfileCompleted",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "SpecialtyId",
                table: "DoctorProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DoctorProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "DoctorProfiles",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }
    }
}
