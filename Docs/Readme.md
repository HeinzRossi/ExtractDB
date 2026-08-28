# Readme.md — ExtractDB Schema Reverse Engineering

## 1. Objetivo

Implementar **duas aplicações independentes de Schema Reverse Engineering**:

1. **C#**
   - .NET 10
   - WPF
   - CommunityToolkit.Mvvm
   - Microsoft.Extensions.DependencyInjection

2. **Delphi**
   - Delphi 12 Athens ou superior
   - VCL
   - FireDAC
   - SimpleORM
   - `TTask` para processamento assíncrono

As aplicações devem conectar-se a:

- Firebird
- PostgreSQL
- Microsoft SQL Server

O objetivo é ler o schema existente e gerar:

| Objeto | Saída |
|---|---|
| Table | Model C# / Delphi |
| View | Script `.sql` |
| Procedure | Script `.sql` |
| Trigger | Script `.sql` |
| Enum reconhecido | Enum C# / Delphi |
| Sequence | Metadata/atributo da coluna |

## 2. Regra Fundamental

O sistema deve **refletir o banco exatamente como ele existe**.

Não tentar corrigir, melhorar ou reinterpretar o schema.

### Nunca fazer

- inferir chave primária;
- singularizar nome de tabela;
- inferir relacionamento inexistente;
- transformar tabela N:N em coleção;
- criar PK para tabela sem PK;
- remover coluna desconhecida;
- bloquear geração por causa de tipo desconhecido;
- converter SQL de um SGBD para outro;
- alterar scripts extraídos;
- persistir connection strings;
- criar migrations;
- criar banco;
- executar DDL;
- gerar `.csproj`, `.sln`, `.dproj` ou `.dpr`.

## 3. Estrutura de Arquitetura

A arquitetura conceitual das duas aplicações deve ser equivalente.

```text
UI
 ↓
Application
 ↓
Core

Providers ─────→ Core
Generators ────→ Core
```

`Core` não pode depender de:

- WPF;
- VCL;
- FireDAC;
- Npgsql;
- SqlClient;
- FirebirdClient;
- SimpleORM;
- DataAnnotations;
- sistema de arquivos;
- UI.

## 4. Estrutura C#

Criar:

```text
ExtractDB.slnx

src/
├── ExtractDB.Core
├── ExtractDB.Application
├── ExtractDB.Providers
├── ExtractDB.Generators.CSharp
└── ExtractDB.Wpf

tests/
├── ExtractDB.Core.Tests
├── ExtractDB.Generators.CSharp.Tests
└── ExtractDB.Providers.IntegrationTests
```

### Dependências permitidas

```text
ExtractDB.Core
    ↑
    ├── ExtractDB.Application
    ├── ExtractDB.Providers
    └── ExtractDB.Generators.CSharp

ExtractDB.Wpf
    ↓
Application
Providers
Generators
```

Evitar dependências circulares.

## 5. Estrutura Delphi

```text
Source/
├── Core/
│   ├── Model/
│   ├── Contracts/
│   └── Types/
│
├── Application/
│   └── Services/
│
├── Providers/
│   ├── PostgreSql/
│   ├── SqlServer/
│   └── Firebird/
│
├── Generators/
│   ├── Naming/
│   ├── Mapping/
│   ├── Attributes/
│   └── Code/
│
└── UI/
    ├── Forms/
    └── Bootstrap/

Tests/
├── Unit/
└── Integration/
```

Não criar packages Delphi adicionais sem necessidade.

## 6. Modelo Normalizado

As duas aplicações devem possuir modelos conceitualmente equivalentes.

### C#

```text
DatabaseMetadata
TableMetadata
ColumnMetadata
PrimaryKeyMetadata
ForeignKeyMetadata
SequenceMetadata
ViewMetadata
ProcedureMetadata
TriggerMetadata
EnumMetadata
DefaultValueMetadata
ValueGenerationMetadata
```

### Delphi

```text
TDatabaseMetadata
TTableMetadata
TColumnMetadata
TPrimaryKeyMetadata
TForeignKeyMetadata
TSequenceMetadata
TViewMetadata
TProcedureMetadata
TTriggerMetadata
TEnumMetadata
TDefaultValueMetadata
TValueGenerationMetadata
```

## 7. DatabaseMetadata

Representar:

```text
DatabaseMetadata
├── Provider
├── DatabaseName
├── DefaultSchema
├── Tables
├── Views
├── Procedures
├── Triggers
├── Sequences
└── Enums
```

## 8. TableMetadata

Representar:

```text
TableMetadata
├── Schema
├── Name
├── Columns
├── PrimaryKey
└── ForeignKeys
```

## 9. ColumnMetadata

Obrigatório possuir:

```text
ColumnMetadata
├── Name
├── OrdinalPosition
├── NativeType
├── DbType
├── Length
├── Precision
├── Scale
├── IsUnicode
├── IsNullable
├── IsPrimaryKey
├── IsComputed
├── ComputedExpression
├── DefaultValue
└── ValueGeneration
```

Exemplo C#:

```csharp
public sealed record ColumnMetadata
{
    public required string Name { get; init; }
    public required int OrdinalPosition { get; init; }
    public required string NativeType { get; init; }
    public required CommonDbType DbType { get; init; }

    public int? Length { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }

    public bool IsUnicode { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsComputed { get; init; }

    public string? ComputedExpression { get; init; }
    public DefaultValueMetadata? DefaultValue { get; init; }
    public ValueGenerationMetadata? ValueGeneration { get; init; }
}
```

