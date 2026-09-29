unit ExtractDB.Core.Contracts;

interface

uses
  System.Classes,
  System.SysUtils,
  System.Generics.Collections,
  ExtractDB.Core.Types;

type
  TGenerationContext = class
  private
    FOutputDirectory: string;
    FUnitPrefix: string;
    FSelectedTables: TStringList;
    FSelectedViews: TStringList;
    FSelectedProcedures: TStringList;
    FSelectedTriggers: TStringList;
    FSelectedSequences: TStringList;
  public
    constructor Create;
    destructor Destroy; override;

    function IsTableSelected(const ATableName: string): Boolean;
    function IsViewSelected(const AName: string): Boolean;
    function IsProcedureSelected(const AName: string): Boolean;
    function IsTriggerSelected(const AName: string): Boolean;
    function IsSequenceSelected(const AName: string): Boolean;

    property OutputDirectory: string read FOutputDirectory write FOutputDirectory;
    property UnitPrefix: string read FUnitPrefix write FUnitPrefix;
    property SelectedTables: TStringList read FSelectedTables;
    property SelectedViews: TStringList read FSelectedViews;
    property SelectedProcedures: TStringList read FSelectedProcedures;
    property SelectedTriggers: TStringList read FSelectedTriggers;
    property SelectedSequences: TStringList read FSelectedSequences;
  end;

  TGenerationMessage = class
  private
    FSeverity: TGenerationMessageSeverity;
    FObjectType: TDatabaseObjectType;
    FSchema: string;
    FObjectName: string;
    FStage: string;
    FMessage: string;
  public
    property Severity: TGenerationMessageSeverity read FSeverity write FSeverity;
    property ObjectType: TDatabaseObjectType read FObjectType write FObjectType;
    property Schema: string read FSchema write FSchema;
    property ObjectName: string read FObjectName write FObjectName;
    property Stage: string read FStage write FStage;
    property Message: string read FMessage write FMessage;
  end;

  TGenerationResult = class
  private
    FSuccessCount: Integer;
    FMessages: TObjectList<TGenerationMessage>;
    function GetWarningCount: Integer;
    function GetErrorCount: Integer;
  public
    constructor Create;
    destructor Destroy; override;

    procedure AddMessage(
      ASeverity: TGenerationMessageSeverity;
      AObjectType: TDatabaseObjectType;
      const ASchema: string;
      const AObjectName: string;
      const AStage: string;
      const AMessage: string);

    property SuccessCount: Integer read FSuccessCount write FSuccessCount;
    property WarningCount: Integer read GetWarningCount;
    property ErrorCount: Integer read GetErrorCount;
    property Messages: TObjectList<TGenerationMessage> read FMessages;
  end;

implementation

constructor TGenerationContext.Create;
begin
  inherited Create;
  FSelectedTables := TStringList.Create;
  FSelectedTables.CaseSensitive := False;
  FSelectedTables.Sorted := True;
  FSelectedTables.Duplicates := dupIgnore;

  FSelectedViews := TStringList.Create;
  FSelectedViews.CaseSensitive := False;
  FSelectedViews.Sorted := True;
  FSelectedViews.Duplicates := dupIgnore;

  FSelectedProcedures := TStringList.Create;
  FSelectedProcedures.CaseSensitive := False;
  FSelectedProcedures.Sorted := True;
  FSelectedProcedures.Duplicates := dupIgnore;

  FSelectedTriggers := TStringList.Create;
  FSelectedTriggers.CaseSensitive := False;
  FSelectedTriggers.Sorted := True;
  FSelectedTriggers.Duplicates := dupIgnore;

  FSelectedSequences := TStringList.Create;
  FSelectedSequences.CaseSensitive := False;
  FSelectedSequences.Sorted := True;
  FSelectedSequences.Duplicates := dupIgnore;
end;

destructor TGenerationContext.Destroy;
begin
  FSelectedSequences.Free;
  FSelectedTriggers.Free;
  FSelectedProcedures.Free;
  FSelectedViews.Free;
  FSelectedTables.Free;
  inherited;
end;

function TGenerationContext.IsTableSelected(const ATableName: string): Boolean;
begin
  Result := (FSelectedTables.Count = 0) or (FSelectedTables.IndexOf(ATableName) >= 0);
end;

function TGenerationContext.IsViewSelected(const AName: string): Boolean;
begin
  Result := (FSelectedViews.Count = 0) or (FSelectedViews.IndexOf(AName) >= 0);
end;

function TGenerationContext.IsProcedureSelected(const AName: string): Boolean;
begin
  Result := (FSelectedProcedures.Count = 0) or (FSelectedProcedures.IndexOf(AName) >= 0);
end;

function TGenerationContext.IsTriggerSelected(const AName: string): Boolean;
begin
  Result := (FSelectedTriggers.Count = 0) or (FSelectedTriggers.IndexOf(AName) >= 0);
end;

function TGenerationContext.IsSequenceSelected(const AName: string): Boolean;
begin
  Result := (FSelectedSequences.Count = 0) or (FSelectedSequences.IndexOf(AName) >= 0);
end;

constructor TGenerationResult.Create;
begin
  inherited Create;
  FMessages := TObjectList<TGenerationMessage>.Create(True);
end;

destructor TGenerationResult.Destroy;
begin
  FMessages.Free;
  inherited;
end;

procedure TGenerationResult.AddMessage(
  ASeverity: TGenerationMessageSeverity;
  AObjectType: TDatabaseObjectType;
  const ASchema: string;
  const AObjectName: string;
  const AStage: string;
  const AMessage: string);
var
  LMessage: TGenerationMessage;
begin
  LMessage := TGenerationMessage.Create;
  LMessage.Severity := ASeverity;
  LMessage.ObjectType := AObjectType;
  LMessage.Schema := ASchema;
  LMessage.ObjectName := AObjectName;
  LMessage.Stage := AStage;
  LMessage.Message := AMessage;
  FMessages.Add(LMessage);
end;

function TGenerationResult.GetWarningCount: Integer;
var
  LMessage: TGenerationMessage;
begin
  Result := 0;
  for LMessage in FMessages do
    if LMessage.Severity = gmsWarning then
      Inc(Result);
end;

function TGenerationResult.GetErrorCount: Integer;
var
  LMessage: TGenerationMessage;
begin
  Result := 0;
  for LMessage in FMessages do
    if LMessage.Severity = gmsError then
      Inc(Result);
end;

end.
