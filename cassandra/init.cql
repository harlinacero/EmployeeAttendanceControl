CREATE KEYSPACE IF NOT EXISTS kusers
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

CREATE TABLE IF NOT EXISTS kusers.user
(
    user_username text PRIMARY KEY,
    user_pasword text,
    user_role text
);

INSERT INTO kusers."User" (user_username, user_pasword, user_role)
VALUES ('admin', '1234', 'admin')
IF NOT EXISTS;