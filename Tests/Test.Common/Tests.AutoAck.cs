using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestAutoAck()
        {
            var queueName = GetQueueName(nameof(TestAutoAck));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            var result = await _dbq.Pop<string>(queueName).WithAutoAck();

            Assert.AreEqual(text, result);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestAutoAckStream()
        {
            var queueName = GetQueueName(nameof(TestAutoAckStream));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            using (var stream = await _dbq.Pop<Stream>(queueName).WithAutoAck())
            {
                // unlock test
            }

            using (var stream = await _dbq.Pop<Stream>(queueName).WithAutoAck())
            using (var reader = new StreamReader(stream))
            {
                var result = await reader.ReadToEndAsync();
                Assert.AreEqual(text, result);
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestAutoAckUnlock()
        {
            var queueName = GetQueueName(nameof(TestAutoAckUnlock));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            await using (var ack1 = await _dbq.Pop(queueName).WithAutoAck())
            {
                // skip
            }

            await using (var data = await _dbq.Pop(queueName).WithAutoAck())
                while (await data.MoveNextAsync())
                {
                    // read all
                }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestAutoAckManyBytes()
        {
            var queueName = GetQueueName(nameof(TestAutoAckManyBytes));
            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var index = 0;
            await foreach (var enumerator in _dbq.PopMany(queueName).WithAutoAck())
                using (var ms = new MemoryStream())
                {
                    while (await enumerator.MoveNextAsync())
                        ms.Write(enumerator.Current);

                    var result = ms.ToArray();
                    var data = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                    CollectionAssert.AreEqual(data, result);
                    index++;
                }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestAutoAckManyText()
        {
            var queueName = GetQueueName(nameof(TestAutoAckManyText));
            await _dbq.Clear(queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PopMany<string>(queueName).WithAutoAck())
            {
                var text = texts[_dbq.StackMode ? texts.Length - index - 1 : index];
                Assert.AreEqual(text, result);
                index++;
            }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }


        [TestMethod]
        public async Task TestAutoAckManyStream()
        {
            var queueName = GetQueueName(nameof(TestAutoAckManyStream));
            await _dbq.Clear(queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var stream in _dbq.PopMany<Stream>(queueName).WithAutoAck())
                using (var reader = new StreamReader(stream))
                {
                    var result = await reader.ReadToEndAsync();
                    var text = texts[_dbq.StackMode ? texts.Length - index - 1 : index];
                    Assert.AreEqual(text, result);
                    index++;
                }

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestAutoAckBreak()
        {
            var queueName = GetQueueName(nameof(TestAutoAckBreak));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            await using (var ack1 = await _dbq.Pop(queueName).WithAutoAck())
            {
                // break
            }

            Assert.AreEqual(1, await _dbq.Count(queueName));

            var result = await _dbq.Pop<string>(queueName).WithAutoAck();

            Assert.AreEqual(text, result);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestAutoAckThrow()
        {
            var queueName = GetQueueName(nameof(TestAutoAckThrow));
            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueName, text);

            try
            {
                await using (var ack1 = await _dbq.Pop(queueName).WithAutoAck())
                    throw new Exception("test");
            }
            catch
            {

            }

            Assert.AreEqual(1, await _dbq.Count(queueName));

            var result = await _dbq.Pop<string>(queueName).WithAutoAck();

            Assert.AreEqual(text, result);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

    }
}