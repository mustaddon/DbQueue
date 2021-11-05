using DbQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.RestClient
{
    [TestClass()]
    public class StackTests : CommonTests
    {
        public StackTests() : base(() => App.Instance.Value.Services.GetService<IDbStack>() as IDbqBoth)
        {
        }
    }
}