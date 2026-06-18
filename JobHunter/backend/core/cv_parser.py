import os
import json
from openai import AsyncOpenAI
from pydantic import BaseModel, Field
from typing import List, Optional

# Load OpenAI API Key from environment
client = AsyncOpenAI(api_key=os.environ.get("OPENAI_API_KEY"))

class ExtractedCVProfile(BaseModel):
    name: str = Field(description="Full name of the candidate")
    skills: List[str] = Field(description="List of technical skills, e.g. AWS, Python, Linux")
    years_experience: dict = Field(description="Dictionary mapping skills to years of experience based on the timeline. e.g. {'AWS': 3, 'Python': 5}")
    industries: List[str] = Field(description="Industries worked in, e.g. Defence, Logistics")
    certifications: List[str] = Field(description="List of certifications")
    locations: List[str] = Field(description="Locations the candidate has lived or worked in")
    preferred_roles: List[str] = Field(description="Job titles that fit this profile, e.g. DevOps Engineer, Cloud Architect")
    clearance_level: Optional[str] = Field(description="Mentioned security clearance, e.g. DV, SC. Can be 'Expired DV'. Null if none.")

async def parse_cv_text(cv_text: str) -> ExtractedCVProfile:
    """
    Parses raw CV text and extracts structured candidate profile data using OpenAI.
    """
    prompt = f"""
    You are an expert technical recruiter and CV parser. 
    Analyze the following CV text and extract the required information into the defined JSON schema.
    
    CV Text:
    {cv_text}
    """

    response = await client.chat.completions.create(
        model="gpt-4o",
        messages=[
            {"role": "system", "content": "You are a helpful assistant that parses resumes into structured JSON."},
            {"role": "user", "content": prompt}
        ],
        response_format={
            "type": "json_schema",
            "json_schema": {
                "name": "candidate_profile_schema",
                "schema": ExtractedCVProfile.model_json_schema()
            }
        },
        temperature=0.0,
    )
    
    raw_json = response.choices[0].message.content
    data = json.loads(raw_json)
    return ExtractedCVProfile(**data)
