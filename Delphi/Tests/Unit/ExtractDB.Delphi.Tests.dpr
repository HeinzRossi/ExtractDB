program ExtractDB.Delphi.Tests;

{$APPTYPE CONSOLE}

uses
  System.SysUtils,
  System.Classes,
  System.IOUtils,
  Data.DB,
  FireDAC.Comp.Client,
  ExtractDB.Core.Types in '..\..\Source\Core\Types\ExtractDB.Core.Types.pas',
  ExtractDB.Core.Model in '..\..\Source\Core\Model\ExtractDB.Core.Model.pas',
  ExtractDB.Core.Contracts in '..\..\Source\Core\Contracts\ExtractDB.Core.Contracts.pas',
  ExtractDB.Generators.Naming in '..\..\Source\Generators\Naming\ExtractDB.Generators.Naming.pas',
  ExtractDB.Generators.Mapping in '..\..\Source\Generators\Mapping\ExtractDB.Generators.Mapping.pas',
  ExtractDB.Generators.Attributes.MetadataAttributes in '..\..\Source\Generators\Attributes\ExtractDB.Generators.Attributes.MetadataAttributes.pas',
  ExtractDB.Generators.Attributes in '..\..\Source\Generators\Attributes\ExtractDB.Generators.Attributes.pas',
  ExtractDB.Generators.Code in '..\..\Source\Generators\Code\ExtractDB.Generators.Code.pas',
  ExtractDB.Application.Connection in '..\..\Source\Application\Services\ExtractDB.Application.Connection.pas',
  ExtractDB.Application.OperationMessages in '..\..\Source\Application\Services\ExtractDB.Application.OperationMessages.pas',
  ExtractDB.Providers.Contracts in '..\..\Source\Providers\ExtractDB.Providers.Contracts.pas',
  ExtractDB.Providers.SystemFilter in '..\..\Source\Providers\ExtractDB.Providers.SystemFilter.pas',
  ExtractDB.Providers.DataExport in '..\..\Source\Providers\ExtractDB.Providers.DataExport.pas',
  ExtractDB.Providers.MetadataProviders in '..\..\Source\Providers\ExtractDB.Providers.MetadataProviders.pas';

type
  TBooleanProbeProvider = class(TDatabaseMetadataProviderBase)
  public
    function ProviderName: string; override;
    function ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata; override;
    function ReadBoolean(AQuery: TDataSet; const AFieldName: string): Boolean;
    procedure ReadColumn(ATable: TTableMetadata; AQuery: TDataSet; AProvider: TDatabaseProvider);
  end;

procedure Fail(const AMessage: string);
begin
  raise Exception.Create(AMessage);
end;

procedure AssertTrue(ACondition: Boolean; const AMessage: string);
begin
  if not ACondition then
    Fail(AMessage);
end;

procedure AssertEquals(const AExpected, AActual, AMessage: string);
begin
  if AExpected <> AActual then
    Fail(Format('%s Expected "%s", actual "%s".', [AMessage, AExpected, AActual]));
end;

function Column(
  const AName: string;
  AType: TCommonDbType;
  const ANativeType: string;
  AOrdinal: Integer): TColumnMetadata;
begin
  Result := TColumnMetadata.Create;
  Result.Name := AName;
  Result.DbType := AType;
  Result.NativeType := ANativeType;
  Result.OrdinalPosition := AOrdinal;
end;

function TBooleanProbeProvider.ProviderName: string;
begin
  Result := 'Probe';
end;

function TBooleanProbeProvider.ReadBoolean(AQuery: TDataSet; const AFieldName: string): Boolean;
begin
  Result := GetBoolean(AQuery, AFieldName);
end;

procedure TBooleanProbeProvider.ReadColumn(
  ATable: TTableMetadata;
  AQuery: TDataSet;
  AProvider: TDatabaseProvider);
begin
  AddColumnFromQuery(ATable, AQuery, AProvider);
end;

function TBooleanProbeProvider.ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata;
begin
  Result := nil;
end;

procedure TestFireDacBooleanConversion;
var
  LFactory: TFireDacConnectionFactory;
  LProvider: TBooleanProbeProvider;
  LTable: TFDMemTable;
