import asyncio
import random
from playwright.async_api import async_playwright, Browser, BrowserContext, Page

class PlaywrightManager:
    """
    Manages a singleton Playwright browser instance to be shared across scrapers.
    """
    def __init__(self):
        self.playwright = None
        self.browser: Browser = None

    async def start(self):
        if not self.playwright:
            self.playwright = await async_playwright().start()
        if not self.browser:
            self.browser = await self.playwright.chromium.launch(headless=True)
            
    async def get_new_context(self) -> BrowserContext:
        if not self.browser:
            await self.start()
        
        # Add basic stealth/anti-bot headers and settings
        context = await self.browser.new_context(
            user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            viewport={"width": 1920, "height": 1080}
        )
        return context

    async def stop(self):
        if self.browser:
            await self.browser.close()
        if self.playwright:
            await self.playwright.stop()

# Singleton instance
browser_manager = PlaywrightManager()

async def random_delay(min_seconds: float = 1.0, max_seconds: float = 3.0):
    """Wait for a random amount of time to simulate human behavior."""
    delay = random.uniform(min_seconds, max_seconds)
    await asyncio.sleep(delay)
