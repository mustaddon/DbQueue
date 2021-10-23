using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestPushBytes()
        {
            var queueName = GetQueueName(nameof(TestPushBytes));

            await _dbq.Clear(queueName);

            var data = Utils.GenerateData();

            await _dbq.Push(queueName, data);

            await using var ack = await _dbq.Pop<byte[]>(queueName);
            await ack.Commit();

            CollectionAssert.AreEqual(data, ack.Data);
        }

        [TestMethod]
        public async Task TestPushStream()
        {
            var queueName = GetQueueName(nameof(TestPushStream));

            await _dbq.Clear(queueName);

            var data = Utils.GenerateData();
            using (var stream = new MemoryStream(data))
                await _dbq.Push(queueName, stream);

            await using var ack = await _dbq.Pop(queueName);

            var content = new List<byte[]>();

            while (await ack.Data.MoveNextAsync())
                content.Add(ack.Data.Current);

            await ack.Commit();
            var result = content.SelectMany(x => x).ToArray();

            CollectionAssert.AreEqual(data, result);
        }

        [TestMethod]
        public async Task TestPushText()
        {
            var queueName = GetQueueName(nameof(TestPushText));

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);

            await using var ack = await _dbq.Pop<string>(queueName);
            await ack.Commit();

            Assert.AreEqual(text, ack.Data);
        }

        [TestMethod]
        public async Task TestPushObject()
        {
            var queueName = GetQueueName(nameof(TestPushObject));

            await _dbq.Clear(queueName);

            var obj = Utils.GenerateObject();

            await _dbq.Push(queueName, obj);

            await using var ack = await _dbq.Pop<TestObject>(queueName);
            await ack.Commit();

            Assert.AreEqual(obj, ack.Data);
        }

        [TestMethod]
        public async Task TestPushManyQueues()
        {
            var queueNames = Enumerable.Range(1, 10).Select(i => GetQueueName($"{nameof(TestPushManyQueues)}_{i}"));

            foreach (var queueName in queueNames)
                await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueNames, text);

            foreach (var queueName in queueNames)
                await using (var ack = await _dbq.Pop<string>(queueName))
                {
                    await ack.Commit();
                    Assert.AreEqual(text, ack.Data);
                }
        }

        [TestMethod]
        public async Task TestPushExtensions()
        {
            var queueName = GetQueueName(nameof(TestPushExtensions));

            await _dbq.Clear(queueName);

            var items = new[] {
                DateTime.Today.AddDays(-1),
                DateTime.Today,
            };

            for(var i=0; i<items.Length;i++)
            {
                var item = items[i];
                await _dbq.Push(queueName, Utils.GenerateText(), i, item, item.AddDays(1));
                await _dbq.Push(queueName, Utils.GenerateData(), i, item, item.AddDays(1));
                await _dbq.Push(queueName, new MemoryStream(Utils.GenerateData()), i, item, item.AddDays(1));
                await _dbq.Push(queueName, Utils.GenerateObject(), i, item, item.AddDays(1));
            }

            Assert.AreEqual(4, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }

    }
}