begin
  LFactory := TFireDacConnectionFactory.Create;
  LProvider := TBooleanProbeProvider.Create(LFactory);
  LTable := TFDMemTable.Create(nil);
  try
    LTable.FieldDefs.Add('int_true', ftInteger);
    LTable.FieldDefs.Add('int_false', ftInteger);
    LTable.FieldDefs.Add('text_yes', ftString, 10);
    LTable.FieldDefs.Add('text_no', ftString, 10);
    LTable.FieldDefs.Add('text_unknown', ftString, 10);
    LTable.CreateDataSet;
    LTable.Append;
    LTable.FieldByName('int_true').AsInteger := 1;
    LTable.FieldByName('int_false').AsInteger := 0;
    LTable.FieldByName('text_yes').AsString := 'YES';
    LTable.FieldByName('text_no').AsString := 'NO';
    LTable.FieldByName('text_unknown').AsString := 'maybe';
    LTable.Post;

    AssertTrue(LProvider.ReadBoolean(LTable, 'int_true'), 'Integer 1 should convert to true.');
    AssertTrue(not LProvider.ReadBoolean(LTable, 'int_false'), 'Integer 0 should convert to false.');
    AssertTrue(LProvider.ReadBoolean(LTable, 'text_yes'), 'YES should convert to true.');
    AssertTrue(not LProvider.ReadBoolean(LTable, 'text_no'), 'NO should convert to false.');
    AssertTrue(not LProvider.ReadBoolean(LTable, 'text_unknown'), 'Unexpected values should convert to false.');
  finally
    LTable.Free;
    LProvider.Free;
    LFactory.Free;
  end;
end;

procedure TestColumnAliases;
var
  LFactory: TFireDacConnectionFactory;
  LProvider: TBooleanProbeProvider;
  LTable: TTableMetadata;
  LDataSet: TFDMemTable;
  LColumn: TColumnMetadata;
begin
  LFactory := TFireDacConnectionFactory.Create;
  LProvider := TBooleanProbeProvider.Create(LFactory);
  LTable := TTableMetadata.Create;
  LDataSet := TFDMemTable.Create(nil);
  try
    LDataSet.FieldDefs.Add('column_name', ftString, 30);
    LDataSet.FieldDefs.Add('ordinal_position', ftInteger);
    LDataSet.FieldDefs.Add('native_type', ftString, 30);
    LDataSet.FieldDefs.Add('column_length', ftInteger);
    LDataSet.FieldDefs.Add('column_precision', ftInteger);
    LDataSet.FieldDefs.Add('column_scale', ftInteger);
    LDataSet.FieldDefs.Add('is_nullable', ftInteger);
    LDataSet.FieldDefs.Add('is_unicode', ftInteger);
    LDataSet.FieldDefs.Add('default_value', ftString, 100);
    LDataSet.FieldDefs.Add('is_identity', ftInteger);
    LDataSet.FieldDefs.Add('computed_expression', ftString, 100);
    LDataSet.CreateDataSet;
    LDataSet.Append;
    LDataSet.FieldByName('column_name').AsString := 'VALOR';
    LDataSet.FieldByName('ordinal_position').AsInteger := 2;
    LDataSet.FieldByName('native_type').AsString := 'numeric';
    LDataSet.FieldByName('column_length').AsInteger := 12;
    LDataSet.FieldByName('column_precision').AsInteger := 18;
    LDataSet.FieldByName('column_scale').AsInteger := 2;
    LDataSet.FieldByName('is_nullable').AsInteger := 1;
    LDataSet.FieldByName('is_unicode').AsInteger := 0;
    LDataSet.FieldByName('default_value').AsString := '0';
    LDataSet.FieldByName('is_identity').AsInteger := 0;
    LDataSet.FieldByName('computed_expression').AsString := '';
    LDataSet.Post;

    LProvider.ReadColumn(LTable, LDataSet, dpFirebird);
    AssertEquals('1', LTable.Columns.Count.ToString, 'Column should be added from neutral aliases.');
    LColumn := LTable.Columns[0];
    AssertEquals('VALOR', LColumn.Name, 'Column name should be copied.');
    AssertEquals('12', LColumn.Length.ToString, 'Column length should use column_length alias.');
    AssertEquals('18', LColumn.Precision.ToString, 'Column precision should use column_precision alias.');
    AssertEquals('2', LColumn.Scale.ToString, 'Column scale should use column_scale alias.');
    AssertTrue(LColumn.HasLength, 'Column length flag should be set.');
    AssertTrue(LColumn.HasPrecision, 'Column precision flag should be set.');
    AssertTrue(LColumn.HasScale, 'Column scale flag should be set.');
  finally
    LDataSet.Free;
    LTable.Free;
    LProvider.Free;
    LFactory.Free;
  end;
end;

procedure TestFirebirdNativeTypes;
var
  LFactory: TFireDacConnectionFactory;
  LProvider: TBooleanProbeProvider;
  LTable: TTableMetadata;
  LDataSet: TFDMemTable;
  LColumn: TColumnMetadata;
