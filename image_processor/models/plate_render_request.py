from pydantic import BaseModel, Field


class PlateRenderRequest(BaseModel):
	car_type: str = Field(min_length=1, max_length=100)
	license_plate: str = Field(min_length=1, max_length=32)
	spot_number: int = Field(ge=1)
