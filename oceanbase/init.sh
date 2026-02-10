# docker pull oceanbase/ocp-ce:4.2.2

## set environment parameters
export OCP_CPU_COUNT=4
export OCP_MEMORY_GB=8
export OCP_METADB_HOST=xxx.xxx.xxx.xxx # do not use 127.0.0.1 or localhsot
export OCP_METADB_PORT=2881
export OCP_METADB_USER=root@ocp_meta
export OCP_METADB_PASSWORD=ocp_meta_password
export OCP_METADB_DBNAME=ocp_meta
export OCP_MONITORDB_USER=root@ocp_monitor
export OCP_MONITORDB_PASSWORD=ocp_monitor_password
export OCP_MONITORDB_DBNAME=ocp_monitor
export OCP_INITIAL_ADMIN_PASSWORD=Adelinold00! #should match ocp's password validation
export OCP_CONFIG_PROPERTIES=`cat << EOF
server.port:8080
ocp.site.url:http://xxx.xxx.xxx.xxx:8080
obsdk.ob.connection.mode:direct
EOF
`

# start ocp container
docker run -d --name ocp-422 \
--network host \
--cpu-period 100000 --cpu-quota ${OCP_CPU_COUNT}00000 --memory=${OCP_MEMORY_GB}G \
 -e OCP_METADB_HOST="${OCP_METADB_HOST}" \
 -e OCP_METADB_PORT="${OCP_METADB_PORT}" \
 -e OCP_METADB_DBNAME="${OCP_METADB_DBNAME}" \
 -e OCP_METADB_USER="${OCP_METADB_USER}" \
 -e OCP_METADB_PASSWORD="${OCP_METADB_PASSWORD}" \
 -e OCP_MONITORDB_DBNAME="${OCP_MONITORDB_DBNAME}" \
 -e OCP_MONITORDB_USER="${OCP_MONITORDB_USER}" \
 -e OCP_MONITORDB_PASSWORD="${OCP_MONITORDB_PASSWORD}" \
 -e OCP_INITIAL_ADMIN_PASSWORD="${OCP_INITIAL_ADMIN_PASSWORD}" \
 -e OCP_CONFIG_PROPERTIES="${OCP_CONFIG_PROPERTIES}" \
oceanbase/ocp-ce:4.2.2