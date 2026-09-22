FROM mcr.microsoft.com/mssql/server

USER root
COPY initsqldatabase.sql /usr/src/app/initsqldatabase.sql
COPY sqlserver/entrypoint.sh /usr/src/app/entrypoint.sh
RUN chmod +x /usr/src/app/entrypoint.sh
USER root

ENTRYPOINT ["/usr/src/app/entrypoint.sh"]