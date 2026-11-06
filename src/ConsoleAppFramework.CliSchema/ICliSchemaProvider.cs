namespace ConsoleAppFramework;

/// <summary>Provides CLI metadata to optional tooling without exposing the generated builder type.</summary>
public interface ICliSchemaProvider
{
    CommandHelpDefinition[] GetCliSchema();
}
