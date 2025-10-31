# Docker Setup for StuffTracker

This project uses Docker Compose to run MySQL databases for development and testing.

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

### Main Database (Development)
- **Container Name:** `stufftracker-mysql`
- **Port:** `3306`
- **Database:** `stufftracker`
- **Username:** `root`
- **Password:** `Password12`
- **Connection String:** `Server=localhost;Database=stufftracker;User=root;Password=Password12;`

### Test Database
- **Database:** `stufftracker_test` (same MySQL instance)
- **Connection String:** `Server=localhost;Database=stufftracker_test;User=root;Password=Password12;`

The test database is automatically created on first startup via the initialization script.

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

