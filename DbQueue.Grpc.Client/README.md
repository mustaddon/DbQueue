# DbQueue.Grpc.Client [![NuGet version](https://badge.fury.io/nu/DbQueue.Grpc.Client.svg)](http://badge.fury.io/nu/DbQueue.Grpc.Client)
.NET DbQueue gRPC client


## Example: Usage
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
