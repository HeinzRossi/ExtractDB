unit ExtractDB.Core.Model;

interface

uses
  System.Generics.Collections,
  ExtractDB.Core.Types;

type
  TDefaultValueMetadata = class
  private
    FRawExpression: string;
    FKind: TDefaultValueKind;
  public
    property RawExpression: string read FRawExpression write FRawExpression;
    property Kind: TDefaultValueKind read FKind write FKind;
  end;

  TValueGenerationMetadata = class
  private
    FStrategy: TValueGenerationStrategy;
    FSequenceName: string;
    FTriggerName: string;
  public
    property Strategy: TValueGenerationStrategy read FStrategy write FStrategy;
    property SequenceName: string read FSequenceName write FSequenceName;
    property TriggerName: string read FTriggerName write FTriggerName;
  end;

  TColumnMetadata = class
  private
    FName: string;
    FOrdinalPosition: Integer;
    FNativeType: string;
    FDbType: TCommonDbType;
    FLength: Integer;
    FHasLength: Boolean;
    FPrecision: Integer;
    FHasPrecision: Boolean;
    FScale: Integer;
    FHasScale: Boolean;
    FIsUnicode: Boolean;
    FIsNullable: Boolean;
    FIsPrimaryKey: Boolean;
    FIsComputed: Boolean;
    FComputedExpression: string;
    FDefaultValue: TDefaultValueMetadata;
    FValueGeneration: TValueGenerationMetadata;
  public
    destructor Destroy; override;

    property Name: string read FName write FName;
    property OrdinalPosition: Integer read FOrdinalPosition write FOrdinalPosition;
    property NativeType: string read FNativeType write FNativeType;
    property DbType: TCommonDbType read FDbType write FDbType;
    property Length: Integer read FLength write FLength;
    property HasLength: Boolean read FHasLength write FHasLength;
    property Precision: Integer read FPrecision write FPrecision;
    property HasPrecision: Boolean read FHasPrecision write FHasPrecision;
    property Scale: Integer read FScale write FScale;
    property HasScale: Boolean read FHasScale write FHasScale;
    property IsUnicode: Boolean read FIsUnicode write FIsUnicode;
    property IsNullable: Boolean read FIsNullable write FIsNullable;
    property IsPrimaryKey: Boolean read FIsPrimaryKey write FIsPrimaryKey;
    property IsComputed: Boolean read FIsComputed write FIsComputed;
    property ComputedExpression: string read FComputedExpression write FComputedExpression;
    property DefaultValue: TDefaultValueMetadata read FDefaultValue write FDefaultValue;
    property ValueGeneration: TValueGenerationMetadata read FValueGeneration write FValueGeneration;
  end;

  TPrimaryKeyMetadata = class
  private
    FName: string;
    FColumns: TArray<string>;
  public
    property Name: string read FName write FName;
    property Columns: TArray<string> read FColumns write FColumns;
  end;

  TForeignKeyMetadata = class
  private
    FName: string;
    FSourceSchema: string;
    FSourceTable: string;
    FSourceColumns: TArray<string>;
    FTargetSchema: string;
    FTargetTable: string;
    FTargetColumns: TArray<string>;
    FIsNullable: Boolean;
  public
    property Name: string read FName write FName;
    property SourceSchema: string read FSourceSchema write FSourceSchema;
    property SourceTable: string read FSourceTable write FSourceTable;
    property SourceColumns: TArray<string> read FSourceColumns write FSourceColumns;
    property TargetSchema: string read FTargetSchema write FTargetSchema;
    property TargetTable: string read FTargetTable write FTargetTable;
    property TargetColumns: TArray<string> read FTargetColumns write FTargetColumns;
    property IsNullable: Boolean read FIsNullable write FIsNullable;
  end;

  TTableMetadata = class
  private
    FSchema: string;
    FName: string;
    FColumns: TObjectList<TColumnMetadata>;
    FPrimaryKey: TPrimaryKeyMetadata;
    FForeignKeys: TObjectList<TForeignKeyMetadata>;
  public
    constructor Create;
    destructor Destroy; override;

    property Schema: string read FSchema write FSchema;
    property Name: string read FName write FName;
    property Columns: TObjectList<TColumnMetadata> read FColumns;
    property PrimaryKey: TPrimaryKeyMetadata read FPrimaryKey write FPrimaryKey;
    property ForeignKeys: TObjectList<TForeignKeyMetadata> read FForeignKeys;
  end;

  TSqlObjectMetadata = class
  private
    FSchema: string;
    FName: string;
    FSql: string;
  public
    property Schema: string read FSchema write FSchema;
    property Name: string read FName write FName;
    property Sql: string read FSql write FSql;
  end;

  TViewMetadata = class(TSqlObjectMetadata);
  TProcedureMetadata = class(TSqlObjectMetadata);
  TTriggerMetadata = class(TSqlObjectMetadata);

  TSequenceMetadata = class(TSqlObjectMetadata);

  TEnumMetadata = class
  private
    FSchema: string;
    FName: string;
    FValues: TArray<string>;
  public
    property Schema: string read FSchema write FSchema;
    property Name: string read FName write FName;
    property Values: TArray<string> read FValues write FValues;
  end;

  TDatabaseMetadata = class
  private
    FProvider: TDatabaseProvider;
    FDatabaseName: string;
    FDefaultSchema: string;
    FTables: TObjectList<TTableMetadata>;
    FViews: TObjectList<TViewMetadata>;
    FProcedures: TObjectList<TProcedureMetadata>;
    FTriggers: TObjectList<TTriggerMetadata>;
    FSequences: TObjectList<TSequenceMetadata>;
    FEnums: TObjectList<TEnumMetadata>;
  public
    constructor Create;
    destructor Destroy; override;

    property Provider: TDatabaseProvider read FProvider write FProvider;
    property DatabaseName: string read FDatabaseName write FDatabaseName;
    property DefaultSchema: string read FDefaultSchema write FDefaultSchema;
    property Tables: TObjectList<TTableMetadata> read FTables;
    property Views: TObjectList<TViewMetadata> read FViews;
    property Procedures: TObjectList<TProcedureMetadata> read FProcedures;
    property Triggers: TObjectList<TTriggerMetadata> read FTriggers;
    property Sequences: TObjectList<TSequenceMetadata> read FSequences;
    property Enums: TObjectList<TEnumMetadata> read FEnums;
  end;

implementation

destructor TColumnMetadata.Destroy;
begin
  FDefaultValue.Free;
  FValueGeneration.Free;
  inherited;
end;

constructor TTableMetadata.Create;
begin
  inherited Create;
  FColumns := TObjectList<TColumnMetadata>.Create(True);
  FForeignKeys := TObjectList<TForeignKeyMetadata>.Create(True);
end;

destructor TTableMetadata.Destroy;
begin
  FColumns.Free;
  FPrimaryKey.Free;
  FForeignKeys.Free;
  inherited;
end;

constructor TDatabaseMetadata.Create;
begin
  inherited Create;
  FTables := TObjectList<TTableMetadata>.Create(True);
  FViews := TObjectList<TViewMetadata>.Create(True);
  FProcedures := TObjectList<TProcedureMetadata>.Create(True);
  FTriggers := TObjectList<TTriggerMetadata>.Create(True);
  FSequences := TObjectList<TSequenceMetadata>.Create(True);
  FEnums := TObjectList<TEnumMetadata>.Create(True);
end;

destructor TDatabaseMetadata.Destroy;
begin
  FTables.Free;
  FViews.Free;
  FProcedures.Free;
  FTriggers.Free;
  FSequences.Free;
  FEnums.Free;
  inherited;
end;

end.
