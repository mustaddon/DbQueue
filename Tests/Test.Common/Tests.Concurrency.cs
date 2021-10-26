using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestConcurrency()
        {
            var queueName = GetQueueName(nameof(TestConcurrency));

            await _dbq.Clear(queueName);

            var workers = 11;
            var datas = Enumerable.Range(0, workers * 5).Select(i => $"item-{i}").ToArray();

            foreach (var data in datas)
                await _dbq.Push(queueName, data);

            var results = new ConcurrentDictionary<string, int>();

            var tasks = Enumerable.Range(0, workers).Select(i => Task.Run(async () =>
            {
                await Task.Delay(Utils.Rnd.Next(0, 100));

                var dbq = _dbqFactory();

                await foreach (var result in dbq.PopMany<string>(queueName).WithAutoAck())
                {
                    Assert.AreEqual(i, results.GetOrAdd(result, i));
                    Assert.IsTrue(datas.Any(x => x.Equals(result)));
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.IsTrue(results.GroupBy(x => x.Value).Count() > 1);
            Assert.AreEqual(datas.Length, results.Count);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

    }
}