unit ExtractDB.Providers.DataExport;

interface

uses
  System.Classes,
  System.Generics.Collections,
  Data.DB,
  ExtractDB.Application.Connection,
  ExtractDB.Core.Contracts,
  ExtractDB.Core.Model,
  ExtractDB.Core.Types;

type
  TDataExportTableGroup = class
  private
    FSchema: string;
    FNames: TStringList;
  public
    constructor Create;
    destructor Destroy; override;

    property Schema: string read FSchema write FSchema;
    property Names: TStringList read FNames;
  end;

  TDataExportConfiguration = class
  private
    FMaxRowsPerTable: Integer;
    FTables: TObjectList<TDataExportTableGroup>;
  public
    constructor Create;
    destructor Destroy; override;

    class function LoadFromFile(const APath: string): TDataExportConfiguration; static;

    property MaxRowsPerTable: Integer read FMaxRowsPerTable write FMaxRowsPerTable;
    property Tables: TObjectList<TDataExportTableGroup> read FTables;
  end;

  TDatabaseDataExportProvider = class
  private
    FConnectionFactory: TFireDacConnectionFactory;

    function FindTable(ADatabase: TDatabaseMetadata; const ASchema: string; const AName: string): TTableMetadata;
    function IsConfiguredTable(const ASchema: string; const AName: string; AConfiguration: TDataExportConfiguration): Boolean;
    function IsExportableColumn(AColumn: TColumnMetadata): Boolean;
    function QuoteIdentifier(const AIdentifier: string; AProvider: TDatabaseProvider): string;
    function QualifyTable(ATable: TTableMetadata; AProvider: TDatabaseProvider): string;
    function BuildSelectSql(ATable: TTableMetadata; AColumns: TList<TColumnMetadata>; AProvider: TDatabaseProvider; AMaxRows: Integer): string;
    function BuildInsertScript(ATable: TTableMetadata; AColumns: TList<TColumnMetadata>; AQuery: TDataSet; AProvider: TDatabaseProvider): string;
    function FormatField(AField: TField; AProvider: TDatabaseProvider): string;
    function BytesToSql(ABlob: TStream; AProvider: TDatabaseProvider): string;
    function NormalizeLineEndings(const AContent: string): string;
    function GetDataScriptPath(const AOutputDirectory: string; const ASchema: string; const ATableName: string): string;
    procedure WriteFile(const APath: string; const AContent: string);
    procedure AddExportableColumns(ATable: TTableMetadata; AColumns: TList<TColumnMetadata>);
  public
    constructor Create;
    destructor Destroy; override;

    procedure ExportData(
      AOptions: TDatabaseConnectionOptions;
      ADatabase: TDatabaseMetadata;
      const AOutputDirectory: string;
      AConfiguration: TDataExportConfiguration;
      AResult: TGenerationResult);
  end;

implementation

uses
  System.IOUtils,
  System.JSON,
  System.SysUtils,
  FireDAC.Comp.Client;

const
  DefaultMaxRowsPerTable = 10000;

constructor TDataExportTableGroup.Create;
begin
  inherited Create;
  FNames := TStringList.Create;
  FNames.CaseSensitive := False;
  FNames.Duplicates := dupIgnore;
end;

destructor TDataExportTableGroup.Destroy;
begin
  FNames.Free;
  inherited;
end;

constructor TDataExportConfiguration.Create;
begin
  inherited Create;
  FMaxRowsPerTable := DefaultMaxRowsPerTable;
  FTables := TObjectList<TDataExportTableGroup>.Create(True);
end;

destructor TDataExportConfiguration.Destroy;
begin
  FTables.Free;
  inherited;
end;

class function TDataExportConfiguration.LoadFromFile(const APath: string): TDataExportConfiguration;
var
  LJson: TJSONObject;
  LTables: TJSONArray;
  LTableValue: TJSONValue;
  LTableObject: TJSONObject;
  LNames: TJSONArray;
  LNameValue: TJSONValue;
  LGroup: TDataExportTableGroup;
