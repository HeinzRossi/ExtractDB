program ExtractDB;

uses
  Vcl.Forms,
  frmPrincipal in 'UI\Forms\frmPrincipal.pas' {Form1},
  ExtractDB.Core.Types in 'Core\Types\ExtractDB.Core.Types.pas',
  ExtractDB.Core.Model in 'Core\Model\ExtractDB.Core.Model.pas',
  ExtractDB.Core.Contracts in 'Core\Contracts\ExtractDB.Core.Contracts.pas',
  ExtractDB.Generators.Naming in 'Generators\Naming\ExtractDB.Generators.Naming.pas',
  ExtractDB.Generators.Mapping in 'Generators\Mapping\ExtractDB.Generators.Mapping.pas',
  ExtractDB.Generators.Attributes.MetadataAttributes in 'Generators\Attributes\ExtractDB.Generators.Attributes.MetadataAttributes.pas',
  ExtractDB.Generators.Attributes in 'Generators\Attributes\ExtractDB.Generators.Attributes.pas',
  ExtractDB.Generators.Code in 'Generators\Code\ExtractDB.Generators.Code.pas',
  ExtractDB.Application.Connection in 'Application\Services\ExtractDB.Application.Connection.pas',
  ExtractDB.Application.OperationMessages in 'Application\Services\ExtractDB.Application.OperationMessages.pas',
  ExtractDB.Providers.Contracts in 'Providers\ExtractDB.Providers.Contracts.pas',
  ExtractDB.Providers.SystemFilter in 'Providers\ExtractDB.Providers.SystemFilter.pas',
  ExtractDB.Providers.DataExport in 'Providers\ExtractDB.Providers.DataExport.pas',
  ExtractDB.Providers.MetadataProviders in 'Providers\ExtractDB.Providers.MetadataProviders.pas';

{$R *.res}

begin
  Application.Initialize;
  Application.MainFormOnTaskbar := True;
  Application.CreateForm(TForm1, Form1);
  Application.Run;
end.
