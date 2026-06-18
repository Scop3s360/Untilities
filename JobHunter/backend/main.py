import logging
import os
from fastapi import FastAPI, Depends, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from sqlalchemy.orm import Session
from .core import models
from .core.database import engine, get_db
from .core.scheduler import start_scheduler

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

# Create DB tables on startup (For MVP, later we use Alembic)
models.Base.metadata.create_all(bind=engine)

app = FastAPI(title="Job Hunter AI API")

@app.on_event("startup")
async def startup_event():
    start_scheduler()

# Configure CORS for frontend access
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/api/status")
def read_root():
    return {"status": "Job Hunter AI Backend is Running"}


@app.get("/profile")
def get_profile(db: Session = Depends(get_db)):
    profile = db.query(models.CandidateProfile).first()
    if not profile:
        raise HTTPException(status_code=404, detail="Profile not found")
    return profile

@app.post("/profile")
def create_or_update_profile(profile_data: dict, db: Session = Depends(get_db)):
    profile = db.query(models.CandidateProfile).first()
    if not profile:
        profile = models.CandidateProfile(**profile_data)
        db.add(profile)
    else:
        for key, value in profile_data.items():
            setattr(profile, key, value)
    db.commit()
    db.refresh(profile)
    return profile

@app.get("/applications")
def get_applications(db: Session = Depends(get_db)):
    apps = db.query(models.Application).all()
    return apps

@app.get("/jobs")
def get_jobs(db: Session = Depends(get_db)):
    jobs = db.query(models.Job).all()
    return jobs

@app.post("/trigger-scrape")
async def trigger_scrape():
    """Manual trigger for scraping"""
    from .core.scheduler import daily_scrape_job
    import asyncio
    asyncio.create_task(daily_scrape_job())
    return {"status": "Scrape job triggered in background"}

@app.post("/parse-cv")
async def parse_cv(cv_data: dict, db: Session = Depends(get_db)):
    """Parse CV text and update profile"""
    from .core.cv_parser import parse_cv_text
    cv_text = cv_data.get("cv_text")
    if not cv_text:
        raise HTTPException(status_code=400, detail="No cv_text provided")
    
    extracted = await parse_cv_text(cv_text)
    
    # Update profile
    profile = db.query(models.CandidateProfile).first()
    if not profile:
        profile = models.CandidateProfile(name=extracted.name)
        db.add(profile)
        
    profile.skills = extracted.skills
    profile.years_experience = extracted.years_experience
    profile.industries = extracted.industries
    profile.certifications = extracted.certifications
    profile.clearance_status = extracted.clearance_level
    profile.cv_text = cv_text
    
    db.commit()
    db.refresh(profile)
    return {"status": "success", "profile": profile}

# Serve static frontend files - MUST be at the end to not catch API routes
frontend_dir = os.path.join(os.path.dirname(__file__), "..", "frontend")
if os.path.isdir(frontend_dir):
    app.mount("/", StaticFiles(directory=frontend_dir, html=True), name="frontend")
