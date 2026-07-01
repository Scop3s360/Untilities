using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaverSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdAndImportJobRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_ProviderId",
                table: "Offers");

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("2bc071fd-22d3-4500-935e-71aa608b16f0"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("51589e94-2ac9-4d9f-a4d2-ccf085ad5191"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("6dc21a48-495b-43b8-8589-8e0acdcb729d"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("8358705f-0146-4b22-8a7c-e51946c518bf"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("9d0bb4ba-371a-489c-a06c-5f6e35da94bf"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("a9e85ccd-5cdf-4643-b7d4-b4f892cfec54"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("ae151add-57fa-4172-9eb9-b7726a7e3b83"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("c25317d2-d237-4d99-931e-f6c0e89b8fd7"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("eec80046-18b7-4f98-a096-58d5fa647ec9"));

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Offers",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ConnectorVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    OffersDownloaded = table.Column<int>(type: "INTEGER", nullable: false),
                    OffersValidated = table.Column<int>(type: "INTEGER", nullable: false),
                    OffersAdded = table.Column<int>(type: "INTEGER", nullable: false),
                    OffersUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                    OffersDeactivated = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidationWarningCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Warnings = table.Column<string>(type: "TEXT", maxLength: 16000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("11b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(5967), new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(5968) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("22b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6720), new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6720) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("33b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6724), new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6724) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("44b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6726), new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6726) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("55b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6728), new DateTime(2026, 7, 1, 10, 51, 56, 698, DateTimeKind.Utc).AddTicks(6728) });

            migrationBuilder.InsertData(
                table: "RetailerAliases",
                columns: new[] { "Id", "AliasName", "RetailerId" },
                values: new object[,]
                {
                    { new Guid("3b758414-69c0-44fc-ada6-0a997867210e"), "PC World", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("43fc3186-c08e-47d6-85ec-6bb8e0787f0e"), "Amazon", new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("4f94ba77-a63a-45cc-af75-5167db778bd6"), "currys.co.uk", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("5ecde822-ece3-45da-a40d-8543b108e041"), "Currys PC World", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("5fd83782-862a-450d-9082-6b5fc5b6724b"), "amazon.co.uk", new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("9dbbb772-5a20-4b0e-8d98-65306e8ec8fa"), "Argos", new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("b8f5feba-0699-4dde-8cdb-5cde5af0add4"), "argos.co.uk", new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("daf7c97c-0ff8-44ec-bde1-2b8259598a39"), "Currys", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("e607d710-cb94-4045-a2df-251b989bc5fe"), "Amazon UK", new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281") }
                });

            migrationBuilder.UpdateData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 701, DateTimeKind.Utc).AddTicks(969), new DateTime(2026, 7, 1, 10, 51, 56, 701, DateTimeKind.Utc).AddTicks(970) });

            migrationBuilder.UpdateData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 701, DateTimeKind.Utc).AddTicks(1776), new DateTime(2026, 7, 1, 10, 51, 56, 701, DateTimeKind.Utc).AddTicks(1776) });

            migrationBuilder.UpdateData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 10, 51, 56, 701, DateTimeKind.Utc).AddTicks(1771), new DateTime(2026, 7, 1, 10, 51, 56, 701, DateTimeKind.Utc).AddTicks(1771) });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ProviderId_ExternalId",
                table: "Offers",
                columns: new[] { "ProviderId", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_ProviderName",
                table: "ImportJobs",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_StartedAt",
                table: "ImportJobs",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_Offers_ProviderId_ExternalId",
                table: "Offers");

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("3b758414-69c0-44fc-ada6-0a997867210e"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("43fc3186-c08e-47d6-85ec-6bb8e0787f0e"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("4f94ba77-a63a-45cc-af75-5167db778bd6"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("5ecde822-ece3-45da-a40d-8543b108e041"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("5fd83782-862a-450d-9082-6b5fc5b6724b"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("9dbbb772-5a20-4b0e-8d98-65306e8ec8fa"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("b8f5feba-0699-4dde-8cdb-5cde5af0add4"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("daf7c97c-0ff8-44ec-bde1-2b8259598a39"));

            migrationBuilder.DeleteData(
                table: "RetailerAliases",
                keyColumn: "Id",
                keyValue: new Guid("e607d710-cb94-4045-a2df-251b989bc5fe"));

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Offers");

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("11b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(2359), new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(2360) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("22b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(2993), new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(2993) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("33b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(2996), new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(2996) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("44b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(3001), new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(3001) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("55b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(3004), new DateTime(2026, 7, 1, 8, 54, 6, 963, DateTimeKind.Utc).AddTicks(3004) });

            migrationBuilder.InsertData(
                table: "RetailerAliases",
                columns: new[] { "Id", "AliasName", "RetailerId" },
                values: new object[,]
                {
                    { new Guid("2bc071fd-22d3-4500-935e-71aa608b16f0"), "Amazon", new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("51589e94-2ac9-4d9f-a4d2-ccf085ad5191"), "PC World", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("6dc21a48-495b-43b8-8589-8e0acdcb729d"), "Argos", new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("8358705f-0146-4b22-8a7c-e51946c518bf"), "amazon.co.uk", new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("9d0bb4ba-371a-489c-a06c-5f6e35da94bf"), "Currys", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("a9e85ccd-5cdf-4643-b7d4-b4f892cfec54"), "argos.co.uk", new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("ae151add-57fa-4172-9eb9-b7726a7e3b83"), "currys.co.uk", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("c25317d2-d237-4d99-931e-f6c0e89b8fd7"), "Amazon UK", new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281") },
                    { new Guid("eec80046-18b7-4f98-a096-58d5fa647ec9"), "Currys PC World", new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281") }
                });

            migrationBuilder.UpdateData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3122), new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3123) });

            migrationBuilder.UpdateData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3925), new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3925) });

            migrationBuilder.UpdateData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3920), new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3921) });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ProviderId",
                table: "Offers",
                column: "ProviderId");
        }
    }
}
