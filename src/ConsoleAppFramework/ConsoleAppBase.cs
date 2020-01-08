namespace ConsoleAppFramework
{
    public abstract class ConsoleAppBase
    {
        // Context will be set non-null value by ConsoleAppEngine,
        // but it might be null because it has public setter.
        #nullable disable warnings
        public ConsoleAppContext Context { get; set; }
        #nullable restore warnings
    }
}
