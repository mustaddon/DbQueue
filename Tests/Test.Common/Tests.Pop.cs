using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {

        [TestMethod]
        public async Task TestPop()
        {
            var queueName = GetQueueName(nameof(TestPop));

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);

            await using var ack = await _dbq.Pop<string>(queueName);
            await ack.Commit();

            Assert.AreEqual(text, ack.Data);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopStream()
        {
            var queueName = GetQueueName(nameof(TestPopStream));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            await using (var ack = await _dbq.Pop<Stream>(queueName))
            {
                // unlock test
            }

            await using (var ack = await _dbq.Pop<Stream>(queueName))
            using (var reader = new StreamReader(ack.Data))
            {
                var result = await reader.ReadToEndAsync();
                await ack.Commit();
                Assert.AreEqual(text, result);
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopManyBytes()
        {
            var queueName = GetQueueName(nameof(TestPopManyBytes));

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var index = 0;
            await foreach (var ack in _dbq.PopMany<byte[]>(queueName))
            {
                var data = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                var result = ack.Data;
                await ack.Commit();
                CollectionAssert.AreEqual(data, ack.Data);
                index++;
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopManyStream()
        {
            var queueName = GetQueueName(nameof(TestPopManyStream));

            await _dbq.Clear(queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var ack in _dbq.PopMany<Stream>(queueName))
                using (var reader = new StreamReader(ack.Data))
                {
                    var text = texts[_dbq.StackMode ? texts.Length - index - 1 : index];
                    var result = await reader.ReadToEndAsync();
                    await ack.Commit();
                    Assert.AreEqual(text, result);
                    index++;
                }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopManyText()
        {
            var queueName = GetQueueName(nameof(TestPopManyText));

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in datas)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var ack in _dbq.PopMany<string>(queueName))
            {
                var data = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                await ack.Commit();
                Assert.AreEqual(data, ack.Data);
                index++;
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopManyGeneric()
        {
            var queueName = GetQueueName(nameof(TestPopManyGeneric));

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateObject()).ToArray();

            foreach (var obj in datas)
                await _dbq.Push(queueName, obj);

            var index = 0;
            await foreach (var ack in _dbq.PopMany<TestObject>(queueName))
            {
                var data = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                await ack.Commit();
                Assert.AreEqual(data, ack.Data);
                index++;
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopBreak()
        {
            var queueName = GetQueueName(nameof(TestPopBreak));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            await using (var ack1 = await _dbq.Pop(queueName))
            {
            }

            Assert.AreEqual(1, await _dbq.Count(queueName));

            await using (var ack2 = await _dbq.Pop<string>(queueName))
            {
                Assert.AreEqual(text, ack2.Data);
                await ack2.Commit();
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestPopThrow()
        {
            var queueName = GetQueueName(nameof(TestPopThrow));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            try
            {
                await using (var ack1 = await _dbq.Pop(queueName))
                    throw new Exception("test");
            }
            catch { }

            Assert.AreEqual(1, await _dbq.Count(queueName));

            await using (var ack2 = await _dbq.Pop<string>(queueName))
            {
                Assert.AreEqual(text, ack2.Data);
                await ack2.Commit();
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

    }
}