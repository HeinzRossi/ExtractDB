unit frmPrincipal;

interface

uses
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes,
  System.Threading, Vcl.Graphics, Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.ExtCtrls,
  Vcl.StdCtrls, Vcl.ComCtrls, ExtractDB.Application.Connection, ExtractDB.Core.Model,
  ExtractDB.Core.Contracts, ExtractDB.Core.Types;

type
  TForm1 = class(TForm)
    pnlBottomBar: TPanel;
    pnlSidebar: TPanel;
    pnlContent: TPanel;
    lblStatusMessage: TLabel;
    lblProgressMessage: TLabel;
    btnCancelar: TButton;
    ProgressBarBusy: TProgressBar;
    lblAppTitle: TLabel;
    lblCurrentStepTitle: TLabel;
    btnStepConexao: TButton;
    btnStepObjetos: TButton;
    btnStepSaida: TButton;
    pnlHint: TPanel;
    lblHint: TLabel;
    PageControl1: TPageControl;
    TabConexao: TTabSheet;
    TabSelecao: TTabSheet;
    TabConfiguracao: TTabSheet;
    pnlConexao: TPanel;
    lblConexaoTitulo: TLabel;
    lblConexaoSubtitulo: TLabel;
    lblProvider: TLabel;
    lblServidor: TLabel;
    lblPorta: TLabel;
    lblDatabase: TLabel;
    lblUsuario: TLabel;
    lblSenha: TLabel;
    cbxProvider: TComboBox;
    edtServidor: TEdit;
    edtPorta: TEdit;
    edtDatabase: TEdit;
    edtUsuario: TEdit;
    edtSenha: TEdit;
    btnTestarConexao: TButton;
    btnLerMetadata: TButton;
    pnlSelecaoObjetos: TPanel;
    lblSelecaoTitulo: TLabel;
    lblSelecaoSubtitulo: TLabel;
    edtBusca: TEdit;
    cbxTipoFiltro: TComboBox;
    btnTodos: TButton;
    btnNenhum: TButton;
    btnInverter: TButton;
    lvObjetos: TListView;
    btnConfigurarGeracao: TButton;
    TabResultado: TTabSheet;
    pnlResultado: TPanel;
    lblResultadoTitulo: TLabel;
    pnlSuccess: TPanel;
    lblSuccessCaption: TLabel;
    lblSuccessValor: TLabel;
    pnlWarnings: TPanel;
    lblWarningsCaption: TLabel;
    lblWarningsValor: TLabel;
    pnlErrors: TPanel;
    lblErrorsCaption: TLabel;
    lblErrorsValor: TLabel;
    lvMensagens: TListView;
    pnlConfiguracaoGeracao: TPanel;
    lblConfigTitulo: TLabel;
    lblConfigSubtitulo: TLabel;
    lblNamespace: TLabel;
    lblPastaSaida: TLabel;
    lblJsonDados: TLabel;
    edtNamespace: TEdit;
    edtPastaSaida: TEdit;
    edtJsonDados: TEdit;
    btnEscolherPasta: TButton;
    btnEscolherJsonDados: TButton;
    btnVoltarSelecao: TButton;
    btnGerar: TButton;
    procedure FormCreate(Sender: TObject);
  private
    FMetadata: TDatabaseMetadata;
    FCancelRequested: Boolean;

    function BuildConnectionOptions: TDatabaseConnectionOptions;
    function CurrentProvider: TDatabaseProvider;
    function ObjectTypeText(AObjectType: TDatabaseObjectType): string;
    function TryObjectTypeFromText(const AValue: string; out AObjectType: TDatabaseObjectType): Boolean;
    procedure AddObjectItem(AObjectType: TDatabaseObjectType; const ASchema: string; const AName: string);
    procedure AddResultMessage(AResultMessage: TGenerationMessage);
    procedure ChooseDataExportConfiguration(Sender: TObject);
    procedure ChooseOutputDirectory(Sender: TObject);
    procedure ConfigureGeneration(Sender: TObject);
    procedure FilterObjects(Sender: TObject);
    procedure GenerateSources(Sender: TObject);
    procedure InvertSelection(Sender: TObject);
    procedure LoadObjects;
    procedure ProviderChanged(Sender: TObject);
    procedure ReadMetadata(Sender: TObject);
    procedure SelectAllObjects(Sender: TObject);
    procedure SelectNoObjects(Sender: TObject);
    procedure SetBusy(AValue: Boolean; const AStatus: string; const AProgress: string = '');
    procedure ShowConnection(Sender: TObject);
    procedure ShowObjects(Sender: TObject);
    procedure ShowOutput(Sender: TObject);
    procedure TestConnection(Sender: TObject);
    procedure CancelOperation(Sender: TObject);
  public
    destructor Destroy; override;
  end;

