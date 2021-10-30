using DbQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Common;

namespace Test.EFCore
{
    [TestClass()]
    public class QueueTests : Tests
    {
        public QueueTests() : base(() => App.Instance.Value.Services.GetService<IDbQueue>() as IDbqBoth)
        {
        }
    }
}