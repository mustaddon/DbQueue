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

//// net5.0: add REST controllers
//builder.Services.AddControllers().AddDbqRest();

var app = builder.Build();

//// net5.0: map controllers
//app.MapControllers();

// net6.0: map REST service to the endpoint
app.MapDbqRest();

app.Run();
```


## Example 2: Push
```
// request
{
    "url": "/dbq/queue/queue_name",
    "method": "POST",
    "body": "some data",
}

// request with type
{
    "url": "/dbq/queue/queue_name?type=some_type",
    "method": "POST",
    "body": "some data",
}

// request with delays
{
    "url": "/dbq/queue/queue_name?availableAfter=2021-11-01T00:00:00Z&removeAfter=2021-12-01T00:00:00Z",
    "method": "POST",
    "body": "some data",
}

// request with several queues 
{
    "url": "/dbq/queue/queue_name1,queue_name2,queue_name3",
    "method": "POST",
    "body": "some data",
}
```


## Example 3: Pop
```
// request with auto acknowledgement
{
    "url": "/dbq/queue/queue_name",
    "method": "GET",
}
// response
{
    "status": 200, // or 204 if null
    "body": "some data",
}


// request with manual acknowledgement
{
    "url": "/dbq/queue/queue_name?useAck=true&lockTimeout=60000",
    "method": "GET",
}
// response
{
    "status": 200,
    "headers": { "ack-key" : "006841a012d84cada37a5f1ff6a1ee40" },
    "body": "some data",
}


// request with commit the acknowledgement
{
    "url": "/dbq/ack/006841a012d84cada37a5f1ff6a1ee40",
    "method": "POST", // or "DELETE" for cancelling
}
```


## Example 4: Peek
```
// request
{
    "url": "/dbq/queue/queue_name/peek",
    "method": "GET",
}
// response
{
    "status": 200, // or 204 if null
    "body": "some data",
}


// request with offset
{
    "url": "/dbq/queue/queue_name/peek?index=1",
    "method": "GET",
}
// response
{
    "status": 200, // or 204 if null
    "body": "some data",
}
```


## Example 5: Clear
```
// clear by types request
{
    "url": "/dbq/queue/queue_name?type=some_type1,some_type2",
    "method": "DELETE",
}

// clear all
{
    "url": "/dbq/queue/queue_name",
    "method": "DELETE",
}
```


## Example 6: Stack
```
// similar to the above examples,
// change '/dbq/queue/...' to '/dbq/stack/...'
```


## Example 7: Client usage
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
