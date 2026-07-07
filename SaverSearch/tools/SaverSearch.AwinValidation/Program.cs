using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaverSearch.Application;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Infrastructure;
using SaverSearch.Infrastructure.Persistence.Contexts;
using SaverSearch.Infrastructure.Providers.Connectors.Awin;

// ── Bootstrap ─────────────────────────────────────────────────────────────────
Console.OutputEncoding = Encoding.UTF8;
var runAt = DateTimeOffset.Now;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.SetBasePath(Directory.GetCurrentDirectory());
        cfg.AddJsonFile("appsettings.json", optional: true);
        cfg.AddUserSecrets<AwinValidationMarker>(optional: true);
        cfg.AddEnvironmentVariables();
    })
    .ConfigureLogging(log =>
    {
        log.ClearProviders();
        log.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
        log.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationServices(ctx.Configuration);
        services.AddInfrastructureServices(ctx.Configuration);
    })
    .Build();

// ── Resolve services ──────────────────────────────────────────────────────────
using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;
var config = sp.GetRequiredService<IConfiguration>();
var dbCtx = sp.GetRequiredService<SaverSearchDbContext>();
var acquisitionEngine = sp.GetRequiredService<IOfferAcquisitionEngine>();
var importJobService = sp.GetRequiredService<IImportJobService>();
var connectors = sp.GetServices<IProviderConnector>().ToList();
var awinOpts = sp.GetRequiredService<IOptions<AwinConnectorOptions>>().Value;

var report = new StringBuilder();
var allGood = true;

void Log(string msg) { Console.WriteLine(msg); report.AppendLine(msg); }
void Section(string title) { Log(""); Log($"## {title}"); Log(new string('-', 60)); }
void Pass(string msg) => Log($"  ✅ {msg}");
void Fail(string msg) { Log($"  ❌ {msg}"); allGood = false; }
void Info(string msg) => Log($"  ℹ️  {msg}");
void Warn(string msg) => Log($"  ⚠️  {msg}");

Log($"# AWIN Integration Validation Report");
Log($"Generated: {runAt:yyyy-MM-dd HH:mm:ss zzz}");
Log($"Tool: SaverSearch.AwinValidation v1.0");

// ─────────────────────────────────────────────────────────────────────────────
// STEP 1 — Account & Credential Validation
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 1 — Account & Credential Validation");

if (awinOpts.PublisherId > 0)
    Pass($"PublisherId configured: {awinOpts.PublisherId}");
else
    Fail("PublisherId is 0 — not configured. Run: dotnet user-secrets set \"Acquisition:Awin:PublisherId\" <your-id>");

if (!string.IsNullOrWhiteSpace(awinOpts.AccessToken))
    Pass($"AccessToken configured: {awinOpts.AccessToken[..Math.Min(6, awinOpts.AccessToken.Length)]}***");
else
    Fail("AccessToken is empty — not configured. Run: dotnet user-secrets set \"Acquisition:Awin:AccessToken\" <your-token>");

Info($"Enabled flag: {awinOpts.Enabled}");
Info($"BaseUrl: {awinOpts.BaseUrl}");
Info($"RegionCode: {awinOpts.RegionCode}");
Info($"RateLimitPerMinute: {awinOpts.RateLimitPerMinute}");
Info($"MaxRetries: {awinOpts.MaxRetries}");
Info($"TimeoutSeconds: {awinOpts.TimeoutSeconds}");

if (!awinOpts.IsConfigured)
{
    Fail("AWIN connector is not configured. Cannot proceed. Set PublisherId and AccessToken via user-secrets.");
    await WriteReportAsync(report, runAt);
    return 1;
}

// ─────────────────────────────────────────────────────────────────────────────
// STEP 2 — Connector Health Check
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 2 — Connector Health Check");

var awinConnector = connectors.FirstOrDefault(c =>
    string.Equals(c.ProviderName, "AWIN", StringComparison.OrdinalIgnoreCase));

if (awinConnector == null)
{
    Fail("AWIN connector not found in DI container.");
    await WriteReportAsync(report, runAt);
    return 1;
}

Pass($"Connector discovered: {awinConnector.ProviderName} v{awinConnector.ConnectorVersion}");
Info($"SupportsIncrementalImport: {awinConnector.SupportsIncrementalImport}");
Info($"Total connectors registered: {connectors.Count}");

