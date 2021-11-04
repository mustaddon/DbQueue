using DbQueue;


// create client
using var queue = new DbqRestClient("https://localhost:7273");
var queueName = "examples";

// push
await queue.Push(queueName, "Some byte[], stream, string and etc...");

// peek
var peeked = await queue.Peek<string>(queueName);
Console.WriteLine($"peek: {peeked}");

// pop
var popped = await queue.Pop<string>(queueName).WithAutoAck();
Console.WriteLine($"pop: {popped}");

// count
var count = await queue.Count(queueName);
Console.WriteLine($"{count} left in queue '{queueName}'");