begin
  AssertEquals('NUMERIC', TFirebirdMetadataProvider.GetNativeType(7, 1), 'Firebird subtype 1 should be NUMERIC.');
  AssertEquals('DECIMAL', TFirebirdMetadataProvider.GetNativeType(8, 2), 'Firebird subtype 2 should be DECIMAL.');
  AssertEquals('BIGINT', TFirebirdMetadataProvider.GetNativeType(16, 0), 'Firebird int64 without exact subtype should stay BIGINT.');
  AssertEquals('VARCHAR', TFirebirdMetadataProvider.GetNativeType(37, 0), 'Firebird varchar should stay physical VARCHAR.');
  AssertEquals('BLOB SUB_TYPE TEXT', TFirebirdMetadataProvider.GetNativeType(261, 1), 'Firebird text blob should preserve subtype.');
  AssertEquals('BLOB', TFirebirdMetadataProvider.GetNativeType(261, 0), 'Firebird binary blob should preserve physical BLOB.');

  LFactory := TFireDacConnectionFactory.Create;
  LProvider := TBooleanProbeProvider.Create(LFactory);
  LTable := TTableMetadata.Create;
  LDataSet := TFDMemTable.Create(nil);
  try
    LDataSet.FieldDefs.Add('column_name', ftString, 30);
    LDataSet.FieldDefs.Add('ordinal_position', ftInteger);
    LDataSet.FieldDefs.Add('field_type', ftInteger);
    LDataSet.FieldDefs.Add('field_sub_type', ftInteger);
    LDataSet.FieldDefs.Add('native_type', ftString, 30);
    LDataSet.FieldDefs.Add('column_length', ftInteger);
    LDataSet.FieldDefs.Add('column_precision', ftInteger);
    LDataSet.FieldDefs.Add('column_scale', ftInteger);
    LDataSet.FieldDefs.Add('is_nullable', ftInteger);
    LDataSet.FieldDefs.Add('is_unicode', ftInteger);
    LDataSet.FieldDefs.Add('default_value', ftString, 100);
    LDataSet.FieldDefs.Add('is_identity', ftInteger);
    LDataSet.FieldDefs.Add('computed_expression', ftString, 100);
    LDataSet.CreateDataSet;
    LDataSet.Append;
    LDataSet.FieldByName('column_name').AsString := 'VALOR';
    LDataSet.FieldByName('ordinal_position').AsInteger := 1;
    LDataSet.FieldByName('field_type').AsInteger := 16;
    LDataSet.FieldByName('field_sub_type').AsInteger := 1;
    LDataSet.FieldByName('native_type').AsString := 'bigint';
    LDataSet.FieldByName('column_length').AsInteger := 0;
    LDataSet.FieldByName('column_precision').AsInteger := 18;
    LDataSet.FieldByName('column_scale').AsInteger := 2;
    LDataSet.FieldByName('is_nullable').AsInteger := 0;
    LDataSet.FieldByName('is_unicode').AsInteger := 0;
    LDataSet.FieldByName('default_value').AsString := '';
    LDataSet.FieldByName('is_identity').AsInteger := 0;
    LDataSet.FieldByName('computed_expression').AsString := '';
    LDataSet.Post;

    LProvider.ReadColumn(LTable, LDataSet, dpFirebird);
    LColumn := LTable.Columns[0];
    AssertEquals('NUMERIC', LColumn.NativeType, 'Firebird native type should come from field type and subtype.');
    AssertEquals(Ord(cdtDecimal).ToString, Ord(LColumn.DbType).ToString, 'Firebird NUMERIC should map to decimal common type.');
  finally
    LDataSet.Free;
    LTable.Free;
    LProvider.Free;
    LFactory.Free;
  end;
end;

procedure TestOperationMessages;
begin
  AssertEquals(
    'Aguarde... testando conexão com PostgreSQL.',
    TOperationMessages.ConnectionWaiting(dpPostgreSql),
    'Connection wait message should include provider.');
  AssertEquals(
    'Aguarde... lendo tabelas, views, procedures e triggers.',
    TOperationMessages.MetadataWaiting,
    'Metadata wait message.');
  AssertEquals(
    'Aguarde... gerando models e scripts Delphi.',
    TOperationMessages.GenerationWaiting,
    'Generation wait message.');
  AssertEquals(
    'Aguardando a operação encerrar com segurança.',
    TOperationMessages.CancelWaiting,
    'Cancel wait message.');
end;

procedure TestTypeMapper;
var
  LMapper: TDelphiTypeMapper;
  LColumn: TColumnMetadata;
  LMapping: TDelphiTypeMapping;
