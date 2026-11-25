using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class DomainImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantInfos_AspNetUsers_UserId",
                table: "ApplicantInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalUnits_Properties_PropertyId",
                table: "RentalUnits");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantInfos_UserId",
                table: "ApplicantInfos");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "UnitTypes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ApplicantInfos");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UnitTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RentalUnits",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicantUserId",
                table: "RentalApplications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RentalApplications",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_ApplicantUserId_RentalUnitId",
                table: "RentalApplications",
                columns: new[] { "ApplicantUserId", "RentalUnitId" },
                unique: true,
                filter: "[ApplicationStatusId] IN (1, 2, 3)");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalApplications_AspNetUsers_ApplicantUserId",
                table: "RentalApplications",
                column: "ApplicantUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalUnits_Properties_PropertyId",
                table: "RentalUnits",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "PropertyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentalApplications_AspNetUsers_ApplicantUserId",
                table: "RentalApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalUnits_Properties_PropertyId",
                table: "RentalUnits");

            migrationBuilder.DropIndex(
                name: "IX_RentalApplications_ApplicantUserId_RentalUnitId",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UnitTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RentalUnits");

            migrationBuilder.DropColumn(
                name: "ApplicantUserId",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RentalApplications");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "UnitTypes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ApplicantInfos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantInfos_UserId",
                table: "ApplicantInfos",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantInfos_AspNetUsers_UserId",
                table: "ApplicantInfos",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalUnits_Properties_PropertyId",
                table: "RentalUnits",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "PropertyId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