Preferir objetos imutáveis.

## 10. CommonDbType

Implementar:

```csharp
public enum CommonDbType
{
    Unknown,

    SmallInt,
    Integer,
    BigInt,

    Decimal,
    Float,
    Double,

    Boolean,

    Char,
    VarChar,
    Text,

    Date,
    Time,
    DateTime,

    Binary,
    Blob,

    Guid,

    Json,

    Enum
}
```

Não implementar nesta versão suporte especial para tipos complexos como:

```text
TIMESTAMP WITH TIME ZONE
DATETIMEOFFSET
```

Quando encontrados e não houver mapper explícito:

```text
CommonDbType.Unknown
```

## 11. Type Mapping

O provider deve converter:

```text
Tipo nativo do banco
        ↓
CommonDbType
```

O generator converte:

```text
CommonDbType
        ↓
Tipo da linguagem
```

Nunca permitir:

```text
PostgreSqlProvider
        ↓
"DateOnly"
```

Isso seria acoplamento incorreto.

## 12. PostgreSQL Provider

Utilizar:

```text
Npgsql
```

Pode herdar de uma implementação comum baseada em `information_schema`.

Exemplo:

```csharp
public sealed class PostgreSqlMetadataProvider
    : AnsiInformationSchemaProvider
{
    public override string ProviderName => "PostgreSQL";

    protected override string DefaultSchema => "public";

    protected override DbConnection CreateConnection(
        string connectionString)
        => new NpgsqlConnection(connectionString);

    protected override CommonDbType MapNativeType(
        string nativeType,
        int? numericScale)
    {
        return nativeType switch
        {
            "smallint" => CommonDbType.SmallInt,
            "integer" => CommonDbType.Integer,
            "bigint" => CommonDbType.BigInt,

            "numeric" or "decimal"
                => CommonDbType.Decimal,

            "real" => CommonDbType.Float,
            "double precision" => CommonDbType.Double,

            "boolean" => CommonDbType.Boolean,

            "character" => CommonDbType.Char,
            "character varying" => CommonDbType.VarChar,
            "text" => CommonDbType.Text,

            "date" => CommonDbType.Date,

            "time" or "time without time zone"
                => CommonDbType.Time,

            "timestamp" or "timestamp without time zone"
                => CommonDbType.DateTime,

            "bytea" => CommonDbType.Blob,

            "uuid" => CommonDbType.Guid,

            "json" or "jsonb"
                => CommonDbType.Json,

            _ => CommonDbType.Unknown
        };
    }
}
```

Complementar `information_schema` com `pg_catalog` quando necessário.

## 13. SQL Server Provider

Utilizar:

```text
Microsoft.Data.SqlClient
```

Consultar conforme necessário:

```text
INFORMATION_SCHEMA
sys.tables
sys.columns
sys.types
sys.key_constraints
sys.index_columns
sys.foreign_keys
sys.foreign_key_columns
sys.identity_columns
sys.computed_columns
sys.default_constraints
sys.sql_modules
```

Mapeamento pertence exclusivamente ao provider SQL Server.

## 14. Firebird Provider

Utilizar no C#:

```text
FirebirdSql.Data.FirebirdClient
```

No Delphi:

```text
FireDAC
```

Consultar catálogos:

```text
RDB$RELATIONS
RDB$RELATION_FIELDS
RDB$FIELDS
RDB$INDICES
RDB$INDEX_SEGMENTS
RDB$RELATION_CONSTRAINTS
RDB$REF_CONSTRAINTS
RDB$GENERATORS
RDB$TRIGGERS
RDB$PROCEDURES
```

e demais tabelas de sistema necessárias.

Filtrar:

```text
RDB$*
MON$*
SEC$*
```

quando representarem objetos de sistema.

## 15. Objetos Internos

Objetos internos nunca devem aparecer no Wizard.

Filtragem ocorre no provider.

### PostgreSQL

Ignorar:

```text
pg_catalog
information_schema
```

e objetos internos.

### SQL Server

Ignorar objetos marcados como system.

### Firebird

Ignorar objetos internos do sistema.

## 16. Schema

Usar apenas **um schema**, sempre o schema padrão da conexão.

Não criar UI de seleção de schema.

### PostgreSQL

Usar schema efetivo da conexão/search path.

### SQL Server

Usar schema padrão do usuário.

### Firebird

Não expor conceito de schema.

## 17. Conexão

A conexão sempre será temporária.

Nunca persistir:

- servidor;
- usuário;
- senha;
- connection string.

Campos da UI:

```text
Provider
Server
Port
Database
UserName
Password
```

Valores default:

```text
PostgreSQL → 5432
SQL Server → 1433
Firebird   → 3050
```

## 18. TestConnection

O teste de conexão é obrigatório.

Fluxo:

```text
Informar dados
    ↓
Testar conexão
    ↓
Sucesso?
├── Não → não habilitar Avançar
└── Sim → habilitar Avançar
```

Se qualquer dado da conexão mudar:

```text
IsConnectionValidated = false
```

## 19. Provider Contract

C#:

