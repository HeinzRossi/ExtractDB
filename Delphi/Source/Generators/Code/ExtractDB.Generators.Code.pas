unit ExtractDB.Generators.Code;

interface

uses
  System.Classes,
  System.SysUtils,
  System.Generics.Collections,
  ExtractDB.Core.Contracts,
  ExtractDB.Core.Model,
  ExtractDB.Core.Types,
  ExtractDB.Generators.Attributes,
  ExtractDB.Generators.Mapping,
  ExtractDB.Generators.Naming;

type
  TDelphiEntityCodeGenerator = class
  private
    FNameConverter: TDelphiNameConverter;
    FTypeMapper: TDelphiTypeMapper;
    FAttributeWriter: TSimpleOrmAttributeWriter;

    function BuildUnitName(AContext: TGenerationContext; const AObjectName: string): string;
    function BuildClassName(const ATableName: string): string;
    function GetNavigationName(AForeignKey: TForeignKeyMetadata): string;
    function IsTargetTableSelected(AContext: TGenerationContext; AForeignKey: TForeignKeyMetadata): Boolean;
    function NeedsMetadataAttributes(ATable: TTableMetadata): Boolean;
    procedure AddUnique(AStrings: TStrings; const AValue: string);
    procedure WriteAttributes(ASource: TStrings; AAttributes: TStrings; const AIndent: string);
    procedure WriteColumn(
      ASource: TStrings;
      AColumn: TColumnMetadata;
      const APropertyName: string;
      const AFieldName: string;
      ATable: TTableMetadata);
    procedure WriteRelationship(
      ASource: TStrings;
      AForeignKey: TForeignKeyMetadata;
      const APropertyName: string;
      ATable: TTableMetadata);
  public
    constructor Create;
    destructor Destroy; override;

    function Generate(ATable: TTableMetadata; AContext: TGenerationContext; AResult: TGenerationResult): string;
  end;

  TDelphiDatabaseGenerator = class
  private
    FEntityGenerator: TDelphiEntityCodeGenerator;
    FNameConverter: TDelphiNameConverter;

    function GetModelPath(AContext: TGenerationContext; const ATableName: string): string;
    function GetEnumPath(AContext: TGenerationContext; const AEnumName: string): string;
    function GetScriptPath(
      AContext: TGenerationContext;
      const AFolderName: string;
      const ASchema: string;
      const AName: string): string;
    function GenerateEnum(AEnum: TEnumMetadata; AContext: TGenerationContext): string;
    procedure WriteFile(const APath: string; const AContent: string);
    function NormalizeLineEndings(const AContent: string): string;
    function IsSystemObject(const ASchema: string; const AName: string; AProvider: TDatabaseProvider): Boolean;
    procedure GenerateScripts<T: TSqlObjectMetadata>(
      AObjects: TObjectList<T>;
      AContext: TGenerationContext;
      const AFolderName: string;
      AObjectType: TDatabaseObjectType;
      AProvider: TDatabaseProvider;
      AResult: TGenerationResult);
  public
    constructor Create;
    destructor Destroy; override;

    function Generate(ADatabase: TDatabaseMetadata; AContext: TGenerationContext): TGenerationResult;
  end;

implementation

uses
  System.IOUtils,
  System.StrUtils;

constructor TDelphiEntityCodeGenerator.Create;
begin
  inherited Create;
  FNameConverter := TDelphiNameConverter.Create;
  FTypeMapper := TDelphiTypeMapper.Create;
  FAttributeWriter := TSimpleOrmAttributeWriter.Create;
end;

destructor TDelphiEntityCodeGenerator.Destroy;
begin
  FAttributeWriter.Free;
  FTypeMapper.Free;
  FNameConverter.Free;
  inherited;
end;

procedure TDelphiEntityCodeGenerator.AddUnique(AStrings: TStrings; const AValue: string);
begin
  if (AValue <> '') and (AStrings.IndexOf(AValue) < 0) then
    AStrings.Add(AValue);
end;

function TDelphiEntityCodeGenerator.BuildClassName(const ATableName: string): string;
begin
  Result := 'T' + FNameConverter.ToPascalCase(ATableName);
end;

function TDelphiEntityCodeGenerator.BuildUnitName(AContext: TGenerationContext; const AObjectName: string): string;
begin
  Result := FNameConverter.ToPascalCase(AObjectName);
  if AContext.UnitPrefix <> '' then
    Result := AContext.UnitPrefix + '.' + Result;
end;

