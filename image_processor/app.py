from fastapi import FastAPI

from routers.health import router as health_router
from routers.plate import router as plate_router


app = FastAPI(title="Parking Image Processor", version="1.0.0")
app.include_router(health_router)
app.include_router(plate_router)
