using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Test.Common
{
    public class Utils
    {
        public static Random Rnd = new();

        public static string GenerateText(string line = "text текст", int? count = null)
        {
            return string.Join("\n", Enumerable.Range(0, count ?? Rnd.Next(1, 5000)).Select(i => $"{i + 1}: {line}"));
        }

        public static byte[] GenerateData(string line = "text текст", int? count = null)
        {
            return Encoding.UTF8.GetBytes(GenerateText(line, count));
        }

        public static TestObject GenerateObject(string line = "text текст", int? count = null)
        {
            return new TestObject()
            {
                Number = Rnd.Next(),
                Text = GenerateText(line, count),
            };
        }
    }
}
