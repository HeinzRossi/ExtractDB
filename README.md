# ExtractDB

**ExtractDB** é uma ferramenta de **Schema Reverse Engineering** voltada à leitura de metadados de bancos de dados e geração de Models e scripts de objetos SQL.

O projeto é composto por **duas aplicações desktop independentes**:

- **ExtractDB C#** — .NET 10 + WPF
- **ExtractDB Delphi** — Delphi 12 Athens+ + VCL

Ambas seguem o mesmo modelo conceitual de metadados, mas possuem implementações nativas e independentes para cada ecossistema.

---

## Objetivo

O ExtractDB conecta-se a um banco existente, lê sua estrutura e gera código-fonte que reflita fielmente o schema encontrado.

SGBDs suportados:

- Firebird
- PostgreSQL
- Microsoft SQL Server

Objetos processados:

| Objeto do banco | Saída |
|---|---|
| Tabela | Model C# ou Delphi |
| View | Script `.sql` |
| Procedure | Script `.sql` |
| Trigger | Script `.sql` |
| Enum reconhecido | Enum C# ou Delphi |
| Sequence / Generator | Metadata / atributo da coluna |

O princípio central do projeto é:

> **Refletir o banco como ele existe, sem tentar corrigir, reinterpretar ou transformar seu modelo físico.**

---

## Características principais

- Reverse engineering de tabelas
- Leitura de colunas e ordem ordinal
- Chaves primárias simples e compostas
- Chaves estrangeiras simples e compostas
- Navegação por Foreign Keys
- Suporte a FKs nullable
- Identity / Auto Increment
- Sequences / Generators
- Detecção de Sequence + Trigger quando aplicável
- Default Values
- Computed / Generated Columns
- Tamanho de campos
- Precision / Scale
- Metadata Unicode
- Enums reconhecidos
- Views como SQL
- Procedures como SQL
- Triggers como SQL
- Fallback para tipos desconhecidos
- Seleção individual de objetos
- Progresso e cancelamento
- Geração resiliente por objeto
- Sobrescrita automática dos arquivos
- UTF-8 sem BOM
- CRLF

---

## Tecnologias

### C#

- .NET 10
- WPF
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Microsoft.Data.SqlClient
- Npgsql
- FirebirdSql.Data.FirebirdClient
- Data Annotations

### Delphi

- Delphi 12 Athens ou superior
- VCL
- FireDAC
- SimpleORM
- Parallel Programming Library / `TTask`

---

## Arquitetura

As duas aplicações seguem o mesmo desenho conceitual:

```text
UI
 ↓
Application
 ↓
Core

Providers  ─────→ Core
Generators ─────→ Core
```

O `Core` não depende de UI, drivers de banco, ORM ou sistema de arquivos.

Fluxo principal:

```text
Database
   ↓
Metadata Provider
   ↓
Normalized Metadata
   ↓
Generator
   ├── Models
   └── SQL Scripts
   ↓
File Writer
```

---

## Estrutura do repositório

```text
ExtractDB/
├── README.md
├── Readme.md
├── docs/
├── csharp/
│   ├── ExtractDB.slnx
│   ├── src/
│   │   ├── ExtractDB.Core/
│   │   ├── ExtractDB.Application/
│   │   ├── ExtractDB.Providers/
│   │   ├── ExtractDB.Generators.CSharp/
│   │   └── ExtractDB.Wpf/
│   └── tests/
│
├── delphi/
│   ├── ExtractDB.dproj
│   ├── ExtractDB.dpr
│   ├── Source/
│   │   ├── Core/
│   │   ├── Application/
│   │   ├── Providers/
│   │   ├── Generators/
│   │   └── UI/
│   └── Tests/
│
└── database-tests/
    ├── docker-compose.yml
    ├── postgresql/
    ├── sqlserver/
    └── firebird/
```

---

## Modelo normalizado

Cada provider converte os tipos nativos de seu SGBD para um modelo comum.

```text
PostgreSQL integer
SQL Server int
Firebird INTEGER
        ↓
CommonDbType.Integer
        ↓
C#     → int
Delphi → Integer
```

O provider nunca decide qual será o tipo final da linguagem.

---

## Regras de naming

Os nomes físicos são normalizados para PascalCase.

```text
CLIENTE_ENDERECO  → ClienteEndereco
ID_CLIENTE        → IdCliente
DATA_CADASTRO     → DataCadastro
CPF_CNPJ          → CpfCnpj
URL_IMAGEM        → UrlImagem
UUID_EXTERNO      → UuidExterno
```

No Delphi, classes recebem o prefixo `T`:

```text
CLIENTE_ENDERECO
→ TClienteEndereco
```

O arquivo continua sem o prefixo:

```text
ClienteEndereco.pas
```

Não há singularização automática.

Colisões são resolvidas automaticamente:

