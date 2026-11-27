using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistResidenceHistoryCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResidenceHistorySaved",
                table: "RentalApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill
            migrationBuilder.Sql(
                """
                UPDATE application
                SET application.IsResidenceHistorySaved = 1
                FROM RentalApplications AS application
                WHERE EXISTS (
                    SELECT 1
                    FROM ResidenceHistories AS residence
                    WHERE residence.RentalApplicationId = application.RentalApplicationId
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsResidenceHistorySaved",
                table: "RentalApplications");
        }
    }
}
