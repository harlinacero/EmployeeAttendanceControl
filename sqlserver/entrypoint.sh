#!/bin/bash
set -e

/opt/mssql/bin/sqlservr &
sqlserver_pid=$!

until /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD}" -Q "SELECT 1" >/dev/null 2>&1; do
    sleep 2
done

/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD}" -i /usr/src/app/initsqldatabase.sql -b

wait "${sqlserver_pid}"