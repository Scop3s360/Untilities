import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

# Default to local SQLite database if no DATABASE_URL is provided in environment
DATABASE_URL = os.getenv("DATABASE_URL", "sqlite:///./jobhunter.db")

# SQLite needs specific connect_args in FastAPI
if DATABASE_URL.startswith("sqlite"):
    engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
else:
    engine = create_engine(DATABASE_URL)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
