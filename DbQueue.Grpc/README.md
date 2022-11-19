# DbQueue.Grpc [![NuGet version](https://badge.fury.io/nu/DbQueue.Grpc.svg)](http://badge.fury.io/nu/DbQueue.Grpc)
DbQueue gRPC service


## Features
* SQL/NoSQL database
* Queue/Stack mode
* Concurrency
* AvailableAfter/RemoveAfter date
* Storing BLOBs in the file system


## Example 1: gRPC Queue with MongoDB
.NET CLI
```
dotnet new web --name "DbqGrpcExample"
cd DbqGrpcExample
dotnet add package DbQueue.Grpc
dotnet add package DbQueue.MongoDB
```

Program.cs:
```C#
var builder = WebApplication.CreateBuilder(args);

// add services to the container
builder.Services.AddGrpc();
builder.Services.AddDbqMongo((services, options) =>
{
    // add database settings 
    options.Database.ConnectionString = "mongodb://localhost:27017";

    // add blob's path construction algorithm 
    options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
});

var app = builder.Build();

// map gRPC service to the endpoint
app.MapDbqGrpc();

app.Run();
```

## Example 2: gRPC client
.NET CLI
```
dotnet new console --name "DbqGrpcClient"
cd DbqGrpcClient
dotnet add package DbQueue.Grpc.Client
```

Program.cs:
```C#
using DbQueue;

// create client
using var queue = new DbqGrpcClient("https://localhost:7271");
var queueName = "examples";

// push
await queue.Push(queueName, "Some byte[], stream, string and etc...");

// pop
var received = await queue.Pop<string>(queueName).WithAutoAck();
Console.WriteLine($"pop: {received}");
```

[More examples...](https://github.com/mustaddon/DbQueue/tree/main/Examples/)
