from sqlalchemy import Column, Integer, String, Float, Boolean, JSON, DateTime, ForeignKey, Text
from sqlalchemy.orm import declarative_base, relationship
from datetime import datetime

Base = declarative_base()

class CandidateProfile(Base):
    __tablename__ = "candidate_profiles"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, nullable=False)
    location = Column(String)
    postcode = Column(String)
    salary_minimum = Column(Integer)
    salary_ideal = Column(Integer)
    max_commute_minutes = Column(Integer)
    remote_preference = Column(String) # Remote, Hybrid, Onsite, Any
    clearance_status = Column(String)

    # JSON fields for complex data
    skills = Column(JSON, default=list) # e.g. ["AWS", "Python", "Linux"]
    years_experience = Column(JSON, default=dict)
    industries = Column(JSON, default=list)
    certifications = Column(JSON, default=list)
    preferred_roles = Column(JSON, default=list)
    
    cv_text = Column(Text)
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    applications = relationship("Application", back_populates="profile")


class Job(Base):
    __tablename__ = "jobs"

    id = Column(Integer, primary_key=True, index=True)
    job_id = Column(String, unique=True, index=True) # Hash or source ID for deduplication
    title = Column(String, nullable=False)
    company = Column(String, nullable=False)
    location = Column(String)
    salary_min = Column(Integer)
    salary_max = Column(Integer)
    remote_type = Column(String)
    description = Column(Text)
    url = Column(String)
    source = Column(String) # LinkedIn, Indeed, etc.
    posted_date = Column(DateTime)
    contract_type = Column(String)
    skills = Column(JSON, default=list)
    clearance_required = Column(String)
    
    created_at = Column(DateTime, default=datetime.utcnow)

    applications = relationship("Application", back_populates="job")


class Application(Base):
    __tablename__ = "applications"

    id = Column(Integer, primary_key=True, index=True)
    profile_id = Column(Integer, ForeignKey("candidate_profiles.id"))
    job_id = Column(Integer, ForeignKey("jobs.id"))
    
    status = Column(String, default="Discovered") # Discovered, Applied, Interview, Rejected, Offer
    match_score = Column(Float)
    commute_time_mins = Column(Integer)
    commute_distance_miles = Column(Float)
    
    # AI Analysis fields
    ai_summary = Column(Text)
    ai_pros = Column(JSON, default=list)
    ai_cons = Column(JSON, default=list)
    chance_of_interview = Column(String)
    missing_skills = Column(JSON, default=list)
    
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

    profile = relationship("CandidateProfile", back_populates="applications")
    job = relationship("Job", back_populates="applications")
