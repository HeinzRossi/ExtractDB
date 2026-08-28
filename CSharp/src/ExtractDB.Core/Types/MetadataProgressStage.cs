namespace ExtractDB.Core.Types;

public enum MetadataProgressStage
{
    Connecting,
    ReadingTables,
    ReadingColumns,
    ReadingPrimaryKeys,
    ReadingForeignKeys,
    ReadingSequences,
    ReadingViews,
    ReadingProcedures,
    ReadingTriggers,
    GeneratingModels,
    GeneratingScripts,
    Completed
}
