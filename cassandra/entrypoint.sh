#!/bin/bash
set -e

/usr/local/bin/docker-entrypoint.sh cassandra -f &
cassandra_pid=$!

until cqlsh localhost 9042 -e "SELECT now() FROM system.local" >/dev/null 2>&1; do
    sleep 2
done

cqlsh localhost 9042 -f /usr/src/app/init.cql

wait "$cassandra_pid"