using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedReviewsAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentDoctorReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ModerationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModerationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModeratedByApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentDoctorReviews", x => x.Id);
                    table.CheckConstraint("CK_AppointmentDoctorReviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_AppointmentDoctorReviews_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentDoctorReviews_AspNetUsers_ModeratedByApplicationUserId",
                        column: x => x.ModeratedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentDoctorReviews_DoctorProfiles_DoctorProfileId",
                        column: x => x.DoctorProfileId,
                        principalTable: "DoctorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentDoctorReviews_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentHospitalReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ModerationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModerationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModeratedByApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentHospitalReviews", x => x.Id);
                    table.CheckConstraint("CK_AppointmentHospitalReviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_AppointmentHospitalReviews_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentHospitalReviews_AspNetUsers_ModeratedByApplicationUserId",
                        column: x => x.ModeratedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentHospitalReviews_HospitalProfiles_HospitalProfileId",
                        column: x => x.HospitalProfileId,
                        principalTable: "HospitalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentHospitalReviews_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicalServiceProviderReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalServiceProviderProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ModerationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModerationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModeratedByApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalServiceProviderReviews", x => x.Id);
                    table.CheckConstraint("CK_MedicalServiceProviderReviews_Rating", "[Rating] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_MedicalServiceProviderReviews_AspNetUsers_ModeratedByApplicationUserId",
                        column: x => x.ModeratedByApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceProviderReviews_MedicalServiceProviderProfiles_MedicalServiceProviderProfileId",
                        column: x => x.MedicalServiceProviderProfileId,
                        principalTable: "MedicalServiceProviderProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceProviderReviews_MedicalServiceRequests_MedicalServiceRequestId",
                        column: x => x.MedicalServiceRequestId,
                        principalTable: "MedicalServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicalServiceProviderReviews_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDoctorReviews_AppointmentId",
                table: "AppointmentDoctorReviews",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDoctorReviews_DoctorProfileId",
                table: "AppointmentDoctorReviews",
                column: "DoctorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDoctorReviews_ModeratedByApplicationUserId",
                table: "AppointmentDoctorReviews",
                column: "ModeratedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDoctorReviews_ModerationStatus",
                table: "AppointmentDoctorReviews",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDoctorReviews_PatientProfileId",
                table: "AppointmentDoctorReviews",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHospitalReviews_AppointmentId",
                table: "AppointmentHospitalReviews",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHospitalReviews_HospitalProfileId",
                table: "AppointmentHospitalReviews",
                column: "HospitalProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHospitalReviews_ModeratedByApplicationUserId",
                table: "AppointmentHospitalReviews",
                column: "ModeratedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHospitalReviews_ModerationStatus",
                table: "AppointmentHospitalReviews",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentHospitalReviews_PatientProfileId",
                table: "AppointmentHospitalReviews",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderReviews_MedicalServiceProviderProfileId",
                table: "MedicalServiceProviderReviews",
                column: "MedicalServiceProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderReviews_MedicalServiceRequestId",
                table: "MedicalServiceProviderReviews",
                column: "MedicalServiceRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderReviews_ModeratedByApplicationUserId",
                table: "MedicalServiceProviderReviews",
                column: "ModeratedByApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderReviews_ModerationStatus",
                table: "MedicalServiceProviderReviews",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalServiceProviderReviews_PatientProfileId",
                table: "MedicalServiceProviderReviews",
                column: "PatientProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentDoctorReviews");

            migrationBuilder.DropTable(
                name: "AppointmentHospitalReviews");

            migrationBuilder.DropTable(
                name: "MedicalServiceProviderReviews");
        }
    }
}
