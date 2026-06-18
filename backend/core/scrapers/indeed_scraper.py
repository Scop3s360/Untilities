import logging
import urllib.parse
from typing import List
from datetime import datetime
from bs4 import BeautifulSoup

from ..scraper_utils import browser_manager, random_delay
from ..job_source import JobSource, ScrapedJob

logger = logging.getLogger(__name__)

class IndeedScraper(JobSource):
    """
    A concrete scraper for Indeed using Playwright to handle dynamic rendering.
    """
    BASE_URL = "https://uk.indeed.com"

    async def search(self, keywords: str, location: str) -> List[str]:
        """
        Search for jobs and return a list of URLs using Playwright.
        """
        encoded_keywords = urllib.parse.quote(keywords)
        encoded_location = urllib.parse.quote(location)
        url = f"{self.BASE_URL}/jobs?q={encoded_keywords}&l={encoded_location}"
        
        logger.info(f"Searching Indeed for '{keywords}' in '{location}' at {url}")
        
        context = await browser_manager.get_new_context()
        page = await context.new_page()
        
        job_urls = []
        try:
            await page.goto(url, wait_until="domcontentloaded", timeout=30000)
            await random_delay(2, 4) # Simulate human
            
            # Extract job cards
            html = await page.content()
            soup = BeautifulSoup(html, 'html.parser')
            
            for a in soup.find_all('a', id=lambda x: x and x.startswith('job_')):
                href = a.get('href')
                if href:
                    full_url = self.BASE_URL + href if href.startswith('/') else href
                    job_urls.append(full_url)
                    
        except Exception as e:
            logger.error(f"Indeed search failed: {e}")
        finally:
            await page.close()
            
        return job_urls[:10]

    async def extract(self, url: str) -> dict:
        """
        Extract raw data from a specific job posting on Indeed.
        """
        logger.debug(f"Extracting Indeed job details from {url}")
        context = await browser_manager.get_new_context()
        page = await context.new_page()
        
        try:
            await page.goto(url, wait_until="domcontentloaded", timeout=30000)
            await random_delay(1, 3)
            
            html = await page.content()
            soup = BeautifulSoup(html, 'html.parser')
            
            def get_text(selector):
                elem = soup.select_one(selector)
                return elem.get_text(strip=True) if elem else ""

            title = get_text('h1') or get_text('.jobsearch-JobInfoHeader-title')
            company = get_text('[data-testid="inlineHeader-companyName"]')
            location = get_text('[data-testid="inlineHeader-companyLocation"]')
            
            # Description
            desc_elem = soup.find('div', id='jobDescriptionText')
            description = desc_elem.get_text(separator='\n', strip=True) if desc_elem else ""

            # Salary parsing
            salary_text = get_text('#salaryInfoAndJobType')
            
            return {
                "url": url,
                "title": title,
                "company": company,
                "location": location,
                "salary_text": salary_text,
                "description": description,
                "posted_date": datetime.utcnow().isoformat()
            }
        except Exception as e:
            logger.error(f"Indeed extraction failed for {url}: {e}")
            return {}
        finally:
            await page.close()

    def normalise(self, raw_data: dict) -> ScrapedJob:
        """
        Convert raw Indeed data into our common ScrapedJob schema.
        """
        if not raw_data:
            logger.warning("No raw data to normalise.")
            raise ValueError("Empty raw data")
            
        import urllib.parse as urlparse
        parsed = urlparse.urlparse(raw_data.get("url", ""))
        qs = urlparse.parse_qs(parsed.query)
        source_id = qs.get("jk", ["unknown"])[0] if "jk" in qs else parsed.path.split("-")[-1]
        
        salary_text = raw_data.get("salary_text", "").replace(",", "")
        import re
        numbers = re.findall(r'\d{4,}', salary_text)
        
        salary_min = int(numbers[0]) if len(numbers) > 0 else None
        salary_max = int(numbers[1]) if len(numbers) > 1 else salary_min

        remote_type = "Remote" if "remote" in raw_data.get("location", "").lower() or "remote" in raw_data.get("title", "").lower() else "Onsite"

        return ScrapedJob(
            source_id=f"indeed_{source_id}",
            title=raw_data.get("title", "Unknown Title"),
            company=raw_data.get("company", "Unknown Company"),
            location=raw_data.get("location", "Unknown Location"),
            salary_min=salary_min,
            salary_max=salary_max,
            remote_type=remote_type,
            description=raw_data.get("description", ""),
            url=raw_data.get("url", ""),
            source="Indeed",
            posted_date=datetime.utcnow()
        )
