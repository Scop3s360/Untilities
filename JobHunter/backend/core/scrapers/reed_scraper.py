import logging
import urllib.parse
from typing import List
import requests
from bs4 import BeautifulSoup
from datetime import datetime

logger = logging.getLogger(__name__)

from ..job_source import JobSource, ScrapedJob

class ReedScraper(JobSource):
    """
    A concrete scraper for Reed.co.uk using BeautifulSoup and Requests.
    Reed is generally friendly to scraping public pages.
    """
    BASE_URL = "https://www.reed.co.uk"

    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update({
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        })

    async def search(self, keywords: str, location: str) -> List[str]:
        """
        Search for jobs and return a list of URLs.
        """
        encoded_keywords = urllib.parse.quote(keywords)
        encoded_location = urllib.parse.quote(location)
        url = f"{self.BASE_URL}/jobs/{encoded_keywords}-jobs-in-{encoded_location}"
        
        logger.info(f"Searching Reed for '{keywords}' in '{location}' at {url}")
        
        try:
            response = self.session.get(url, timeout=10)
            response.raise_for_status()
        except requests.RequestException as e:
            logger.error(f"Failed to fetch search page from Reed: {e}")
            return []
            
        soup = BeautifulSoup(response.text, 'html.parser')
        job_urls = []
        
        # Reed job cards typically have the class 'job-result-card' or similar, 
        # and links contain '/jobs/' followed by the title and ID.
        # A robust way is looking for <a> tags within article cards.
        for article in soup.find_all('article', class_='job-result-card'):
            title_elem = article.find('h2', class_='job-result-heading__title')
            if title_elem and title_elem.find('a'):
                href = title_elem.find('a').get('href')
                if href:
                    full_url = self.BASE_URL + href if href.startswith('/') else href
                    job_urls.append(full_url)
                    
        # Let's fallback to finding any link with '/jobs/' and a numeric ID if structure changed
        if not job_urls:
            import re
            for a in soup.find_all('a', href=True):
                href = a['href']
                path = urllib.parse.urlparse(href).path
                # Real Reed job links end with a 6-9 digit number (the job ID)
                if '/jobs/' in path and re.search(r'/\d{6,9}$', path):
                    full_url = self.BASE_URL + href if href.startswith('/') else href
                    if full_url not in job_urls:
                        job_urls.append(full_url)
        
        # Return top 10 for now
        return job_urls[:10]

    async def extract(self, url: str) -> dict:
        """
        Extract raw data from a specific job posting on Reed.
        """
        logger.debug(f"Extracting job details from {url}")
        try:
            response = self.session.get(url, timeout=10)
            response.raise_for_status()
        except requests.RequestException as e:
            logger.error(f"Failed to fetch job details from {url}: {e}")
            return {}
            
        soup = BeautifulSoup(response.text, 'html.parser')
        
        # Safe extraction helpers
        def get_text(selector, attr='text'):
            elem = soup.select_one(selector)
            if not elem: return ""
            return elem.get_text(strip=True) if attr == 'text' else elem.get(attr, '')

        title = get_text('h1')
        company = get_text('span[itemprop="name"]') or get_text('.posted-by a')
        location = get_text('span[itemprop="addressLocality"]') or get_text('.location span')
        
        # Description
        desc_elem = soup.find('span', itemprop='description') or soup.find('div', class_='description')
        description = desc_elem.get_text(separator='\n', strip=True) if desc_elem else ""

        # Salary parsing (very basic)
        salary_text = get_text('.salary span')
        
        raw_data = {
            "url": url,
            "title": title,
            "company": company,
            "location": location,
            "salary_text": salary_text,
            "description": description,
            "posted_date": datetime.utcnow().isoformat() # Placeholder, would parse actual date
        }
        return raw_data

    def normalise(self, raw_data: dict) -> ScrapedJob:
        """
        Convert raw Reed data into our common ScrapedJob schema.
        """
        if not raw_data:
            logger.warning("No raw data to normalise.")
            raise ValueError("Empty raw data")
            
        # Extract ID from URL (usually the last part of reed URLs)
        source_id = raw_data.get("url", "").split("/")[-1]
        
        # Attempt to parse min/max from string like "£40,000 - £50,000 per annum"
        salary_text = raw_data.get("salary_text", "").replace(",", "")
        import re
        numbers = re.findall(r'\d{4,}', salary_text)
        
        salary_min = int(numbers[0]) if len(numbers) > 0 else None
        salary_max = int(numbers[1]) if len(numbers) > 1 else salary_min

        remote_type = "Remote" if "remote" in raw_data.get("location", "").lower() or "remote" in raw_data.get("title", "").lower() else "Onsite"

        return ScrapedJob(
            source_id=f"reed_{source_id}",
            title=raw_data.get("title", "Unknown Title"),
            company=raw_data.get("company", "Unknown Company"),
            location=raw_data.get("location", "Unknown Location"),
            salary_min=salary_min,
            salary_max=salary_max,
            remote_type=remote_type,
            description=raw_data.get("description", ""),
            url=raw_data.get("url", ""),
            source="Reed",
            posted_date=datetime.utcnow()
        )
