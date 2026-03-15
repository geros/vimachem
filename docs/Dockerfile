FROM python:3.12-slim

WORKDIR /docs

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy documentation source
COPY . .

# Expose MkDocs dev server port
EXPOSE 8000

# Serve the documentation with live reload
CMD ["mkdocs", "serve", "--dev-addr=0.0.0.0:8000"]