```csharp
public interface IDatabaseMetadataProvider
{
    string ProviderName { get; }

    Task TestConnectionAsync(
        DatabaseConnectionOptions options,
        CancellationToken cancellationToken);

    Task<DatabaseMetadata> ReadAsync(
        DatabaseConnectionOptions options,
        IProgress<MetadataProgress>? progress,
        CancellationToken cancellationToken);
}
```

Criar equivalente Delphi.

## 20. Provider Factory

Criar:

```text
IMetadataProviderFactory
```

Exemplo C#:

```csharp
public interface IMetadataProviderFactory
{
    IDatabaseMetadataProvider Create(
        DatabaseProvider provider);
}
```

UI não pode conhecer implementações concretas.

## 21. Naming

Implementar um serviço separado.

Pipeline:

```text
Database Name
    ↓
Normalize separators
    ↓
PascalCase
    ↓
Reserved word handling
    ↓
Collision handling
    ↓
Final name
```

Reconhecer como separadores:

```text
_
-
espaço
.
caracteres inválidos
```

## 22. Classes

Tabela:

```text
CLIENTE_ENDERECO
```

C#:

```text
ClienteEndereco
```

Delphi:

```text
TClienteEndereco
```

## 23. Propriedades

```text
ID_CLIENTE
→ IdCliente

DATA_CADASTRO
→ DataCadastro

CPF_CNPJ
→ CpfCnpj

URL_IMAGEM
→ UrlImagem

XML_NOTA
→ XmlNota

UUID_EXTERNO
→ UuidExterno
```

## 24. Não Singularizar

Não fazer:

```text
CLIENTES → Cliente
```

Fazer:

```text
CLIENTES
→ Clientes
→ TClientes
```

## 25. Reserved Words

Exemplo:

```text
CLASS → ClassValue
END   → EndValue
```

Não utilizar escaping como solução principal.

## 26. Colisões

Exemplo:

```text
ID_CLIENTE
IDCLIENTE
```

Gerar:

```text
IdCliente
IdCliente2
IdCliente3
```

quando necessário.

## 27. Ordem

Preservar:

```text
ColumnMetadata.OrdinalPosition
```

As propriedades devem sair exatamente na ordem física das colunas.

## 28. Tabela sem PK

Gerar normalmente.

Não adicionar:

```text
[Key]
[PK]
```

Não emitir erro impeditivo.

Pode gerar warning informativo.

## 29. PK Simples C#

```csharp
[Key]
[Column("ID_CLIENTE")]
public int IdCliente { get; set; }
```

## 30. PK Composta C#

Aplicar `[Key]` a todas as colunas participantes.

```csharp
[Key]
[Column("ID_USUARIO")]
public int IdUsuario { get; set; }

[Key]
[Column("ID_PERFIL")]
public int IdPerfil { get; set; }
```

Não criar `CompositeKeyAttribute`.

## 31. PK Delphi

Simples:

```delphi
[Campo('ID_CLIENTE'), PK]
```

Composta:

```delphi
[Campo('ID_USUARIO'), PK]
[Campo('ID_PERFIL'), PK]
```

## 32. FK Simples

C#:

```csharp
[Column("ID_CLIENTE")]
public int IdCliente { get; set; }

[ForeignKey(nameof(IdCliente))]
public Cliente Cliente { get; set; } = null!;
```

Delphi:

```delphi
[Campo('ID_CLIENTE'), FK, NotNull]
property IdCliente: Integer
  read FIdCliente
  write FIdCliente;

[BelongsTo('CLIENTE', 'ID_CLIENTE')]
property Cliente: TCliente
  read FCliente
  write FCliente;
```

## 33. Nome da Navegação

Derivar da coluna FK.

```text
ID_CLIENTE
→ Cliente

ID_VENDEDOR
→ Vendedor

ID_USUARIO_CRIACAO
→ UsuarioCriacao
```

## 34. FK Nullable

C#:

```csharp
public int? IdVendedor { get; set; }

[ForeignKey(nameof(IdVendedor))]
public Vendedor? Vendedor { get; set; }
```

Delphi:

```delphi
[Campo('ID_VENDEDOR'), FK]
property IdVendedor: Integer;
```

Sem `[NotNull]`.

## 35. Tabela Relacionada Não Selecionada

Se:

```text
[x] PEDIDO
[ ] CLIENTE
```

preservar:

```text
ID_CLIENTE
FK metadata
```

mas não gerar propriedade de navegação.

C#:

```csharp
public int IdCliente { get; set; }
```

Delphi deve seguir a mesma regra.

## 36. FK Composta

O modelo deve suportar:

```text
SourceColumns[]
TargetColumns[]
```

C# pode gerar:

```csharp
[CompositeForeignKey(
    nameof(EmpresaId),
    nameof(DocumentoId))]
public Documento Documento { get; set; } = null!;
```

Criar:

```text
CompositeForeignKeyAttribute.cs
```

Delphi deverá utilizar atributo equivalente no SimpleORM.

Se não existir, adicioná-lo ao SimpleORM.

## 37. Tabela N:N

Tratar como tabela normal.

```text
USUARIO_PERFIL
├── ID_USUARIO
└── ID_PERFIL
```

Gerar:

```text
UsuarioPerfil
TUsuarioPerfil
```

Não gerar coleções automaticamente.

## 38. ValueGeneration

Representar:

```text
None
Identity
Sequence
TriggerSequence
```

## 39. Identity C#

Quando:

```text
Strategy = Identity
```

