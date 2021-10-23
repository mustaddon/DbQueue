using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestAvailableAfterPeek()
        {
            var queueName = GetQueueName(nameof(TestAvailableAfterPeek));

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            var delay = 2000;

            await _dbq.Push(queueName, text,
                availableAfter: DateTime.Now.AddMilliseconds(delay));

            var result = await _dbq.Peek<string>(queueName);

            Assert.AreEqual(null, result);

            await Task.Delay(delay + 1);

            result = await _dbq.Peek<string>(queueName);

            Assert.AreEqual(text, result);

            await _dbq.Clear(queueName);
        }

        [TestMethod]
        public async Task TestAvailableAfterPop()
        {
            var queueName = GetQueueName(nameof(TestAvailableAfterPop));

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            var delay = 2000;

            await _dbq.Push(queueName, text,
                availableAfter: DateTime.Now.AddMilliseconds(delay));

            var result = await _dbq.Pop<string>(queueName).WithAutoAck();

            Assert.AreEqual(null, result);

            await Task.Delay(delay + 1);

            result = await _dbq.Pop<string>(queueName).WithAutoAck();

            Assert.AreEqual(text, result);
        }
    }
}