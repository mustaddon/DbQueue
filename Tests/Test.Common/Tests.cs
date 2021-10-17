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
        }

        readonly IDbQueue _dbq;
        static readonly string _queueName = "common_tests";


        public async Task TestPushBytes()
        {
            await _dbq.Clear(_queueName);

            var data = Utils.GenerateData();
            await _dbq.Push(_queueName, data);

            var content = new List<byte[]>();
            await foreach (var chunk in _dbq.Pop(_queueName))
                content.Add(chunk);

            var result = content.SelectMany(x => x).ToArray();

            Assert.IsTrue(result.Length == data.Length, nameof(data.Length));
            Assert.IsFalse(result.Select((x, i) => data[i] == x).Any(x => !x), nameof(data));
        }

        public async Task TestPushStream()
        {
            await _dbq.Clear(_queueName);

            var data = Utils.GenerateData();
            using var stream = new MemoryStream(data);
            await _dbq.Push(_queueName, stream);

            var content = new List<byte[]>();
            await foreach (var chunk in _dbq.Pop(_queueName))
                content.Add(chunk);

            var result = content.SelectMany(x => x).ToArray();

            Assert.IsTrue(result.Length == data.Length, nameof(data.Length));
            Assert.IsFalse(result.Select((x, i) => data[i] == x).Any(x => !x), nameof(data));
        }

        public async Task TestPushText()
        {
            await _dbq.Clear(_queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(_queueName, text);

            var result = await _dbq.Pop<string>(_queueName);

            Assert.IsTrue(text.Equals(result));
        }

        public async Task TestPushObject()
        {
            await _dbq.Clear(_queueName);

            var obj = Utils.GenerateObject();

            await _dbq.Push(_queueName, obj);

            var result = await _dbq.Pop<TestObject>(_queueName);

            Assert.IsTrue(obj.Equals(result));
        }

        public async Task TestPushManyQueues()
        {
            var queueNames = Enumerable.Range(1, 10).Select(i => $"{_queueName}_{i}");

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

        public async Task TestPopMany()
        {
            await _dbq.Clear(_queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(_queueName, data);

            var index = 0;
            await foreach (var enumerator in _dbq.PopMany(_queueName))
            {
                var content = new List<byte[]>();
                while (await enumerator.MoveNextAsync())
                    content.Add(enumerator.Current);

                var result = content.SelectMany(x => x).ToArray();

                Assert.IsTrue(result.Length == datas[index].Length, nameof(result.Length));
                Assert.IsFalse(result.Select((x, i) => datas[index][i] == x).Any(x => !x), "data");

                index++;
            }

            Assert.IsTrue(await _dbq.Count(_queueName) == 0, nameof(_dbq.Count));
        }

        public async Task TestPopManyGeneric()
        {
            await _dbq.Clear(_queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(_queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PopMany<string>(_queueName))
            {
                Assert.IsTrue(texts[index].Equals(result));
                index++;
            }

            Assert.IsTrue(await _dbq.Count(_queueName) == 0, nameof(_dbq.Count));
        }

        public async Task TestPeekMany()
        {
            await _dbq.Clear(_queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(_queueName, data);

            var index = 0;
            await foreach (var enumerator in _dbq.PeekMany(_queueName))
            {
                var content = new List<byte[]>();
                while (await enumerator.MoveNextAsync())
                    content.Add(enumerator.Current);

                var result = content.SelectMany(x => x).ToArray();

                Assert.IsTrue(result.Length == datas[index].Length, nameof(result.Length));
                Assert.IsFalse(result.Select((x, i) => datas[index][i] == x).Any(x => !x), "data");

                index++;
            }

            Assert.IsTrue(await _dbq.Count(_queueName) == datas.Length, nameof(_dbq.Count));
        }

        public async Task TestPeekManyGeneric()
        {
            await _dbq.Clear(_queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(_queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PeekMany<string>(_queueName))
            {
                Assert.IsTrue(texts[index].Equals(result));
                index++;
            }

            Assert.IsTrue(await _dbq.Count(_queueName) == texts.Length, nameof(_dbq.Count));
        }

        public async Task TestCount()
        {
            await _dbq.Clear(_queueName);
            Assert.IsTrue(await _dbq.Count(_queueName) == 0, nameof(_dbq.Count));

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(_queueName, text);

            Assert.IsTrue(await _dbq.Count(_queueName) == texts.Length, nameof(_dbq.Count));
        }

    }
}