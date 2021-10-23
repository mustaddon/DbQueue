using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestClearType()
        {
            var queueName = GetQueueName(nameof(TestClearType));

            await _dbq.Clear(queueName);

            var text = Utils.GenerateText();
            await _dbq.Push(queueName, text);
            await _dbq.Push(queueName, text, 1);
            await _dbq.Push(queueName, text, 2);
            await _dbq.Push(queueName, text, 2);
            await _dbq.Push(queueName, text, 3);
            await _dbq.Push(queueName, text, 4);

            Assert.AreEqual(6, await _dbq.Count(queueName));

            await _dbq.Clear(queueName, new[] { 3, 4 });

            Assert.AreEqual(4, await _dbq.Count(queueName));

            await _dbq.Clear(queueName, 2);

            Assert.AreEqual(2, await _dbq.Count(queueName));

            await _dbq.Clear(queueName);

            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

    }
}