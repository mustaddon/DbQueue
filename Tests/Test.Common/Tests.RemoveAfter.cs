﻿using DbQueue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Test.Common
{
    public partial class Tests
    {
        [TestMethod]
        public async Task TestRemoveAfterPeek()
        {
            var queueName = GetQueueName(nameof(TestRemoveAfterPeek));

            await _dbq.Clear(queueName);

            var text = "short"; // Utils.GenerateText();
            var delay = 2000;

            var items = new[] {
                Tuple.Create(string.Empty, delay),
                Tuple.Create(text, 2*delay)
            };

            foreach (var x in _dbq.StackMode ? items.Reverse() : items)
                await _dbq.Push(queueName, x.Item1,
                    removeAfter: DateTime.Now.AddMilliseconds(x.Item2));

            Assert.AreEqual(2, await _dbq.Count(queueName));

            await Task.Delay(delay + 1000);

            Assert.AreEqual(1, await _dbq.Count(queueName));

            var result = await _dbq.Peek<string>(queueName);

            Assert.AreEqual(text, result);

            await Task.Delay(delay);

            result = await _dbq.Peek<string>(queueName);

            Assert.AreEqual(null, result);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }

        [TestMethod]
        public async Task TestRemoveAfterPop()
        {
            var queueName = GetQueueName(nameof(TestRemoveAfterPop));

            await _dbq.Clear(queueName);

            var text = "short"; // Utils.GenerateText();
            var delay = 2000;

            var items = new[] {
                Tuple.Create(string.Empty, delay),
                Tuple.Create(text, 2*delay)
            };

            foreach (var x in _dbq.StackMode ? items.Reverse() : items)
                await _dbq.Push(queueName, x.Item1,
                    removeAfter: DateTime.Now.AddMilliseconds(x.Item2));

            Assert.AreEqual(2, await _dbq.Count(queueName));

            await Task.Delay(delay + 1000);

            Assert.AreEqual(1, await _dbq.Count(queueName));

            var result = await _dbq.Pop<string>(queueName).WithAutoAck();

            Assert.AreEqual(text, result);
            Assert.AreEqual(0, await _dbq.Count(queueName));
        }
    }
}