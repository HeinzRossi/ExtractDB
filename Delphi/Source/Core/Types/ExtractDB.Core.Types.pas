unit ExtractDB.Core.Types;

interface

type
  TDatabaseProvider = (
    dpPostgreSql,
    dpSqlServer,
    dpFirebird
  );

  TCommonDbType = (
    cdtUnknown,
    cdtSmallInt,
    cdtInteger,
    cdtBigInt,
    cdtDecimal,
    cdtFloat,
    cdtDouble,
    cdtBoolean,
    cdtChar,
    cdtVarChar,
    cdtText,
    cdtDate,
    cdtTime,
    cdtDateTime,
    cdtBinary,
    cdtBlob,
    cdtGuid,
    cdtJson,
    cdtEnum
  );

  TDefaultValueKind = (
    dvkUnknown,
    dvkLiteral,
    dvkExpression,
    dvkSequence,
    dvkSystemFunction
  );

  TValueGenerationStrategy = (
    vgsNone,
    vgsIdentity,
    vgsSequence,
    vgsTriggerSequence
  );

  TGenerationMessageSeverity = (
    gmsInfo,
    gmsWarning,
    gmsError
  );

  TDatabaseObjectType = (
    dotTable,
    dotView,
    dotProcedure,
    dotTrigger,
    dotSequence,
    dotEnum,
    dotAttribute
  );

implementation

end.
