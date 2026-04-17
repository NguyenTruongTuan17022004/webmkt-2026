# Build Docker image
docker build -t webmkt:latest .

# Tag for Google Container Registry (replace YOUR_PROJECT_ID)
# docker tag webmkt:latest gcr.io/YOUR_PROJECT_ID/webmkt:latest

# Push to GCR (replace YOUR_PROJECT_ID)
# docker push gcr.io/YOUR_PROJECT_ID/webmkt:latest

# Deploy to Cloud Run (replace YOUR_PROJECT_ID)
# gcloud run deploy webmkt-service --image gcr.io/YOUR_PROJECT_ID/webmkt:latest --platform managed --region asia-southeast1 --allow-unauthenticated --port 80