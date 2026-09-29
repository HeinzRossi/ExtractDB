unit ExtractDB.Application.Connection;

interface

uses
  System.SysUtils,
  FireDAC.Comp.Client,
  ExtractDB.Core.Types;

type
  TDatabaseConnectionOptions = class
  private
    FProvider: TDatabaseProvider;
    FHost: string;
    FPort: Integer;
    FDatabase: string;
    FUserName: string;
    FPassword: string;
  public
    constructor Create(AProvider: TDatabaseProvider);

    class function DefaultPort(AProvider: TDatabaseProvider): Integer; static;
    class function ProviderDisplayName(AProvider: TDatabaseProvider): string; static;
    class function TryParseProvider(const AValue: string; out AProvider: TDatabaseProvider): Boolean; static;

    property Provider: TDatabaseProvider read FProvider write FProvider;
    property Host: string read FHost write FHost;
    property Port: Integer read FPort write FPort;
    property Database: string read FDatabase write FDatabase;
    property UserName: string read FUserName write FUserName;
    property Password: string read FPassword write FPassword;
  end;

  TFireDacConnectionFactory = class
  public
    class function DriverId(AProvider: TDatabaseProvider): string; static;
    function CreateConnection(AOptions: TDatabaseConnectionOptions): TFDConnection;
  end;

implementation

uses
  FireDAC.Stan.Def,
  FireDAC.Phys.PG,
  FireDAC.Phys.MSSQL,
  FireDAC.Phys.FB;

constructor TDatabaseConnectionOptions.Create(AProvider: TDatabaseProvider);
begin
  inherited Create;
  FProvider := AProvider;
  FHost := 'localhost';
  FPort := DefaultPort(AProvider);
end;

class function TDatabaseConnectionOptions.DefaultPort(AProvider: TDatabaseProvider): Integer;
begin
  case AProvider of
    dpPostgreSql:
      Result := 5432;
    dpSqlServer:
      Result := 1433;
    dpFirebird:
      Result := 3050;
  else
    Result := 0;
  end;
end;

class function TDatabaseConnectionOptions.ProviderDisplayName(AProvider: TDatabaseProvider): string;
begin
  case AProvider of
    dpPostgreSql:
      Result := 'PostgreSQL';
    dpSqlServer:
      Result := 'SQL Server';
    dpFirebird:
      Result := 'Firebird';
  else
    Result := '';
  end;
end;

class function TDatabaseConnectionOptions.TryParseProvider(
  const AValue: string;
  out AProvider: TDatabaseProvider): Boolean;
begin
  Result := True;

  if SameText(AValue, 'PostgreSQL') or SameText(AValue, 'PostgreSql') then
    AProvider := dpPostgreSql
  else if SameText(AValue, 'SQL Server') or SameText(AValue, 'SqlServer') then
    AProvider := dpSqlServer
  else if SameText(AValue, 'Firebird') then
    AProvider := dpFirebird
  else
    Result := False;
end;

function TFireDacConnectionFactory.CreateConnection(AOptions: TDatabaseConnectionOptions): TFDConnection;
begin
  Result := TFDConnection.Create(nil);
  Result.LoginPrompt := False;
  Result.Params.Clear;
  Result.Params.Values['DriverID'] := DriverId(AOptions.Provider);

  case AOptions.Provider of
    dpPostgreSql:
      begin
        Result.Params.Values['Server'] := AOptions.Host;
        Result.Params.Values['Port'] := AOptions.Port.ToString;
        Result.Params.Values['Database'] := AOptions.Database;
        Result.Params.Values['User_Name'] := AOptions.UserName;
        Result.Params.Values['Password'] := AOptions.Password;
      end;
    dpSqlServer:
      begin
        Result.Params.Values['Server'] := AOptions.Host;
        Result.Params.Values['Port'] := AOptions.Port.ToString;
        Result.Params.Values['Database'] := AOptions.Database;
        Result.Params.Values['User_Name'] := AOptions.UserName;
        Result.Params.Values['Password'] := AOptions.Password;
        Result.Params.Values['OSAuthent'] := 'No';
        Result.Params.Values['Encrypt'] := 'No';
      end;
    dpFirebird:
      begin
        Result.Params.Values['Server'] := AOptions.Host;
        Result.Params.Values['Port'] := AOptions.Port.ToString;
        Result.Params.Values['Database'] := AOptions.Database;
        Result.Params.Values['User_Name'] := AOptions.UserName;
        Result.Params.Values['Password'] := AOptions.Password;
      end;
  end;
end;

class function TFireDacConnectionFactory.DriverId(AProvider: TDatabaseProvider): string;
begin
  case AProvider of
    dpPostgreSql:
      Result := 'PG';
    dpSqlServer:
      Result := 'MSSQL';
    dpFirebird:
      Result := 'FB';
  else
    Result := '';
  end;
end;

end.
