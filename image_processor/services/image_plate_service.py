import json
from pathlib import Path
from typing import Any, cast

from fastapi import HTTPException
from PIL import Image, ImageDraw, ImageFont


class ImagePlateService:
	def __init__(self, base_dir: Path | None = None) -> None:
		self.base_dir = base_dir or Path(__file__).resolve().parent.parent
		self.project_root = self.base_dir.parent
		self.templates_dir = self.project_root / "images" / "templates"
		self.real_dir = self.project_root / "images" / "real"
		self.coordinates_path = self.base_dir / "plate_coordinates.json"

	def render_plate_image(self, car_type: str, plate_text: str, spot_number: int) -> None:
		if not self.templates_dir.exists():
			raise HTTPException(status_code=500, detail="Templates directory does not exist.")

		template_path = self._resolve_template_path(car_type)
		rectangle = self._read_plate_coordinates(car_type)

		with Image.open(template_path) as template_image:
			image = template_image.convert("RGB")

		image = self._draw_centered_plate_text(image, plate_text, rectangle)

		self.real_dir.mkdir(parents=True, exist_ok=True)
		output_path = self.real_dir / f"spot_{spot_number}.png"
		image.save(output_path, format="PNG")

	def _normalize_car_type(self, value: str) -> str:
		return value.strip().lower().replace(" ", "")

	def _read_plate_coordinates(self, car_type: str) -> tuple[int, int, int, int]:
		if not self.coordinates_path.exists():
			raise HTTPException(status_code=500, detail="Missing plate_coordinates.json file.")

		with self.coordinates_path.open("r", encoding="utf-8") as file:
			payload = json.load(file)

		if not isinstance(payload, dict):
			raise HTTPException(status_code=500, detail="Invalid plate coordinate format.")

		payload_dict = cast(dict[str, Any], payload)

		normalized = self._normalize_car_type(car_type)

		try:
			coordinates = payload_dict[normalized]
			top_left = coordinates["topLeft"]
			bottom_right = coordinates["bottomRight"]
			x1 = int(top_left["x"])
			y1 = int(top_left["y"])
			x2 = int(bottom_right["x"])
			y2 = int(bottom_right["y"])
		except (KeyError, TypeError, ValueError) as exc:
			available = sorted(payload_dict.keys())
			raise HTTPException(
				status_code=500,
				detail={
					"message": f"Missing or invalid plate coordinates for car type '{car_type}'.",
					"available_types": available,
				},
			) from exc

		if x2 <= x1 or y2 <= y1:
			raise HTTPException(status_code=500, detail="Plate coordinates rectangle is invalid.")

		return x1, y1, x2, y2

	def _resolve_template_path(self, car_type: str) -> Path:
		normalized = self._normalize_car_type(car_type)
		expected = self.templates_dir / f"{normalized}.png"

		if expected.exists():
			return expected

		available = sorted(path.stem for path in self.templates_dir.glob("*.png"))
		raise HTTPException(
			status_code=404,
			detail={
				"message": f"Template for car type '{car_type}' was not found.",
				"available_types": available,
			},
		)

	def _draw_centered_plate_text(
		self,
		image: Image.Image,
		plate_text: str,
		rect: tuple[int, int, int, int],
	) -> Image.Image:
		x1, y1, x2, y2 = rect
		draw = ImageDraw.Draw(image)
		plate_text = plate_text.strip().upper()

		rect_width = x2 - x1
		rect_height = y2 - y1
		font_size = max(10, int(rect_height * 0.7))

		preferred_fonts = ["arial.ttf", "DejaVuSans.ttf"]
		font = ImageFont.load_default()
		for fpath in preferred_fonts:
			try:
				font = ImageFont.truetype(fpath, font_size)
				break
			except OSError:
				continue

		text_box = draw.textbbox((0, 0), plate_text, font=font)
		text_width = text_box[2] - text_box[0]
		text_height = text_box[3] - text_box[1]

		text_x = x1 + (rect_width - text_width) / 2 - text_box[0]
		text_y = y1 + (rect_height - text_height) / 2 - text_box[1]

		draw.text((text_x, text_y), plate_text, fill=(20, 20, 20), font=font)
		return image
