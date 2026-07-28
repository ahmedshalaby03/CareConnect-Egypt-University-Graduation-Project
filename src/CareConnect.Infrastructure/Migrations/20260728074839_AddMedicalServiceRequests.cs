using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalServiceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryModeAvailability",
                table: "MedicalServiceOfferings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "AtProviderLocationOnly");

            migrationBuilder.CreateTable(
                name: "MedicalServiceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceProviderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DeliveryMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredStartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HomeVisitAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProviderResponseNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ServiceNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CategoryNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DurationMinutesSnapshot = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServiceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalServiceRequests_MedicalServiceOfferings_MedicalServiceOfferingId",
                        column: x => x.MedicalServiceOfferingId,
                        principalTable: "MedicalServiceOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceRequests_MedicalServiceProviderProfiles_MedicalServiceProviderProfileId",
                        column: x => x.MedicalServiceProviderProfileId,
                        principalTable: "MedicalServiceProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceRequests_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicalServiceRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChangedByApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServiceRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalServiceRequestStatusHistory_AspNetUsers_ChangedByApplicationUserId",
                        column: x => x.ChangedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceRequestStatusHistory_MedicalServiceRequests_MedicalServiceRequestId",
                        column: x => x.MedicalServiceRequestId,
                        principalTable: "MedicalServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_MedicalServiceOfferingId",
                table: "MedicalServiceRequests",
                column: "MedicalServiceOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_MedicalServiceProviderProfileId",
                table: "MedicalServiceRequests",
                column: "MedicalServiceProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_Patient_Service_Date_Time_ActiveUnique",
                table: "MedicalServiceRequests",
                columns: new[] { "PatientProfileId", "MedicalServiceOfferingId", "RequestedDate", "PreferredStartTime" },
                unique: true,
                filter: "[Status] IN ('Pending', 'Accepted')");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_PatientProfileId",
                table: "MedicalServiceRequests",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_RequestedDate",
                table: "MedicalServiceRequests",
                column: "RequestedDate");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_RequestNumber",
                table: "MedicalServiceRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_ScheduledAt",
                table: "MedicalServiceRequests",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequests_Status",
                table: "MedicalServiceRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequestStatusHistory_ChangedByApplicationUserId",
                table: "MedicalServiceRequestStatusHistory",
                column: "ChangedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequestStatusHistory_CreatedAt",
                table: "MedicalServiceRequestStatusHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceRequestStatusHistory_MedicalServiceRequestId",
                table: "MedicalServiceRequestStatusHistory",
                column: "MedicalServiceRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalServiceRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "MedicalServiceRequests");

            migrationBuilder.DropColumn(
                name: "DeliveryModeAvailability",
                table: "MedicalServiceOfferings");
        }
    }
}
