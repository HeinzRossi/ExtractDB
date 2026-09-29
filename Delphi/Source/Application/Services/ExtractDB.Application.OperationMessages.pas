unit ExtractDB.Application.OperationMessages;

interface

uses
  ExtractDB.Core.Types;

type
  TOperationMessages = class
  public
    class function ConnectionWaiting(AProvider: TDatabaseProvider): string; static;
    class function MetadataWaiting: string; static;
    class function GenerationWaiting: string; static;
    class function CancelWaiting: string; static;
  end;

implementation

uses
  System.SysUtils,
  ExtractDB.Application.Connection;

class function TOperationMessages.CancelWaiting: string;
begin
  Result := 'Aguardando a operação encerrar com segurança.';
end;

class function TOperationMessages.ConnectionWaiting(AProvider: TDatabaseProvider): string;
begin
  Result := Format(
    'Aguarde... testando conexão com %s.',
    [TDatabaseConnectionOptions.ProviderDisplayName(AProvider)]);
end;

class function TOperationMessages.GenerationWaiting: string;
begin
  Result := 'Aguarde... gerando models e scripts Delphi.';
end;

class function TOperationMessages.MetadataWaiting: string;
begin
  Result := 'Aguarde... lendo tabelas, views, procedures e triggers.';
end;

end.
