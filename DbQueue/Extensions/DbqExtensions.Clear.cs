using System.Threading;
using System.Threading.Tasks;

namespace DbQueue
{
    public static partial class DbqExtensions
    {
        public static Task Clear(this IDbQueue dbq,
               string queue, int? type = null, CancellationToken cancellationToken = default)
        {
            return dbq.Clear(queue, type.HasValue ? new[] { type.Value } : null, cancellationToken);
        }

    }
}