```text
ID_CLIENTE → IdCliente
IDCLIENTE  → IdCliente2
```

Palavras reservadas recebem sufixo:

```text
CLASS → ClassValue
END   → EndValue
```

---

## Geração C#

O C# utiliza Data Annotations sempre que possível.

```csharp
[Table("CLIENTE")]
public class Cliente
{
    [Key]
    [Column("ID_CLIENTE")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCliente { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("NOME")]
    public string Nome { get; set; } = string.Empty;
}
```

Atributos próprios serão gerados quando necessário:

- `SequenceAttribute`
- `GeneratedByTriggerAttribute`
- `DatabaseDefaultAttribute`
- `DatabaseComputedAttribute`
- `CompositeForeignKeyAttribute`

Namespace:

```text
<NamespaceBase>.Metadata
```

---

## Geração Delphi

O Delphi utiliza atributos do SimpleORM.

```delphi
[Tabela('CLIENTE')]
TCliente = class
private
  FIdCliente: Integer;
  FNome: string;
published
  [Campo('ID_CLIENTE'), PK, AutoInc, NotNull]
  property IdCliente: Integer
    read FIdCliente
    write FIdCliente;

  [Campo('NOME'), NotNull]
  property Nome: string
    read FNome
    write FNome;
end;
```

Caso algum atributo necessário não exista no SimpleORM, poderá ser incorporado ao framework.

Possíveis extensões:

- `Sequence`
- `GeneratedByTrigger`
- `DatabaseDefault`
- `DatabaseComputed`
- `CompositeForeignKey`

---

## BLOB / IMAGE

### C#

Campos binários são gerados como `byte[]`.

```csharp
[Column("DOCUMENTO")]
public byte[]? Documento { get; set; }
```

### Delphi

Campos binários são gerados como `TStream`, com implementação interna em `TMemoryStream`.

```delphi
private
  FDocumento: TStream;

  procedure SetDocumento(const Value: TStream);

published
  [Campo('DOCUMENTO')]
  property Documento: TStream
    read FDocumento
    write SetDocumento;
```

A classe gerencia o ciclo de vida do stream:

```delphi
constructor TDocumento.Create;
begin
  inherited Create;
  FDocumento := TMemoryStream.Create;
end;

destructor TDocumento.Destroy;
begin
  FDocumento.Free;
  inherited;
end;
```

Setter:

```delphi
procedure TDocumento.SetDocumento(const Value: TStream);
begin
  if Assigned(Value) then
    TMemoryStream(FDocumento).LoadFromStream(Value)
  else
    TMemoryStream(FDocumento).Clear;
end;
```

A entidade mantém ownership do `TMemoryStream`.

---

## Foreign Keys

Uma FK gera:

1. propriedade escalar;
2. propriedade de navegação, desde que a tabela relacionada também tenha sido selecionada.

C#:

```csharp
[Column("ID_CLIENTE")]
public int IdCliente { get; set; }

[ForeignKey(nameof(IdCliente))]
public Cliente Cliente { get; set; } = null!;
```

Caso `CLIENTE` não tenha sido selecionada, a navegação não é gerada, mas o metadata da FK permanece preservado.

---

## Chaves compostas

PKs e FKs compostas são preservadas.

C#:

```csharp
[Key]
public int IdUsuario { get; set; }

[Key]
public int IdPerfil { get; set; }
```

Delphi:

```delphi
[Campo('ID_USUARIO'), PK]
property IdUsuario: Integer;

[Campo('ID_PERFIL'), PK]
property IdPerfil: Integer;
```

---

## Tabelas sem Primary Key

Tabelas sem PK são geradas normalmente.

O ExtractDB não tenta inferir ou criar uma chave.

---

## Tipos desconhecidos

Tipos não reconhecidos não interrompem a geração.

C#:

```csharp
public object? CampoEspecial { get; set; }
```

Delphi:

```delphi
property CampoEspecial: Variant;
```

Um warning é exibido na interface.

---

## SQL gerado

Views, Procedures e Triggers são exportados preservando o SQL nativo do banco.

Não são realizadas:

- conversões de dialect;
- reformatações;
- alterações de nomes;
- inclusão automática de comentários.

Estrutura:

```text
Output/
└── Scripts/
    ├── Views/
    ├── Procedures/
    └── Triggers/
```

Quando aplicável:

```text
Scripts/
└── Views/
    └── public/
        └── VW_CLIENTES.sql
```

No Firebird não é criada uma pasta artificial de schema.

---

## Estrutura dos arquivos gerados

### C#

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

### Delphi

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

---

## Wizard

As duas aplicações seguem o mesmo fluxo:

```text
1. Conexão
2. Leitura dos metadados
3. Seleção dos objetos
4. Configuração da geração
5. Geração
6. Resultado
```

### Conexão

