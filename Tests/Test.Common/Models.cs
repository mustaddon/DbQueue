using System;

namespace Test.Common
{
    public class TestObject
    {
        public int Number { get; set; }
        public string Text { get; set; }
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

        public override int GetHashCode() => HashCode.Combine(Number, Text, Date);
        public override bool Equals(object obj) => GetHashCode() == (obj as TestObject)?.GetHashCode();
    }
}
