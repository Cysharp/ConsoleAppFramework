namespace ConsoleAppFramework;

readonly record struct ConsoleAppFrameworkGeneratorOptions(bool DisableNamingConversion);

readonly record struct DllReference(bool HasDependencyInjection, bool HasLogging, bool HasConfiguration, bool HasJsonConfiguration, bool HasHost);
