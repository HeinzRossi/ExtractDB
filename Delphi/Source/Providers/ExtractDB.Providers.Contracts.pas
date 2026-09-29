unit ExtractDB.Providers.Contracts;

interface

uses
  ExtractDB.Application.Connection,
  ExtractDB.Core.Model;

type
  IDatabaseMetadataProvider = interface
    ['{7539C89D-332F-4A8C-8863-C5437469F9AC}']
    function ProviderName: string;
    function TestConnection(AOptions: TDatabaseConnectionOptions): Boolean;
    function ReadMetadata(AOptions: TDatabaseConnectionOptions): TDatabaseMetadata;
  end;

implementation

end.
