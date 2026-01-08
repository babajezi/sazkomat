#!/bin/bash

# SSL Certificate Initialization Script for sazkomat.herma.cz
# This script obtains the initial SSL certificate from Let's Encrypt

set -e

DOMAIN="sazkomat.herma.cz"
EMAIL="${ADMIN_EMAIL:-admin@herma.cz}"
STAGING="${STAGING:-0}"  # Set to 1 for testing to avoid rate limits

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}SSL Certificate Initialization${NC}"
echo -e "${GREEN}Domain: ${DOMAIN}${NC}"
echo -e "${GREEN}Email: ${EMAIL}${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Please run as root or with sudo${NC}"
    exit 1
fi

# Create required directories
mkdir -p nginx/ssl
mkdir -p /var/www/certbot

# Check if certificate already exists
if [ -d "nginx/ssl/live/${DOMAIN}" ]; then
    echo -e "${YELLOW}Certificate already exists for ${DOMAIN}${NC}"
    echo -e "${YELLOW}Run scripts/renew-ssl.sh to renew${NC}"
    exit 0
fi

# Create temporary nginx config for certificate acquisition
echo -e "${GREEN}Creating temporary nginx configuration...${NC}"
cat > nginx/nginx-init.conf << 'NGINX_CONF'
events {
    worker_connections 1024;
}

http {
    server {
        listen 80;
        server_name sazkomat.herma.cz;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            return 200 'Server is being configured...';
            add_header Content-Type text/plain;
        }
    }
}
NGINX_CONF

# Start nginx with temporary config
echo -e "${GREEN}Starting temporary nginx server...${NC}"
docker run -d --name nginx-init \
    -p 80:80 \
    -v $(pwd)/nginx/nginx-init.conf:/etc/nginx/nginx.conf:ro \
    -v /var/www/certbot:/var/www/certbot \
    nginx:alpine

# Wait for nginx to start
sleep 5

# Staging flag for testing
STAGING_FLAG=""
if [ "$STAGING" = "1" ]; then
    echo -e "${YELLOW}Using Let's Encrypt staging environment${NC}"
    STAGING_FLAG="--staging"
fi

# Obtain certificate
echo -e "${GREEN}Obtaining SSL certificate from Let's Encrypt...${NC}"
docker run --rm \
    -v $(pwd)/nginx/ssl:/etc/letsencrypt \
    -v /var/www/certbot:/var/www/certbot \
    certbot/certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email "${EMAIL}" \
    --agree-tos \
    --no-eff-email \
    ${STAGING_FLAG} \
    -d "${DOMAIN}"

# Stop and remove temporary nginx
echo -e "${GREEN}Cleaning up temporary nginx...${NC}"
docker stop nginx-init
docker rm nginx-init
rm nginx/nginx-init.conf

# Check if certificate was obtained
if [ -d "nginx/ssl/live/${DOMAIN}" ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}SSL Certificate obtained successfully!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${GREEN}Certificate location: nginx/ssl/live/${DOMAIN}/${NC}"
    echo ""
    echo -e "${GREEN}You can now start the production stack:${NC}"
    echo -e "${YELLOW}docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d${NC}"
else
    echo -e "${RED}Failed to obtain SSL certificate${NC}"
    exit 1
fi
