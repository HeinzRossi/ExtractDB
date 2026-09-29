if object_id('dbo.tr_cliente_bu', 'TR') is not null drop trigger dbo.tr_cliente_bu;
if object_id('dbo.sp_cliente_nome', 'P') is not null drop procedure dbo.sp_cliente_nome;
if object_id('dbo.vw_cliente', 'V') is not null drop view dbo.vw_cliente;
if object_id('dbo.pedido_tag', 'U') is not null drop table dbo.pedido_tag;
if object_id('dbo.lote_item', 'U') is not null drop table dbo.lote_item;
if object_id('dbo.lote', 'U') is not null drop table dbo.lote;
if object_id('dbo.total_pedido', 'U') is not null drop table dbo.total_pedido;
if object_id('dbo.pedido_item', 'U') is not null drop table dbo.pedido_item;
if object_id('dbo.pedido', 'U') is not null drop table dbo.pedido;
if object_id('dbo.tabela_sem_pk', 'U') is not null drop table dbo.tabela_sem_pk;
if object_id('dbo.cliente', 'U') is not null drop table dbo.cliente;
if type_id('dbo.CodigoExterno') is not null drop type dbo.CodigoExterno;

create type dbo.CodigoExterno from varchar(20) null;

create table dbo.cliente (
    id_cliente int identity(1,1) not null constraint pk_cliente primary key,
    nome varchar(120) not null,
    nome_unicode nvarchar(120) not null,
    descricao text null,
    observacao ntext null,
    email varchar(180) null constraint df_cliente_email default ('sem-email'),
    ativo bit not null constraint df_cliente_ativo default ((1)),
    criado_em datetime2 not null constraint df_cliente_criado default (sysdatetime()),
    data_nascimento date null,
    hora_cadastro time null,
    foto varbinary(max) null,
    imagem image null,
    identificador uniqueidentifier not null constraint df_cliente_guid default (newid()),
    total numeric(12, 2) null,
    legado dbo.CodigoExterno null
);

create table dbo.tabela_sem_pk (
    nome varchar(80) not null
);

create table dbo.pedido (
    id_pedido int not null constraint pk_pedido primary key,
    id_cliente int not null constraint fk_pedido_cliente references dbo.cliente(id_cliente),
    id_cliente_indicador int null constraint fk_pedido_cliente_indicador references dbo.cliente(id_cliente),
    valor decimal(14, 2) not null,
    data_pedido date not null,
    hora_pedido time not null
);

create table dbo.pedido_item (
    id_pedido int not null,
    item int not null,
    produto varchar(80) not null,
    constraint pk_pedido_item primary key (id_pedido, item),
    constraint fk_pedido_item_pedido foreign key (id_pedido) references dbo.pedido(id_pedido)
);

create table dbo.lote (
    id_lote int not null,
    item int not null,
    constraint pk_lote primary key (id_lote, item)
);

create table dbo.lote_item (
    id_lote int not null,
    item int not null,
    descricao varchar(80) null,
    constraint fk_lote_item_lote foreign key (id_lote, item) references dbo.lote(id_lote, item)
);

create table dbo.pedido_tag (
    id_pedido int not null constraint fk_pedido_tag_pedido references dbo.pedido(id_pedido),
    tag varchar(40) not null,
    constraint pk_pedido_tag primary key (id_pedido, tag)
);

create table dbo.total_pedido (
    valor numeric(12, 2) not null,
    imposto numeric(12, 2) not null,
    total as (valor + imposto) persisted
);

exec('create view dbo.vw_cliente as select id_cliente, nome from dbo.cliente');
exec('create procedure dbo.sp_cliente_nome @id int as select nome from dbo.cliente where id_cliente = @id');
exec('create trigger dbo.tr_cliente_bu on dbo.cliente after update as begin set nocount on; select 1 as touched; end');