begin
  Result := TDataExportConfiguration.Create;
  LJson := TJSONObject.ParseJSONValue(TFile.ReadAllText(APath, TEncoding.UTF8)) as TJSONObject;
  if not Assigned(LJson) then
    raise Exception.Create('Arquivo JSON de dados inválido.');

  try
    if LJson.TryGetValue<Integer>('maxRowsPerTable', Result.FMaxRowsPerTable) and
       (Result.FMaxRowsPerTable <= 0) then
      Result.FMaxRowsPerTable := DefaultMaxRowsPerTable;

    if not LJson.TryGetValue<TJSONArray>('tables', LTables) then
      Exit;

    for LTableValue in LTables do
    begin
      LTableObject := LTableValue as TJSONObject;
      LGroup := TDataExportTableGroup.Create;
      if not LTableObject.TryGetValue<string>('schema', LGroup.FSchema) then
        LGroup.FSchema := '';

      if LTableObject.TryGetValue<TJSONArray>('name', LNames) then
        for LNameValue in LNames do
          if Trim(LNameValue.Value) <> '' then
            LGroup.Names.Add(Trim(LNameValue.Value));

      if LGroup.Names.Count > 0 then
        Result.Tables.Add(LGroup)
      else
        LGroup.Free;
    end;
  finally
    LJson.Free;
  end;
end;

constructor TDatabaseDataExportProvider.Create;
begin
  inherited Create;
  FConnectionFactory := TFireDacConnectionFactory.Create;
end;

destructor TDatabaseDataExportProvider.Destroy;
begin
  FConnectionFactory.Free;
  inherited;
end;

procedure TDatabaseDataExportProvider.AddExportableColumns(
  ATable: TTableMetadata;
  AColumns: TList<TColumnMetadata>);
var
  LColumn: TColumnMetadata;
begin
  for LColumn in ATable.Columns do
    if IsExportableColumn(LColumn) then
      AColumns.Add(LColumn);
end;

function TDatabaseDataExportProvider.BuildInsertScript(
  ATable: TTableMetadata;
  AColumns: TList<TColumnMetadata>;
  AQuery: TDataSet;
  AProvider: TDatabaseProvider): string;
var
  LLines: TStringList;
  LColumnNames: TStringList;
  LValues: TStringList;
  LColumn: TColumnMetadata;
begin
  LLines := TStringList.Create;
  LColumnNames := TStringList.Create;
  LValues := TStringList.Create;
  try
    LLines.LineBreak := sLineBreak;
    for LColumn in AColumns do
      LColumnNames.Add(QuoteIdentifier(LColumn.Name, AProvider));

    AQuery.First;
    while not AQuery.Eof do
    begin
      LValues.Clear;
      for LColumn in AColumns do
        LValues.Add(FormatField(AQuery.FieldByName(LColumn.Name), AProvider));

      LLines.Add(Format(
        'INSERT INTO %s (%s) VALUES (%s);',
        [QualifyTable(ATable, AProvider), string.Join(', ', LColumnNames.ToStringArray), string.Join(', ', LValues.ToStringArray)]));
      AQuery.Next;
    end;

    Result := LLines.Text;
  finally
    LValues.Free;
    LColumnNames.Free;
    LLines.Free;
  end;
end;

function TDatabaseDataExportProvider.BuildSelectSql(
  ATable: TTableMetadata;
  AColumns: TList<TColumnMetadata>;
  AProvider: TDatabaseProvider;
  AMaxRows: Integer): string;
var
  LColumnNames: TStringList;
  LColumn: TColumnMetadata;
begin
  LColumnNames := TStringList.Create;
  try
    for LColumn in AColumns do
      LColumnNames.Add(QuoteIdentifier(LColumn.Name, AProvider));

    case AProvider of
      dpPostgreSql:
        Result := Format(
          'select %s from %s limit %d',
          [string.Join(', ', LColumnNames.ToStringArray), QualifyTable(ATable, AProvider), AMaxRows + 1]);
      dpSqlServer:
        Result := Format(
          'select top (%d) %s from %s',
          [AMaxRows + 1, string.Join(', ', LColumnNames.ToStringArray), QualifyTable(ATable, AProvider)]);
      dpFirebird:
        Result := Format(
          'select first %d %s from %s',
          [AMaxRows + 1, string.Join(', ', LColumnNames.ToStringArray), QualifyTable(ATable, AProvider)]);
    end;
  finally
    LColumnNames.Free;
  end;
