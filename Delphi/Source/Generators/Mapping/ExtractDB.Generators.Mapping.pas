unit ExtractDB.Generators.Mapping;

interface

uses
  ExtractDB.Core.Model,
  ExtractDB.Core.Types,
  ExtractDB.Generators.Naming;

type
  TDelphiTypeMapping = record
    TypeName: string;
    UsesUnit: string;
    Warning: string;
    IsStream: Boolean;
  end;

  TDelphiTypeMapper = class
  private
    FNameConverter: TDelphiNameConverter;
    function MapDecimal(AColumn: TColumnMetadata): TDelphiTypeMapping;
  public
    constructor Create;
    destructor Destroy; override;

    function Map(AColumn: TColumnMetadata): TDelphiTypeMapping;
  end;

implementation

uses
  System.SysUtils;

constructor TDelphiTypeMapper.Create;
begin
  inherited Create;
  FNameConverter := TDelphiNameConverter.Create;
end;

destructor TDelphiTypeMapper.Destroy;
begin
  FNameConverter.Free;
  inherited;
end;

function TDelphiTypeMapper.Map(AColumn: TColumnMetadata): TDelphiTypeMapping;
begin
  Result.TypeName := 'Variant';
  Result.UsesUnit := '';
  Result.Warning := '';
  Result.IsStream := False;

  case AColumn.DbType of
    cdtSmallInt:
      Result.TypeName := 'SmallInt';
    cdtInteger:
      Result.TypeName := 'Integer';
    cdtBigInt:
      Result.TypeName := 'Int64';
    cdtDecimal:
      Exit(MapDecimal(AColumn));
    cdtFloat:
      Result.TypeName := 'Single';
    cdtDouble:
      Result.TypeName := 'Double';
    cdtBoolean:
      Result.TypeName := 'Boolean';
    cdtChar, cdtVarChar, cdtText, cdtJson:
      Result.TypeName := 'string';
    cdtDate:
      Result.TypeName := 'TDate';
    cdtTime:
      Result.TypeName := 'TTime';
    cdtDateTime:
      Result.TypeName := 'TDateTime';
    cdtBinary, cdtBlob:
      begin
        Result.TypeName := 'TStream';
        Result.UsesUnit := 'System.Classes';
        Result.IsStream := True;
      end;
    cdtGuid:
      begin
        Result.TypeName := 'TGuid';
        Result.UsesUnit := 'System.SysUtils';
      end;
    cdtEnum:
      Result.TypeName := 'T' + FNameConverter.ToPascalCase(AColumn.NativeType);
    cdtUnknown:
      begin
        Result.TypeName := 'Variant';
        Result.UsesUnit := 'System.Variants';
        Result.Warning := Format(
          'Column "%s" has unknown database type "%s".',
          [AColumn.Name, AColumn.NativeType]);
      end;
  end;
end;

function TDelphiTypeMapper.MapDecimal(AColumn: TColumnMetadata): TDelphiTypeMapping;
begin
  Result.Warning := '';
  Result.IsStream := False;

  if AColumn.HasPrecision and AColumn.HasScale and
     (AColumn.Precision <= 18) and (AColumn.Scale <= 4) then
  begin
    Result.TypeName := 'Currency';
    Result.UsesUnit := '';
  end
  else
  begin
    Result.TypeName := 'TBcd';
    Result.UsesUnit := 'Data.FmtBcd';
  end;
end;

end.