function TDelphiEntityCodeGenerator.Generate(
  ATable: TTableMetadata;
  AContext: TGenerationContext;
  AResult: TGenerationResult): string;
var
  LSource: TStringList;
  LUses: TStringList;
  LFields: TStringList;
  LProperties: TStringList;
  LImplementation: TStringList;
  LColumnNames: TArray<string>;
  LPropertyNames: TArray<string>;
  LColumn: TColumnMetadata;
  LForeignKey: TForeignKeyMetadata;
  LMapping: TDelphiTypeMapping;
  LIndex: Integer;
  LNeedsStreams: Boolean;
  LUnitName: string;
  LRelationshipNames: TArray<string>;
  LRelationshipBaseNames: TArray<string>;
begin
  LSource := TStringList.Create;
  LUses := TStringList.Create;
  LFields := TStringList.Create;
  LProperties := TStringList.Create;
  LImplementation := TStringList.Create;
  try
    LSource.LineBreak := sLineBreak;
    LUses.Sorted := True;
    LUses.Duplicates := dupIgnore;
    LFields.LineBreak := sLineBreak;
    LProperties.LineBreak := sLineBreak;
    LImplementation.LineBreak := sLineBreak;
    LNeedsStreams := False;

    SetLength(LColumnNames, ATable.Columns.Count);
    for LIndex := 0 to ATable.Columns.Count - 1 do
      LColumnNames[LIndex] := ATable.Columns[LIndex].Name;

    LPropertyNames := FNameConverter.ToUniquePascalCase(LColumnNames);

    AddUnique(LUses, 'SimpleAttributes');
    if NeedsMetadataAttributes(ATable) then
      AddUnique(LUses, 'ExtractDB.Generators.Attributes.MetadataAttributes');

    for LIndex := 0 to ATable.Columns.Count - 1 do
    begin
      LColumn := ATable.Columns[LIndex];
      LMapping := FTypeMapper.Map(LColumn);

      if LMapping.Warning <> '' then
        AResult.AddMessage(gmsWarning, dotTable, ATable.Schema, ATable.Name, 'DelphiTypeMapper', LMapping.Warning);

      AddUnique(LUses, LMapping.UsesUnit);
      if (LColumn.DbType = cdtEnum) and (LColumn.NativeType <> '') then
        AddUnique(LUses, BuildUnitName(AContext, LColumn.NativeType));
      LNeedsStreams := LNeedsStreams or LMapping.IsStream;
      LFields.Add(Format('    %s: %s;', [FNameConverter.ToFieldName(LPropertyNames[LIndex]), LMapping.TypeName]));

      if LMapping.IsStream then
        LFields.Add(Format('    procedure Set%s(const Value: TStream);', [LPropertyNames[LIndex]]));

      WriteColumn(LProperties, LColumn, LPropertyNames[LIndex], FNameConverter.ToFieldName(LPropertyNames[LIndex]), ATable);
    end;

    SetLength(LRelationshipBaseNames, ATable.ForeignKeys.Count);
    for LIndex := 0 to ATable.ForeignKeys.Count - 1 do
      LRelationshipBaseNames[LIndex] := GetNavigationName(ATable.ForeignKeys[LIndex]);
    LRelationshipNames := FNameConverter.ToUniquePascalCase(LRelationshipBaseNames);

    for LIndex := 0 to ATable.ForeignKeys.Count - 1 do
    begin
      LForeignKey := ATable.ForeignKeys[LIndex];
      if IsTargetTableSelected(AContext, LForeignKey) then
      begin
        AddUnique(LUses, BuildUnitName(AContext, LForeignKey.TargetTable));
        LFields.Add(Format(
          '    F%s: %s;',
          [LRelationshipNames[LIndex], BuildClassName(LForeignKey.TargetTable)]));
        WriteRelationship(LProperties, LForeignKey, LRelationshipNames[LIndex], ATable);
      end;
    end;

    LUnitName := BuildUnitName(AContext, ATable.Name);
    LSource.Add('unit ' + LUnitName + ';');
    LSource.Add('');
    LSource.Add('interface');
    LSource.Add('');
    if LUses.Count > 0 then
    begin
      LSource.Add('uses');
      for LIndex := 0 to LUses.Count - 1 do
      begin
        if LIndex = LUses.Count - 1 then
          LSource.Add('  ' + LUses[LIndex] + ';')
        else
          LSource.Add('  ' + LUses[LIndex] + ',');
      end;
      LSource.Add('');
    end;

    LSource.Add('type');
    LSource.Add('  ' + FAttributeWriter.BuildTableAttribute(ATable));
    LSource.Add('  ' + BuildClassName(ATable.Name) + ' = class');
    LSource.Add('  private');
    LSource.AddStrings(LFields);
    if LNeedsStreams then
    begin
      LSource.Add('  public');
      LSource.Add('    constructor Create;');
      LSource.Add('    destructor Destroy; override;');
    end;
    LSource.Add('  published');
    LSource.AddStrings(LProperties);
    LSource.Add('  end;');
    LSource.Add('');
    LSource.Add('implementation');
    LSource.Add('');

    if LNeedsStreams then
    begin
      LSource.Add('constructor ' + BuildClassName(ATable.Name) + '.Create;');
      LSource.Add('begin');
      LSource.Add('  inherited Create;');
      for LIndex := 0 to ATable.Columns.Count - 1 do
      begin
        LMapping := FTypeMapper.Map(ATable.Columns[LIndex]);
        if LMapping.IsStream then
          LSource.Add(Format('  %s := TMemoryStream.Create;', [FNameConverter.ToFieldName(LPropertyNames[LIndex])]));
      end;
      LSource.Add('end;');
      LSource.Add('');
      LSource.Add('destructor ' + BuildClassName(ATable.Name) + '.Destroy;');
      LSource.Add('begin');
      for LIndex := 0 to ATable.Columns.Count - 1 do
      begin
        LMapping := FTypeMapper.Map(ATable.Columns[LIndex]);
        if LMapping.IsStream then
          LSource.Add(Format('  %s.Free;', [FNameConverter.ToFieldName(LPropertyNames[LIndex])]));
      end;
      LSource.Add('  inherited;');
      LSource.Add('end;');
      LSource.Add('');
    end;

    LSource.AddStrings(LImplementation);

    for LIndex := 0 to ATable.Columns.Count - 1 do
    begin
      LMapping := FTypeMapper.Map(ATable.Columns[LIndex]);
      if LMapping.IsStream then
      begin
        LSource.Add(Format(
          'procedure %s.Set%s(const Value: TStream);',
          [BuildClassName(ATable.Name), LPropertyNames[LIndex]]));
        LSource.Add('begin');
        LSource.Add('  if Assigned(Value) then');
        LSource.Add(Format('    TMemoryStream(%s).LoadFromStream(Value)', [FNameConverter.ToFieldName(LPropertyNames[LIndex])]));
        LSource.Add('  else');
        LSource.Add(Format('    TMemoryStream(%s).Clear;', [FNameConverter.ToFieldName(LPropertyNames[LIndex])]));
        LSource.Add('end;');
        LSource.Add('');
      end;
    end;

    LSource.Add('end.');
    Result := LSource.Text;
  finally
    LImplementation.Free;
    LProperties.Free;
    LFields.Free;
    LUses.Free;
    LSource.Free;
  end;
