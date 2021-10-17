using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Test.Common;

namespace Test.Mongo
{
    [TestClass()]
    public class MongoTests
    {
        public MongoTests()
        {
            _common = App.Instance.Value.Services.GetService<Tests>();
        }

        public Tests _common;

        [TestMethod]
        public Task TestPushBytes() => _common.TestPushBytes();

        [TestMethod]
        public Task TestPushStream() => _common.TestPushStream();

        [TestMethod]
        public Task TestPushText() => _common.TestPushText();

        [TestMethod]
        public Task TestPushObject() => _common.TestPushObject();

        [TestMethod]
        public Task TestPushManyQueues() => _common.TestPushManyQueues();

        [TestMethod]
        public Task TestPop() => _common.TestPop();

        [TestMethod]
        public Task TestPeek() => _common.TestPeek();

        [TestMethod]
        public Task TestPopMany() => _common.TestPopMany();

        [TestMethod]
        public Task TestPopManyGeneric() => _common.TestPopManyGeneric();

        [TestMethod]
        public Task TestPeekMany() => _common.TestPeekMany();

        [TestMethod]
        public Task TestPeekManyGeneric() => _common.TestPeekManyGeneric();

        [TestMethod]
        public Task TestCount() => _common.TestCount();
    }
}