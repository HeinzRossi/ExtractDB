drop table if exists pedido_tag cascade;
drop table if exists lote_item cascade;
drop table if exists lote cascade;
drop table if exists total_pedido cascade;
drop table if exists pedido_item cascade;
drop table if exists pedido cascade;
drop table if exists tabela_sem_pk cascade;
drop table if exists cliente cascade;
drop sequence if exists seq_codigo cascade;
drop function if exists fn_cliente_nome(integer) cascade;
drop function if exists trg_cliente_touch() cascade;
drop type if exists status_pedido cascade;
drop type if exists endereco_composto cascade;

create type status_pedido as enum ('aberto', 'fechado', 'cancelado');
create type endereco_composto as (rua text, numero integer);

create sequence seq_codigo start with 100 increment by 1;

create table cliente (
    id_cliente integer generated always as identity primary key,
    nome varchar(120) not null,
    apelido text null,
    email varchar(180) default 'sem-email',
    ativo boolean not null default true,
    criado_em timestamp without time zone default current_timestamp,
    atualizado_em timestamp with time zone,
    payload jsonb,
    foto bytea,
    identificador uuid,
    status status_pedido not null default 'aberto',
    total numeric(12, 2),
    legacy endereco_composto
);

create table tabela_sem_pk (
    nome varchar(80) not null
);

create table pedido (
    id_pedido integer primary key default nextval('seq_codigo'::regclass),
    id_cliente integer not null references cliente(id_cliente),
    id_cliente_indicador integer null references cliente(id_cliente),
    valor numeric(14, 2) not null,
    data_pedido date not null,
    hora_pedido time without time zone not null
);

create table pedido_item (
    id_pedido integer not null,
    item integer not null,
    produto varchar(80) not null,
    primary key (id_pedido, item),
    foreign key (id_pedido) references pedido(id_pedido)
);

create table lote (
    id_lote integer not null,
    item integer not null,
    primary key (id_lote, item)
);

create table lote_item (
    id_lote integer not null,
    item integer not null,
    descricao varchar(80),
    foreign key (id_lote, item) references lote(id_lote, item)
);

create table pedido_tag (
    id_pedido integer not null references pedido(id_pedido),
    tag varchar(40) not null,
    primary key (id_pedido, tag)
);

create table total_pedido (
    valor numeric(12, 2) not null,
    imposto numeric(12, 2) not null,
    total numeric(12, 2) generated always as (valor + imposto) stored
);

create view vw_cliente as
select id_cliente, nome
from cliente;

create function fn_cliente_nome(p_id integer)
returns text
language sql
as $$
    select nome from cliente where id_cliente = p_id
$$;

create function trg_cliente_touch()
returns trigger
language plpgsql
as $$
begin
    new.atualizado_em = current_timestamp;
    return new;
end;
$$;

create trigger tr_cliente_bu
before update on cliente
for each row
execute function trg_cliente_touch();
