using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VFXRates.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FxRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Bid = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Ask = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxRates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "FxRates",
                columns: ["Id", "Ask", "BaseCurrency", "Bid", "LastUpdated", "QuoteCurrency"],
                values: new object[,]
                {
                    { 1, 0.9100m, "USD", 0.9000m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "EUR" },
                    { 2, 0.8100m, "EUR", 0.8000m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "GBP" },
                    { 3, 110.6000m, "USD", 110.5000m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "JPY" },
                    { 4, 0.7100m, "AUD", 0.7000m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD" },
                    { 5, 0.7600m, "CAD", 0.7500m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "USD" },
                    { 6, 0.9300m, "CHF", 0.9200m, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "EUR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_BaseCurrency_QuoteCurrency",
                table: "FxRates",
                columns: ["BaseCurrency", "QuoteCurrency"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FxRates");
        }
    }
}
