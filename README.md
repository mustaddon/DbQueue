# DbQueue [![NuGet version](https://badge.fury.io/nu/DbQueue.svg)](http://badge.fury.io/nu/DbQueue)
.NET Database concurrent Queue/Stack


## Features
* SQL/NoSQL database
* Queue/Stack mode
* Concurrency
* AvailableAfter/RemoveAfter date
* Storing BLOBs in the file system


## Tested on
* MS SQL Server 2019
* PostgreSQL 14
* MySQL 8.0.27
* MongoDB 5.0.3


## gRPC endpoint
* [Service](https://github.com/mustaddon/DbQueue/tree/main/DbQueue.Grpc/)
* [Client](https://github.com/mustaddon/DbQueue/tree/main/DbQueue.Grpc.Client/)


## REST endpoint
* [Service](https://github.com/mustaddon/DbQueue/tree/main/DbQueue.Rest/)
* [Client](https://github.com/mustaddon/DbQueue/tree/main/DbQueue.Rest.Client/)


## Example 1: Queue with MsSQL via EFCore
SQL
```sql
CREATE DATABASE [DbqDatabase] 
GO
CREATE TABLE [DbqDatabase].[dbo].[DbQueue]
(
    [Id] BIGINT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [Queue] NVARCHAR(255) NOT NULL,
    [Data] VARBINARY (MAX) NOT NULL,
    [Hash] BIGINT NOT NULL,
    [IsBlob] BIT NOT NULL DEFAULT 0,
    [Type] NVARCHAR(255) NULL,
    [AvailableAfter] DATETIME NULL,
    [RemoveAfter] DATETIME NULL,
    [LockId] BIGINT NULL,
    INDEX [IX_DbQueue_Queue] NONCLUSTERED ([Queue]),
    INDEX [IX_DbQueue_Hash] NONCLUSTERED ([Hash]),
    INDEX [IX_DbQueue_LockId] NONCLUSTERED ([LockId]),
)
```

.NET CLI
```cli
dotnet new console --name "DbQueueExample"
cd DbQueueExample
dotnet add package DbQueue.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Program.cs:
```C#
using DbQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


// add services to the container
var services = new ServiceCollection()
    .AddDbqEfc((sp, options) =>
    {
        // add database provider 
        options.Database.ContextConfigurator = (db) => db.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DbqDatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False");

        // add blob's path construction algorithm 
        options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
    })
    .BuildServiceProvider();


var queue = services.GetRequiredService<IDbQueue>();
var queueName = "examples";

// push
await queue.Push(queueName, "Some byte[], stream, string and etc...");

// pop
var received = await queue.Pop<string>(queueName).WithAutoAck();
Console.WriteLine($"pop: {received}");
```


## Example 2: Acknowledgement usage
```C#
await using (var ack = await queue.Pop<string>(queueName))
{
    // some code to save the received data, etc
    // ...
    // commit the acknowledgment to remove the item from the queue
    await ack.Commit();
}
```


## Example 3: Delays
```C#
await queue.Push(queueName, "example data", 
    availableAfter: DateTime.Now.AddDays(3),
    removeAfter: DateTime.Now.AddDays(5));
```


## Example 4: Receive many
```C#
for (var i = 0; i < 5; i++)
    await queue.Push(queueName, $"item-{i}");

await foreach(var data in queue.PopMany<string>(queueName).WithAutoAck())
    Console.WriteLine(data);


// Console output:
// item-0
// item-1
// item-2
// item-3
// item-4
```


## Example 5: Stack usage
```C#
var stack = services.GetRequiredService<IDbStack>();
var stackName = "examples";

for (var i = 0; i < 5; i++)
    await stack.Push(stackName, $"item-{i}");

await foreach(var data in stack.PopMany<string>(stackName).WithAutoAck())
    Console.WriteLine(data);


// Console output:
// item-4
// item-3
// item-2
// item-1
// item-0
```


## Example 6: Removing
```C#
await queue.Push(queueName, "example data 1");
await queue.Push(queueName, "example data 2", "example_type");
await queue.Push(queueName, "example data 3", "example_type");

// clear by type
await queue.Clear(queueName, "example_type");

// clear all
await queue.Clear(queueName);
```

[More examples...](https://github.com/mustaddon/DbQueue/tree/main/Examples/)