gerar:

```csharp
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
```

## 40. AutoInc Delphi

Utilizar atributo SimpleORM:

```delphi
AutoInc
```

Exemplo:

```delphi
[Campo('ID_CLIENTE'), PK, AutoInc, NotNull]
```

## 41. Sequence

Preservar nome da sequence.

C#:

```csharp
[Sequence("SEQ_CLIENTE_ID")]
```

Criar atributo:

```text
Metadata/SequenceAttribute.cs
```

Delphi:

```delphi
[Sequence('GEN_CLIENTE_ID')]
```

Se não existir no SimpleORM, implementar.

## 42. Trigger + Sequence

Quando provider detectar trigger responsável pela geração:

C#:

```csharp
[Sequence("GEN_CLIENTE_ID")]
[GeneratedByTrigger("TR_CLIENTE_BI")]
```

Delphi:

```delphi
[Sequence('GEN_CLIENTE_ID')]
[GeneratedByTrigger('TR_CLIENTE_BI')]
```

Não inferir isso se não houver evidência no schema.

## 43. Default

Preservar expressão SQL original.

Metadata:

```text
DefaultValue
├── RawExpression
└── Kind
```

Kind:

```text
Unknown
Literal
Expression
Sequence
SystemFunction
```

C#:

```csharp
[DatabaseDefault("CURRENT_TIMESTAMP")]
```

Delphi:

```delphi
[DatabaseDefault('CURRENT_TIMESTAMP')]
```

## 44. Computed

Metadata:

```text
IsComputed = true
ComputedExpression = original
```

C#:

```csharp
[DatabaseComputed]
```

Delphi:

```delphi
[DatabaseComputed]
```

Não criar propriedades readonly.

## 45. Unicode

Preservar:

```text
IsUnicode
```

Exemplo:

```text
VARCHAR  → false
NVARCHAR → true
```

Não criar `NVarChar` em `CommonDbType`.

## 46. Decimal

Metadata sempre deve preservar:

```text
Precision
Scale
```

C#:

```csharp
[Column("VALOR", TypeName = "decimal(18,4)")]
public decimal Valor { get; set; }
```

## 47. Decimal Delphi

Regra:

```text
Decimal/Numeric
├── compatível com Currency
│   → Currency
└── precisão/escala incompatíveis
    → TBcd
```

Nunca utilizar `Double` como fallback de decimal exato.

## 48. Tipos C#

Mapear:

```text
SmallInt → short
Integer  → int
BigInt   → long

Decimal  → decimal
Float    → float
Double   → double

Boolean  → bool

Char     → string
VarChar  → string
Text     → string

Date     → DateOnly
Time     → TimeOnly
DateTime → DateTime

Guid     → Guid

Blob     → byte[]
Binary   → byte[]

Json     → string

Unknown  → object
```

## 49. Nullability C#

Value types:

```text
NULL → ?
```

Exemplo:

```csharp
public int? Codigo { get; set; }
```

Reference types:

```csharp
public string? Observacao { get; set; }
```

NOT NULL:

```csharp
[Required]
public string Nome { get; set; } = string.Empty;
```

## 50. Texto Delphi

Todo texto:

```text
CHAR
VARCHAR
TEXT
BLOB SUB_TYPE TEXT
```

mapear para:

```delphi
string
```

## 51. Datas Delphi

```text
Date     → TDate
Time     → TTime
DateTime → TDateTime
```

## 52. Unknown

Tipo desconhecido nunca deve impedir geração.

C#:

```csharp
public object? CampoEspecial { get; set; }
```

Delphi:

```delphi
property CampoEspecial: Variant;
```

Registrar warning.

## 53. BLOB C#

Mapear:

```text
BLOB
IMAGE
BYTEA
VARBINARY
```

para:

```csharp
byte[]
```

Nullable:

```csharp
public byte[]? Documento { get; set; }
```

NOT NULL:

```csharp
public byte[] Documento { get; set; } = [];
```

## 54. BLOB Delphi

Mapear tipos binários para:

```delphi
TStream
```

Implementação interna:

```delphi
TMemoryStream
```

## 55. Stream Field

Gerar:

```delphi
private
  FDocumento: TStream;

  procedure SetDocumento(const Value: TStream);
```

Propriedade:

```delphi
[Campo('DOCUMENTO')]
property Documento: TStream
  read FDocumento
  write SetDocumento;
```

## 56. Stream Create

Classes que possuem pelo menos um `TStream` devem gerar construtor.

```delphi
constructor TDocumento.Create;
begin
  inherited Create;

  FDocumento := TMemoryStream.Create;
end;
```

## 57. Stream Destroy

```delphi
destructor TDocumento.Destroy;
begin
  FDocumento.Free;

  inherited;
end;
```

Com vários streams:

```delphi
destructor TDocumento.Destroy;
begin
  FDocumento.Free;
  FImagem.Free;
  FAnexo.Free;

  inherited;
end;
```

## 58. Stream Setter

Gerar:

```delphi
procedure TDocumento.SetDocumento(
  const Value: TStream);
begin
  if Assigned(Value) then
    TMemoryStream(FDocumento).LoadFromStream(Value)
  else
    TMemoryStream(FDocumento).Clear;
end;
```

Nunca fazer:

```delphi
FDocumento := Value;
```

Ownership pertence à entidade.

## 59. Construtores

Regra geral:

