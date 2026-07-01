using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaverSearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfferTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Website = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Retailers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Website = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retailers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Retailers_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RetailerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfferTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MinimumSpend = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    MaximumReward = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Terms = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OfferUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsExclusive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_OfferTypes_OfferTypeId",
                        column: x => x.OfferTypeId,
                        principalTable: "OfferTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("23a2a3ad-cc52-472e-8390-50d41f3d3281"), "TVs, laptops, phones, and smart home appliances", true, "Electronics" },
                    { new Guid("3d64c148-d3c5-4148-912f-db4fe72b0c95"), "Furniture, garden, and home improvement decor", true, "Home" },
                    { new Guid("501bd346-63e8-466d-b873-67ca2b6eb2b5"), "Clothing, footwear, and accessories", true, "Fashion" },
                    { new Guid("8b72de70-c081-420a-9d62-f6cb3ce183a3"), "Groceries, weekly shops, and household essentials", true, "Supermarkets" },
                    { new Guid("9a8cd34a-9ef8-4ca7-9e7f-b67f2e1a3bc8"), "Hotels, flights, holidays, and transport", true, "Travel" }
                });

            migrationBuilder.InsertData(
                table: "OfferTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("11c223ad-cc52-472e-8390-50d41f3d3281"), "Return of a percentage of the amount spent to the consumer", "Cashback" },
                    { new Guid("22c223ad-cc52-472e-8390-50d41f3d3281"), "Direct price reduction applied to purchase price", "Discount" },
                    { new Guid("33c223ad-cc52-472e-8390-50d41f3d3281"), "Loyalty system points earned per dollar/pound spent", "Reward Points" },
                    { new Guid("44c223ad-cc52-472e-8390-50d41f3d3281"), "A discount code or certificate for specific promotions", "Voucher" },
                    { new Guid("55c223ad-cc52-472e-8390-50d41f3d3281"), "Cashback specifically directed to pay down linked mortgage balances", "Mortgage Cashback" }
                });

            migrationBuilder.InsertData(
                table: "Providers",
                columns: new[] { "Id", "CreatedDate", "Description", "IsActive", "LogoUrl", "Name", "UpdatedDate", "Website" },
                values: new object[,]
                {
                    { new Guid("11b223ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(2525), "UK's highest paying cashback site", true, null, "TopCashback", new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(2528), "https://www.topcashback.co.uk" },
                    { new Guid("22b223ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3164), "Great rates and easy cashback search", true, null, "Quidco", new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3164), "https://www.quidco.com" },
                    { new Guid("33b223ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3171), "Smart app offering cashback used to pay down mortgages", true, null, "Sprive", new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3171), "https://sprive.com" },
                    { new Guid("44b223ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3174), "Credit card reward schemes and retailer cashback partnerships", true, null, "Barclaycard", new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3174), "https://www.barclaycard.co.uk" },
                    { new Guid("55b223ad-cc52-472e-8390-50d41f3d3281"), new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3176), "1% cashback on eligible everyday debit card spending", true, null, "Chase", new DateTime(2026, 7, 1, 8, 22, 15, 891, DateTimeKind.Utc).AddTicks(3176), "https://www.chase.co.uk" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OfferTypeId",
                table: "Offers",
                column: "OfferTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ProviderId",
                table: "Offers",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_RetailerId",
                table: "Offers",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_Retailers_CategoryId",
                table: "Retailers",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Retailers_Slug",
                table: "Retailers",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "OfferTypes");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Retailers");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