begin
  LMapper := TDelphiTypeMapper.Create;
  try
    LColumn := Column('VALOR', cdtDecimal, 'numeric', 1);
    try
      LColumn.HasPrecision := True;
      LColumn.Precision := 18;
      LColumn.HasScale := True;
      LColumn.Scale := 2;
      LMapping := LMapper.Map(LColumn);
      AssertEquals('Currency', LMapping.TypeName, 'Currency-compatible decimal should map to Currency.');

      LColumn.Precision := 30;
      LMapping := LMapper.Map(LColumn);
      AssertEquals('TBcd', LMapping.TypeName, 'Large decimal should map to TBcd.');
      AssertEquals('Data.FmtBcd', LMapping.UsesUnit, 'TBcd should require Data.FmtBcd.');
    finally
      LColumn.Free;
    end;

    LColumn := Column('DOCUMENTO', cdtBlob, 'blob', 1);
    try
      LMapping := LMapper.Map(LColumn);
      AssertEquals('TStream', LMapping.TypeName, 'Blob should map to TStream.');
      AssertTrue(LMapping.IsStream, 'Blob mapping should be marked as stream.');
    finally
      LColumn.Free;
    end;

    LColumn := Column('ESPECIAL', cdtUnknown, 'geography', 1);
    try
      LMapping := LMapper.Map(LColumn);
      AssertEquals('Variant', LMapping.TypeName, 'Unknown should map to Variant.');
      AssertTrue(LMapping.Warning <> '', 'Unknown mapping should emit warning text.');
    finally
      LColumn.Free;
    end;
  finally
    LMapper.Free;
  end;
end;

procedure TestConnectionDefaultsAndFactories;
var
  LOptions: TDatabaseConnectionOptions;
  LConnectionFactory: TFireDacConnectionFactory;
  LConnection: TFDConnection;
  LProviderFactory: TDatabaseMetadataProviderFactory;
  LProvider: IDatabaseMetadataProvider;
begin
  AssertEquals('5432', TDatabaseConnectionOptions.DefaultPort(dpPostgreSql).ToString, 'PostgreSQL default port.');
  AssertEquals('1433', TDatabaseConnectionOptions.DefaultPort(dpSqlServer).ToString, 'SQL Server default port.');
  AssertEquals('3050', TDatabaseConnectionOptions.DefaultPort(dpFirebird).ToString, 'Firebird default port.');
  AssertEquals('PG', TFireDacConnectionFactory.DriverId(dpPostgreSql), 'PostgreSQL FireDAC driver.');
  AssertEquals('MSSQL', TFireDacConnectionFactory.DriverId(dpSqlServer), 'SQL Server FireDAC driver.');
  AssertEquals('FB', TFireDacConnectionFactory.DriverId(dpFirebird), 'Firebird FireDAC driver.');

  LOptions := TDatabaseConnectionOptions.Create(dpSqlServer);
  LConnectionFactory := TFireDacConnectionFactory.Create;
  try
    LOptions.Host := 'localhost';
    LOptions.Database := 'demo';
    LOptions.UserName := 'user';
    LOptions.Password := 'secret';
    LConnection := LConnectionFactory.CreateConnection(LOptions);
    try
      AssertEquals('MSSQL', LConnection.Params.Values['DriverID'], 'Connection factory should set driver id.');
      AssertEquals('localhost', LConnection.Params.Values['Server'], 'Connection factory should set host.');
      AssertEquals('1433', LConnection.Params.Values['Port'], 'Connection factory should keep default port.');
    finally
      LConnection.Free;
    end;
  finally
    LConnectionFactory.Free;
    LOptions.Free;
  end;

  LProviderFactory := TDatabaseMetadataProviderFactory.Create;
  try
    LProvider := LProviderFactory.CreateProvider(dpPostgreSql);
    AssertEquals('PostgreSQL', LProvider.ProviderName, 'Provider factory should resolve PostgreSQL.');
    LProvider := LProviderFactory.CreateProvider(dpSqlServer);
    AssertEquals('SQL Server', LProvider.ProviderName, 'Provider factory should resolve SQL Server.');
    LProvider := LProviderFactory.CreateProvider(dpFirebird);
    AssertEquals('Firebird', LProvider.ProviderName, 'Provider factory should resolve Firebird.');
  finally
    LProviderFactory.Free;
  end;
end;

procedure TestSystemObjectFilter;
begin
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpPostgreSql, 'pg_catalog', 'pg_class'), 'PostgreSQL pg_catalog should be system.');
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpPostgreSql, 'information_schema', 'tables'), 'PostgreSQL information_schema should be system.');
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpSqlServer, 'sys', 'tables'), 'SQL Server sys schema should be system.');
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpSqlServer, 'INFORMATION_SCHEMA', 'TABLES'), 'SQL Server information schema should be system.');
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpFirebird, '', 'RDB$RELATIONS'), 'Firebird RDB$ object should be system.');
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpFirebird, '', 'MON$ATTACHMENTS'), 'Firebird MON$ object should be system.');
  AssertTrue(TSystemObjectFilter.IsSystemObject(dpFirebird, '', 'SEC$USERS'), 'Firebird SEC$ object should be system.');
  AssertTrue(TSystemObjectFilter.IsUserObject(dpPostgreSql, 'public', 'cliente'), 'User table should pass filter.');
end;

