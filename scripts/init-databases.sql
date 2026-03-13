-- scripts/init-databases.sql
-- This script runs automatically when the postgres container starts for the first time.
-- It creates the three databases for our microservices.

CREATE DATABASE party_db;
CREATE DATABASE catalog_db;
CREATE DATABASE lending_db;
