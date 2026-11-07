CREATE KEYSPACE IF NOT EXISTS kusers
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

CREATE TABLE IF NOT EXISTS kusers.user
(
    user_username text PRIMARY KEY,
    user_pasword text,
    user_role text
);

-- Sessions and single-use refresh tokens (ms.users feature 001). Rows carry a time to live, so they expire on their own.
CREATE TABLE IF NOT EXISTS kusers.user_session
(
    session_id uuid PRIMARY KEY,
    session_username text,
    session_created_at timestamp,
    session_last_renewed_at timestamp,
    session_ended_at timestamp,
    session_end_reason text
);

-- Only the SHA-256 of a refresh token is stored, never the token
CREATE TABLE IF NOT EXISTS kusers.refresh_token
(
    token_hash text PRIMARY KEY,
    token_session_id uuid,
    token_spent_at timestamp
);

-- Unused table from an older version of this script. It holds readable passwords on volumes created back then
DROP TABLE IF EXISTS kusers."User";

-- The seeded admin's password (1234) is stored already protected (PBKDF2), never readable
INSERT INTO kusers.user (user_username, user_pasword, user_role)
VALUES ('admin', 'pbkdf2-sha256$600000$CWQKhv+LpaYLdaoZPqBCGg==$NNyKuz15jcd5tjc79nK26r+W2hYgCd8/SSCT8vNy15E=', 'admin')
IF NOT EXISTS;