procedure TestOptionalIntegration(
  const AEnvironmentVariable: string;
  AProvider: TDatabaseProvider);
var
  LConnectionString: string;
  LOptions: TDatabaseConnectionOptions;
  LFactory: TDatabaseMetadataProviderFactory;
  LProvider: IDatabaseMetadataProvider;
begin
  LConnectionString := GetEnvironmentVariable(AEnvironmentVariable);
  if LConnectionString = '' then
  begin
    Writeln('Skipped ', AEnvironmentVariable, ' integration test; set variable to enable.');
    Exit;
  end;

  LOptions := TDatabaseConnectionOptions.Create(AProvider);
  LFactory := TDatabaseMetadataProviderFactory.Create;
  try
    LOptions.Database := LConnectionString;
    LProvider := LFactory.CreateProvider(AProvider);
    AssertTrue(LProvider.TestConnection(LOptions), AEnvironmentVariable + ' should connect.');
  finally
    LFactory.Free;
    LOptions.Free;
  end;
end;

function BuildMetadata: TDatabaseMetadata;
var
  LCliente: TTableMetadata;
  LPedido: TTableMetadata;
  LJoin: TTableMetadata;
  LColumn: TColumnMetadata;
  LDefault: TDefaultValueMetadata;
  LGeneration: TValueGenerationMetadata;
  LForeignKey: TForeignKeyMetadata;
  LView: TViewMetadata;
  LProcedure: TProcedureMetadata;
  LTrigger: TTriggerMetadata;
  LSequence: TSequenceMetadata;
  LEnum: TEnumMetadata;
begin
  Result := TDatabaseMetadata.Create;
  Result.Provider := dpPostgreSql;
  Result.DatabaseName := 'extractdb';
  Result.DefaultSchema := 'public';

  LCliente := TTableMetadata.Create;
  LCliente.Schema := 'public';
  LCliente.Name := 'CLIENTE';

  LColumn := Column('ID_CLIENTE', cdtInteger, 'integer', 1);
  LColumn.IsPrimaryKey := True;
  LColumn.IsNullable := False;
  LGeneration := TValueGenerationMetadata.Create;
  LGeneration.Strategy := vgsIdentity;
  LColumn.ValueGeneration := LGeneration;
  LCliente.Columns.Add(LColumn);

  LColumn := Column('CODIGO', cdtInteger, 'integer', 2);
  LColumn.IsNullable := False;
  LGeneration := TValueGenerationMetadata.Create;
  LGeneration.Strategy := vgsSequence;
  LGeneration.SequenceName := 'GEN_CLIENTE_CODIGO';
  LColumn.ValueGeneration := LGeneration;
  LCliente.Columns.Add(LColumn);

  LColumn := Column('NOME', cdtVarChar, 'varchar', 3);
  LColumn.IsNullable := False;
  LColumn.HasLength := True;
  LColumn.Length := 100;
  LDefault := TDefaultValueMetadata.Create;
  LDefault.RawExpression := '''SEM NOME''';
  LDefault.Kind := dvkLiteral;
  LColumn.DefaultValue := LDefault;
  LCliente.Columns.Add(LColumn);

  LColumn := Column('VALOR', cdtDecimal, 'numeric', 4);
  LColumn.IsNullable := False;
  LColumn.HasPrecision := True;
  LColumn.Precision := 18;
  LColumn.HasScale := True;
  LColumn.Scale := 2;
  LCliente.Columns.Add(LColumn);

  LColumn := Column('DOCUMENTO', cdtBlob, 'bytea', 5);
  LColumn.IsNullable := True;
  LCliente.Columns.Add(LColumn);

  Result.Tables.Add(LCliente);

  LPedido := TTableMetadata.Create;
  LPedido.Schema := 'public';
  LPedido.Name := 'PEDIDO';
  LPedido.Columns.Add(Column('ID_PEDIDO', cdtInteger, 'integer', 1));
  LPedido.Columns[0].IsPrimaryKey := True;
  LPedido.Columns[0].IsNullable := False;
  LGeneration := TValueGenerationMetadata.Create;
  LGeneration.Strategy := vgsTriggerSequence;
  LGeneration.SequenceName := 'GEN_PEDIDO_ID';
  LGeneration.TriggerName := 'TR_PEDIDO_BI';
  LPedido.Columns[0].ValueGeneration := LGeneration;
  LPedido.Columns.Add(Column('ID_CLIENTE', cdtInteger, 'integer', 2));
  LPedido.Columns[1].IsNullable := False;
  LForeignKey := TForeignKeyMetadata.Create;
  LForeignKey.Name := 'FK_PEDIDO_CLIENTE';
  LForeignKey.SourceTable := 'PEDIDO';
  LForeignKey.SourceColumns := ['ID_CLIENTE'];
  LForeignKey.TargetTable := 'CLIENTE';
  LForeignKey.TargetColumns := ['ID_CLIENTE'];
  LPedido.ForeignKeys.Add(LForeignKey);
  LForeignKey := TForeignKeyMetadata.Create;
  LForeignKey.Name := 'FK_PEDIDO_CLIENTE_COMPOSTA';
  LForeignKey.SourceTable := 'PEDIDO';
  LForeignKey.SourceColumns := ['ID_PEDIDO', 'ID_CLIENTE'];
  LForeignKey.TargetTable := 'CLIENTE';
  LForeignKey.TargetColumns := ['ID_CLIENTE', 'NOME'];
  LPedido.ForeignKeys.Add(LForeignKey);
  Result.Tables.Add(LPedido);

  LJoin := TTableMetadata.Create;
  LJoin.Schema := 'public';
  LJoin.Name := 'USUARIO_PERFIL';
  LJoin.Columns.Add(Column('ID_USUARIO', cdtInteger, 'integer', 1));
  LJoin.Columns[0].IsPrimaryKey := True;
  LJoin.Columns.Add(Column('ID_PERFIL', cdtInteger, 'integer', 2));
  LJoin.Columns[1].IsPrimaryKey := True;
  Result.Tables.Add(LJoin);

  LEnum := TEnumMetadata.Create;
  LEnum.Schema := 'public';
  LEnum.Name := 'STATUS_PEDIDO';
  LEnum.Values := ['ABERTO', 'PAGO', 'CANCELADO'];
  Result.Enums.Add(LEnum);

  LView := TViewMetadata.Create;
  LView.Schema := 'public';
  LView.Name := 'VW_CLIENTES';
  LView.Sql := 'select * from cliente';
  Result.Views.Add(LView);

  LProcedure := TProcedureMetadata.Create;
  LProcedure.Schema := 'pg_catalog';
  LProcedure.Name := 'PG_SLEEP';
  LProcedure.Sql := 'system procedure';
  Result.Procedures.Add(LProcedure);

  LTrigger := TTriggerMetadata.Create;
  LTrigger.Name := 'TR_CLIENTE';
  LTrigger.Sql := 'before insert trigger';
  Result.Triggers.Add(LTrigger);

  LSequence := TSequenceMetadata.Create;
  LSequence.Schema := 'public';
  LSequence.Name := 'GEN_CLIENTE_CODIGO';
  LSequence.Sql := 'CREATE SEQUENCE public.GEN_CLIENTE_CODIGO START WITH 0;';
  Result.Sequences.Add(LSequence);
