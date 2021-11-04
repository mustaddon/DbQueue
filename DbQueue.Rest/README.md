# DbQueue.Rest [![NuGet version](https://badge.fury.io/nu/DbQueue.Rest.svg)](http://badge.fury.io/nu/DbQueue.Rest)
.NET DbQueue REST service


## Features
* SQL/NoSQL database
* Queue/Stack mode
* Concurrency
* AvailableAfter/RemoveAfter date
* Storing BLOBs in the file system


## Example 1: REST Queue with MongoDB
.NET CLI
```
dotnet new web --name "DbqRestExample"
cd DbqRestExample
dotnet add package DbQueue.Rest
dotnet add package DbQueue.MongoDB
```

Program.cs:
```C#
var builder = WebApplication.CreateBuilder(args);

// add services to the container
builder.Services.AddDbqMongo((services, options) =>
{
    // add database settings 
    options.Database.ConnectionString = "mongodb://localhost:27017";

    // add blob's path construction algorithm 
    options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
});

//// for net5.0 add via controllers
//builder.Services.AddControllers().AddDbqRest();

var app = builder.Build();

// map REST service to the endpoint
app.MapDbqRest();

//// for net5.0 map controllers
//app.MapControllers();

app.Run();
```

## Example 2: REST client
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
using var queue = new DbqRestClient("https://localhost:7271");
var queueName = "examples";

// push
await queue.Push(queueName, "Some byte[], stream, string and etc...");

// pop
var received = await queue.Pop<string>(queueName).WithAutoAck();
Console.WriteLine($"pop: {received}");
```

[More examples...](https://github.com/mustaddon/DbQueue/tree/main/Examples/)