Campos:

```text
Provider
Server
Port
Database
UserName
Password
```

O teste de conexão é obrigatório antes de avançar.

As credenciais nunca são persistidas.

---

## Seleção de objetos

O usuário pode selecionar individualmente:

- Tables
- Views
- Procedures
- Triggers

Recursos:

- busca;
- marcar todos;
- desmarcar todos;
- inverter seleção;
- filtro por tipo.

Enums são gerados automaticamente quando necessários.

---

## Progresso e cancelamento

Operações longas devem reportar progresso e suportar cancelamento.

C#:

```text
CancellationToken
IProgress<T>
```

Delphi:

```text
TTask.Run
```

com cancelamento cooperativo.

---

## Tratamento de erros

Falha em um objeto não interrompe a geração dos demais.

```text
Sucesso : 297
Warnings: 5
Erros   : 3
```

Os detalhes são exibidos somente na interface.

Nenhum arquivo de log é criado.

---

## Arquivos gerados

```text
Encoding    → UTF-8 sem BOM
Line ending → CRLF
Overwrite   → Sempre
```

Arquivos existentes são sobrescritos automaticamente.

---

## Testes

### Unitários

Cobrir:

- NameConverter
- palavras reservadas
- colisões
- type mapping
- nullability
- decimal mapping
- Unicode
- PK
- PK composta
- FK
- FK nullable
- FK composta
- Identity
- Sequence
- TriggerSequence
- Default
- Computed
- Enums
- Unknown
- imports dinâmicos
- uses dinâmicos
- lifecycle de streams Delphi
- geração de source

### Integração

Utilizar containers para:

- PostgreSQL
- SQL Server
- Firebird

Cada banco deve possuir scripts versionados de inicialização.

---

## Princípios de desenvolvimento

Prioridades:

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

Aplicar SOLID, especialmente:

- Single Responsibility Principle
- Open/Closed Principle
- Dependency Inversion Principle

Preferir transformações determinísticas e manter I/O nas bordas da aplicação.

---

## Roadmap

### Sprint 1 — Core — Parcial
- [x] estrutura C# da solução;
- [ ] estrutura Delphi da solução;
- [x] modelo normalizado C#;
- [x] CommonDbType C#;
- [x] naming C#;
- [x] testes unitários iniciais C#.

### Sprint 2 — Connection Infrastructure — Concluído C#
- [x] connection options C#;
- [x] connection string builders C#;
- [x] provider factory C#;
- [x] test connection C#;
- [x] schema padrão C#;
- [x] filtros de objetos internos C#.

### Sprint 3 — PostgreSQL Provider — Concluído C#
- [x] tables C#;
- [x] columns C#;
- [x] PK C#;
- [x] FK C#;
- [x] sequences C#;
- [x] identity C#;
- [x] defaults C#;
- [x] enums C#;
- [x] views C#;
- [x] procedures C#;
- [x] triggers C#;
- [x] progress C#;
- [x] cancellation C#.

### Sprint 4 — SQL Server Provider
- tables;
- columns;
- Unicode;
- PK;
- FK;
- identity;
- defaults;
- computed;
- views;
- procedures;
- triggers.

### Sprint 5 — Firebird Provider
- tables;
- columns;
- BLOB subtype;
- PK;
- FK;
- generators/sequences;
- identity;
- trigger + sequence;
- defaults;
- views;
- procedures;
- triggers.

### Sprint 6 — C# Generator
- type mapper;
- Data Annotations;
- custom attributes;
- relationships;
- enums;
- dynamic imports;
- file writer.

### Sprint 7 — Delphi Generator
- type mapper;
- decimal mapper;
- SimpleORM attributes;
- relationships;
- enums;
- streams;
- dynamic uses;
- file writer.

### Sprint 8 — SQL Export
- views;
- procedures;
- triggers;
- organização por schema.

### Sprint 9 — WPF Wizard
- MVVM;
- DI;
- conexão;
- leitura;
- seleção;
- geração;
- resultado.

### Sprint 10 — Delphi VCL Wizard
- bootstrap;
- forms;
- TTask;
- cancelamento;
- geração;
- resultado.

### Sprint 11 — Integration Tests
- containers;
- schemas;
- assertions;
- snapshots.

### Sprint 12 — Hardening
- banco grande;
- tipos desconhecidos;
- colisões;
- stress de cancelamento;
- falhas isoladas;
- compilação C#;
- compilação Delphi.

---

## Documentação para agentes de código

A especificação operacional detalhada para Codex e outros agentes está disponível em:

```text
Docs\Readme.md
```

Esse arquivo contém contratos, regras invariáveis, ordem de implementação, Sprints, Tasks e critérios de aceite.

---

## Status

Projeto em fase de especificação e implementação inicial.

---

## Licença

Definir antes da publicação pública do repositório.