end;

procedure TestDatabaseGenerator;
var
  LMetadata: TDatabaseMetadata;
  LContext: TGenerationContext;
  LGenerator: TDelphiDatabaseGenerator;
  LResult: TGenerationResult;
  LOutput: string;
  LClientePath: string;
  LPedidoPath: string;
  LJoinPath: string;
  LViewPath: string;
  LSystemProcedurePath: string;
  LTriggerPath: string;
  LSequencePath: string;
  LBytes: TBytes;
  LText: string;
begin
  LMetadata := BuildMetadata;
  LContext := TGenerationContext.Create;
  LGenerator := TDelphiDatabaseGenerator.Create;
  LResult := nil;
  try
    LOutput := TPath.Combine(TPath.GetTempPath, 'ExtractDB.Delphi.Tests');
    if TDirectory.Exists(LOutput) then
      TDirectory.Delete(LOutput, True);

    LContext.OutputDirectory := LOutput;
    LContext.SelectedTables.Add('CLIENTE');
    LContext.SelectedTables.Add('PEDIDO');
    LContext.SelectedTables.Add('USUARIO_PERFIL');

    LResult := LGenerator.Generate(LMetadata, LContext);
    AssertEquals('7', LResult.SuccessCount.ToString, 'Generator should write three models, one enum, one view, one trigger and one sequence.');
    AssertEquals('0', LResult.ErrorCount.ToString, 'Generator should not report errors.');

    LClientePath := TPath.Combine(TPath.Combine(LOutput, 'Models'), 'Cliente.pas');
    LPedidoPath := TPath.Combine(TPath.Combine(LOutput, 'Models'), 'Pedido.pas');
    LJoinPath := TPath.Combine(TPath.Combine(LOutput, 'Models'), 'UsuarioPerfil.pas');
    LViewPath := TPath.Combine(TPath.Combine(TPath.Combine(TPath.Combine(LOutput, 'Scripts'), 'Views'), 'public'), 'VW_CLIENTES.sql');
    LSystemProcedurePath := TPath.Combine(TPath.Combine(TPath.Combine(TPath.Combine(LOutput, 'Scripts'), 'Procedures'), 'pg_catalog'), 'PG_SLEEP.sql');
    LTriggerPath := TPath.Combine(TPath.Combine(TPath.Combine(LOutput, 'Scripts'), 'Triggers'), 'TR_CLIENTE.sql');
    LSequencePath := TPath.Combine(TPath.Combine(TPath.Combine(TPath.Combine(LOutput, 'Scripts'), 'Sequences'), 'public'), 'GEN_CLIENTE_CODIGO.sql');

    AssertTrue(TFile.Exists(LClientePath), 'Cliente model should be generated.');
    AssertTrue(TFile.Exists(LPedidoPath), 'Pedido model should be generated.');
    AssertTrue(TFile.Exists(LJoinPath), 'Join table should be generated as a normal model.');
    AssertTrue(TFile.Exists(LViewPath), 'Schema view script should be generated under schema folder.');
    AssertTrue(not TFile.Exists(LSystemProcedurePath), 'System procedure should not be generated.');
    AssertTrue(TFile.Exists(LTriggerPath), 'Trigger without schema should not get artificial schema folder.');
    AssertTrue(TFile.Exists(LSequencePath), 'Sequence script should be generated under schema folder.');
    AssertEquals('CREATE SEQUENCE public.GEN_CLIENTE_CODIGO START WITH 0;', TFile.ReadAllText(LSequencePath), 'Sequence SQL should be preserved.');

    LText := TFile.ReadAllText(LClientePath);
    AssertTrue(LText.Contains('SimpleAttributes'), 'Model should use local SimpleORM attributes.');
    AssertTrue(LText.Contains('ExtractDB.Generators.Attributes.MetadataAttributes'), 'Model with defaults should use ExtractDB metadata attributes.');
    AssertTrue(LText.Contains('[Tabela(''CLIENTE'')]'), 'Model should contain table attribute.');
    AssertTrue(LText.Contains('Campo(''ID_CLIENTE'', ''integer'')'), 'Integer should use native database type without size.');
    AssertTrue(LText.Contains('Sequence(''GEN_CLIENTE_CODIGO'')'), 'Direct sequence should use the native SimpleORM attribute.');
    AssertTrue(LText.Contains('Campo(''NOME'', ''varchar'', 100)'), 'Varchar native type and length should be emitted in Campo.');
    AssertTrue(LText.Contains('Campo(''VALOR'', ''numeric'', 18.2)'), 'Numeric precision and scale should be emitted as Campo size.');
    AssertTrue(LText.Contains('DatabaseDefault') and LText.Contains('SEM NOME'), 'Default SQL expression should be preserved.');
    AssertTrue(LText.Contains('Campo(''DOCUMENTO'', ''bytea'')'), 'Blob should use native database type.');
    AssertTrue(LText.Contains('property Documento: TStream'), 'Blob should be generated as TStream.');
    AssertTrue(LText.Contains('TMemoryStream(FDocumento).LoadFromStream(Value)'), 'Stream setter should copy stream contents.');

    LText := TFile.ReadAllText(LPedidoPath);
    AssertTrue(LText.Contains('Sequence(''GEN_PEDIDO_ID'')'), 'Sequence should use the native SimpleORM attribute.');
    AssertTrue(LText.Contains('GeneratedByTrigger(''TR_PEDIDO_BI'')'), 'Trigger sequence should preserve trigger metadata.');
    AssertTrue(LText.Contains('FK') and LText.Contains('ForeignKey(''ID_CLIENTE'', ''FK_PEDIDO_CLIENTE'', ''CLIENTE'', ''ID_CLIENTE'')'), 'Simple FK column should generate SimpleORM ForeignKey metadata.');
    AssertTrue(LText.Contains('BelongsTo(''TCliente'')'), 'Simple FK should generate local SimpleORM BelongsTo navigation.');
    AssertTrue(LText.Contains('CompositeForeignKey(''FK_PEDIDO_CLIENTE_COMPOSTA'', ''ID_PEDIDO;ID_CLIENTE'', ''CLIENTE'', ''ID_CLIENTE;NOME'')'), 'Composite FK should use ExtractDB metadata attribute.');

    LBytes := TFile.ReadAllBytes(LClientePath);
    AssertTrue(not ((Length(LBytes) >= 3) and (LBytes[0] = $EF) and (LBytes[1] = $BB) and (LBytes[2] = $BF)), 'Writer should use UTF-8 without BOM.');
    AssertTrue(TFile.ReadAllText(LClientePath).Contains(#13#10), 'Writer should use CRLF.');

    TFile.WriteAllText(LViewPath, 'old');
    LResult.Free;
    LResult := LGenerator.Generate(LMetadata, LContext);
    AssertEquals('select * from cliente', TFile.ReadAllText(LViewPath), 'Writer should overwrite SQL scripts.');
  finally
    LResult.Free;
    LGenerator.Free;
    LContext.Free;
    LMetadata.Free;
  end;
end;

procedure TestSequenceSelection;
var
  LMetadata: TDatabaseMetadata;
  LContext: TGenerationContext;
  LGenerator: TDelphiDatabaseGenerator;
  LResult: TGenerationResult;
  LOutput: string;
  LSequence: TSequenceMetadata;
begin
  LMetadata := TDatabaseMetadata.Create;
  LContext := TGenerationContext.Create;
  LGenerator := TDelphiDatabaseGenerator.Create;
  LResult := nil;
  try
    LMetadata.Provider := dpPostgreSql;
    LMetadata.DatabaseName := 'extractdb';

    LSequence := TSequenceMetadata.Create;
    LSequence.Schema := 'public';
    LSequence.Name := 'SEQ_EXPORTADA';
    LSequence.Sql := 'CREATE SEQUENCE public.SEQ_EXPORTADA START WITH 0;';
    LMetadata.Sequences.Add(LSequence);

    LSequence := TSequenceMetadata.Create;
    LSequence.Schema := 'public';
    LSequence.Name := 'SEQ_IGNORADA';
    LSequence.Sql := 'CREATE SEQUENCE public.SEQ_IGNORADA START WITH 0;';
    LMetadata.Sequences.Add(LSequence);

    LOutput := TPath.Combine(TPath.GetTempPath, 'ExtractDB.Delphi.SequenceSelection.Tests');
    if TDirectory.Exists(LOutput) then
      TDirectory.Delete(LOutput, True);

    LContext.OutputDirectory := LOutput;
    LContext.SelectedSequences.Add('SEQ_EXPORTADA');

    LResult := LGenerator.Generate(LMetadata, LContext);

    AssertEquals('1', LResult.SuccessCount.ToString, 'Generator should write only selected sequence.');
    AssertTrue(
      TFile.Exists(TPath.Combine(TPath.Combine(TPath.Combine(TPath.Combine(LOutput, 'Scripts'), 'Sequences'), 'public'), 'SEQ_EXPORTADA.sql')),
      'Selected sequence should be exported.');
    AssertTrue(
      not TFile.Exists(TPath.Combine(TPath.Combine(TPath.Combine(TPath.Combine(LOutput, 'Scripts'), 'Sequences'), 'public'), 'SEQ_IGNORADA.sql')),
      'Unselected sequence should not be exported.');
  finally
    LResult.Free;
    LGenerator.Free;
    LContext.Free;
    LMetadata.Free;
  end;
end;

procedure TestDataExportConfigurationJson;
var
  LPath: string;
  LConfiguration: TDataExportConfiguration;
begin
  LPath := TPath.Combine(TPath.GetTempPath, 'extractdb-data-export-config-test.json');
  TFile.WriteAllText(
    LPath,
    '{' +
    '"maxRowsPerTable":10000,' +
    '"tables":[' +
    '{"schema":"public","name":["cliente_tipo","cidade"]},' +
    '{"name":["PAIS"]}' +
    ']' +
    '}',
    TEncoding.UTF8);

  LConfiguration := TDataExportConfiguration.LoadFromFile(LPath);
  try
    AssertEquals('10000', LConfiguration.MaxRowsPerTable.ToString, 'Data export limit should be read.');
    AssertEquals('2', LConfiguration.Tables.Count.ToString, 'Data export table groups should be read.');
    AssertEquals('public', LConfiguration.Tables[0].Schema, 'Data export schema should be read.');
    AssertEquals('cliente_tipo', LConfiguration.Tables[0].Names[0], 'First grouped table should be read.');
    AssertEquals('cidade', LConfiguration.Tables[0].Names[1], 'Second grouped table should be read.');
    AssertEquals('', LConfiguration.Tables[1].Schema, 'Schema should be optional.');
    AssertEquals('PAIS', LConfiguration.Tables[1].Names[0], 'Table without schema should be read.');
  finally
    LConfiguration.Free;
    TFile.Delete(LPath);
  end;
end;

begin
  try
    TestFireDacBooleanConversion;
    TestColumnAliases;
    TestFirebirdNativeTypes;
    TestOperationMessages;
    TestConnectionDefaultsAndFactories;
    TestSystemObjectFilter;
    TestTypeMapper;
    TestDatabaseGenerator;
    TestSequenceSelection;
    TestDataExportConfigurationJson;
    TestOptionalIntegration('EXTRACTDB_POSTGRESQL_TEST_CONNECTION', dpPostgreSql);
    TestOptionalIntegration('EXTRACTDB_SQLSERVER_TEST_CONNECTION', dpSqlServer);
    TestOptionalIntegration('EXTRACTDB_FIREBIRD_TEST_CONNECTION', dpFirebird);
    Writeln('ExtractDB Delphi tests passed.');
  except
    on E: Exception do
    begin
      Writeln(E.ClassName + ': ' + E.Message);
      Halt(1);
    end;
  end;
end.
