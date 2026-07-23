using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBloodBankModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloodStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AvailableUnits = table.Column<int>(type: "int", nullable: false),
                    MinimumRequiredUnits = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodStocks_AspNetUsers_LastUpdatedByUserId",
                        column: x => x.LastUpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloodStocks_HospitalProfiles_HospitalProfileId",
                        column: x => x.HospitalProfileId,
                        principalTable: "HospitalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BloodRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodStockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodGroup = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitsRequested = table.Column<int>(type: "int", nullable: false),
                    BeneficiaryName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BeneficiaryAge = table.Column<int>(type: "int", nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MedicalCondition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HospitalOrFacilityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HospitalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Urgency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloodRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloodRequests_BloodStocks_BloodStockId",
                        column: x => x.BloodStockId,
                        principalTable: "BloodStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloodRequests_HospitalProfiles_HospitalProfileId",
                        column: x => x.HospitalProfileId,
                        principalTable: "HospitalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BloodRequests_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_BloodGroup",
                table: "BloodRequests",
                column: "BloodGroup");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_BloodStockId",
                table: "BloodRequests",
                column: "BloodStockId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_Hospital_BloodGroup_Status",
                table: "BloodRequests",
                columns: new[] { "HospitalProfileId", "BloodGroup", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_Hospital_Status",
                table: "BloodRequests",
                columns: new[] { "HospitalProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_HospitalProfileId",
                table: "BloodRequests",
                column: "HospitalProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_Patient_SubmittedAt",
                table: "BloodRequests",
                columns: new[] { "PatientProfileId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_PatientProfileId",
                table: "BloodRequests",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_Status",
                table: "BloodRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_SubmittedAt",
                table: "BloodRequests",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BloodRequests_Urgency",
                table: "BloodRequests",
                column: "Urgency");

            migrationBuilder.CreateIndex(
                name: "IX_BloodStocks_AvailableUnits",
                table: "BloodStocks",
                column: "AvailableUnits");

            migrationBuilder.CreateIndex(
                name: "IX_BloodStocks_BloodGroup",
                table: "BloodStocks",
                column: "BloodGroup");

            migrationBuilder.CreateIndex(
                name: "IX_BloodStocks_Hospital_BloodGroup_Unique",
                table: "BloodStocks",
                columns: new[] { "HospitalProfileId", "BloodGroup" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BloodStocks_HospitalProfileId",
                table: "BloodStocks",
                column: "HospitalProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BloodStocks_IsAvailable",
                table: "BloodStocks",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_BloodStocks_LastUpdatedByUserId",
                table: "BloodStocks",
                column: "LastUpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloodRequests");

            migrationBuilder.DropTable(
                name: "BloodStocks");
        }
    }
}
