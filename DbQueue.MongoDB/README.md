# DbQueue.MongoDB [![NuGet version](https://badge.fury.io/nu/DbQueue.MongoDB.svg)](http://badge.fury.io/nu/DbQueue.MongoDB)
.NET DbQueue with MongoDB implementation of IDbqDatabase


## Features
* SQL/NoSQL database
* Queue/Stack mode
* Storing BLOBs in the file system
* AvailableAfter/RemoveAfter date


## Example 1: Queue with MongoDB
.NET CLI
```cli
dotnet new console --name "DbQueueExample"
cd DbQueueExample
dotnet add package DbQueue.MongoDB
dotnet add package Microsoft.Extensions.DependencyInjection
```

Program.cs:
```C#
using DbQueue;
using Microsoft.Extensions.DependencyInjection;


// add services to the container
var services = new ServiceCollection()
    .AddDbqMongo((sp, options) =>
    {
        // add database settings 
        options.Database.ConnectionString = "mongodb://localhost:27017";

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
