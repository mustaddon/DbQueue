using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        public Tests(Func<IDbqBoth> dbqFactory)
        {
            _dbqFactory = dbqFactory;
            _dbq = dbqFactory();
        }

        readonly Func<IDbqBoth> _dbqFactory;
        readonly IDbqBoth _dbq;

        protected string GetQueueName(string val = null) => $"test_{(_dbq.StackMode ? "stack" : "queue")}_{val ?? default}";



        [TestMethod]
        public async Task TestCount()
        {
            var queueName = GetQueueName(nameof(TestCount));
            await _dbq.Clear(queueName);
            Assert.AreEqual(0, await _dbq.Count(queueName));

            var texts = Enumerable.Range(1, 10).Select(i => Utils.GenerateText()).ToArray();

            foreach (var text in texts)
                await _dbq.Push(queueName, text);

            Assert.AreEqual(texts.Length, await _dbq.Count(queueName));
            await _dbq.Clear(queueName);
        }
    }
}