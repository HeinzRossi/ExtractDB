unit ExtractDB.Generators.Attributes;

interface

uses
  System.SysUtils,
  System.Classes,
  System.Generics.Collections,
  ExtractDB.Core.Model,
  ExtractDB.Core.Types;

type
  TSimpleOrmAttributeWriter = class
  private
    function Escape(const AValue: string): string;
    function BuildCampoAttribute(AColumn: TColumnMetadata): string;
    function GetCampoSize(AColumn: TColumnMetadata): string;
    function GetCampoType(AColumn: TColumnMetadata): string;
    function JoinColumns(const AColumns: TArray<string>): string;
    function TryGetColumnForeignKey(
      AColumn: TColumnMetadata;
      AForeignKeys: TObjectList<TForeignKeyMetadata>;
      out AForeignKey: TForeignKeyMetadata): Boolean;
  public
    function BuildTableAttribute(ATable: TTableMetadata): string;
    procedure AddColumnAttributes(
      AColumn: TColumnMetadata;
      AForeignKeys: TObjectList<TForeignKeyMetadata>;
      AAttributes: TStrings);
    procedure AddRelationshipAttributes(
      AForeignKey: TForeignKeyMetadata;
      const ATargetEntityName: string;
      AAttributes: TStrings);
  end;

implementation

procedure TSimpleOrmAttributeWriter.AddColumnAttributes(
  AColumn: TColumnMetadata;
  AForeignKeys: TObjectList<TForeignKeyMetadata>;
  AAttributes: TStrings);
var
  LForeignKey: TForeignKeyMetadata;
begin
  AAttributes.Add(BuildCampoAttribute(AColumn));

  if AColumn.IsPrimaryKey then
    AAttributes.Add('PK');

  if TryGetColumnForeignKey(AColumn, AForeignKeys, LForeignKey) then
  begin
    AAttributes.Add('FK');

    if Length(LForeignKey.SourceColumns) = 1 then
      AAttributes.Add(Format(
        'ForeignKey(''%s'', ''%s'', ''%s'', ''%s'')',
        [
          Escape(LForeignKey.SourceColumns[0]),
          Escape(LForeignKey.Name),
          Escape(LForeignKey.TargetTable),
          Escape(LForeignKey.TargetColumns[0])
        ]));
  end;

  if not AColumn.IsNullable then
    AAttributes.Add('NotNull');

  if Assigned(AColumn.ValueGeneration) then
  begin
    if AColumn.ValueGeneration.Strategy = vgsIdentity then
      AAttributes.Add('AutoInc');

    if AColumn.ValueGeneration.Strategy in [vgsSequence, vgsTriggerSequence] then
      if AColumn.ValueGeneration.SequenceName <> '' then
        AAttributes.Add(Format('Sequence(''%s'')', [Escape(AColumn.ValueGeneration.SequenceName)]));

    if AColumn.ValueGeneration.Strategy = vgsTriggerSequence then
      if AColumn.ValueGeneration.TriggerName <> '' then
        AAttributes.Add(Format('GeneratedByTrigger(''%s'')', [Escape(AColumn.ValueGeneration.TriggerName)]));
  end;

  if Assigned(AColumn.DefaultValue) then
    AAttributes.Add(Format('DatabaseDefault(''%s'')', [Escape(AColumn.DefaultValue.RawExpression)]));

  if AColumn.IsComputed then
  begin
    if AColumn.ComputedExpression = '' then
      AAttributes.Add('DatabaseComputed')
    else
      AAttributes.Add(Format('DatabaseComputed(''%s'')', [Escape(AColumn.ComputedExpression)]));
  end;
end;

procedure TSimpleOrmAttributeWriter.AddRelationshipAttributes(
  AForeignKey: TForeignKeyMetadata;
  const ATargetEntityName: string;
  AAttributes: TStrings);
begin
  if Length(AForeignKey.SourceColumns) = 1 then
    AAttributes.Add(Format(
      'BelongsTo(''%s'')',
      [Escape(ATargetEntityName)]))
  else
    AAttributes.Add(Format(
      'CompositeForeignKey(''%s'', ''%s'', ''%s'', ''%s'')',
      [
        Escape(AForeignKey.Name),
        Escape(JoinColumns(AForeignKey.SourceColumns)),
        Escape(AForeignKey.TargetTable),
        Escape(JoinColumns(AForeignKey.TargetColumns))
      ]));
end;

function TSimpleOrmAttributeWriter.BuildTableAttribute(ATable: TTableMetadata): string;
begin
  Result := Format('[Tabela(''%s'')]', [Escape(ATable.Name)]);
end;

function TSimpleOrmAttributeWriter.BuildCampoAttribute(AColumn: TColumnMetadata): string;
var
  LSize: string;
begin
  LSize := GetCampoSize(AColumn);

  if LSize = '' then
    Result := Format(
      'Campo(''%s'', ''%s'')',
      [Escape(AColumn.Name), Escape(GetCampoType(AColumn))])
  else
    Result := Format(
      'Campo(''%s'', ''%s'', %s)',
      [Escape(AColumn.Name), Escape(GetCampoType(AColumn)), LSize]);
end;

function TSimpleOrmAttributeWriter.Escape(const AValue: string): string;
begin
  Result := AValue.Replace('''', '''''');
end;

function TSimpleOrmAttributeWriter.GetCampoSize(AColumn: TColumnMetadata): string;
begin
  Result := '';

  if AColumn.HasLength and (AColumn.Length > 0) and
     (AColumn.DbType in [cdtChar, cdtVarChar, cdtText, cdtBinary, cdtBlob]) then
    Exit(AColumn.Length.ToString);

  if AColumn.HasPrecision and (AColumn.Precision > 0) and
     (AColumn.DbType in [cdtDecimal, cdtFloat, cdtDouble]) then
  begin
    if AColumn.HasScale and (AColumn.Scale > 0) then
      Exit(AColumn.Precision.ToString + '.' + AColumn.Scale.ToString);

    Exit(AColumn.Precision.ToString);
  end;
end;

function TSimpleOrmAttributeWriter.GetCampoType(AColumn: TColumnMetadata): string;
begin
  Result := AColumn.NativeType.Trim;

  if Result = '' then
    Result := 'Unknown';
end;

function TSimpleOrmAttributeWriter.JoinColumns(const AColumns: TArray<string>): string;
begin
  Result := string.Join(';', AColumns);
end;

function TSimpleOrmAttributeWriter.TryGetColumnForeignKey(
  AColumn: TColumnMetadata;
  AForeignKeys: TObjectList<TForeignKeyMetadata>;
  out AForeignKey: TForeignKeyMetadata): Boolean;
var
  LForeignKey: TForeignKeyMetadata;
  LColumnName: string;
begin
  Result := False;
  AForeignKey := nil;

  if not Assigned(AForeignKeys) then
    Exit;

  for LForeignKey in AForeignKeys do
    for LColumnName in LForeignKey.SourceColumns do
      if SameText(LColumnName, AColumn.Name) then
      begin
        AForeignKey := LForeignKey;
        Exit(True);
      end;
end;

end.