end;

function TDelphiEntityCodeGenerator.IsTargetTableSelected(
  AContext: TGenerationContext;
  AForeignKey: TForeignKeyMetadata): Boolean;
begin
  Result := AContext.IsTableSelected(AForeignKey.TargetTable);
end;

function TDelphiEntityCodeGenerator.NeedsMetadataAttributes(ATable: TTableMetadata): Boolean;
var
  LColumn: TColumnMetadata;
  LForeignKey: TForeignKeyMetadata;
begin
  for LColumn in ATable.Columns do
  begin
    if Assigned(LColumn.DefaultValue) or LColumn.IsComputed then
      Exit(True);

    if Assigned(LColumn.ValueGeneration) and
       (LColumn.ValueGeneration.Strategy = vgsTriggerSequence) and
       (LColumn.ValueGeneration.TriggerName <> '') then
      Exit(True);
  end;

  for LForeignKey in ATable.ForeignKeys do
    if Length(LForeignKey.SourceColumns) > 1 then
      Exit(True);

  Result := False;
end;

function TDelphiEntityCodeGenerator.GetNavigationName(AForeignKey: TForeignKeyMetadata): string;
var
  LName: string;
begin
  if Length(AForeignKey.SourceColumns) = 1 then
  begin
    LName := FNameConverter.ToPascalCase(AForeignKey.SourceColumns[0]);
    if LName.StartsWith('Id') and (Length(LName) > 2) then
      Exit(Copy(LName, 3, MaxInt));

    Exit(LName);
  end;

  Result := FNameConverter.ToPascalCase(AForeignKey.TargetTable);
end;

procedure TDelphiEntityCodeGenerator.WriteAttributes(
  ASource: TStrings;
  AAttributes: TStrings;
  const AIndent: string);
