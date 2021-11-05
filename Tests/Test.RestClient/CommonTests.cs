using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Test.Common;

namespace Test.RestClient
{
    public class CommonTests : Common.Tests
    {
        public CommonTests(Func<IDbqBoth> dbqFactory) : base(dbqFactory)
        {
            _dbq = dbqFactory();
        }

        readonly IDbqBoth _dbq;


        [TestMethod]
        public async Task TestUrlEncode()
        {
            var queueNames = Enumerable.Range(1, 10)
                .Select(i => GetQueueName($"{nameof(TestUrlEncode)}_{i}/?&#/test,тест"));

            foreach (var queueName in queueNames)
                await _dbq.Clear(queueName);

            var text = Utils.GenerateText();

            await _dbq.Push(queueNames, text);

            foreach (var queueName in queueNames)
            {
                Assert.AreEqual(1, await _dbq.Count(queueName));

                var result = await _dbq.Pop<string>(queueName).WithAutoAck();
                Assert.AreEqual(text, result);

                Assert.AreEqual(0, await _dbq.Count(queueName));
            }
        }
    }
}
