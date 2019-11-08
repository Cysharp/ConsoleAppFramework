namespace MicroBatchFramework
{
    public abstract class BatchBase
    {
        // Context will be set non-null value by BatchEngine,
        // but it might be null because it has public setter.
        #nullable disable warnings
        public BatchContext Context { get; set; }
        #nullable restore warnings
    }
}
