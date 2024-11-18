using System;

namespace DbQueue.MongoDB
{
    internal class DbqMongo(DbqDatabase database, DbqBlobStorage blobStorage, DbqSettings? settings)
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