var
  Form1: TForm1;

implementation

uses
  System.IOUtils,
  Vcl.FileCtrl,
  ExtractDB.Application.OperationMessages,
  ExtractDB.Generators.Code,
  ExtractDB.Providers.DataExport,
  ExtractDB.Providers.Contracts,
  ExtractDB.Providers.MetadataProviders,
  ExtractDB.Providers.SystemFilter;

{$R *.dfm}

procedure TForm1.AddObjectItem(
  AObjectType: TDatabaseObjectType;
  const ASchema: string;
  const AName: string);
var
  LItem: TListItem;
begin
  LItem := lvObjetos.Items.Add;
  LItem.Caption := AName;
  LItem.Checked := True;
  LItem.SubItems.Add(ObjectTypeText(AObjectType));
  LItem.SubItems.Add(ASchema);
end;

procedure TForm1.AddResultMessage(AResultMessage: TGenerationMessage);
var
  LItem: TListItem;
begin
  LItem := lvMensagens.Items.Add;
  case AResultMessage.Severity of
    gmsInfo:
      LItem.Caption := 'Info';
    gmsWarning:
      LItem.Caption := 'Warning';
    gmsError:
      LItem.Caption := 'Error';
  end;

  LItem.SubItems.Add(AResultMessage.Stage);
  LItem.SubItems.Add(AResultMessage.ObjectName);
  LItem.SubItems.Add(AResultMessage.Message);
end;

function TForm1.BuildConnectionOptions: TDatabaseConnectionOptions;
begin
  Result := TDatabaseConnectionOptions.Create(CurrentProvider);
  Result.Host := Trim(edtServidor.Text);
  Result.Port := StrToIntDef(Trim(edtPorta.Text), TDatabaseConnectionOptions.DefaultPort(Result.Provider));
  Result.Database := Trim(edtDatabase.Text);
  Result.UserName := Trim(edtUsuario.Text);
  Result.Password := edtSenha.Text;
end;

procedure TForm1.CancelOperation(Sender: TObject);
begin
  FCancelRequested := True;
  lblStatusMessage.Caption := 'Cancelamento solicitado.';
  lblProgressMessage.Caption := TOperationMessages.CancelWaiting;
end;

procedure TForm1.ChooseOutputDirectory(Sender: TObject);
var
  LDirectory: string;
begin
  LDirectory := edtPastaSaida.Text;
  if SelectDirectory('Escolha a pasta de saída', '', LDirectory) then
    edtPastaSaida.Text := LDirectory;
end;

procedure TForm1.ChooseDataExportConfiguration(Sender: TObject);
var
  LDialog: TOpenDialog;
begin
  LDialog := TOpenDialog.Create(nil);
  try
    LDialog.Filter := 'Arquivos JSON (*.json)|*.json|Todos os arquivos (*.*)|*.*';
    LDialog.FileName := edtJsonDados.Text;
    if LDialog.Execute then
      edtJsonDados.Text := LDialog.FileName;
  finally
    LDialog.Free;
  end;
end;

procedure TForm1.ConfigureGeneration(Sender: TObject);
begin
  ShowOutput(Sender);
end;

function TForm1.CurrentProvider: TDatabaseProvider;
begin
  if not TDatabaseConnectionOptions.TryParseProvider(cbxProvider.Text, Result) then
    Result := dpPostgreSql;
end;

destructor TForm1.Destroy;
begin
  FMetadata.Free;
  inherited;
end;