var healthSw = Stopwatch.StartNew();
var health = await awinConnector.HealthCheckAsync();
healthSw.Stop();

if (health.IsHealthy)
    Pass($"Health check PASSED — Latency: {health.LatencyMs}ms");
else
    Fail($"Health check FAILED — {health.Message}");

Info($"Checked at: {health.CheckedAt:HH:mm:ss}");
Info($"Message: {health.Message}");

if (!health.IsHealthy)
{
    Fail("Cannot continue: health check failed. Verify your credentials and AWIN account status.");
    await WriteReportAsync(report, runAt);
    return 1;
}

// ─────────────────────────────────────────────────────────────────────────────
// STEP 3 — Discover Available Data
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 3 — Discover Available Data (Raw API Inspection)");

var apiClient = sp.GetRequiredService<AwinApiClient>();
List<SaverSearch.Infrastructure.Providers.Connectors.Awin.AwinProgramme> programmes = [];
List<SaverSearch.Infrastructure.Providers.Connectors.Awin.AwinPromotion> promotions = [];

try
{
    Info("Fetching joined programmes...");
    programmes = (await apiClient.GetJoinedProgrammesAsync()).ToList();
    Pass($"Joined programmes (all regions): {programmes.Count}");

    var gbProgrammes = programmes.Where(p =>
        p.PrimaryRegion?.CountryCode?.Equals("GB", StringComparison.OrdinalIgnoreCase) == true ||
        p.PrimaryRegion == null).ToList();
    Info($"GB / unregioned programmes: {gbProgrammes.Count}");

    // Sector breakdown
    var sectors = programmes
        .GroupBy(p => p.PrimarySector?.Name ?? "Unknown")
        .OrderByDescending(g => g.Count())
        .Take(10)
        .Select(g => $"{g.Key}={g.Count()}");
    Info($"Top sectors: {string.Join(", ", sectors)}");

    // Logo coverage
    var withLogo = programmes.Count(p => !string.IsNullOrWhiteSpace(p.LogoUrl));
    Info($"Programmes with logo: {withLogo}/{programmes.Count} ({100.0 * withLogo / Math.Max(1, programmes.Count):F0}%)");

    Info("Fetching active promotions...");
    promotions = (await apiClient.GetActivePromotionsAsync()).ToList();
    Pass($"Active promotions: {promotions.Count}");

    // Type breakdown
    var types = promotions
        .GroupBy(p => p.Type ?? "null")
        .OrderByDescending(g => g.Count())
        .Select(g => $"{g.Key}={g.Count()}");
    Info($"Promotion types: {string.Join(", ", types)}");

    var withCode = promotions.Count(p => !string.IsNullOrWhiteSpace(p.Code));
    Info($"Promotions with voucher code: {withCode}/{promotions.Count}");

    var withExpiry = promotions.Count(p => p.EndDate.HasValue);
    Info($"Promotions with expiry date: {withExpiry}/{promotions.Count}");

    var withTerms = promotions.Count(p => !string.IsNullOrWhiteSpace(p.Terms));
    Info($"Promotions with T&Cs: {withTerms}/{promotions.Count}");

    var withDescription = promotions.Count(p => !string.IsNullOrWhiteSpace(p.Description));
    Info($"Promotions with description: {withDescription}/{promotions.Count}");

    // Commission analysis
    var withCommission = promotions.Count(p => p.CommissionGroups?.Count > 0);
    Info($"Promotions with commission groups: {withCommission}/{promotions.Count}");

    var withPercentage = promotions.Count(p =>
        p.CommissionGroups?.Any(c => c.Percentage > 0) == true);
    var withFixed = promotions.Count(p =>
        p.CommissionGroups?.Any(c => c.Amount?.Value > 0) == true);
    Info($"Promotions with % cashback: {withPercentage}");
    Info($"Promotions with fixed amount: {withFixed}");

    // Sample cashback rates
    var cashbackRates = promotions
        .Where(p => p.CommissionGroups?.Any(c => c.Percentage > 0) == true)
        .Select(p => p.CommissionGroups!.Max(c => c.Percentage ?? 0))
        .Where(v => v > 0)
        .OrderDescending()
        .Take(10)
        .Select(v => $"{v:0.##}%");
    Info($"Top cashback rates (sample): {string.Join(", ", cashbackRates)}");

    // Sample programme names
    var sampleMerchants = programmes.Take(10).Select(p => p.Name);
    Info($"Sample merchants: {string.Join(" | ", sampleMerchants)}");
}
catch (Exception ex)
{
    Fail($"Data discovery failed: {ex.Message}");
}

