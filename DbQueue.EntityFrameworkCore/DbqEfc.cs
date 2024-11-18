using System;

namespace DbQueue.EntityFrameworkCore
{
    internal class DbqEfc(DbqDatabase database, DbqBlobStorage blobStorage, DbqSettings? settings)
        : Dbq(database, blobStorage, settings),
        IDisposable
    {
        public void Dispose()
        {
            database.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