procedure TForm1.FilterObjects(Sender: TObject);
begin
  LoadObjects;
end;

procedure TForm1.FormCreate(Sender: TObject);
begin
  PageControl1.TabWidth := 0;
  PageControl1.ActivePage := TabConexao;

  cbxProvider.Items.Clear;
  cbxProvider.Items.Add(TDatabaseConnectionOptions.ProviderDisplayName(dpPostgreSql));
  cbxProvider.Items.Add(TDatabaseConnectionOptions.ProviderDisplayName(dpSqlServer));
  cbxProvider.Items.Add(TDatabaseConnectionOptions.ProviderDisplayName(dpFirebird));
  cbxProvider.ItemIndex := 0;

  cbxTipoFiltro.Items.Clear;
  cbxTipoFiltro.Items.Add('Todos');
  cbxTipoFiltro.Items.Add('Tables');
  cbxTipoFiltro.Items.Add('Views');
  cbxTipoFiltro.Items.Add('Procedures');
  cbxTipoFiltro.Items.Add('Triggers');
  cbxTipoFiltro.Items.Add('Sequences');
  cbxTipoFiltro.ItemIndex := 0;

  lvObjetos.Columns.Clear;
  lvObjetos.Columns.Add.Caption := 'Nome';
  lvObjetos.Columns[0].Width := 320;
  lvObjetos.Columns.Add.Caption := 'Tipo';
  lvObjetos.Columns[1].Width := 120;
  lvObjetos.Columns.Add.Caption := 'Schema';
  lvObjetos.Columns[2].Width := 160;

  lvMensagens.Columns.Clear;
  lvMensagens.Columns.Add.Caption := 'Severidade';
  lvMensagens.Columns[0].Width := 100;
  lvMensagens.Columns.Add.Caption := 'Etapa';
  lvMensagens.Columns[1].Width := 150;
  lvMensagens.Columns.Add.Caption := 'Objeto';
  lvMensagens.Columns[2].Width := 180;
  lvMensagens.Columns.Add.Caption := 'Mensagem';
  lvMensagens.Columns[3].Width := 360;

  edtServidor.Text := 'localhost';
  edtNamespace.Text := 'ExtractDB.Generated';
  edtPastaSaida.Text := TPath.Combine(TPath.GetDocumentsPath, 'ExtractDBOutput');
  ProviderChanged(Sender);

  cbxProvider.OnChange := ProviderChanged;
  edtBusca.OnChange := FilterObjects;
  cbxTipoFiltro.OnChange := FilterObjects;
  btnTestarConexao.OnClick := TestConnection;
  btnLerMetadata.OnClick := ReadMetadata;
  btnTodos.OnClick := SelectAllObjects;
  btnNenhum.OnClick := SelectNoObjects;
  btnInverter.OnClick := InvertSelection;
  btnConfigurarGeracao.OnClick := ConfigureGeneration;
  btnEscolherPasta.OnClick := ChooseOutputDirectory;
  btnEscolherJsonDados.OnClick := ChooseDataExportConfiguration;
  btnVoltarSelecao.OnClick := ShowObjects;
  btnGerar.OnClick := GenerateSources;
  btnCancelar.OnClick := CancelOperation;
  btnStepConexao.OnClick := ShowConnection;
  btnStepObjetos.OnClick := ShowObjects;
  btnStepSaida.OnClick := ShowOutput;

  SetBusy(False, 'Informe a conexão para começar.');
end;

procedure TForm1.GenerateSources(Sender: TObject);
var
  LContext: TGenerationContext;
  LItem: TListItem;
  LObjectType: TDatabaseObjectType;
  LGenerator: TDelphiDatabaseGenerator;
  LOptions: TDatabaseConnectionOptions;
  LDataExportConfigurationPath: string;
