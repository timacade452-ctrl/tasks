# Deploy StudyFlow to Render

1. Push this project to GitHub.
2. In Render, choose **New > Blueprint** and select the repository.
3. Render will detect `render.yaml` and deploy the Docker service.

The public URL will use HTTPS automatically. Render forwards HTTP traffic to the container on port `8080`.

## Data storage

This version uses in-memory repositories and local file storage. Uploaded files and application data may be lost whenever the Render service restarts or redeploys. Use a Render persistent disk or replace the demo repositories with a database for durable data.
