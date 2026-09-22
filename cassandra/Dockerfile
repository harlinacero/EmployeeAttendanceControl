FROM cassandra

COPY cassandra/init.cql /usr/src/app/init.cql
COPY cassandra/entrypoint.sh /usr/src/app/entrypoint.sh
RUN chmod +x /usr/src/app/entrypoint.sh

ENTRYPOINT ["/usr/src/app/entrypoint.sh"]