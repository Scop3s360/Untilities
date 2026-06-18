from typing import List, Dict
import hashlib
from .models import CandidateProfile, Job
from .job_source import ScrapedJob

def generate_dedupe_hash(title: str, company: str) -> str:
    """
    Generates a unique hash to identify duplicate jobs across sources.
    Matches heavily on normalized Title + Company.
    """
    normalised_string = f"{title.lower().strip()}|{company.lower().strip()}"
    return hashlib.md5(normalised_string.encode('utf-8')).hexdigest()

def calculate_match_score(job: Job, profile: CandidateProfile) -> float:
    """
    Match Scoring Engine
    Weights:
    Skills = 40%
    Experience = 25%
    Salary = 15%
    Commute = 10%
    Industry Match = 5%
    Clearance Match = 5%
    """
    score = 0.0
    
    # Skills Match (40 points)
    if job.skills and profile.skills:
        job_skills = set([s.lower() for s in job.skills])
        profile_skills = set([s.lower() for s in profile.skills])
        if job_skills:
            intersection = job_skills.intersection(profile_skills)
            score += (len(intersection) / len(job_skills)) * 40

    # Salary Match (15 points)
    if job.salary_max and profile.salary_minimum:
        if job.salary_max >= profile.salary_minimum:
            score += 15
        elif job.salary_min and job.salary_min >= profile.salary_minimum:
            score += 15
        else:
            # Partial score if max is slightly below minimum
            deficit = profile.salary_minimum - job.salary_max
            if deficit < 5000:
                score += 5

    # Commute Match (10 points) - Simplified logic
    # Real logic would use Maps API for driving time
    if job.remote_type == "Remote":
        score += 10
    elif profile.max_commute_minutes and profile.max_commute_minutes >= 60:
        score += 8 # Assume commutable if willing to travel 1hr+
    else:
        score += 5 # Unknown commute

    # Clearance Match (5 points)
    if job.clearance_required and profile.clearance_status:
        # Standardise clearance strings
        job_clear = job.clearance_required.lower()
        prof_clear = profile.clearance_status.lower()
        if "dv" in job_clear and "dv" in prof_clear:
            score += 5
        elif "sc" in job_clear and ("sc" in prof_clear or "dv" in prof_clear):
            score += 5

    # Experience & Industry (30 points remaining, simplified for MVP)
    score += 20 # Baseline for passing initial filters
    
    return min(100.0, score)

def identify_missing_skills(job: Job, profile: CandidateProfile) -> List[str]:
    """
    Identify skills required by the job but missing from the profile.
    """
    if not job.skills or not profile.skills:
        return []
        
    job_skills = set([s.lower() for s in job.skills])
    profile_skills = set([s.lower() for s in profile.skills])
    
    missing = job_skills - profile_skills
    return list(missing)
