FROM python:3.12-slim

WORKDIR /app/image_processor

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1
ENV CV_PORT=1414

COPY requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

# Install common TrueType fonts so PIL can load DejaVu/Arial-like fonts
RUN apt-get update \
	&& apt-get install -y --no-install-recommends fonts-dejavu-core \
	&& rm -rf /var/lib/apt/lists/*

COPY . /app/image_processor

EXPOSE 1414

CMD ["sh", "-c", "mkdir -p /app/images/real && uvicorn app:app --host 0.0.0.0 --port ${CV_PORT}"]
