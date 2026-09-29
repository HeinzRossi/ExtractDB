unit ExtractDB.Providers.SystemFilter;

interface

uses
  ExtractDB.Core.Types;

type
  TSystemObjectFilter = class
  public
    class function IsSystemObject(
      AProvider: TDatabaseProvider;
      const ASchema: string;
      const AName: string): Boolean; static;

    class function IsUserObject(
      AProvider: TDatabaseProvider;
      const ASchema: string;
      const AName: string): Boolean; static;
  end;

implementation

uses
  System.SysUtils,
  System.StrUtils;

class function TSystemObjectFilter.IsSystemObject(
  AProvider: TDatabaseProvider;
  const ASchema: string;
  const AName: string): Boolean;
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

class function TSystemObjectFilter.IsUserObject(
  AProvider: TDatabaseProvider;
  const ASchema: string;
  const AName: string): Boolean;
begin
  Result := not IsSystemObject(AProvider, ASchema, AName);
end;

end.
