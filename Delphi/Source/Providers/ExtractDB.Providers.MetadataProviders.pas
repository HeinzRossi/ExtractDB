unit ExtractDB.Providers.MetadataProviders;

interface

uses
  System.SysUtils,
  System.Generics.Collections,
  Data.DB,
  FireDAC.Comp.Client,
  FireDAC.DApt,
  FireDAC.Stan.Async,
  ExtractDB.Application.Connection,
  ExtractDB.Core.Model,
  ExtractDB.Core.Types,
  ExtractDB.Providers.Contracts;

type
  TDatabaseMetadataProviderBase = class(TInterfacedObject, IDatabaseMetadataProvider)
  private
    FConnectionFactory: TFireDacConnectionFactory;
  protected
    function CreateConnection(AOptions: TDatabaseConnectionOptions): TFDConnection;
    function OpenQuery(AConnection: TFDConnection; const ASql: string): TFDQuery;
    function FindTable(ADatabase: TDatabaseMetadata; const ASchema: string; const AName: string): TTableMetadata;
    function GetString(AQuery: TDataSet; const AFieldName: string): string;
    function GetInteger(AQuery: TDataSet; const AFieldName: string): Integer;
    function GetBoolean(AQuery: TDataSet; const AFieldName: string): Boolean;
    function MapCommonType(const ANativeType: string; AProvider: TDatabaseProvider): TCommonDbType;
    procedure AddColumnFromQuery(ATable: TTableMetadata; AQuery: TDataSet; AProvider: TDatabaseProvider);
    procedure AddPrimaryKeyColumn(ATable: TTableMetadata; const AKeyName: string; const AColumnName: string);
    procedure MarkPrimaryKeyColumn(ATable: TTableMetadata; const AColumnName: string);
  public
    constructor Create(AConnectionFactory: TFireDacConnectionFactory);
    function ProviderName: string; virtual; abstract;
    function TestConnection(AOptions: TDatabaseConnectionOptions): Boolean;
    function ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata; virtual; abstract;
  end;

  TPostgreSqlMetadataProvider = class(TDatabaseMetadataProviderBase)
  public
    function ProviderName: string; override;
    function ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata; override;
  end;

  TSqlServerMetadataProvider = class(TDatabaseMetadataProviderBase)
  public
    function ProviderName: string; override;
    function ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata; override;
  end;

  TFirebirdMetadataProvider = class(TDatabaseMetadataProviderBase)
  public
    class function GetNativeType(AFieldType: Integer; AFieldSubType: Integer): string; static;
    function ProviderName: string; override;
    function ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata; override;
  end;

  TDatabaseMetadataProviderFactory = class
  private
    FConnectionFactory: TFireDacConnectionFactory;
  public
    constructor Create;
    destructor Destroy; override;
    function CreateProvider(AProvider: TDatabaseProvider): IDatabaseMetadataProvider;
  end;

implementation

uses
  System.StrUtils,
  FireDAC.Stan.Param,
  ExtractDB.Providers.SystemFilter;

constructor TDatabaseMetadataProviderBase.Create(AConnectionFactory: TFireDacConnectionFactory);
begin
  inherited Create;
  FConnectionFactory := AConnectionFactory;
end;

procedure TDatabaseMetadataProviderBase.AddColumnFromQuery(
  ATable: TTableMetadata;
  AQuery: TDataSet;
  AProvider: TDatabaseProvider);
var
  LColumn: TColumnMetadata;
  LDefault: string;
  LComputed: string;
