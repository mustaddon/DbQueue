using DbQueue.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

var app = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // add services to the container
        services.AddDbqEfc((sp, options) =>
        {
            // add database provider 
            options.Database.ContextConfigurator = (db) => db.UseSqlServer(hostContext.Configuration.GetConnectionString("dbq"));

            // add blob's path construction algorithm 
            options.BlobStorage.PathBuilder = (filename) => Path.GetFullPath($@"_blob\{DateTime.Now:yyyy\\MM\\dd}\{filename}");
        });
    })
    .Build();


var queue = app.Services.GetRequiredService<IDbQueue>();
var queueName = "examples";

// push
await queue.Push(queueName, "some string, byte array, stream and etc");

// peek
var peeked = await queue.Peek<string>(queueName);
Console.WriteLine($"peek: {peeked}");

// pop
var poped = await queue.Pop<string>(queueName);
Console.WriteLine($"pop: {poped}");

// count
var count = await queue.Count(queueName);
Console.WriteLine($"{count} left in queue '{queueName}'");