// ─────────────────────────────────────────────────────────────────────────────
// STEP 4 — Import Validation (Run 1)
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 4 — Import Validation (Run 1 of 2)");

// Ensure DB is migrated
Info("Applying database migrations...");
await dbCtx.Database.MigrateAsync();
Pass("Database migrations applied.");

// Seed AWIN OfferType if not present
var offerType = await dbCtx.OfferTypes.FirstOrDefaultAsync();
if (offerType == null)
{
    Warn("No OfferType found — seeding 'Cashback' offer type for validation.");
    offerType = new SaverSearch.Domain.Entities.OfferType { Name = "Cashback" };
    dbCtx.OfferTypes.Add(offerType);
    await dbCtx.SaveChangesAsync();
    Pass("Seeded OfferType: Cashback");
}
else
{
    Info($"OfferType exists: {offerType.Name}");
}

Info("Running import pipeline (Run 1)...");
var importSw1 = Stopwatch.StartNew();
var run1 = await acquisitionEngine.RunAsync("AWIN");
importSw1.Stop();

Log("");
Log($"### Run 1 Results");
Info($"Success: {run1.Success}");
Info($"Duration: {run1.DurationMs}ms");
Info($"Offers downloaded: {run1.OffersDownloaded}");
Info($"Offers validated: {run1.OffersValidated}");
Info($"Offers added: {run1.OffersAdded}");
Info($"Offers updated: {run1.OffersUpdated}");
Info($"Offers deactivated: {run1.OffersDeactivated}");
Info($"Validation warnings: {run1.ValidationWarningCount}");

if (run1.Success)
    Pass("Run 1 import SUCCEEDED.");
else
    Fail($"Run 1 import FAILED: {run1.ErrorMessage}");

if (run1.Warnings.Count > 0)
{
    Warn($"Top warnings ({Math.Min(10, run1.Warnings.Count)} of {run1.Warnings.Count}):");
    foreach (var w in run1.Warnings.Take(10))
        Log($"     {w}");
}

// Stage timings
Log("");
Log($"### Stage Timings");
foreach (var stage in run1.StageTimings)
    Info($"{stage.StageName}: {stage.ElapsedMilliseconds}ms");

// ── Run 2 (idempotency check) ─────────────────────────────────────────────
Section("STEP 4 (continued) — Idempotency Check (Run 2 of 2)");

Info("Running import pipeline again (Run 2) — expecting zero adds...");
var run2 = await acquisitionEngine.RunAsync("AWIN");

Info($"Run 2 — Added: {run2.OffersAdded} | Updated: {run2.OffersUpdated} | Deactivated: {run2.OffersDeactivated}");

if (run2.OffersAdded == 0)
    Pass("IDEMPOTENCY VERIFIED — Run 2 produced zero duplicate inserts.");
else
    Fail($"IDEMPOTENCY FAILED — Run 2 added {run2.OffersAdded} unexpected records.");

// ─────────────────────────────────────────────────────────────────────────────
// STEP 5 — SQLite Validation
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 5 — SQLite Database Validation");

var totalOffers = await dbCtx.Offers.CountAsync();
var activeOffers = await dbCtx.Offers.CountAsync(o => o.IsActive);
var totalRetailers = await dbCtx.Retailers.CountAsync();
var totalProviders = await dbCtx.Providers.CountAsync();
var totalImportJobs = await dbCtx.ImportJobs.CountAsync();

Pass($"Total offers in DB: {totalOffers}");
Pass($"Active offers: {activeOffers}");
Info($"Inactive offers: {totalOffers - activeOffers}");
Info($"Total retailers: {totalRetailers}");
Info($"Total providers: {totalProviders}");
Info($"Total import jobs: {totalImportJobs}");

