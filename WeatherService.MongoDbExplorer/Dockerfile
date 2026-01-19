FROM python:3.10-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

# Expose Streamlit port
EXPOSE 8501

# Default environment variables
ENV MODE=cli
ENV MONGO_URI="mongodb://mongo:27017"

ENTRYPOINT ["python", "main.py"]