begin
  LColumn := TColumnMetadata.Create;
  LColumn.Name := GetString(AQuery, 'column_name');
  LColumn.OrdinalPosition := GetInteger(AQuery, 'ordinal_position');
  if (AProvider = dpFirebird) and Assigned(AQuery.FindField('field_type')) then
    LColumn.NativeType := TFirebirdMetadataProvider.GetNativeType(
      GetInteger(AQuery, 'field_type'),
      GetInteger(AQuery, 'field_sub_type'))
  else
    LColumn.NativeType := GetString(AQuery, 'native_type');
  LColumn.DbType := MapCommonType(LColumn.NativeType, AProvider);
  LColumn.IsNullable := GetBoolean(AQuery, 'is_nullable');
  LColumn.IsUnicode := GetBoolean(AQuery, 'is_unicode');

  if GetInteger(AQuery, 'column_length') > 0 then
  begin
    LColumn.HasLength := True;
    LColumn.Length := GetInteger(AQuery, 'column_length');
  end;

  if GetInteger(AQuery, 'column_precision') > 0 then
  begin
    LColumn.HasPrecision := True;
    LColumn.Precision := GetInteger(AQuery, 'column_precision');
  end;

  if GetInteger(AQuery, 'column_scale') >= 0 then
  begin
    LColumn.HasScale := True;
    LColumn.Scale := GetInteger(AQuery, 'column_scale');
  end;

  LDefault := GetString(AQuery, 'default_value');
  if GetBoolean(AQuery, 'is_identity') then
  begin
    LColumn.ValueGeneration := TValueGenerationMetadata.Create;
    LColumn.ValueGeneration.Strategy := vgsIdentity;
  end;

  if LDefault <> '' then
  begin
    LColumn.DefaultValue := TDefaultValueMetadata.Create;
    LColumn.DefaultValue.RawExpression := LDefault;
    if (Pos('nextval', LowerCase(LDefault)) > 0) or
       (Pos('next value for', LowerCase(LDefault)) > 0) or
       (Pos('gen_id', LowerCase(LDefault)) > 0) then
      LColumn.DefaultValue.Kind := dvkSequence
    else
      LColumn.DefaultValue.Kind := dvkExpression;
  end;

  LComputed := GetString(AQuery, 'computed_expression');
  if LComputed <> '' then
  begin
    LColumn.IsComputed := True;
    LColumn.ComputedExpression := LComputed;
  end;

  ATable.Columns.Add(LColumn);
end;

procedure TDatabaseMetadataProviderBase.AddPrimaryKeyColumn(
  ATable: TTableMetadata;
  const AKeyName: string;
  const AColumnName: string);
var
  LColumns: TArray<string>;
begin
  if not Assigned(ATable.PrimaryKey) then
  begin
    ATable.PrimaryKey := TPrimaryKeyMetadata.Create;
    ATable.PrimaryKey.Name := AKeyName;
  end;

  LColumns := ATable.PrimaryKey.Columns;
  SetLength(LColumns, Length(LColumns) + 1);
  LColumns[High(LColumns)] := AColumnName;
  ATable.PrimaryKey.Columns := LColumns;
  MarkPrimaryKeyColumn(ATable, AColumnName);
end;

function TDatabaseMetadataProviderBase.CreateConnection(AOptions: TDatabaseConnectionOptions): TFDConnection;
begin
  Result := FConnectionFactory.CreateConnection(AOptions);
end;

function TDatabaseMetadataProviderBase.FindTable(
  ADatabase: TDatabaseMetadata;
  const ASchema: string;
  const AName: string): TTableMetadata;
var
  LTable: TTableMetadata;
begin
  for LTable in ADatabase.Tables do
    if SameText(LTable.Name, AName) and SameText(LTable.Schema, ASchema) then
      Exit(LTable);

  Result := nil;
end;

function TDatabaseMetadataProviderBase.GetBoolean(AQuery: TDataSet; const AFieldName: string): Boolean;
var
  LValue: string;
  LInteger: Integer;
begin
  if not Assigned(AQuery.FindField(AFieldName)) or AQuery.FieldByName(AFieldName).IsNull then
    Exit(False);

  LValue := AQuery.FieldByName(AFieldName).AsString.Trim;

  if TryStrToInt(LValue, LInteger) then
    Exit(LInteger <> 0);

  Result :=
    SameText(LValue, 'YES') or
    SameText(LValue, 'TRUE') or
    SameText(LValue, 'T') or
    SameText(LValue, 'Y');
end;

function TDatabaseMetadataProviderBase.GetInteger(AQuery: TDataSet; const AFieldName: string): Integer;
begin
  if not Assigned(AQuery.FindField(AFieldName)) or AQuery.FieldByName(AFieldName).IsNull then
    Exit(0);

  Result := AQuery.FieldByName(AFieldName).AsInteger;
end;

function TDatabaseMetadataProviderBase.GetString(AQuery: TDataSet; const AFieldName: string): string;
begin
  if not Assigned(AQuery.FindField(AFieldName)) or AQuery.FieldByName(AFieldName).IsNull then
    Exit('');

  Result := AQuery.FieldByName(AFieldName).AsString.Trim;
end;