begin
  if not Assigned(FMetadata) then
  begin
    ShowMessage('Leia a metadata antes de gerar.');
    Exit;
  end;

  if Trim(edtPastaSaida.Text) = '' then
  begin
    ShowMessage('Informe a pasta de saída.');
    Exit;
  end;

  LContext := TGenerationContext.Create;
  LContext.OutputDirectory := Trim(edtPastaSaida.Text);
  LGenerator := TDelphiDatabaseGenerator.Create;
  LOptions := BuildConnectionOptions;
  LDataExportConfigurationPath := Trim(edtJsonDados.Text);
  FCancelRequested := False;
  lblSuccessValor.Caption := '0';
  lblWarningsValor.Caption := '0';
  lblErrorsValor.Caption := '0';
  lvMensagens.Items.Clear;
  SetBusy(
    True,
    TOperationMessages.GenerationWaiting,
    'Os arquivos serão sobrescritos na pasta de saída selecionada.');

  for LItem in lvObjetos.Items do
    if LItem.Checked and TryObjectTypeFromText(LItem.SubItems[0], LObjectType) then
      case LObjectType of
        dotTable:
          LContext.SelectedTables.Add(LItem.Caption);
        dotView:
          LContext.SelectedViews.Add(LItem.Caption);
        dotProcedure:
          LContext.SelectedProcedures.Add(LItem.Caption);
        dotTrigger:
          LContext.SelectedTriggers.Add(LItem.Caption);
        dotSequence:
          LContext.SelectedSequences.Add(LItem.Caption);
      end;

  TTask.Run(TProc(
    procedure
    var
      LResult: TGenerationResult;
      LConfiguration: TDataExportConfiguration;
      LDataExporter: TDatabaseDataExportProvider;
      LError: string;
    begin
      LResult := nil;
      LConfiguration := nil;
      LDataExporter := nil;
      LError := '';
      try
        if not FCancelRequested then
        begin
          LResult := LGenerator.Generate(FMetadata, LContext);
          if (LDataExportConfigurationPath <> '') and not FCancelRequested then
          begin
            LConfiguration := TDataExportConfiguration.LoadFromFile(LDataExportConfigurationPath);
            LDataExporter := TDatabaseDataExportProvider.Create;
            LDataExporter.ExportData(LOptions, FMetadata, LContext.OutputDirectory, LConfiguration, LResult);
          end;
        end;
      except
        on E: Exception do
          LError := E.Message;
      end;

      LDataExporter.Free;
      LConfiguration.Free;
      LOptions.Free;

      TThread.Queue(nil, TThreadProcedure(
        procedure
        var
          LMessage: TGenerationMessage;
        begin
          SetBusy(False, '');
          PageControl1.ActivePage := TabResultado;
          lvMensagens.Items.Clear;

          if LError <> '' then
          begin
            lblStatusMessage.Caption := 'Geração falhou.';
            lblErrorsValor.Caption := '1';
            lvMensagens.Items.Add.Caption := LError;
          end
          else if FCancelRequested then
          begin
            lblStatusMessage.Caption := 'Geração cancelada.';
            lblProgressMessage.Caption := 'Nenhum novo resultado foi carregado.';
          end
          else
          begin
            lblStatusMessage.Caption := 'Geração concluída.';
            lblProgressMessage.Caption := Format(
              '%d arquivos gerados, %d avisos, %d erros.',
              [LResult.SuccessCount, LResult.WarningCount, LResult.ErrorCount]);
            lblSuccessValor.Caption := LResult.SuccessCount.ToString;
            lblWarningsValor.Caption := LResult.WarningCount.ToString;
            lblErrorsValor.Caption := LResult.ErrorCount.ToString;
            for LMessage in LResult.Messages do
              AddResultMessage(LMessage);
          end;

          LResult.Free;
          LGenerator.Free;
          LContext.Free;
        end));
    end));
end;

procedure TForm1.InvertSelection(Sender: TObject);
var
  LItem: TListItem;
begin
  for LItem in lvObjetos.Items do
    LItem.Checked := not LItem.Checked;
end;

procedure TForm1.LoadObjects;
var
  LSearch: string;
  LTypeFilter: string;
  LTable: TTableMetadata;
  LView: TViewMetadata;
  LProcedure: TProcedureMetadata;
  LTrigger: TTriggerMetadata;
  LSequence: TSequenceMetadata;

  function ShouldShow(AObjectType: TDatabaseObjectType; const ASchema: string; const AName: string): Boolean;
  begin
    Result :=
      Assigned(FMetadata) and
      TSystemObjectFilter.IsUserObject(FMetadata.Provider, ASchema, AName) and
      ((LSearch = '') or (Pos(LSearch, LowerCase(AName)) > 0)) and
      ((LTypeFilter = 'Todos') or SameText(LTypeFilter, ObjectTypeText(AObjectType)));
  end;

