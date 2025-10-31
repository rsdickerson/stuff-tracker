#!/bin/bash
set -euo pipefail

echo "Starting MySQL database container..."

docker run -d \
  --name mysql-sandbox \
  -e MYSQL_ROOT_PASSWORD=Password12 \
  -e MYSQL_DATABASE=stufftracker \
  -p 3306:3306 \
  mysql:8

echo "MySQL container started. Waiting for database to be ready..."
sleep 5

echo "MySQL database is ready!"
echo "Connection string: Server=localhost;Database=stufftracker;User=root;Password=Password12;"