end;

function TDatabaseDataExportProvider.BytesToSql(ABlob: TStream; AProvider: TDatabaseProvider): string;
var
  LBytes: TBytes;
  LIndex: Integer;
  LHex: string;
begin
  SetLength(LBytes, ABlob.Size);
  ABlob.Position := 0;
  if Length(LBytes) > 0 then
    ABlob.ReadBuffer(LBytes[0], Length(LBytes));

  LHex := '';
  for LIndex := 0 to High(LBytes) do
    LHex := LHex + IntToHex(LBytes[LIndex], 2);

  case AProvider of
    dpSqlServer:
      Result := '0x' + LHex;
    dpPostgreSql:
      Result := '''\x' + LowerCase(LHex) + '''';
  else
    Result := 'x''' + LHex + '''';
  end;
end;

procedure TDatabaseDataExportProvider.ExportData(
  AOptions: TDatabaseConnectionOptions;
  ADatabase: TDatabaseMetadata;
  const AOutputDirectory: string;
  AConfiguration: TDataExportConfiguration;
  AResult: TGenerationResult);
var
  LConnection: TFDConnection;
  LQuery: TFDQuery;
  LGroup: TDataExportTableGroup;
  LTableName: string;
  LTable: TTableMetadata;
  LColumns: TList<TColumnMetadata>;
  LScript: string;
begin
  LConnection := FConnectionFactory.CreateConnection(AOptions);
  try
    LConnection.Connected := True;

    for LGroup in AConfiguration.Tables do
      for LTableName in LGroup.Names do
      begin
        LTable := FindTable(ADatabase, LGroup.Schema, LTableName);
        if not Assigned(LTable) then
        begin
          AResult.AddMessage(gmsWarning, dotTable, LGroup.Schema, LTableName, 'DataExportConfig', 'Tabela configurada não foi encontrada na metadata.');
          Continue;
        end;

        if not IsConfiguredTable(LTable.Schema, LTable.Name, AConfiguration) then
          Continue;

        LColumns := TList<TColumnMetadata>.Create;
        LQuery := TFDQuery.Create(nil);
        try
          try
            AddExportableColumns(LTable, LColumns);
            if LColumns.Count = 0 then
            begin
              AResult.AddMessage(gmsWarning, dotTable, LTable.Schema, LTable.Name, 'DataExport', 'Tabela não possui colunas exportáveis.');
              Continue;
            end;

            LQuery.Connection := LConnection;
            LQuery.SQL.Text := BuildSelectSql(LTable, LColumns, AOptions.Provider, AConfiguration.MaxRowsPerTable);
            LQuery.Open;
            LQuery.FetchAll;

            if LQuery.RecordCount > AConfiguration.MaxRowsPerTable then
            begin
              AResult.AddMessage(
                gmsError,
                dotTable,
                LTable.Schema,
                LTable.Name,
                'DataExport',
                Format('Tabela possui mais de %d linhas e não foi exportada.', [AConfiguration.MaxRowsPerTable]));
              Continue;
            end;

            LScript := BuildInsertScript(LTable, LColumns, LQuery, AOptions.Provider);
            WriteFile(GetDataScriptPath(AOutputDirectory, LTable.Schema, LTable.Name), LScript);
            AResult.SuccessCount := AResult.SuccessCount + 1;
          except
            on E: Exception do
              AResult.AddMessage(gmsError, dotTable, LTable.Schema, LTable.Name, 'DataExport', E.Message);
          end;
        finally
          LQuery.Free;
          LColumns.Free;
        end;
      end;
  finally
    LConnection.Free;
  end;
end;

function TDatabaseDataExportProvider.FindTable(
  ADatabase: TDatabaseMetadata;
  const ASchema: string;
  const AName: string): TTableMetadata;
var
  LTable: TTableMetadata;
begin
  Result := nil;
  for LTable in ADatabase.Tables do
    if SameText(LTable.Name, AName) and
       ((ASchema = '') or SameText(LTable.Schema, ASchema)) then
      Exit(LTable);
end;

function TDatabaseDataExportProvider.FormatField(
  AField: TField;
  AProvider: TDatabaseProvider): string;
var
  LStream: TMemoryStream;
begin
  if AField.IsNull then
    Exit('NULL');

  case AField.DataType of
    ftBoolean:
      if AField.AsBoolean then
        if AProvider = dpSqlServer then
          Result := '1'
        else
          Result := 'TRUE'
      else if AProvider = dpSqlServer then
        Result := '0'
      else
        Result := 'FALSE';
    ftSmallint, ftInteger, ftWord, ftFloat, ftCurrency, ftBCD, ftLargeint, ftFMTBcd, ftLongWord, ftShortint, ftByte, ftExtended, ftSingle:
      Result := StringReplace(AField.AsString, ',', '.', [rfReplaceAll]);
    ftDate:
      Result := QuotedStr(FormatDateTime('yyyy-mm-dd', AField.AsDateTime));
    ftTime:
      Result := QuotedStr(FormatDateTime('hh:nn:ss.zzz', AField.AsDateTime));
    ftDateTime, ftTimeStamp:
      Result := QuotedStr(FormatDateTime('yyyy-mm-dd hh:nn:ss.zzz', AField.AsDateTime));
    ftBytes, ftVarBytes, ftBlob, ftOraBlob:
      begin
        LStream := TMemoryStream.Create;
        try
          TBlobField(AField).SaveToStream(LStream);
          Result := BytesToSql(LStream, AProvider);
        finally
          LStream.Free;
        end;
      end;
  else
    Result := QuotedStr(StringReplace(AField.AsString, '''', '''''', [rfReplaceAll]));
  end;
end;

function TDatabaseDataExportProvider.GetDataScriptPath(
  const AOutputDirectory: string;
  const ASchema: string;
  const ATableName: string): string;
var
  LBasePath: string;
begin
  LBasePath := TPath.Combine(TPath.Combine(AOutputDirectory, 'Scripts'), 'Data');
  if ASchema <> '' then
    LBasePath := TPath.Combine(LBasePath, ASchema);

  Result := TPath.Combine(LBasePath, ATableName + '.sql');
end;

function TDatabaseDataExportProvider.IsConfiguredTable(
  const ASchema: string;
  const AName: string;
  AConfiguration: TDataExportConfiguration): Boolean;
var
  LGroup: TDataExportTableGroup;
begin
  Result := False;
  for LGroup in AConfiguration.Tables do
    if ((LGroup.Schema = '') or SameText(LGroup.Schema, ASchema)) and
       (LGroup.Names.IndexOf(AName) >= 0) then
      Exit(True);
end;

function TDatabaseDataExportProvider.IsExportableColumn(AColumn: TColumnMetadata): Boolean;
begin
  Result := not AColumn.IsComputed;
  if Result and Assigned(AColumn.ValueGeneration) then
    Result := AColumn.ValueGeneration.Strategy = vgsNone;
end;

function TDatabaseDataExportProvider.NormalizeLineEndings(const AContent: string): string;
begin
  Result := AContent.Replace(#13#10, #10).Replace(#13, #10).Replace(#10, #13#10);
end;

function TDatabaseDataExportProvider.QualifyTable(
  ATable: TTableMetadata;
  AProvider: TDatabaseProvider): string;
begin
  if ATable.Schema = '' then
    Result := QuoteIdentifier(ATable.Name, AProvider)
  else
    Result := QuoteIdentifier(ATable.Schema, AProvider) + '.' + QuoteIdentifier(ATable.Name, AProvider);
end;

function TDatabaseDataExportProvider.QuoteIdentifier(
  const AIdentifier: string;
  AProvider: TDatabaseProvider): string;
begin
  if AProvider = dpSqlServer then
    Result := '[' + StringReplace(AIdentifier, ']', ']]', [rfReplaceAll]) + ']'
  else
    Result := '"' + StringReplace(AIdentifier, '"', '""', [rfReplaceAll]) + '"';
end;

procedure TDatabaseDataExportProvider.WriteFile(const APath: string; const AContent: string);
var
  LEncoding: TUTF8Encoding;
begin
  TDirectory.CreateDirectory(TPath.GetDirectoryName(APath));
  LEncoding := TUTF8Encoding.Create(False);
  try
    TFile.WriteAllText(APath, NormalizeLineEndings(AContent), LEncoding);
  finally
    LEncoding.Free;
  end;
end;

end.
