using Newtonsoft.Json;
using System;

namespace DbQueue.EntityFrameworkCore
{
    public class DbqDbSettings
    {
        public int LockRetries { get; set; } = 2;

        public TimeSpan AutoUnlockDelay { get; set; } = TimeSpan.FromMinutes(5); 

        public DfsDbContextConfigurator ContextConfigurator { get; set; } = static x =>
        {
            throw new InvalidOperationException($"Database provider not configured. A provider can be configured by setting the '{nameof(DbqDbSettings)}.{nameof(ContextConfigurator)}' property.");
        };
    }
}