begin
  lvObjetos.Items.BeginUpdate;
  try
    lvObjetos.Items.Clear;
    if not Assigned(FMetadata) then
      Exit;

    LSearch := LowerCase(Trim(edtBusca.Text));
    LTypeFilter := cbxTipoFiltro.Text;

    for LTable in FMetadata.Tables do
      if ShouldShow(dotTable, LTable.Schema, LTable.Name) then
        AddObjectItem(dotTable, LTable.Schema, LTable.Name);

    for LView in FMetadata.Views do
      if ShouldShow(dotView, LView.Schema, LView.Name) then
        AddObjectItem(dotView, LView.Schema, LView.Name);

    for LProcedure in FMetadata.Procedures do
      if ShouldShow(dotProcedure, LProcedure.Schema, LProcedure.Name) then
        AddObjectItem(dotProcedure, LProcedure.Schema, LProcedure.Name);

    for LTrigger in FMetadata.Triggers do
      if ShouldShow(dotTrigger, LTrigger.Schema, LTrigger.Name) then
        AddObjectItem(dotTrigger, LTrigger.Schema, LTrigger.Name);

    for LSequence in FMetadata.Sequences do
      if ShouldShow(dotSequence, LSequence.Schema, LSequence.Name) then
        AddObjectItem(dotSequence, LSequence.Schema, LSequence.Name);
  finally
    lvObjetos.Items.EndUpdate;
  end;
end;

function TForm1.ObjectTypeText(AObjectType: TDatabaseObjectType): string;
begin
  case AObjectType of
    dotTable:
      Result := 'Tables';
    dotView:
      Result := 'Views';
    dotProcedure:
      Result := 'Procedures';
    dotTrigger:
      Result := 'Triggers';
    dotSequence:
      Result := 'Sequences';
  else
    Result := '';
  end;
end;

procedure TForm1.ProviderChanged(Sender: TObject);
begin
  edtPorta.Text := TDatabaseConnectionOptions.DefaultPort(CurrentProvider).ToString;
end;

procedure TForm1.ReadMetadata(Sender: TObject);
var
  LOptions: TDatabaseConnectionOptions;
begin
  LOptions := BuildConnectionOptions;
  FCancelRequested := False;
  SetBusy(
    True,
    TOperationMessages.MetadataWaiting,
    'A conexão está aberta apenas durante esta operação.');

  TTask.Run(TProc(
    procedure
    var
      LFactory: TDatabaseMetadataProviderFactory;
      LProvider: IDatabaseMetadataProvider;
      LMetadata: TDatabaseMetadata;
      LError: string;
    begin
      LFactory := TDatabaseMetadataProviderFactory.Create;
      LMetadata := nil;
      LError := '';
      try
        try
          LProvider := LFactory.CreateProvider(LOptions.Provider);
          if not FCancelRequested then
            LMetadata := LProvider.ReadMetadata(LOptions);
        except
          on E: Exception do
            LError := E.Message;
        end;
      finally
        LFactory.Free;
        LOptions.Free;
      end;

      TThread.Queue(nil, TThreadProcedure(
        procedure
        begin
          SetBusy(False, '');

          if LError <> '' then
          begin
            LMetadata.Free;
            lblStatusMessage.Caption := 'Falha ao ler metadata.';
            lblProgressMessage.Caption := LError;
          end
          else if FCancelRequested then
          begin
            LMetadata.Free;
            lblStatusMessage.Caption := 'Leitura cancelada.';
            lblProgressMessage.Caption := 'Nenhuma metadata nova foi carregada.';
          end
          else
          begin
            FMetadata.Free;
            FMetadata := LMetadata;
            LoadObjects;
            PageControl1.ActivePage := TabSelecao;
            lblStatusMessage.Caption := 'Metadata lida.';
            lblProgressMessage.Caption := Format('%d tabelas, %d views, %d procedures, %d triggers, %d sequences.',
              [FMetadata.Tables.Count, FMetadata.Views.Count, FMetadata.Procedures.Count, FMetadata.Triggers.Count, FMetadata.Sequences.Count]);
          end;
        end));
    end));
