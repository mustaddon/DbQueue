using DbQueue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Common;

namespace Test.EFCore
{
    [TestClass()]
    public class StackTests : Tests
    {
        public StackTests() : base(() => App.Instance.Value.Services.GetService<IDbStack>() as Dbq)
        {
        }
    }
}