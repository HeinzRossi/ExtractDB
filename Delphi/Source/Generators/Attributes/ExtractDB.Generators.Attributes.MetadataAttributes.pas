unit ExtractDB.Generators.Attributes.MetadataAttributes;

interface

type
  GeneratedByTriggerAttribute = class(TCustomAttribute)
  public
    constructor Create(const AName: string);
  end;

  DatabaseDefaultAttribute = class(TCustomAttribute)
  public
    constructor Create(const AExpression: string);
  end;

  DatabaseComputedAttribute = class(TCustomAttribute)
  public
    constructor Create; overload;
    constructor Create(const AExpression: string); overload;
  end;

  CompositeForeignKeyAttribute = class(TCustomAttribute)
  public
    constructor Create(
      const AName: string;
      const ASourceColumns: string;
      const ATargetTable: string;
      const ATargetColumns: string);
  end;

implementation

constructor GeneratedByTriggerAttribute.Create(const AName: string);
begin
  inherited Create;
end;

constructor DatabaseDefaultAttribute.Create(const AExpression: string);
begin
  inherited Create;
end;

constructor DatabaseComputedAttribute.Create;
begin
  inherited Create;
end;

constructor DatabaseComputedAttribute.Create(const AExpression: string);
begin
  inherited Create;
end;

constructor CompositeForeignKeyAttribute.Create(
  const AName: string;
  const ASourceColumns: string;
  const ATargetTable: string;
  const ATargetColumns: string);
begin
  inherited Create;
end;

end.
