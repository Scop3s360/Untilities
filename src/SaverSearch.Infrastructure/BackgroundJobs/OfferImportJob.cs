namespace SaverSearch.Infrastructure.BackgroundJobs;

public class OfferImportJob
{
    public Task ExecuteAsync()
    {
        // Future-proof: Background job scheduled to run scraper engines and update SQLite/DB offers daily/hourly
        return Task.CompletedTask;
    }
}
