# DbQueue.Rest.Client [![NuGet version](https://badge.fury.io/nu/DbQueue.Rest.Client.svg)](http://badge.fury.io/nu/DbQueue.Rest.Client)
DbQueue REST client


## Example: Usage
.NET CLI
```
dotnet new console --name "DbqRestClient"
cd DbqRestClient
dotnet add package DbQueue.Rest.Client
```

Program.cs:
```C#
using DbQueue;

// create client
using var queue = new DbqRestClient("https://localhost:7273");
var queueName = "examples";

// push
await queue.Push(queueName, "Some byte[], stream, string and etc...");

// pop
var received = await queue.Pop<string>(queueName).WithAutoAck();
Console.WriteLine($"pop: {received}");
```

[More examples...](https://github.com/mustaddon/DbQueue/tree/main/Examples/)