function TDatabaseMetadataProviderBase.MapCommonType(
  const ANativeType: string;
  AProvider: TDatabaseProvider): TCommonDbType;
var
  LType: string;
begin
  LType := ANativeType.ToLowerInvariant;

  if MatchText(LType, ['smallint', 'int2']) then
    Exit(cdtSmallInt);
  if MatchText(LType, ['integer', 'int', 'int4', 'serial']) then
    Exit(cdtInteger);
  if MatchText(LType, ['bigint', 'int8', 'bigserial']) then
    Exit(cdtBigInt);
  if MatchText(LType, ['decimal', 'numeric', 'money', 'smallmoney']) then
    Exit(cdtDecimal);
  if MatchText(LType, ['real', 'float4', 'float']) then
    Exit(cdtFloat);
  if MatchText(LType, ['double precision', 'float8']) then
    Exit(cdtDouble);
  if MatchText(LType, ['boolean', 'bool', 'bit']) then
    Exit(cdtBoolean);
  if MatchText(LType, ['char', 'character', 'nchar']) then
    Exit(cdtChar);
  if MatchText(LType, ['varchar', 'character varying', 'nvarchar']) then
    Exit(cdtVarChar);
  if MatchText(LType, ['text', 'ntext', 'blob sub_type 1']) then
    Exit(cdtText);
  if MatchText(LType, ['date']) then
    Exit(cdtDate);
  if MatchText(LType, ['time', 'time without time zone', 'time with time zone']) then
    Exit(cdtTime);
  if MatchText(LType, ['timestamp', 'timestamp without time zone', 'timestamp with time zone', 'datetime', 'datetime2', 'smalldatetime']) then
    Exit(cdtDateTime);
  if MatchText(LType, ['binary', 'varbinary', 'image', 'bytea', 'blob sub_type 0']) then
    Exit(cdtBinary);
  if MatchText(LType, ['blob']) then
    Exit(cdtBlob);
  if MatchText(LType, ['uuid', 'uniqueidentifier', 'guid']) then
    Exit(cdtGuid);
  if MatchText(LType, ['json', 'jsonb']) then
    Exit(cdtJson);

  Result := cdtUnknown;
end;

procedure TDatabaseMetadataProviderBase.MarkPrimaryKeyColumn(
  ATable: TTableMetadata;
  const AColumnName: string);
var
  LColumn: TColumnMetadata;
begin
  for LColumn in ATable.Columns do
    if SameText(LColumn.Name, AColumnName) then
      LColumn.IsPrimaryKey := True;
end;

function TDatabaseMetadataProviderBase.OpenQuery(
  AConnection: TFDConnection;
  const ASql: string): TFDQuery;
begin
  Result := TFDQuery.Create(nil);
  Result.Connection := AConnection;
  Result.SQL.Text := ASql;
  Result.Open;
end;

function TDatabaseMetadataProviderBase.TestConnection(AOptions: TDatabaseConnectionOptions): Boolean;
var
  LConnection: TFDConnection;
begin
  LConnection := CreateConnection(AOptions);
  try
    LConnection.Connected := True;
    Result := LConnection.Connected;
  finally
    LConnection.Free;
  end;
end;

function TPostgreSqlMetadataProvider.ProviderName: string;
begin
  Result := 'PostgreSQL';
end;

function TPostgreSqlMetadataProvider.ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata;
var
  LConnection: TFDConnection;
  LQuery: TFDQuery;
  LTable: TTableMetadata;
  LForeignKey: TForeignKeyMetadata;
  LSequence: TSequenceMetadata;
