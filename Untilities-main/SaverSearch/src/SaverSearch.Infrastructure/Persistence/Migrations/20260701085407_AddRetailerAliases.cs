using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaverSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRetailerAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetailerAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RetailerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AliasName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerAliases_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                table: "Retailers",
                columns: new[] { "Id", "CategoryId", "CreatedDate", "IsActive", "LogoUrl", "Name", "Slug", "UpdatedDate", "Website" },
                values: new object[,]
                {
                    { new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281"), new Guid("23a2a3ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3122), true, null, "Amazon", "amazon", new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3123), "https://amazon.co.uk" },
                    { new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281"), new Guid("23a2a3ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3925), true, null, "Argos", "argos", new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3925), "https://argos.co.uk" },
                    { new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281"), new Guid("23a2a3ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3920), true, null, "Currys", "currys", new DateTime(2026, 7, 1, 8, 54, 6, 966, DateTimeKind.Utc).AddTicks(3921), "https://currys.co.uk" }
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_RetailerAliases_AliasName",
                table: "RetailerAliases",
                column: "AliasName");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerAliases_RetailerId",
                table: "RetailerAliases",
                column: "RetailerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailerAliases");

            migrationBuilder.DeleteData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("a0f2b3ad-cc52-472e-8390-50d41f3d3281"));

            migrationBuilder.DeleteData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("b0f2b3ad-cc52-472e-8390-50d41f3d3281"));

            migrationBuilder.DeleteData(
                table: "Retailers",
                keyColumn: "Id",
                keyValue: new Guid("c0f2b3ad-cc52-472e-8390-50d41f3d3281"));

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("11b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(2525), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(2528) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("22b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3164), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3164) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("33b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3171), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3171) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("44b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3174), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3174) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("55b223ad-cc52-472e-8390-50d41f3d3281"),
                columns: new[] { "CreatedDate", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3176), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3176) });
        }
    }
}
