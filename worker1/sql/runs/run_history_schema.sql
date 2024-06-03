drop table if exists run_history;
create table if not exists run_history
(
    id          int          not null auto_increment,

    method_name varchar(150) null,
    filter      varchar(250) null,

    modified_at datetime     null default now(),
    created_at  datetime     null default now(),
    modified_by varchar(250),
    created_by  varchar(250),

    # PK's
    PRIMARY KEY (id),
    # full-text search
    FULLTEXT (created_by, modified_by)
);

alter table run_history
    add column
        delete_date datetime null
#date_add(now(), interval 10 day)
;

insert into run_history (method_name)
values ('foo bar');

select *
from run_history
order by created_at desc;