```text
não gerar construtor
```

Exceção:

```text
classe Delphi possui TStream
→ gerar Create/Destroy
```

## 60. SimpleORM

Utilizar atributos existentes sempre que possível.

Esperados:

```text
Tabela
Campo
PK
FK
NotNull
AutoInc
BelongsTo
```

Antes de criar atributos novos, verificar se SimpleORM já possui suporte.

## 61. SimpleORM Campo

O atributo `Campo` deve carregar tamanho quando aplicável.

Exemplo conceitual:

```text
VARCHAR(100)
→ Campo(..., 100)
```

Confirmar assinatura real antes da implementação.

## 62. Atributos SimpleORM Adicionais

Se não existirem, adicionar:

```text
Sequence
GeneratedByTrigger
DatabaseDefault
DatabaseComputed
CompositeForeignKey
```

Não duplicar funcionalidade existente.

## 63. Data Annotations C#

Usar nativamente:

```text
Table
Column
Key
Required
MaxLength
ForeignKey
DatabaseGenerated
```

Criar atributo próprio apenas quando Data Annotations não representam o metadata necessário.

## 64. Metadata C#

Possíveis arquivos:

```text
Metadata/
├── SequenceAttribute.cs
├── GeneratedByTriggerAttribute.cs
├── DatabaseDefaultAttribute.cs
├── DatabaseComputedAttribute.cs
└── CompositeForeignKeyAttribute.cs
```

Gerar apenas os realmente usados.

Namespace:

```text
<NamespaceBase>.Metadata
```

## 65. Enums

Quando provider reconhecer explicitamente um enum:

```text
status_pedido
```

gerar:

```text
StatusPedido.cs
StatusPedido.pas
```

## 66. Enum C#

```csharp
public enum StatusPedido
{
    Aberto,
    Pago,
    Cancelado
}
```

## 67. Enum Delphi

```delphi
TStatusPedido = (
  Aberto,
  Pago,
  Cancelado
);
```

Um arquivo por enum.

## 68. UDT Não Reconhecido

Não tentar gerar enum.

Utilizar:

```text
CommonDbType.Unknown
```

## 69. Imports C#

Gerar somente `using` necessários.

Nunca manter lista fixa de imports.

## 70. Uses Delphi

Gerar somente units necessárias.

Exemplo:

```text
TStream/TMemoryStream
→ System.Classes
```

`System.Classes` não deve aparecer em entidade que não usa streams.

## 71. Generator Architecture

Não utilizar template engine.

Geração deve ser programática.

```text
TableMetadata
    ↓
EntityGenerator
    ├── NameConverter
    ├── TypeMapper
    ├── AttributeWriter
    ├── PropertyWriter
    ├── RelationshipWriter
    └── SourceWriter
```

## 72. Generator Contract C#

```csharp
public interface IEntityCodeGenerator
{
    string Generate(
        TableMetadata table,
        GenerationContext context);
}
```

## 73. Type Mapper Contract

```csharp
public interface ICSharpTypeMapper
{
    string Map(
        ColumnMetadata column);
}
```

Delphi deverá possuir contrato conceitualmente equivalente.

## 74. GenerationContext

Deve conter pelo menos:

```text
SelectedTables
Namespace/UnitPrefix
OutputDirectory
```

A geração de navegação deve consultar `SelectedTables`.

## 75. Views

Não gerar classes.

Extrair SQL do banco e escrever:

```text
Scripts/
└── Views/
```

## 76. Procedures

Não gerar classes.

Exportar:

```text
Scripts/
└── Procedures/
```

## 77. Triggers

Não gerar classes.

Exportar:

```text
Scripts/
└── Triggers/
```

## 78. SQL

Preservar SQL nativo.

Não:

- converter dialeto;
- reformatar;
- adicionar comentários;
- alterar nomes;
- substituir `CREATE`;
- interpretar e reconstruir quando o banco fornecer definição original.

## 79. Estrutura de Saída C#

```text
Output/
├── Models/
│   ├── Cliente.cs
│   ├── Pedido.cs
│   └── Enums/
│       └── StatusPedido.cs
│
├── Metadata/
│   ├── SequenceAttribute.cs
│   └── ...
│
└── Scripts/
    ├── Views/
    ├── Procedures/
    └── Triggers/
```

## 80. Estrutura de Saída Delphi

```text
Output/
├── Models/
│   ├── Cliente.pas
│   ├── Pedido.pas
│   └── Enums/
│       └── StatusPedido.pas
│
└── Scripts/
    ├── Views/
    ├── Procedures/
    └── Triggers/
```

## 81. Schema nas Pastas SQL

Quando houver schema:

```text
Scripts/
└── Views/
    └── public/
        └── VW_CLIENTES.sql
```

No Firebird não criar pasta de schema artificial.

## 82. Nome dos Arquivos Delphi

Tabela:

```text
CLIENTE_ENDERECO
```

Gerar:

```text
ClienteEndereco.pas
```

Unit:

```delphi
unit ClienteEndereco;
```

Classe:

```delphi
TClienteEndereco
```

Não gerar:

```text
TClienteEndereco.pas
```

## 83. Namespace C#

Informado pelo usuário.

Exemplo:

```text
MinhaEmpresa.Projeto.Models
```

Metadata customizado:

```text
MinhaEmpresa.Projeto.Models.Metadata
```

