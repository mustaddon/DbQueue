using System;
using System.IO;

namespace DbQueue
{
    public class DbqBlobStorageSettings
    {
        public Func<string, string> PathBuilder { get; set; }
            = static (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
    }
}
