unit ExtractDB.Generators.Naming;

interface

uses
  System.SysUtils,
  System.Character,
  System.Generics.Collections;

type
  TDelphiNameConverter = class
  public
    function ToPascalCase(const AName: string): string;
    function ToFieldName(const APropertyName: string): string;
    function ToUniquePascalCase(const ANames: TArray<string>): TArray<string>;
  end;

implementation

function TDelphiNameConverter.ToFieldName(const APropertyName: string): string;
begin
  Result := 'F' + APropertyName;
end;

function TDelphiNameConverter.ToPascalCase(const AName: string): string;
var
  I: Integer;
  LNextUpper: Boolean;
  LChar: Char;
begin
  Result := '';
  LNextUpper := True;

  for I := 1 to Length(AName) do
  begin
    LChar := AName[I];
    if LChar.IsLetterOrDigit then
    begin
      if LNextUpper then
        Result := Result + LChar.ToUpper
      else
        Result := Result + LChar.ToLower;

      LNextUpper := False;
    end
    else
      LNextUpper := True;
  end;

  if Result = '' then
    Result := 'Value';

  if Result[1].IsDigit then
    Result := 'N' + Result;
end;

function TDelphiNameConverter.ToUniquePascalCase(const ANames: TArray<string>): TArray<string>;
var
  LUsed: TDictionary<string, Integer>;
  I: Integer;
  LBaseName: string;
  LName: string;
  LCount: Integer;
begin
  SetLength(Result, Length(ANames));
  LUsed := TDictionary<string, Integer>.Create;
  try
    for I := 0 to High(ANames) do
    begin
      LBaseName := ToPascalCase(ANames[I]);
      LName := LBaseName;

      if LUsed.TryGetValue(LBaseName.ToUpperInvariant, LCount) then
      begin
        Inc(LCount);
        LUsed[LBaseName.ToUpperInvariant] := LCount;
        LName := LBaseName + LCount.ToString;
      end
      else
        LUsed.Add(LBaseName.ToUpperInvariant, 1);

      Result[I] := LName;
    end;
  finally
    LUsed.Free;
  end;
end;

end.