## 84. Unit Prefix Delphi

A configuração deve existir, mas o arquivo continua sendo:

```text
Cliente.pas
```

Não incluir prefixo `T` no nome do arquivo.

## 85. Arquivos Existentes

Sempre sobrescrever.

Não:

- perguntar;
- criar `.bak`;
- ignorar automaticamente.

## 86. Encoding

Todos os arquivos:

```text
UTF-8 sem BOM
```

## 87. Line Ending

Todos:

```text
CRLF
```

Implementar isso no `FileWriter`.

Generators não devem decidir encoding.

## 88. WPF

Utilizar:

```text
CommunityToolkit.Mvvm
```

Permitido:

```text
ObservableObject
ObservableProperty
RelayCommand
AsyncRelayCommand
```

## 89. DI C#

Utilizar:

```text
Microsoft.Extensions.DependencyInjection
```

Composition Root no projeto WPF.

Constructor injection obrigatória.

Não usar Service Locator.

## 90. Delphi Dependency Injection

Utilizar injeção manual por construtor.

Exemplo:

```delphi
constructor TReadSchemaService.Create(
  const AProviderFactory: IMetadataProviderFactory);
```

Não utilizar container externo nesta versão.

## 91. Delphi Background Work

Utilizar:

```delphi
TTask.Run
```

para:

- leitura do schema;
- geração.

Nunca acessar controle VCL de background thread.

## 92. Progresso

As operações devem reportar:

```text
Connecting
ReadingTables
ReadingColumns
ReadingPrimaryKeys
ReadingForeignKeys
ReadingSequences
ReadingViews
ReadingProcedures
ReadingTriggers
GeneratingModels
GeneratingScripts
Completed
```

## 93. Cancelamento

C#:

```text
CancellationToken
```

Delphi:

cancelamento cooperativo.

Não finalizar thread abruptamente.

## 94. Wizard

As duas aplicações devem seguir:

```text
1. Connection
2. Metadata Reading
3. Object Selection
4. Generation Configuration
5. Generation
6. Result
```

## 95. Seleção de Objetos

Permitir seleção individual de:

```text
Tables
Views
Procedures
Triggers
```

Recursos:

```text
Search
Select All
Unselect All
Invert Selection
Filter by Object Type
```

Enums não são selecionáveis.

Se tabela selecionada depender de enum:

```text
gerar enum automaticamente
```

## 96. Resultado

Mostrar:

```text
Success
Warnings
Errors
```

Exemplo:

```text
Success : 297
Warnings: 5
Errors  : 3
```

Não gerar arquivo de log.

## 97. Erros

Falha em um objeto não pode impedir os demais.

Pseudo fluxo:

```text
for each selected object
    try
        generate
        register success
    catch
        register error
        continue
```

## 98. GenerationResult

Criar estrutura:

```text
GenerationResult
├── SuccessCount
├── WarningCount
├── ErrorCount
└── Messages
```

Mensagem:

```text
Severity
ObjectType
Schema
ObjectName
Stage
Message
```

## 99. Testes Unitários

Obrigatório testar:

```text
NameConverter
Reserved Words
Name Collisions
CommonDbType Mapping
Nullable Mapping
Decimal Mapping
Unicode Metadata
PK
Composite PK
FK
Nullable FK
Composite FK
Identity
Sequence
TriggerSequence
Default
Computed
Enums
Unknown
Dynamic Imports
Dynamic Uses
Delphi Stream Lifecycle
Source Generation
```

## 100. Testes de Integração

Utilizar containers.

```text
tests/
└── Integration/
    ├── PostgreSql/
    ├── SqlServer/
    └── Firebird/
```

Cada banco deve possuir script versionado de inicialização.

## 101. Schema de Teste

Criar pelo menos:

```text
Tabela simples
Tabela sem PK
PK simples
PK composta
FK simples
FK nullable
FK composta
Duas FKs para mesma tabela
Tabela N:N
VARCHAR
Unicode
Decimal
Date
Time
DateTime
BLOB
Default
Identity
Sequence
Trigger + Sequence
Computed
View
Procedure
Trigger
Unknown Type
```

PostgreSQL:

```text
Enum
```

## 102. Estratégia de Teste

Separar:

```text
Database
    ↓
Provider
    ↓
DatabaseMetadata
```

de:

```text
DatabaseMetadata
    ↓
Generator
    ↓
Source
```

Não usar banco real em testes unitários do generator.

## 103. Coding Rules C#

Utilizar:

- nullable reference types;
- `async/await`;
- `CancellationToken`;
- records/objetos imutáveis quando apropriado;
- constructor injection;
- `IReadOnlyList<T>` para coleções expostas;
- funções pequenas;
- responsabilidades únicas.

Evitar:

- classes estáticas globais;
- Service Locator;
- reflection desnecessária;
- lógica SQL em ViewModel;
- lógica de geração em provider;
- lógica de provider em generator.

## 104. Coding Rules Delphi

Utilizar:

- interfaces nos boundaries;
- constructor injection;
- `TTask` para background;
- `try/finally` para ownership;
- `Free` para campos owned;
- Forms leves;
- services sem dependência VCL.

Evitar:

- SQL em Form;
- DataModule global como Service Locator;
- variáveis globais de serviços;
- ownership ambíguo;
- dependência circular entre units.

## 105. SOLID

### SRP

Separar:

