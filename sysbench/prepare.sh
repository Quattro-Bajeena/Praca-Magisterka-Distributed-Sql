sysbench oltp_read_only \
  --pgsql-host=127.0.0.1 \
  --pgsql-port=5433 \
  --pgsql-user=yugabyte \
  --pgsql-db=yb_demo \
  --tables=2 \
  --table-size=13000 \
  --threads=2 \
  --report-interval=1 \
  --db-driver=pgsql \
  --range_key_partitioning=false \
  --serial_cache_size=1000 \
  --create_secondary=true \
  prepare