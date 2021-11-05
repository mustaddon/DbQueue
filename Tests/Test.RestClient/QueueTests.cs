using DbQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Common;

namespace Test.RestClient
{
    [TestClass()]
    public class QueueTests : CommonTests
    {
        public QueueTests() : base(() => App.Instance.Value.Services.GetService<IDbQueue>() as IDbqBoth)
        {
        }
    }
}