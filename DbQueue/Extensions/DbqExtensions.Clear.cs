using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static Task Clear(this IDbQueue dbq,
               string queue, string? type = null, CancellationToken cancellationToken = default)
        {
            return dbq.Clear(queue, type != null ? new[] { type } : null, cancellationToken);
        }

    }
}
