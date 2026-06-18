from abc import ABC, abstractmethod
from typing import List
from pydantic import BaseModel
from datetime import datetime
from typing import Optional

class ScrapedJob(BaseModel):
    source_id: str
    title: str
    company: str
    location: str
    salary_min: Optional[int] = None
    salary_max: Optional[int] = None
    remote_type: Optional[str] = None
    description: str
    url: str
    posted_date: Optional[datetime] = None
    contract_type: Optional[str] = None
    source: str

class JobSource(ABC):
    """
    Abstract base class for all job scrapers/providers.
    """
    
    @abstractmethod
    async def search(self, keywords: str, location: str) -> List[str]:
        """
        Search for jobs and return a list of URLs or IDs.
        """
        pass

    @abstractmethod
    async def extract(self, url_or_id: str) -> dict:
        """
        Extract raw data from a specific job posting.
        """
        pass

    @abstractmethod
    def normalise(self, raw_data: dict) -> ScrapedJob:
        """
        Convert raw provider data into our common ScrapedJob schema.
        """
        pass

    async def fetch_jobs(self, keywords: str, location: str) -> List[ScrapedJob]:
        """
        Helper method to perform full pipeline: search -> extract -> normalise.
        """
        urls = await self.search(keywords, location)
        jobs = []
        for url in urls:
            try:
                raw = await self.extract(url)
                jobs.append(self.normalise(raw))
            except Exception as e:
                print(f"Failed to fetch job at {url}: {e}")
        return jobs
