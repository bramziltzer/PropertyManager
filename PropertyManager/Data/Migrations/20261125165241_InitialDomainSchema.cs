using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDomainSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationStatusTypes",
                columns: table => new
                {
                    ApplicationStatusId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatusTypes", x => x.ApplicationStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.PropertyId);
                });

            migrationBuilder.CreateTable(
                name: "UnitTypes",
                columns: table => new
                {
                    UnitTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTypes", x => x.UnitTypeId);
                });

            migrationBuilder.CreateTable(
                name: "RentalUnits",
                columns: table => new
                {
                    RentalUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    UnitTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalUnits", x => x.RentalUnitId);
                    table.CheckConstraint("CK_RentalUnits_Bedrooms", "[Bedrooms] >= 0");
                    table.CheckConstraint("CK_RentalUnits_MonthlyRent", "[MonthlyRent] >= 0");
                    table.ForeignKey(
                        name: "FK_RentalUnits_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "PropertyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalUnits_UnitTypes_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "UnitTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalApplications",
                columns: table => new
                {
                    RentalApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalUnitId = table.Column<int>(type: "int", nullable: false),
                    ApplicationStatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalApplications", x => x.RentalApplicationId);
                    table.UniqueConstraint("AK_RentalApplications_RentalApplicationId_RentalUnitId", x => new { x.RentalApplicationId, x.RentalUnitId });
                    table.ForeignKey(
                        name: "FK_RentalApplications_ApplicationStatusTypes_ApplicationStatusId",
                        column: x => x.ApplicationStatusId,
                        principalTable: "ApplicationStatusTypes",
                        principalColumn: "ApplicationStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalApplications_RentalUnits_RentalUnitId",
                        column: x => x.RentalUnitId,
                        principalTable: "RentalUnits",
                        principalColumn: "RentalUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantInfos",
                columns: table => new
                {
                    RentalApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CurrentAddress_Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentAddress_Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentAddress_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentAddress_State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentAddress_ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantInfos", x => x.RentalApplicationId);
                    table.ForeignKey(
                        name: "FK_ApplicantInfos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicantInfos_RentalApplications_RentalApplicationId",
                        column: x => x.RentalApplicationId,
                        principalTable: "RentalApplications",
                        principalColumn: "RentalApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationStatusChanges",
                columns: table => new
                {
                    ApplicationStatusChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldApplicationStatusId = table.Column<int>(type: "int", nullable: false),
                    NewApplicationStatusId = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStatusChanges", x => x.ApplicationStatusChangeId);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusChanges_ApplicationStatusTypes_NewApplicationStatusId",
                        column: x => x.NewApplicationStatusId,
                        principalTable: "ApplicationStatusTypes",
                        principalColumn: "ApplicationStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusChanges_ApplicationStatusTypes_OldApplicationStatusId",
                        column: x => x.OldApplicationStatusId,
                        principalTable: "ApplicationStatusTypes",
                        principalColumn: "ApplicationStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusChanges_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationStatusChanges_RentalApplications_RentalApplicationId",
                        column: x => x.RentalApplicationId,
                        principalTable: "RentalApplications",
                        principalColumn: "RentalApplicationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalLeases",
                columns: table => new
                {
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LesseeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalUnitId = table.Column<int>(type: "int", nullable: false),
                    BeginDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalLeases", x => x.LeaseId);
                    table.CheckConstraint("CK_RentalLeases_EndDate", "[EndDate] > [BeginDate]");
                    table.ForeignKey(
                        name: "FK_RentalLeases_AspNetUsers_LesseeId",
                        column: x => x.LesseeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalLeases_RentalApplications_RentalApplicationId_RentalUnitId",
                        columns: x => new { x.RentalApplicationId, x.RentalUnitId },
                        principalTable: "RentalApplications",
                        principalColumns: new[] { "RentalApplicationId", "RentalUnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalLeases_RentalUnits_RentalUnitId",
                        column: x => x.RentalUnitId,
                        principalTable: "RentalUnits",
                        principalColumn: "RentalUnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResidenceHistories",
                columns: table => new
                {
                    ResidenceHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address_Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address_Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address_State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address_ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LandlordName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LandlordPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MoveInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MoveOutDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResidenceHistories", x => x.ResidenceHistoryId);
                    table.CheckConstraint("CK_ResidenceHistories_MoveOutDate", "[MoveOutDate] IS NULL OR [MoveOutDate] >= [MoveInDate]");
                    table.ForeignKey(
                        name: "FK_ResidenceHistories_RentalApplications_RentalApplicationId",
                        column: x => x.RentalApplicationId,
                        principalTable: "RentalApplications",
                        principalColumn: "RentalApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantInfos_UserId",
                table: "ApplicantInfos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusChanges_ChangedByUserId",
                table: "ApplicationStatusChanges",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusChanges_NewApplicationStatusId",
                table: "ApplicationStatusChanges",
                column: "NewApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusChanges_OldApplicationStatusId",
                table: "ApplicationStatusChanges",
                column: "OldApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusChanges_RentalApplicationId_ChangedDate",
                table: "ApplicationStatusChanges",
                columns: new[] { "RentalApplicationId", "ChangedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatusTypes_Name",
                table: "ApplicationStatusTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_ApplicationStatusId",
                table: "RentalApplications",
                column: "ApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_RentalUnitId",
                table: "RentalApplications",
                column: "RentalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalLeases_LesseeId",
                table: "RentalLeases",
                column: "LesseeId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalLeases_RentalApplicationId_RentalUnitId",
                table: "RentalLeases",
                columns: new[] { "RentalApplicationId", "RentalUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalLeases_RentalUnitId_BeginDate_EndDate",
                table: "RentalLeases",
                columns: new[] { "RentalUnitId", "BeginDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RentalUnits_PropertyId_UnitNumber",
                table: "RentalUnits",
                columns: new[] { "PropertyId", "UnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalUnits_UnitTypeId",
                table: "RentalUnits",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResidenceHistories_RentalApplicationId_MoveInDate",
                table: "ResidenceHistories",
                columns: new[] { "RentalApplicationId", "MoveInDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitTypes_Name",
                table: "UnitTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicantInfos");

            migrationBuilder.DropTable(
                name: "ApplicationStatusChanges");

            migrationBuilder.DropTable(
                name: "RentalLeases");

            migrationBuilder.DropTable(
                name: "ResidenceHistories");

            migrationBuilder.DropTable(
                name: "RentalApplications");

            migrationBuilder.DropTable(
                name: "ApplicationStatusTypes");

            migrationBuilder.DropTable(
                name: "RentalUnits");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "UnitTypes");
        }
    }
}