begin
  Result := TDatabaseMetadata.Create;
  Result.Provider := dpPostgreSql;
  Result.DatabaseName := AOptions.Database;

  try
    LConnection := CreateConnection(AOptions);
    try
      LConnection.Connected := True;

    LQuery := OpenQuery(LConnection, 'select current_schema() as schema_name');
    try
      if not LQuery.Eof then
        Result.DefaultSchema := GetString(LQuery, 'schema_name');
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select table_schema, table_name from information_schema.tables ' +
      'where table_type = ''BASE TABLE'' and table_schema = current_schema() ' +
      'order by table_name');
    try
      while not LQuery.Eof do
      begin
        if TSystemObjectFilter.IsUserObject(dpPostgreSql, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name')) then
        begin
          LTable := TTableMetadata.Create;
          LTable.Schema := GetString(LQuery, 'table_schema');
          LTable.Name := GetString(LQuery, 'table_name');
          Result.Tables.Add(LTable);
        end;
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select table_schema, table_name, column_name, ordinal_position, data_type as native_type, ' +
      'coalesce(character_maximum_length, 0) as column_length, coalesce(numeric_precision, 0) as column_precision, ' +
      'coalesce(numeric_scale, -1) as column_scale, is_nullable, 0 as is_unicode, ' +
      'column_default as default_value, is_identity, coalesce(generation_expression, '''') as computed_expression ' +
      'from information_schema.columns where table_schema = current_schema() order by table_name, ordinal_position');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
          AddColumnFromQuery(LTable, LQuery, dpPostgreSql);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select tc.constraint_name, tc.table_schema, tc.table_name, kcu.column_name ' +
      'from information_schema.table_constraints tc join information_schema.key_column_usage kcu ' +
      'on tc.constraint_name = kcu.constraint_name and tc.table_schema = kcu.table_schema ' +
      'where tc.constraint_type = ''PRIMARY KEY'' and tc.table_schema = current_schema() ' +
      'order by tc.table_name, kcu.ordinal_position');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
          AddPrimaryKeyColumn(LTable, GetString(LQuery, 'constraint_name'), GetString(LQuery, 'column_name'));
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select rc.constraint_name, kcu.table_schema, kcu.table_name, kcu.column_name, ' +
      'ccu.table_schema as target_schema, ccu.table_name as target_table, ccu.column_name as target_column ' +
      'from information_schema.referential_constraints rc ' +
      'join information_schema.key_column_usage kcu on rc.constraint_name = kcu.constraint_name and rc.constraint_schema = kcu.constraint_schema ' +
      'join information_schema.constraint_column_usage ccu on rc.unique_constraint_name = ccu.constraint_name and rc.unique_constraint_schema = ccu.constraint_schema ' +
      'where kcu.table_schema = current_schema()');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
        begin
          LForeignKey := TForeignKeyMetadata.Create;
          LForeignKey.Name := GetString(LQuery, 'constraint_name');
          LForeignKey.SourceSchema := GetString(LQuery, 'table_schema');
          LForeignKey.SourceTable := GetString(LQuery, 'table_name');
          LForeignKey.SourceColumns := [GetString(LQuery, 'column_name')];
          LForeignKey.TargetSchema := GetString(LQuery, 'target_schema');
          LForeignKey.TargetTable := GetString(LQuery, 'target_table');
          LForeignKey.TargetColumns := [GetString(LQuery, 'target_column')];
          LTable.ForeignKeys.Add(LForeignKey);
        end;
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select sequence_schema, sequence_name from information_schema.sequences ' +
      'where sequence_schema = current_schema() order by sequence_name');
    try
      while not LQuery.Eof do
      begin
        LSequence := TSequenceMetadata.Create;
        LSequence.Schema := GetString(LQuery, 'sequence_schema');
        LSequence.Name := GetString(LQuery, 'sequence_name');
        LSequence.Sql := Format('CREATE SEQUENCE %s.%s START WITH 0;', [LSequence.Schema, LSequence.Name]);
        Result.Sequences.Add(LSequence);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select table_schema, table_name, view_definition from information_schema.views ' +
      'where table_schema = current_schema() order by table_name');
    try
      while not LQuery.Eof do
      begin
        Result.Views.Add(TViewMetadata.Create);
        Result.Views.Last.Schema := GetString(LQuery, 'table_schema');
        Result.Views.Last.Name := GetString(LQuery, 'table_name');
        Result.Views.Last.Sql := GetString(LQuery, 'view_definition');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select n.nspname as schema_name, p.proname as object_name, pg_get_functiondef(p.oid) as sql_text ' +
      'from pg_proc p join pg_namespace n on n.oid = p.pronamespace ' +
      'where n.nspname = current_schema() order by p.proname');
    try
      while not LQuery.Eof do
      begin
        Result.Procedures.Add(TProcedureMetadata.Create);
        Result.Procedures.Last.Schema := GetString(LQuery, 'schema_name');
        Result.Procedures.Last.Name := GetString(LQuery, 'object_name');
        Result.Procedures.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select event_object_schema as schema_name, trigger_name, action_statement as sql_text ' +
      'from information_schema.triggers where event_object_schema = current_schema() order by trigger_name');
    try
      while not LQuery.Eof do
      begin
        Result.Triggers.Add(TTriggerMetadata.Create);
        Result.Triggers.Last.Schema := GetString(LQuery, 'schema_name');
        Result.Triggers.Last.Name := GetString(LQuery, 'trigger_name');
        Result.Triggers.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;
    finally
      LConnection.Free;
    end;
  except
    Result.Free;
    raise;
  end;
end;

function TSqlServerMetadataProvider.ProviderName: string;
begin
  Result := 'SQL Server';
end;

function TSqlServerMetadataProvider.ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata;
var
  LConnection: TFDConnection;
  LQuery: TFDQuery;
  LTable: TTableMetadata;
  LForeignKey: TForeignKeyMetadata;
  LSequence: TSequenceMetadata;
begin
  Result := TDatabaseMetadata.Create;
  Result.Provider := dpSqlServer;
  Result.DatabaseName := AOptions.Database;

  try
    LConnection := CreateConnection(AOptions);
    try
      LConnection.Connected := True;

    LQuery := OpenQuery(LConnection, 'select schema_name() as schema_name');
    try
      if not LQuery.Eof then
        Result.DefaultSchema := GetString(LQuery, 'schema_name');
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select s.name as schema_name, t.name as table_name from sys.tables t ' +
      'join sys.schemas s on s.schema_id = t.schema_id ' +
      'where t.is_ms_shipped = 0 and s.name = schema_name() order by t.name');
    try
      while not LQuery.Eof do
      begin
        LTable := TTableMetadata.Create;
        LTable.Schema := GetString(LQuery, 'schema_name');
        LTable.Name := GetString(LQuery, 'table_name');
        Result.Tables.Add(LTable);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select s.name as table_schema, t.name as table_name, c.name as column_name, c.column_id as ordinal_position, ' +
      'ty.name as native_type, case when c.max_length < 0 then 0 else c.max_length end as column_length, ' +
      'c.precision as column_precision, c.scale as column_scale, c.is_nullable, case when ty.name in (''nchar'', ''nvarchar'', ''ntext'') then 1 else 0 end as is_unicode, ' +
      'dc.definition as default_value, c.is_identity, cc.definition as computed_expression ' +
      'from sys.columns c join sys.tables t on t.object_id = c.object_id join sys.schemas s on s.schema_id = t.schema_id ' +
      'join sys.types ty on ty.user_type_id = c.user_type_id ' +
      'left join sys.default_constraints dc on dc.object_id = c.default_object_id ' +
      'left join sys.computed_columns cc on cc.object_id = c.object_id and cc.column_id = c.column_id ' +
      'where t.is_ms_shipped = 0 and s.name = schema_name() order by t.name, c.column_id');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
          AddColumnFromQuery(LTable, LQuery, dpSqlServer);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select kc.name as constraint_name, s.name as table_schema, t.name as table_name, c.name as column_name ' +
      'from sys.key_constraints kc join sys.index_columns ic on ic.object_id = kc.parent_object_id and ic.index_id = kc.unique_index_id ' +
      'join sys.columns c on c.object_id = ic.object_id and c.column_id = ic.column_id ' +
      'join sys.tables t on t.object_id = kc.parent_object_id join sys.schemas s on s.schema_id = t.schema_id ' +
      'where kc.type = ''PK'' and t.is_ms_shipped = 0 and s.name = schema_name() order by t.name, ic.key_ordinal');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
          AddPrimaryKeyColumn(LTable, GetString(LQuery, 'constraint_name'), GetString(LQuery, 'column_name'));
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select fk.name as constraint_name, ss.name as table_schema, st.name as table_name, sc.name as column_name, ' +
      'ts.name as target_schema, tt.name as target_table, tc.name as target_column ' +
      'from sys.foreign_keys fk join sys.foreign_key_columns fkc on fkc.constraint_object_id = fk.object_id ' +
      'join sys.tables st on st.object_id = fkc.parent_object_id join sys.schemas ss on ss.schema_id = st.schema_id ' +
      'join sys.columns sc on sc.object_id = st.object_id and sc.column_id = fkc.parent_column_id ' +
      'join sys.tables tt on tt.object_id = fkc.referenced_object_id join sys.schemas ts on ts.schema_id = tt.schema_id ' +
      'join sys.columns tc on tc.object_id = tt.object_id and tc.column_id = fkc.referenced_column_id ' +
      'where fk.is_ms_shipped = 0 and ss.name = schema_name() order by fk.name, fkc.constraint_column_id');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, GetString(LQuery, 'table_schema'), GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
        begin
          LForeignKey := TForeignKeyMetadata.Create;
          LForeignKey.Name := GetString(LQuery, 'constraint_name');
          LForeignKey.SourceSchema := GetString(LQuery, 'table_schema');
          LForeignKey.SourceTable := GetString(LQuery, 'table_name');
          LForeignKey.SourceColumns := [GetString(LQuery, 'column_name')];
          LForeignKey.TargetSchema := GetString(LQuery, 'target_schema');
          LForeignKey.TargetTable := GetString(LQuery, 'target_table');
          LForeignKey.TargetColumns := [GetString(LQuery, 'target_column')];
          LTable.ForeignKeys.Add(LForeignKey);
        end;
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select s.name as schema_name, seq.name as sequence_name, ty.name as type_name, ' +
      'seq.increment from sys.sequences seq ' +
      'join sys.schemas s on s.schema_id = seq.schema_id ' +
      'join sys.types ty on ty.user_type_id = seq.user_type_id ' +
      'where s.name = schema_name() order by seq.name');
    try
      while not LQuery.Eof do
      begin
        LSequence := TSequenceMetadata.Create;
        LSequence.Schema := GetString(LQuery, 'schema_name');
        LSequence.Name := GetString(LQuery, 'sequence_name');
        LSequence.Sql := Format(
          'CREATE SEQUENCE [%s].[%s] AS %s START WITH 0 INCREMENT BY %s;',
          [
            LSequence.Schema,
            LSequence.Name,
            GetString(LQuery, 'type_name'),
            GetString(LQuery, 'increment')
          ]);
        Result.Sequences.Add(LSequence);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select s.name as schema_name, v.name as object_name, m.definition as sql_text ' +
      'from sys.views v join sys.schemas s on s.schema_id = v.schema_id left join sys.sql_modules m on m.object_id = v.object_id ' +
      'where v.is_ms_shipped = 0 and s.name = schema_name() order by v.name');
    try
      while not LQuery.Eof do
      begin
        Result.Views.Add(TViewMetadata.Create);
        Result.Views.Last.Schema := GetString(LQuery, 'schema_name');
        Result.Views.Last.Name := GetString(LQuery, 'object_name');
        Result.Views.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select s.name as schema_name, p.name as object_name, m.definition as sql_text ' +
      'from sys.procedures p join sys.schemas s on s.schema_id = p.schema_id left join sys.sql_modules m on m.object_id = p.object_id ' +
      'where p.is_ms_shipped = 0 and s.name = schema_name() order by p.name');
    try
      while not LQuery.Eof do
      begin
        Result.Procedures.Add(TProcedureMetadata.Create);
        Result.Procedures.Last.Schema := GetString(LQuery, 'schema_name');
        Result.Procedures.Last.Name := GetString(LQuery, 'object_name');
        Result.Procedures.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select s.name as schema_name, tr.name as object_name, m.definition as sql_text ' +
      'from sys.triggers tr join sys.tables t on t.object_id = tr.parent_id join sys.schemas s on s.schema_id = t.schema_id ' +
      'left join sys.sql_modules m on m.object_id = tr.object_id where tr.is_ms_shipped = 0 and s.name = schema_name() order by tr.name');
    try
      while not LQuery.Eof do
      begin
        Result.Triggers.Add(TTriggerMetadata.Create);
        Result.Triggers.Last.Schema := GetString(LQuery, 'schema_name');
        Result.Triggers.Last.Name := GetString(LQuery, 'object_name');
        Result.Triggers.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;
    finally
      LConnection.Free;
    end;
  except
    Result.Free;
    raise;
  end;
end;

function TFirebirdMetadataProvider.ProviderName: string;
begin
  Result := 'Firebird';
end;

class function TFirebirdMetadataProvider.GetNativeType(
  AFieldType: Integer;
  AFieldSubType: Integer): string;
begin
  case AFieldType of
    7:
      case AFieldSubType of
        1:
          Exit('NUMERIC');
        2:
          Exit('DECIMAL');
      else
        Exit('SMALLINT');
      end;
    8:
      case AFieldSubType of
        1:
          Exit('NUMERIC');
        2:
          Exit('DECIMAL');
      else
        Exit('INTEGER');
      end;
    10:
      Exit('FLOAT');
    12:
      Exit('DATE');
    13:
      Exit('TIME');
    14:
      Exit('CHAR');
    16:
      case AFieldSubType of
        1:
          Exit('NUMERIC');
        2:
          Exit('DECIMAL');
      else
        Exit('BIGINT');
      end;
    23:
      Exit('BOOLEAN');
    27:
      Exit('DOUBLE PRECISION');
    35:
      Exit('TIMESTAMP');
    37:
      Exit('VARCHAR');
    261:
      if AFieldSubType = 1 then
        Exit('BLOB SUB_TYPE TEXT')
      else
        Exit('BLOB');
  end;

  Result := Format('UNKNOWN(%d)', [AFieldType]);
end;

function TFirebirdMetadataProvider.ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata;
var
  LConnection: TFDConnection;
  LQuery: TFDQuery;
  LTable: TTableMetadata;
  LForeignKey: TForeignKeyMetadata;
  LSequence: TSequenceMetadata;
begin
  Result := TDatabaseMetadata.Create;
  Result.Provider := dpFirebird;
  Result.DatabaseName := AOptions.Database;
  Result.DefaultSchema := '';

  try
    LConnection := CreateConnection(AOptions);
    try
      LConnection.Connected := True;

    LQuery := OpenQuery(LConnection,
      'select trim(rdb$relation_name) as table_name from rdb$relations ' +
      'where coalesce(rdb$system_flag, 0) = 0 and rdb$view_blr is null order by rdb$relation_name');
    try
      while not LQuery.Eof do
      begin
        if TSystemObjectFilter.IsUserObject(dpFirebird, '', GetString(LQuery, 'table_name')) then
        begin
          LTable := TTableMetadata.Create;
          LTable.Name := GetString(LQuery, 'table_name');
          Result.Tables.Add(LTable);
        end;
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(rf.rdb$relation_name) as table_name, trim(rf.rdb$field_name) as column_name, ' +
      'rf.rdb$field_position + 1 as ordinal_position, ' +
      'f.rdb$field_type as field_type, coalesce(f.rdb$field_sub_type, 0) as field_sub_type, ' +
      'case f.rdb$field_type when 7 then ''smallint'' when 8 then ''integer'' when 16 then ''bigint'' ' +
      'when 10 then ''float'' when 27 then ''double precision'' when 12 then ''date'' when 13 then ''time'' ' +
      'when 35 then ''timestamp'' when 14 then ''char'' when 37 then ''varchar'' when 261 then ''blob'' else ''unknown'' end as native_type, ' +
      'coalesce(f.rdb$character_length, 0) as column_length, coalesce(f.rdb$field_precision, 0) as column_precision, ' +
      'coalesce(f.rdb$field_scale, -1) * -1 as column_scale, case when rf.rdb$null_flag = 1 then 0 else 1 end as is_nullable, ' +
      '0 as is_unicode, rf.rdb$default_source as default_value, 0 as is_identity, f.rdb$computed_source as computed_expression ' +
      'from rdb$relation_fields rf join rdb$fields f on f.rdb$field_name = rf.rdb$field_source ' +
      'join rdb$relations r on r.rdb$relation_name = rf.rdb$relation_name ' +
      'where coalesce(r.rdb$system_flag, 0) = 0 order by rf.rdb$relation_name, rf.rdb$field_position');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, '', GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
          AddColumnFromQuery(LTable, LQuery, dpFirebird);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(rc.rdb$constraint_name) as constraint_name, trim(rc.rdb$relation_name) as table_name, trim(s.rdb$field_name) as column_name ' +
      'from rdb$relation_constraints rc join rdb$index_segments s on s.rdb$index_name = rc.rdb$index_name ' +
      'where rc.rdb$constraint_type = ''PRIMARY KEY'' order by rc.rdb$relation_name, s.rdb$field_position');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, '', GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
          AddPrimaryKeyColumn(LTable, GetString(LQuery, 'constraint_name'), GetString(LQuery, 'column_name'));
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(fk.rdb$constraint_name) as constraint_name, trim(fk.rdb$relation_name) as table_name, ' +
      'trim(fks.rdb$field_name) as column_name, trim(pk.rdb$relation_name) as target_table, trim(pks.rdb$field_name) as target_column ' +
      'from rdb$relation_constraints fk join rdb$ref_constraints rc on rc.rdb$constraint_name = fk.rdb$constraint_name ' +
      'join rdb$relation_constraints pk on pk.rdb$constraint_name = rc.rdb$const_name_uq ' +
      'join rdb$index_segments fks on fks.rdb$index_name = fk.rdb$index_name ' +
      'join rdb$index_segments pks on pks.rdb$index_name = pk.rdb$index_name and pks.rdb$field_position = fks.rdb$field_position ' +
      'where fk.rdb$constraint_type = ''FOREIGN KEY'' order by fk.rdb$constraint_name, fks.rdb$field_position');
    try
      while not LQuery.Eof do
      begin
        LTable := FindTable(Result, '', GetString(LQuery, 'table_name'));
        if Assigned(LTable) then
        begin
          LForeignKey := TForeignKeyMetadata.Create;
          LForeignKey.Name := GetString(LQuery, 'constraint_name');
          LForeignKey.SourceTable := GetString(LQuery, 'table_name');
          LForeignKey.SourceColumns := [GetString(LQuery, 'column_name')];
          LForeignKey.TargetTable := GetString(LQuery, 'target_table');
          LForeignKey.TargetColumns := [GetString(LQuery, 'target_column')];
          LTable.ForeignKeys.Add(LForeignKey);
        end;
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(rdb$generator_name) as sequence_name from rdb$generators ' +
      'where coalesce(rdb$system_flag, 0) = 0 order by rdb$generator_name');
    try
      while not LQuery.Eof do
      begin
        LSequence := TSequenceMetadata.Create;
        LSequence.Name := GetString(LQuery, 'sequence_name');
        LSequence.Sql := Format('CREATE SEQUENCE %s START WITH 0;', [LSequence.Name]);
        Result.Sequences.Add(LSequence);
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(rdb$relation_name) as object_name, rdb$view_source as sql_text from rdb$relations ' +
      'where coalesce(rdb$system_flag, 0) = 0 and rdb$view_blr is not null order by rdb$relation_name');
    try
      while not LQuery.Eof do
      begin
        Result.Views.Add(TViewMetadata.Create);
        Result.Views.Last.Name := GetString(LQuery, 'object_name');
        Result.Views.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(rdb$procedure_name) as object_name, rdb$procedure_source as sql_text from rdb$procedures ' +
      'where coalesce(rdb$system_flag, 0) = 0 order by rdb$procedure_name');
    try
      while not LQuery.Eof do
      begin
        Result.Procedures.Add(TProcedureMetadata.Create);
        Result.Procedures.Last.Name := GetString(LQuery, 'object_name');
        Result.Procedures.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;

    LQuery := OpenQuery(LConnection,
      'select trim(rdb$trigger_name) as object_name, rdb$trigger_source as sql_text from rdb$triggers ' +
      'where coalesce(rdb$system_flag, 0) = 0 order by rdb$trigger_name');
    try
      while not LQuery.Eof do
      begin
        Result.Triggers.Add(TTriggerMetadata.Create);
        Result.Triggers.Last.Name := GetString(LQuery, 'object_name');
        Result.Triggers.Last.Sql := GetString(LQuery, 'sql_text');
        LQuery.Next;
      end;
    finally
      LQuery.Free;
    end;
    finally
      LConnection.Free;
    end;
  except
    Result.Free;
    raise;
  end;
end;

constructor TDatabaseMetadataProviderFactory.Create;
begin
  inherited Create;
  FConnectionFactory := TFireDacConnectionFactory.Create;
end;

destructor TDatabaseMetadataProviderFactory.Destroy;
begin
  FConnectionFactory.Free;
  inherited;
end;

function TDatabaseMetadataProviderFactory.CreateProvider(AProvider: TDatabaseProvider): IDatabaseMetadataProvider;
begin
  case AProvider of
    dpPostgreSql:
      Result := TPostgreSqlMetadataProvider.Create(FConnectionFactory);
    dpSqlServer:
      Result := TSqlServerMetadataProvider.Create(FConnectionFactory);
    dpFirebird:
      Result := TFirebirdMetadataProvider.Create(FConnectionFactory);
  else
    raise EArgumentException.Create('Provider not supported.');
  end;
end;

end.
