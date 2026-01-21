FROM python:3.11-slim

WORKDIR /app

COPY WeatherService.ModelAI/requirements.txt ./requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

COPY WeatherService.ModelAI/train_pipeline.py ./train_pipeline.py

ENV ARTIFACTS_DIR=/app/artifacts

CMD ["python", "train_pipeline.py"]
