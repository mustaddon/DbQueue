using System;
using System.Collections.Generic;
using System.Net;

namespace DbQueue
{
    public class DbqRestClientSettings
    {
        public ICredentials? Credentials { get; set; }

        public Dictionary<string, IEnumerable<string>> DefaultRequestHeaders { get; set; } = new();

        public bool StackMode { get; set; }

        public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
