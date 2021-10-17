using DbQueue;
using DbQueue.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public sealed class Tests
    {
        public Tests(IServiceProvider serviceProvider)
        {
            _dbq = serviceProvider.GetRequiredService<IDbQueue>();
            _dbqSettings = serviceProvider.GetRequiredService<DbqSettings>();
        }

        readonly IDbQueue _dbq;
        readonly DbqSettings _dbqSettings;


        public async Task TestPushBytes()
        {
            var queueName = nameof(TestPushBytes);

            await _dbq.Clear(queueName);

            var data = Utils.GenerateData();

            await _dbq.Push(queueName, data);

            var result = await _dbq.Pop<byte[]>(queueName);

            Assert.IsTrue(result.Length == data.Length, nameof(data.Length));
            Assert.IsFalse(result.Select((x, i) => data[i] == x).Any(x => !x), nameof(data));
        }

        public async Task TestPushStream()
        {
            var queueName = nameof(TestPushStream);

            await _dbq.Clear(queueName);

            var data = Utils.GenerateData();
            using var stream = new MemoryStream(data);
            await _dbq.Push(queueName, stream);

            var enumerator = await _dbq.Pop(queueName);

            var content = new List<byte[]>();
            while (await enumerator.MoveNextAsync())
                content.Add(enumerator.Current);

            var result = content.SelectMany(x => x).ToArray();

            Assert.IsTrue(result.Length == data.Length, nameof(data.Length));
            Assert.IsFalse(result.Select((x, i) => data[i] == x).Any(x => !x), nameof(data));
        }

        public async Task TestPushText()
        {
            var queueName = nameof(TestPushText);

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);

            var result = await _dbq.Pop<string>(queueName);

            Assert.IsTrue(text.Equals(result));
        }

        public async Task TestPushObject()
        {
            var queueName = nameof(TestPushObject);

            await _dbq.Clear(queueName);

            var obj = Utils.GenerateObject();

            await _dbq.Push(queueName, obj);

            var result = await _dbq.Pop<TestObject>(queueName);

            Assert.IsTrue(obj.Equals(result));
        }

        public async Task TestPushManyQueues()
        {
            var queueNames = Enumerable.Range(1, 10).Select(i => $"{nameof(TestPushManyQueues)}_{i}");

            foreach (var queueName in queueNames)
                await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueNames, text);

            foreach (var queueName in queueNames)
            {
                var result = await _dbq.Pop<string>(queueName);
                Assert.IsTrue(text.Equals(result));
            }
        }

        public async Task TestPop()
        {
            var queueName = nameof(TestPop);

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);

            var result = await _dbq.Pop<string>(queueName);

            Assert.IsTrue(text.Equals(result));
            Assert.IsTrue(await _dbq.Count(queueName) == 0, nameof(_dbq.Count));
        }

        public async Task TestPeek()
        {
            var queueName = nameof(TestPeek);

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);

            var result = await _dbq.Peek<string>(queueName);

            Assert.IsTrue(text.Equals(result));
            Assert.IsTrue(await _dbq.Count(queueName) == 1, nameof(_dbq.Count));
        }

        public async Task TestPopMany()
        {
            var queueName = nameof(TestPopMany);

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var index = 0;
            await foreach (var result in _dbq.PopMany<byte[]>(queueName))
            {
                var data = datas[_dbqSettings.StackMode ? datas.Length - index - 1 : index];
                Assert.IsTrue(result.Length == data.Length, nameof(result.Length));
                Assert.IsFalse(result.Select((x, i) => data[i] == x).Any(x => !x), nameof(result));
                index++;
            }

            Assert.IsTrue(await _dbq.Count(queueName) == 0, nameof(_dbq.Count));
        }

        public async Task TestPopManyGeneric()
        {
            var queueName = nameof(TestPopManyGeneric);

            await _dbq.Clear(queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PopMany<string>(queueName))
            {
                var text = texts[_dbqSettings.StackMode ? texts.Length - index - 1 : index];
                Assert.IsTrue(text.Equals(result));
                index++;
            }

            Assert.IsTrue(await _dbq.Count(queueName) == 0, nameof(_dbq.Count));
        }

        public async Task TestPeekMany()
        {
            var queueName = nameof(TestPeekMany);

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var index = 0;
            await foreach (var result in _dbq.PeekMany<byte[]>(queueName))
            {
                var data = datas[_dbqSettings.StackMode ? datas.Length - index - 1 : index];
                Assert.IsTrue(result.Length == data.Length, nameof(result.Length));
                Assert.IsFalse(result.Select((x, i) => data[i] == x).Any(x => !x), nameof(result));
                index++;
            }

            Assert.IsTrue(await _dbq.Count(queueName) == datas.Length, nameof(_dbq.Count));
        }

        public async Task TestPeekManyGeneric()
        {
            var queueName = nameof(TestPeekManyGeneric);

            await _dbq.Clear(queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PeekMany<string>(queueName))
            {
                var text = texts[_dbqSettings.StackMode ? texts.Length - index - 1 : index];
                Assert.IsTrue(text.Equals(result));
                index++;
            }

            Assert.IsTrue(await _dbq.Count(queueName) == texts.Length, nameof(_dbq.Count));
        }

        public async Task TestCount()
        {
            var queueName = nameof(TestCount);
            await _dbq.Clear(queueName);
            Assert.IsTrue(await _dbq.Count(queueName) == 0, nameof(_dbq.Count));

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            Assert.IsTrue(await _dbq.Count(queueName) == texts.Length, nameof(_dbq.Count));
        }

    }
}