import asyncio
from core.scrapers.reed_scraper import ReedScraper

async def main():
    print("Testing ReedScraper...")
    scraper = ReedScraper()
    jobs = await scraper.fetch_jobs("Python Developer", "London")
    print(f"Found {len(jobs)} jobs:")
    for job in jobs:
        print(f"- {job.title} at {job.company} ({job.url})")

if __name__ == "__main__":
    asyncio.run(main())