```text
Metadata Reader
Type Mapper
Name Converter
Attribute Writer
Relationship Writer
File Writer
```

### OCP

Adicionar novo SGBD sem alterar geradores.

### DIP

Application depende de contratos do Core.

## 106. Functional Design

Preferir:

```text
Input
 ↓
Pure transformation
 ↓
Output
```

Exemplo:

```text
ColumnMetadata
 ↓
CSharpTypeMapper
 ↓
"decimal?"
```

Type mappers e NameConverters devem ser determinísticos e sem efeitos colaterais.

I/O deve ficar nas extremidades:

```text
Database access
File access
UI
```

## 107. Sprint 1 — Core

### Objetivo

Criar foundations e contratos.

#### S1-T01 — Estrutura C# — Concluído

**Resultado esperado:** Solution criada com todas as camadas.  
**Aceite:** Compila sem dependências circulares.  
**Prioridade:** Alta.
**Status:** Concluído no C#.

#### S1-T02 — Estrutura Delphi — Pendente

**Resultado esperado:** Estrutura por units/pastas criada.  
**Aceite:** Projeto VCL compila.  
**Prioridade:** Alta.
**Status:** Pendente.

#### S1-T03 — Metadata — Parcial

Implementar todos os tipos normalizados.

**Aceite:** C# e Delphi possuem representação conceitualmente equivalente.  
**Dependência:** S1-T01/S1-T02.  
**Prioridade:** Alta.
**Status:** Concluído no C#; pendente no Delphi.

#### S1-T04 — CommonDbType — Concluído C#

Implementar enum.

**Aceite:** Tipos previstos nesta documentação representáveis.  
**Prioridade:** Alta.
**Status:** Concluído no C#.

#### S1-T05 — Naming — Concluído C#

Implementar:

- PascalCase;
- separators;
- reserved words;
- collisions.

**Aceite:** Testes automatizados cobrindo todos os exemplos deste documento.  
**Prioridade:** Alta.
**Status:** Concluído no C#.

## 108. Sprint 2 — Connection Infrastructure

### Objetivo

Conexão e seleção de provider.

### Tasks

```text
S2-T01 DatabaseConnectionOptions — Concluído C#
S2-T02 Connection String Builders — Concluído C#
S2-T03 MetadataProviderFactory — Concluído C#
S2-T04 TestConnection — Concluído C#
S2-T05 Default Schema Resolution — Concluído C#
S2-T06 System Object Filtering — Concluído C#
```

### Aceite

Os três bancos podem ser testados sem dependência de UI.
**Status:** Concluído no C#.

## 109. Sprint 3 — PostgreSQL Provider

### Tasks

```text
S3-T01 Tables — Concluído C#
S3-T02 Columns — Concluído C#
S3-T03 PK — Concluído C#
S3-T04 FK — Concluído C#
S3-T05 Composite Keys — Concluído C#
S3-T06 Sequences — Concluído C#
S3-T07 Identity — Concluído C#
S3-T08 Defaults — Concluído C#
S3-T09 Enums — Concluído C#
S3-T10 Views — Concluído C#
S3-T11 Procedures — Concluído C#
S3-T12 Triggers — Concluído C#
S3-T13 Progress — Concluído C#
S3-T14 Cancellation — Concluído C#
```

### Aceite

Provider converte banco de teste integralmente para `DatabaseMetadata`.
**Status:** Concluído no C#.

## 110. Sprint 4 — SQL Server Provider

### Tasks

```text
S4-T01 Tables — Concluído C#
S4-T02 Columns — Concluído C#
S4-T03 Unicode — Concluído C#
S4-T04 PK — Concluído C#
S4-T05 FK — Concluído C#
S4-T06 Composite Keys — Concluído C#
S4-T07 Identity — Concluído C#
S4-T08 Defaults — Concluído C#
S4-T09 Computed — Concluído C#
S4-T10 Views — Concluído C#
S4-T11 Procedures — Concluído C#
S4-T12 Triggers — Concluído C#
S4-T13 Progress — Concluído C#
S4-T14 Cancellation — Concluído C#
```

### Aceite

Provider SQL Server converte banco de teste integralmente para `DatabaseMetadata`.
**Status:** Concluído no C#.

## 111. Sprint 5 — Firebird Provider

### Tasks

```text
S5-T01 Tables — Concluído C#
S5-T02 Columns — Concluído C#
S5-T03 BLOB subtype — Concluído C#
S5-T04 PK — Concluído C#
S5-T05 FK — Concluído C#
S5-T06 Composite Keys — Concluído C#
S5-T07 Generator/Sequence — Concluído C#
S5-T08 Identity — Concluído C#
S5-T09 Trigger + Sequence detection — Concluído C#
S5-T10 Defaults — Concluído C#
S5-T11 Views — Concluído C#
S5-T12 Procedures — Concluído C#
S5-T13 Triggers — Concluído C#
S5-T14 Progress — Concluído C#
S5-T15 Cancellation — Concluído C#
```

### Aceite

Provider Firebird converte banco de teste integralmente para `DatabaseMetadata`.
**Status:** Concluído no C#.

## 112. Sprint 6 — C# Generator

### Tasks

