using System;
using System.Threading.Tasks;

namespace DbQueue
{
    public interface IDbqAcknowledgement<out T> : IAsyncDisposable, IDisposable
    {
        T Data { get; }
        Task Commit();
    }
}
