using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security;

namespace Google.Solutions.Settings.Test.Registry
{
    public static class TestDictionaryValueAccessor
    {
        [TestFixture]
        public class TestBoolValueAccessor : TestDictionaryValueAccessorBase<bool>
        {
            protected override bool SampleData => true;
        }

        [TestFixture]
        public class TestIntValueAccessor : TestDictionaryValueAccessorBase<int>
        {
            protected override int SampleData => -1;
        }

        [TestFixture]
        public class TestLongValueAccessor : TestDictionaryValueAccessorBase<long>
        {
            protected override long SampleData => -1;
        }

        [TestFixture]
        public class TestStringValueAccessor : TestDictionaryValueAccessorBase<string>
        {
            protected override string SampleData => "some string";
        }

        [TestFixture]
        public class TestSecureStringValueAccessor : TestDictionaryValueAccessorBase<SecureString>
        {
            protected override SecureString SampleData => null;

            [Test]
            public override void WhenValueSet_ThenTryReadReturnsTrue()
            {
                var dictionary = new Dictionary<string, string>();
                var accessor = CreateAccessor("test");
                accessor.Write(dictionary, this.SampleData);

                Assert.IsFalse(accessor.TryRead(dictionary, out var read));
            }
        }

        [TestFixture]
        public class TestEnumValueAccessor : TestDictionaryValueAccessorBase<ConsoleColor>
        {
            protected override ConsoleColor SampleData => ConsoleColor.Magenta;
        }
    }
}
