use test;
create table hello_world (id int unsigned not null auto_increment primary key, v varchar(32));
select * from information_schema.tikv_region_status where db_name=database() and table_name='hello_world'\G
select tidb_version()\G
select * from information_schema.tikv_store_status\G