```text
S6-T01 CSharpTypeMapper
S6-T02 AttributeWriter
S6-T03 EntityGenerator
S6-T04 RelationshipWriter
S6-T05 EnumGenerator
S6-T06 SequenceAttribute
S6-T07 DatabaseDefaultAttribute
S6-T08 DatabaseComputedAttribute
S6-T09 GeneratedByTriggerAttribute
S6-T10 CompositeForeignKeyAttribute
S6-T11 DynamicUsings
S6-T12 FileWriter
```

### Aceite

Projeto de referência contendo os fontes gerados compila no .NET 10.

## 113. Sprint 7 — Delphi Generator

### Tasks

```text
S7-T01 DelphiTypeMapper
S7-T02 DecimalMapper
S7-T03 SimpleOrmAttributeWriter
S7-T04 EntityGenerator
S7-T05 RelationshipWriter
S7-T06 EnumGenerator
S7-T07 StreamGeneration
S7-T08 StreamSetterGeneration
S7-T09 DynamicUses
S7-T10 FileWriter
```

### Aceite

Projeto de referência contendo os fontes gerados compila no Delphi 12 Athens.

## 114. Sprint 8 — SQL Export

### Tasks

```text
S8-T01 View Export
S8-T02 Procedure Export
S8-T03 Trigger Export
S8-T04 Schema Folder Handling
S8-T05 Firebird Folder Handling
```

### Aceite

Scripts exportados preservam o SQL original.

## 115. Sprint 9 — WPF Wizard

### Tasks

```text
S9-T01 DI Composition Root
S9-T02 Connection View
S9-T03 Test Connection
S9-T04 Read Metadata
S9-T05 Progress
S9-T06 Cancellation
S9-T07 Object Selection
S9-T08 Generation Configuration
S9-T09 Generation
S9-T10 Results
```

## 116. Sprint 10 — Delphi VCL Wizard

### Tasks

```text
S10-T01 Bootstrap
S10-T02 Connection Form
S10-T03 Test Connection
S10-T04 Metadata Task
S10-T05 Object Selection
S10-T06 Generation Configuration
S10-T07 Generation Task
S10-T08 Cancellation
S10-T09 Result Form
```

## 117. Sprint 11 — Integration Tests

### Tasks

```text
S11-T01 PostgreSQL Container
S11-T02 SQL Server Container
S11-T03 Firebird Container
S11-T04 Init Scripts
S11-T05 Provider Assertions
S11-T06 Generator Snapshots
```

## 118. Sprint 12 — Hardening

### Tasks

```text
S12-T01 Large Database
S12-T02 Unknown Types
S12-T03 Naming Collisions
S12-T04 Cancellation Stress
S12-T05 Generation Failure Isolation
S12-T06 C# Compilation
S12-T07 Delphi Compilation
S12-T08 Final Acceptance
```

## 119. Definition of Done

O projeto somente é considerado concluído quando:

- Firebird conecta;
- PostgreSQL conecta;
- SQL Server conecta;
- teste de conexão é obrigatório;
- schema padrão é utilizado;
- objetos internos não aparecem;
- tables são lidas;
- ordem física é preservada;
- PK simples funciona;
- PK composta funciona;
- tabela sem PK é gerada;
- FK simples funciona;
- FK nullable funciona;
- FK composta funciona;
- relacionamento para tabela não selecionada não gera navigation;
- identity funciona;
- sequence funciona;
- trigger + sequence funciona quando detectável;
- defaults são preservados;
- computed columns são preservadas;
- enums reconhecidos são gerados;
- unknown gera fallback;
- C# gera Data Annotations;
- Delphi gera SimpleORM;
- C# BLOB gera `byte[]`;
- Delphi BLOB gera `TStream`;
- Delphi instancia `TMemoryStream`;
- Delphi libera streams no `Destroy`;
- Delphi setter usa `LoadFromStream`;
- Views geram SQL;
- Procedures geram SQL;
- Triggers geram SQL;
- scripts preservam dialect;
- geração sobrescreve arquivos;
- geração continua após erro individual;
- progresso funciona;
- cancelamento funciona;
- arquivos são UTF-8 sem BOM;
- arquivos usam CRLF;
- testes de integração passam;
- output C# compila;
- output Delphi compila.

## 120. Prioridade de Implementação

Codex deve seguir esta ordem:

```text
1. Core
2. Metadata
3. Naming
4. Connection contracts
5. Provider PostgreSQL
6. Provider SQL Server
7. Provider Firebird
8. Integration tests dos providers
9. C# Generator
10. Delphi Generator
11. SQL Export
12. Generator tests
13. WPF Wizard
14. VCL Wizard
15. Hardening
```

Não iniciar UI antes dos contratos principais de Core e Providers estarem estáveis.

## 121. Regra para Alterações

Ao encontrar uma lacuna durante a implementação:

1. não criar regra de negócio baseada em suposição silenciosa;
2. preservar a informação original no metadata;
3. usar `Unknown` quando não houver mapeamento seguro;
4. manter geração funcionando sempre que possível;
5. registrar warning;
6. não aumentar escopo para migrations ou criação de banco;
7. não introduzir biblioteca externa sem necessidade arquitetural clara.

## 122. Regra Final para o Codex

Sempre priorizar:

```text
Fidelidade ao banco
    >
Código gerado compilável
    >
Arquitetura testável
    >
Simplicidade
    >
Conveniência
```

Quando houver conflito entre “inferir algo útil” e “representar o banco exatamente como existe”, escolher:

```text
representar o banco exatamente como existe
```
