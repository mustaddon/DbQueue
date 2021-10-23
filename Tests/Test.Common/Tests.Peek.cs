using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestPeek()
        {
            var queueName = GetQueueName(nameof(TestPeek));

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);

            var result = await _dbq.Peek<string>(queueName);

            Assert.AreEqual(text, result);
            Assert.AreEqual(1, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }

        [TestMethod]
        public async Task TestPeekManyBytes()
        {
            var queueName = GetQueueName(nameof(TestPeekManyBytes));

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateData()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var index = 0;
            await foreach (var result in _dbq.PeekMany<byte[]>(queueName))
            {
                var data = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                CollectionAssert.AreEqual(data, result);
                index++;
            }

            Assert.AreEqual(datas.Length, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }

        [TestMethod]
        public async Task TestPeekManyStream()
        {
            var queueName = GetQueueName(nameof(TestPeekManyStream));

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var index = 0;
            await foreach (var stream in _dbq.PeekMany<Stream>(queueName))
                using (var reader = new StreamReader(stream))
                {
                    var result = await reader.ReadToEndAsync();
                    var text = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                    Assert.AreEqual(text, result);
                    index++;
                }

            Assert.AreEqual(datas.Length, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }

        [TestMethod]
        public async Task TestPeekManyText()
        {
            var queueName = GetQueueName(nameof(TestPeekManyText));

            await _dbq.Clear(queueName);

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PeekMany<string>(queueName))
            {
                var text = texts[_dbq.StackMode ? texts.Length - index - 1 : index];
                Assert.AreEqual(text, result);
                index++;
            }

            Assert.AreEqual(texts.Length, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }

        [TestMethod]
        public async Task TestPeekManyGeneric()
        {
            var queueName = GetQueueName(nameof(TestPeekManyGeneric));

            await _dbq.Clear(queueName);

            var datas = Enumerable.Range(1, 10).Select(i => Utils.GenerateObject()).ToArray();

            foreach (var text in datas)
                await _dbq.Push(queueName, text);

            var index = 0;
            await foreach (var result in _dbq.PeekMany<TestObject>(queueName))
            {
                var data = datas[_dbq.StackMode ? datas.Length - index - 1 : index];
                Assert.AreEqual(data, result);
                index++;
            }

            Assert.AreEqual(datas.Length, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }

    }
}