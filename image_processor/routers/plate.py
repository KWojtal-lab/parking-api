from fastapi import APIRouter
from fastapi.responses import Response

from models.plate_render_request import PlateRenderRequest
from services.image_plate_service import ImagePlateService


router = APIRouter()
image_plate_service = ImagePlateService()


@router.post("/render-plate")
def render_plate(request: PlateRenderRequest) -> Response:
	image_plate_service.render_plate_image(
		car_type=request.car_type,
		plate_text=request.license_plate,
		spot_number=request.spot_number,
	)
	return Response(status_code=204)