begin
  ASource.Add(AIndent + '[' + string.Join(', ', AAttributes.ToStringArray) + ']');
end;

procedure TDelphiEntityCodeGenerator.WriteColumn(
  ASource: TStrings;
  AColumn: TColumnMetadata;
  const APropertyName: string;
  const AFieldName: string;
  ATable: TTableMetadata);
var
  LAttributes: TStringList;
  LMapping: TDelphiTypeMapping;
  LWriteTarget: string;
begin
  LAttributes := TStringList.Create;
  try
    FAttributeWriter.AddColumnAttributes(AColumn, ATable.ForeignKeys, LAttributes);
    WriteAttributes(ASource, LAttributes, '    ');

    LMapping := FTypeMapper.Map(AColumn);
    if LMapping.IsStream then
      LWriteTarget := 'Set' + APropertyName
    else
      LWriteTarget := AFieldName;

    ASource.Add(Format(
      '    property %s: %s read %s write %s;',
      [APropertyName, LMapping.TypeName, AFieldName, LWriteTarget]));
    ASource.Add('');
  finally
    LAttributes.Free;
  end;
end;

procedure TDelphiEntityCodeGenerator.WriteRelationship(
  ASource: TStrings;
  AForeignKey: TForeignKeyMetadata;
  const APropertyName: string;
  ATable: TTableMetadata);
var
  LAttributes: TStringList;
  LFieldName: string;
begin
  LAttributes := TStringList.Create;
  try
    FAttributeWriter.AddRelationshipAttributes(AForeignKey, BuildClassName(AForeignKey.TargetTable), LAttributes);
    WriteAttributes(ASource, LAttributes, '    ');
    LFieldName := 'F' + APropertyName;
    ASource.Add(Format(
      '    property %s: %s read %s write %s;',
      [APropertyName, BuildClassName(AForeignKey.TargetTable), LFieldName, LFieldName]));
    ASource.Add('');
  finally
    LAttributes.Free;
  end;
end;

constructor TDelphiDatabaseGenerator.Create;
begin
  inherited Create;
  FEntityGenerator := TDelphiEntityCodeGenerator.Create;
  FNameConverter := TDelphiNameConverter.Create;
end;

destructor TDelphiDatabaseGenerator.Destroy;
begin
  FNameConverter.Free;
  FEntityGenerator.Free;
  inherited;
end;

function TDelphiDatabaseGenerator.Generate(
  ADatabase: TDatabaseMetadata;
  AContext: TGenerationContext): TGenerationResult;
var
  LTable: TTableMetadata;
  LEnum: TEnumMetadata;
begin
  Result := TGenerationResult.Create;
  TDirectory.CreateDirectory(AContext.OutputDirectory);

  for LTable in ADatabase.Tables do
    if AContext.IsTableSelected(LTable.Name) and not IsSystemObject(LTable.Schema, LTable.Name, ADatabase.Provider) then
      try
        WriteFile(GetModelPath(AContext, LTable.Name), FEntityGenerator.Generate(LTable, AContext, Result));
        Result.SuccessCount := Result.SuccessCount + 1;
      except
        on E: Exception do
          Result.AddMessage(gmsError, dotTable, LTable.Schema, LTable.Name, 'EntityGenerator', E.Message);
      end;

  for LEnum in ADatabase.Enums do
    if not IsSystemObject(LEnum.Schema, LEnum.Name, ADatabase.Provider) then
      try
        WriteFile(GetEnumPath(AContext, LEnum.Name), GenerateEnum(LEnum, AContext));
        Result.SuccessCount := Result.SuccessCount + 1;
      except
        on E: Exception do
          Result.AddMessage(gmsError, dotEnum, LEnum.Schema, LEnum.Name, 'EnumGenerator', E.Message);
      end;

  GenerateScripts<TViewMetadata>(ADatabase.Views, AContext, 'Views', dotView, ADatabase.Provider, Result);
  GenerateScripts<TProcedureMetadata>(ADatabase.Procedures, AContext, 'Procedures', dotProcedure, ADatabase.Provider, Result);
  GenerateScripts<TTriggerMetadata>(ADatabase.Triggers, AContext, 'Triggers', dotTrigger, ADatabase.Provider, Result);
  GenerateScripts<TSequenceMetadata>(ADatabase.Sequences, AContext, 'Sequences', dotSequence, ADatabase.Provider, Result);
end;

function TDelphiDatabaseGenerator.GenerateEnum(AEnum: TEnumMetadata; AContext: TGenerationContext): string;
var
  LSource: TStringList;
  LNames: TArray<string>;
  I: Integer;
