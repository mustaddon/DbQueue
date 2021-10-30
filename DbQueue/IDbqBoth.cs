namespace DbQueue
{
    public interface IDbqBoth : IDbQueue, IDbStack
    {
        bool StackMode { get; set; }
    }
}
