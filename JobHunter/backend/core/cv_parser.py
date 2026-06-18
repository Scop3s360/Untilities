from pydantic import BaseModel, Field
from typing import List, Optional
import re

class ExtractedCVProfile(BaseModel):
    name: str = Field(description="Full name of the candidate")
    skills: List[str] = Field(description="List of technical skills, e.g. AWS, Python, Linux")
    years_experience: dict = Field(default_factory=dict, description="Dictionary mapping skills to years of experience")
    industries: List[str] = Field(default_factory=list, description="Industries worked in")
    certifications: List[str] = Field(default_factory=list, description="List of certifications")
    locations: List[str] = Field(default_factory=list, description="Locations the candidate has lived or worked in")
    preferred_roles: List[str] = Field(default_factory=list, description="Job titles that fit this profile")
    clearance_level: Optional[str] = Field(default=None, description="Mentioned security clearance")

# Comprehensive list of IT skills to search for
TECH_SKILLS = [
    "Python", "Java", "JavaScript", "TypeScript", "C#", "C++", "Ruby", "Go", "Rust", "PHP",
    "React", "Angular", "Vue", "Node.js", "Django", "Flask", "Spring", ".NET", "Express",
    "AWS", "Azure", "GCP", "Kubernetes", "Docker", "Terraform", "Ansible", "Jenkins",
    "SQL", "MySQL", "PostgreSQL", "MongoDB", "Redis", "Cassandra", "Elasticsearch",
    "Kafka", "Spark", "Hadoop", "RabbitMQ", "GraphQL", "REST API", "CI/CD", "Linux",
    "Bash", "Git", "Machine Learning", "Data Science", "DevOps", "Agile", "Scrum"
]

CLEARANCE_LEVELS = {
    "DV": r"\b(dv|developed vetting)\b",
    "SC": r"\b(sc|security check|security cleared)\b",
    "CTC": r"\b(ctc|counter terrorist check)\b",
    "BPSS": r"\b(bpss|baseline personnel security standard)\b"
}

async def parse_cv_text(cv_text: str) -> ExtractedCVProfile:
    """
    Parses raw CV text and extracts structured candidate profile data using RegEx and Keywords.
    (100% Offline, No AI API required!)
    """
    # 1. Extract Name (Heuristic: First non-empty line of the CV)
    lines = [line.strip() for line in cv_text.split("\n") if line.strip()]
    name = "Unknown Candidate"
    if lines:
        name = lines[0]
        # Clean up common header junk if present
        name = re.sub(r'^(CV|Curriculum Vitae|Resume|Profile)\s*[-:]?\s*', '', name, flags=re.IGNORECASE).strip()
        # Fallback if the first line was literally just "CV"
        if len(name) < 2 and len(lines) > 1:
            name = lines[1].strip()

    # 2. Extract Skills
    text_lower = cv_text.lower()
    found_skills = []
    
    for skill in TECH_SKILLS:
        skill_lower = skill.lower()
        # Handle skills with special characters carefully (like C++, C#, .NET)
        if skill_lower in ["c++", "c#", ".net", "node.js"]:
            if skill_lower in text_lower:
                found_skills.append(skill)
        else:
            # Use word boundaries for standard words to avoid matching "Go" inside "Good"
            if re.search(r'\b' + re.escape(skill_lower) + r'\b', text_lower):
                found_skills.append(skill)

    # 3. Extract Clearance
    clearance_level = None
    for level, pattern in CLEARANCE_LEVELS.items():
        if re.search(pattern, text_lower):
            clearance_level = level
            break

    # Return structured profile
    return ExtractedCVProfile(
        name=name,
        skills=list(set(found_skills)), # Deduplicate just in case
        years_experience={},
        industries=[],
        certifications=[],
        locations=[],
        preferred_roles=[],
        clearance_level=clearance_level
    )
