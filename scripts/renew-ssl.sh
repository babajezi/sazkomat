#!/bin/bash

# SSL Certificate Renewal Script for sazkomat.herma.cz
# Run this script periodically (e.g., via cron) to renew SSL certificates

set -e

DOMAIN="sazkomat.herma.cz"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}SSL Certificate Renewal${NC}"
echo -e "${GREEN}Domain: ${DOMAIN}${NC}"
echo -e "${GREEN}Time: $(date)${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if certificate exists
if [ ! -d "nginx/ssl/live/${DOMAIN}" ]; then
    echo -e "${RED}No certificate found for ${DOMAIN}${NC}"
    echo -e "${YELLOW}Run scripts/init-ssl.sh first${NC}"
    exit 1
fi

# Renew certificate
echo -e "${GREEN}Attempting certificate renewal...${NC}"
docker run --rm \
    -v $(pwd)/nginx/ssl:/etc/letsencrypt \
    -v /var/www/certbot:/var/www/certbot \
    certbot/certbot renew --quiet

# Check renewal status
CERT_EXPIRY=$(docker run --rm \
    -v $(pwd)/nginx/ssl:/etc/letsencrypt \
    certbot/certbot certificates 2>/dev/null | grep "Expiry Date" | head -1 | awk '{print $3, $4, $5, $6}')

if [ -n "$CERT_EXPIRY" ]; then
    echo -e "${GREEN}Certificate valid until: ${CERT_EXPIRY}${NC}"
fi

# Reload nginx to pick up new certificate
echo -e "${GREEN}Reloading nginx...${NC}"
docker exec sazkomat-nginx nginx -s reload 2>/dev/null || echo -e "${YELLOW}Nginx not running or reload failed${NC}"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Certificate renewal completed${NC}"
echo -e "${GREEN}========================================${NC}"