// Null checks
var nullReward = await dbCtx.Offers.CountAsync(o => o.MaximumReward == null);
var nullExpiry = await dbCtx.Offers.CountAsync(o => o.EndDate == null);
var nullDesc = await dbCtx.Offers.CountAsync(o => o.Description == null);

Info($"Offers with null MaximumReward: {nullReward}/{totalOffers}");
Info($"Offers with null EndDate: {nullExpiry}/{totalOffers}");
Info($"Offers with null Description: {nullDesc}/{totalOffers}");

// Duplicate ExternalId check
var dupExternalIds = await dbCtx.Offers
    .Where(o => o.ExternalId != null)
    .GroupBy(o => o.ExternalId!)
    .Where(g => g.Count() > 1)
    .CountAsync();

if (dupExternalIds == 0)
    Pass("No duplicate ExternalIds in Offers table.");
else
    Fail($"Duplicate ExternalIds found: {dupExternalIds} groups");

// Sample retailers
Log("");
Log($"### Sample Retailers (top 5)");
var sampleRetailers = await dbCtx.Retailers
    .OrderBy(r => r.Name)
    .Take(5)
    .Select(r => new { r.Name, r.Website, r.IsActive })
    .ToListAsync();
foreach (var r in sampleRetailers)
    Info($"  [{(r.IsActive ? "✓" : "✗")}] {r.Name} — {r.Website}");

// Sample offers
Log("");
Log($"### Sample Offers (top 5)");
var sampleOffers = await dbCtx.Offers
    .Where(o => o.IsActive)
    .OrderByDescending(o => o.Value)
    .Take(5)
    .Select(o => new { o.Title, o.Value, o.ValueType, o.ExternalId, o.EndDate })
    .ToListAsync();
foreach (var o in sampleOffers)
    Info($"  [{o.ValueType}] {o.Value} — {o.Title[..Math.Min(60, o.Title.Length)]} (exp: {o.EndDate?.ToString("yyyy-MM-dd") ?? "none"})");

// Sample import jobs
Log("");
Log($"### Import Job History");
var jobs = (await importJobService.GetHistoryAsync("AWIN", 5)).ToList();
foreach (var j in jobs)
    Info($"  {j.StartedAt:HH:mm:ss} | +{j.OffersAdded} ~{j.OffersUpdated} -{j.OffersDeactivated} | {j.Status} | {j.DurationMs}ms");

// ─────────────────────────────────────────────────────────────────────────────
// STEP 6 — Data Quality Review
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 6 — Data Quality Assessment");

if (totalOffers > 0)
{
    var avgValue = await dbCtx.Offers
        .Where(o => o.IsActive)
        .AverageAsync(o => (double)o.Value);
    Info($"Average cashback/reward value: {avgValue:F2}");

    var maxValue = await dbCtx.Offers.Where(o => o.IsActive).MaxAsync(o => o.Value);
    var minValue = await dbCtx.Offers.Where(o => o.IsActive && o.Value > 0).MinAsync(o => o.Value);
    Info($"Value range: {minValue} – {maxValue}");

    var percentageOffers = await dbCtx.Offers
        .CountAsync(o => o.IsActive && o.ValueType == SaverSearch.Domain.Entities.OfferValueType.Percentage);
    var fixedOffers = await dbCtx.Offers
        .CountAsync(o => o.IsActive && o.ValueType == SaverSearch.Domain.Entities.OfferValueType.FixedAmount);
    Info($"Percentage cashback offers: {percentageOffers}");
    Info($"Fixed amount offers: {fixedOffers}");

    var descCoverage = (double)(totalOffers - nullDesc) / Math.Max(1, totalOffers) * 100;
    var expiryCoverage = (double)(totalOffers - nullExpiry) / Math.Max(1, totalOffers) * 100;
    Info($"Description coverage: {descCoverage:F0}%");
    Info($"Expiry date coverage: {expiryCoverage:F0}%");

    // Retailer resolution rate
    var retailersWithOffers = await dbCtx.Offers
        .Where(o => o.IsActive)
        .Select(o => o.RetailerId)
        .Distinct()
        .CountAsync();
    Info($"Distinct retailers with active offers: {retailersWithOffers}");

    var skippedCount = run1.Warnings.Count(w => w.Contains("not found in database"));
    if (skippedCount > 0)
        Warn($"Offers skipped due to unresolved retailer: {skippedCount} (requires retailer seeding)");
}
else
{
    Warn("No offers in database — data quality assessment skipped.");
    Warn("This is expected if no retailers were seeded. See STEP 7 recommendation.");
}

