-- Create admin user with password and grant privileges
-- This script runs automatically when the MySQL container is first created

-- Create the admin user if it doesn't exist
CREATE USER IF NOT EXISTS 'admin'@'%' IDENTIFIED BY 'Password12';

-- Grant all privileges on both databases to admin user
GRANT ALL PRIVILEGES ON stufftracker.* TO 'admin'@'%';
GRANT ALL PRIVILEGES ON stufftracker_test.* TO 'admin'@'%';

-- Refresh privileges
FLUSH PRIVILEGES;

