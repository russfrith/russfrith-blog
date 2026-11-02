-- Runs once when the postgres container's data volume is first initialized. The app and
-- benchmarks use the "orders" database (created by POSTGRES_DB); integration tests get
-- their own "orders_test" database so a test run can truncate tables without touching
-- seeded benchmark data.
CREATE DATABASE orders_test;