begin
  LSource := TStringList.Create;
  try
    LNames := FNameConverter.ToUniquePascalCase(AEnum.Values);
    if AContext.UnitPrefix <> '' then
      LSource.Add('unit ' + AContext.UnitPrefix + '.' + FNameConverter.ToPascalCase(AEnum.Name) + ';')
    else
      LSource.Add('unit ' + FNameConverter.ToPascalCase(AEnum.Name) + ';');
    LSource.Add('');
    LSource.Add('interface');
    LSource.Add('');
    LSource.Add('type');
    LSource.Add('  T' + FNameConverter.ToPascalCase(AEnum.Name) + ' = (');
    for I := 0 to High(LNames) do
      if I = High(LNames) then
        LSource.Add('    ' + LNames[I])
      else
        LSource.Add('    ' + LNames[I] + ',');
    LSource.Add('  );');
    LSource.Add('');
    LSource.Add('implementation');
    LSource.Add('');
    LSource.Add('end.');
    Result := LSource.Text;
  finally
    LSource.Free;
  end;
end;

procedure TDelphiDatabaseGenerator.GenerateScripts<T>(
  AObjects: TObjectList<T>;
  AContext: TGenerationContext;
  const AFolderName: string;
  AObjectType: TDatabaseObjectType;
  AProvider: TDatabaseProvider;
  AResult: TGenerationResult);
var
  LObject: TSqlObjectMetadata;
  LSelected: Boolean;
begin
  for LObject in AObjects do
  begin
    case AObjectType of
      dotView:
        LSelected := AContext.IsViewSelected(LObject.Name);
      dotProcedure:
        LSelected := AContext.IsProcedureSelected(LObject.Name);
      dotTrigger:
        LSelected := AContext.IsTriggerSelected(LObject.Name);
      dotSequence:
        LSelected := AContext.IsSequenceSelected(LObject.Name);
    else
      LSelected := True;
    end;

    if LSelected and not IsSystemObject(LObject.Schema, LObject.Name, AProvider) then
      try
        WriteFile(GetScriptPath(AContext, AFolderName, LObject.Schema, LObject.Name), LObject.Sql);
        AResult.SuccessCount := AResult.SuccessCount + 1;
      except
        on E: Exception do
          AResult.AddMessage(gmsError, AObjectType, LObject.Schema, LObject.Name, 'SqlExport', E.Message);
      end;
  end;
end;

function TDelphiDatabaseGenerator.GetEnumPath(AContext: TGenerationContext; const AEnumName: string): string;
begin
  Result := TPath.Combine(TPath.Combine(TPath.Combine(AContext.OutputDirectory, 'Models'), 'Enums'), FNameConverter.ToPascalCase(AEnumName) + '.pas');
end;

function TDelphiDatabaseGenerator.GetModelPath(AContext: TGenerationContext; const ATableName: string): string;
begin
  Result := TPath.Combine(TPath.Combine(AContext.OutputDirectory, 'Models'), FNameConverter.ToPascalCase(ATableName) + '.pas');
end;

function TDelphiDatabaseGenerator.GetScriptPath(
  AContext: TGenerationContext;
  const AFolderName: string;
  const ASchema: string;
  const AName: string): string;
var
  LBasePath: string;
begin
  LBasePath := TPath.Combine(TPath.Combine(AContext.OutputDirectory, 'Scripts'), AFolderName);
  if ASchema <> '' then
    LBasePath := TPath.Combine(LBasePath, ASchema);

  Result := TPath.Combine(LBasePath, AName + '.sql');
end;

function TDelphiDatabaseGenerator.IsSystemObject(
  const ASchema: string;
  const AName: string;
  AProvider: TDatabaseProvider): Boolean;
begin
  Result := False;

  case AProvider of
    dpPostgreSql:
      Result := SameText(ASchema, 'pg_catalog') or SameText(ASchema, 'information_schema');
    dpSqlServer:
      Result := SameText(ASchema, 'sys') or SameText(ASchema, 'INFORMATION_SCHEMA');
    dpFirebird:
      Result :=
        StartsText('RDB$', AName) or
        StartsText('MON$', AName) or
        StartsText('SEC$', AName);
  end;
end;

function TDelphiDatabaseGenerator.NormalizeLineEndings(const AContent: string): string;
begin
  Result := AContent.Replace(#13#10, #10).Replace(#13, #10).Replace(#10, #13#10);
end;

procedure TDelphiDatabaseGenerator.WriteFile(const APath: string; const AContent: string);
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