end;

procedure TForm1.SelectAllObjects(Sender: TObject);
var
  LItem: TListItem;
begin
  for LItem in lvObjetos.Items do
    LItem.Checked := True;
end;

procedure TForm1.SelectNoObjects(Sender: TObject);
var
  LItem: TListItem;
begin
  for LItem in lvObjetos.Items do
    LItem.Checked := False;
end;

procedure TForm1.SetBusy(AValue: Boolean; const AStatus: string; const AProgress: string = '');
begin
  btnTestarConexao.Enabled := not AValue;
  btnLerMetadata.Enabled := not AValue;
  btnGerar.Enabled := not AValue;
  btnCancelar.Enabled := AValue;
  cbxProvider.Enabled := not AValue;
  ProgressBarBusy.Visible := AValue;
  ProgressBarBusy.Enabled := AValue;

  if AStatus <> '' then
    lblStatusMessage.Caption := AStatus;

  lblProgressMessage.Caption := AProgress;
end;

procedure TForm1.ShowConnection(Sender: TObject);
begin
  PageControl1.ActivePage := TabConexao;
  lblCurrentStepTitle.Caption := 'Conexão';
end;

procedure TForm1.ShowObjects(Sender: TObject);
begin
  PageControl1.ActivePage := TabSelecao;
  lblCurrentStepTitle.Caption := 'Objetos';
end;

procedure TForm1.ShowOutput(Sender: TObject);
begin
  PageControl1.ActivePage := TabConfiguracao;
  lblCurrentStepTitle.Caption := 'Saída';
end;

procedure TForm1.TestConnection(Sender: TObject);
var
  LOptions: TDatabaseConnectionOptions;
begin
  LOptions := BuildConnectionOptions;
  FCancelRequested := False;
  SetBusy(
    True,
    TOperationMessages.ConnectionWaiting(LOptions.Provider),
    'Validando servidor, porta, database e credenciais.');

  TTask.Run(TProc(
    procedure
    var
      LFactory: TDatabaseMetadataProviderFactory;
      LProvider: IDatabaseMetadataProvider;
      LError: string;
      LSuccess: Boolean;
    begin
      LFactory := TDatabaseMetadataProviderFactory.Create;
      LError := '';
      LSuccess := False;
      try
        try
          LProvider := LFactory.CreateProvider(LOptions.Provider);
          if not FCancelRequested then
            LSuccess := LProvider.TestConnection(LOptions);
        except
          on E: Exception do
            LError := E.Message;
        end;
      finally
        LFactory.Free;
        LOptions.Free;
      end;

      TThread.Queue(nil, TThreadProcedure(
        procedure
        begin
          SetBusy(False, '');
          if LError <> '' then
          begin
            lblStatusMessage.Caption := 'Conexão falhou.';
            lblProgressMessage.Caption := LError;
          end
          else if FCancelRequested then
          begin
            lblStatusMessage.Caption := 'Teste cancelado.';
            lblProgressMessage.Caption := 'A conexão não foi alterada.';
          end
          else if LSuccess then
          begin
            lblStatusMessage.Caption := 'Conexão testada com sucesso.';
            lblProgressMessage.Caption := '';
          end;
        end));
    end));
end;

function TForm1.TryObjectTypeFromText(
  const AValue: string;
  out AObjectType: TDatabaseObjectType): Boolean;
begin
  Result := True;
  if SameText(AValue, 'Tables') then
    AObjectType := dotTable
  else if SameText(AValue, 'Views') then
    AObjectType := dotView
  else if SameText(AValue, 'Procedures') then
    AObjectType := dotProcedure
  else if SameText(AValue, 'Triggers') then
    AObjectType := dotTrigger
  else if SameText(AValue, 'Sequences') then
    AObjectType := dotSequence
  else
    Result := False;
end;

end.