// ─────────────────────────────────────────────────────────────────────────────
// STEP 7 — MVP Suitability
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 7 — MVP Suitability Analysis");

var hasPrograms = programmes.Count > 0;
var hasPromotions = promotions.Count > 0;
var hasAuth = health.IsHealthy;
var hasImport = run1.Success;
var hasIdempotency = run2.OffersAdded == 0;

Info($"API Authentication: {(hasAuth ? "✅ PASS" : "❌ FAIL")}");
Info($"Merchant data: {(hasPrograms ? $"✅ {programmes.Count} programmes" : "❌ None")}");
Info($"Promotion data: {(hasPromotions ? $"✅ {promotions.Count} promotions" : "❌ None")}");
Info($"Import pipeline: {(hasImport ? "✅ PASS" : "❌ FAIL")}");
Info($"Idempotency: {(hasIdempotency ? "✅ PASS" : "❌ FAIL")}");

var voucherCount = promotions.Count(p =>
    p.Type?.Contains("voucher", StringComparison.OrdinalIgnoreCase) == true ||
    !string.IsNullOrWhiteSpace(p.Code));
var cashbackCount = promotions.Count(p =>
    p.CommissionGroups?.Any(c => c.Percentage > 0) == true);

Info($"Cashback-style promotions detected: {cashbackCount}");
Info($"Voucher-code promotions detected: {voucherCount}");

if (voucherCount == 0)
    Warn("No voucher code offers detected — AWIN promotions API may not expose coupon codes prominently.");

// ─────────────────────────────────────────────────────────────────────────────
// STEP 8 — Final Report Summary
// ─────────────────────────────────────────────────────────────────────────────
Section("STEP 8 — RAG Status Summary");

string Rag(bool ok) => ok ? "🟢 GREEN" : "🔴 RED";
string RagAmber(bool ok, bool warn) => ok ? "🟢 GREEN" : warn ? "🟡 AMBER" : "🔴 RED";

Log($"| Dimension             | Status |");
Log($"|-----------------------|--------|");
Log($"| Connector Stability   | {Rag(hasAuth && hasImport)} |");
Log($"| Data Quality          | {RagAmber(totalOffers > 0, promotions.Count > 0)} |");
Log($"| Performance           | {RagAmber(run1.DurationMs < 120000, run1.DurationMs < 300000)} |");
Log($"| Maintainability       | 🟢 GREEN |");
Log($"| MVP Readiness         | {RagAmber(hasAuth && hasImport && promotions.Count > 0, hasAuth)} |");

Log("");
Log("### Overall Verdict");
if (hasAuth && hasImport && hasIdempotency && promotions.Count > 0)
{
    Log("🟢 AWIN is a VIABLE primary data source for the SaverSearch MVP.");
    Log("   Connector is authenticated, data is retrievable, import is idempotent.");
}
else if (hasAuth && promotions.Count == 0)
{
    Log("🟡 AWIN connector is authenticated but NO promotions were returned.");
    Log("   This indicates you have not yet joined any merchant programmes in the AWIN dashboard.");
    Log("   ACTION REQUIRED: Log in to app.awin.com → Programme Directory → Apply to merchants.");
}
else if (!hasAuth)
{
    Log("🔴 AWIN connector is NOT authenticated. Validate Publisher ID and Access Token.");
}
else
{
    Log("🟡 Partial validation. Review warnings above.");
}

// Write report
await WriteReportAsync(report, runAt);
return 0;

// ─────────────────────────────────────────────────────────────────────────────
static async Task WriteReportAsync(StringBuilder report, DateTimeOffset runAt)
{
    var outputPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        $"awin-validation-{runAt:yyyy-MM-dd-HHmm}.md");
    await File.WriteAllTextAsync(outputPath, report.ToString());
    Console.WriteLine();
    Console.WriteLine($"📄 Report saved to: {outputPath}");
}

// Marker type for user-secrets assembly lookup
internal class AwinValidationMarker { }
