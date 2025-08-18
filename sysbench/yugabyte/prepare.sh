sysbench oltp_read_only \
  --pgsql-host=127.0.0.1 \
  --pgsql-port=5433 \
  --pgsql-user=yugabyte \
  --pgsql-db=yb \
  --tables=4 \
  --table-size=5000 \
  --report-interval=1 \
  --db-driver=pgsql \
  --range_key_partitioning=false \
  --serial_cache_size=1000 \
  --create_secondary=true \
  prepare