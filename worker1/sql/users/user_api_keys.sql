drop table if exists user_api_keys;
create table if not exists user_api_keys
(
    user_id         varchar(250),
    todoist_api_key text,
    is_archived     bit,
    is_deleted      bit,
    is_enabled      bit,
#  todo: decide whether you want a status field or not.  Atm, cannot think of a reason for one.

    created_by      varchar(100),
    created_at      datetime,
    modified_by     varchar(100),
    modified_at     datetime
);

select user_id, todoist_api_key, created_at, modified_at
from user_api_keys
order by created_at desc;

# Basic user table.  Can be replaced/supplementd with other auth
drop table if exists todoist_users;
create table if not exists todoist_users
(
    user_id     varchar(250),
    username    varchar(250),
    email       varchar(250),
    password    varchar(250),

    created_by  varchar(100),
    created_at  datetime,
    modified_by varchar(100),
    modified_at datetime
);

select username, email, user_id, created_at, modified_at
from todoist_users
order by created_at desc;

describe todoist_users;
describe user_api_keys;

SHOW COLUMNS FROM user_api_keys;

## https://www.basedash.com/blog/how-to-display-mysql-table-schema
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT, COLUMN_KEY
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'user_api_keys' AND TABLE_SCHEMA = 'railway';


## Get all tables in mysql:
SHOW TABLES;
# 
# SHOW FULL TABLES;
