using System.Text.Json.Serialization;

namespace ConsoleAppFramework;

public record CommandHelpDefinition
{
    public bool IsHidden { get; init; }

    public string CommandName { get; }
    public CommandOptionHelpDefinition[] Options { get; }
    public string Description { get; }

    public CommandHelpDefinition(string commandName, CommandOptionHelpDefinition[] options, string description)
    {
        CommandName = commandName;
        Options = options;
        Description = description;
    }
}

public record CommandOptionHelpDefinition
{
    // Semantic metadata shared by documentation and completion consumers.
    public string ParameterName { get; init; } = "";
    public string[] AllowedValues { get; init; } = [];
    public bool IsGlobal { get; init; }

    public string[] Options { get; }
    public string Description { get; }
    public string? DefaultValue { get; }
    public string ValueTypeName { get; }
    public int? Index { get; }
    public bool IsRequired => DefaultValue == null && !IsParams;
    public bool IsFlag { get; }
    public bool IsParams { get; }
    public bool IsHidden { get; }
    public bool IsDefaultValueHidden { get; }
    public string FormattedValueTypeName => "<" + ValueTypeName + ">";

    public CommandOptionHelpDefinition(string[] options, string description, string valueTypeName, string? defaultValue, int? index, bool isFlag, bool isParams, bool isHidden, bool isDefaultValueHidden)
    {
        Options = options;
        Description = description;
        ValueTypeName = valueTypeName;
        DefaultValue = defaultValue;
        Index = index;
        IsFlag = isFlag;
        IsParams = isParams;
        IsHidden = isHidden;
        IsDefaultValueHidden = isDefaultValueHidden;
    }
}

[JsonSerializable(typeof(CommandHelpDefinition))]
[JsonSerializable(typeof(CommandOptionHelpDefinition))]
[JsonSerializable(typeof(CommandHelpDefinition[]))]
[JsonSerializable(typeof(CommandOptionHelpDefinition[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
public partial class CliSchemaJsonSerializerContext : JsonSerializerContext { }
