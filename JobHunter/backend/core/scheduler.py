import logging
from apscheduler.schedulers.asyncio import AsyncIOScheduler
from datetime import datetime
from .database import SessionLocal
from .models import Job
from .scrapers.reed_scraper import ReedScraper
from .scrapers.indeed_scraper import IndeedScraper

logger = logging.getLogger(__name__)
scheduler = AsyncIOScheduler()

async def daily_scrape_job():
    """
    Background job to trigger scraping across all providers
    """
    logger.info("Starting daily job scrape...")
    
    scrapers = [ReedScraper(), IndeedScraper()]
    
    db = SessionLocal()
    try:
        from .models import CandidateProfile
        profile = db.query(CandidateProfile).first()
        
        keywords = "Software Engineer"
        location = "London"
        
        if profile and profile.location:
            location = profile.location
            if profile.preferred_roles and len(profile.preferred_roles) > 0:
                keywords = profile.preferred_roles[0]
            elif profile.skills and len(profile.skills) > 0:
                keywords = f"{profile.skills[0]} Developer"

        for scraper in scrapers:
            logger.info(f"Running scraper: {scraper.__class__.__name__} for {keywords} in {location}")
            jobs = await scraper.fetch_jobs(keywords, location)
            for job_data in jobs:
                # Basic dedup
                existing = db.query(Job).filter(Job.job_id == job_data.source_id).first()
                if not existing:
                    new_job = Job(
                        job_id=job_data.source_id,
                        title=job_data.title,
                        company=job_data.company,
                        location=job_data.location,
                        salary_min=job_data.salary_min,
                        salary_max=job_data.salary_max,
                        remote_type=job_data.remote_type,
                        description=job_data.description,
                        url=job_data.url,
                        source=job_data.source,
                        posted_date=job_data.posted_date
                    )
                    db.add(new_job)
        db.commit()
        logger.info(f"Scrape complete. Found {len(jobs)} jobs.")
    except Exception as e:
        logger.error(f"Scraper failed: {e}", exc_info=True)
    finally:
        db.close()

async def generate_daily_digest():
    """
    Background job to score new jobs and email the digest to the user
    """
    logger.info("Generating daily digest...")
    # In full implementation, this runs scoring logic and sends an email/discord notification

def start_scheduler():
    """
    Initialize and start the background scheduler
    """
    # Scrape at 2:00 AM
    scheduler.add_job(daily_scrape_job, 'cron', hour=2, minute=0)
    
    # Send digest at 7:00 AM (Wake up every morning with a shortlist!)
    scheduler.add_job(generate_daily_digest, 'cron', hour=7, minute=0)
    
    scheduler.start()
    logger.info("APScheduler started successfully.")
