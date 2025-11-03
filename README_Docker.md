# Docker Setup for StuffTracker

This document describes the Docker Compose setup for running MySQL databases for both development and testing environments.

## Quick Start

### Start the database:
```bash
docker-compose up -d
```

### Stop the database:
```bash
docker-compose down
```

### Stop and remove all data (reset):
```bash
docker-compose down -v
```

## Database Configuration

### Development Database
- **Container Name:** `stufftracker-mysql`
- **Port:** `3306`
- **Database:** `stufftracker`
- **Admin User:** `admin` / `Password12`
- **Root User:** `root` / `Password12`
- **Connection String:** `Server=localhost;Database=stufftracker;User=admin;Password=Password12;`

### Test Database
- **Database:** `stufftracker_test` (same MySQL instance)
- **User:** `admin` / `Password12`
- **Connection String:** `Server=localhost;Database=stufftracker_test;User=admin;Password=Password12;`

The test database is automatically created on first startup via the `docker/mysql/init.sql` initialization script.

## Verification

Check if MySQL is running:
```bash
docker-compose ps
```

Check MySQL logs:
```bash
docker-compose logs mysql
```

Connect to MySQL CLI:
```bash
# Connect as admin user
docker exec -it stufftracker-mysql mysql -uadmin -pPassword12

# Connect as root user
docker exec -it stufftracker-mysql mysql -uroot -pPassword12
```

## Data Persistence

Database data is persisted in a Docker volume (`mysql_data`). To reset the database and start fresh:

```bash
docker-compose down -v
docker-compose up -d
```

## Health Check

The MySQL container includes a health check that verifies the database is ready. The API will wait for the database to be healthy before connecting.

