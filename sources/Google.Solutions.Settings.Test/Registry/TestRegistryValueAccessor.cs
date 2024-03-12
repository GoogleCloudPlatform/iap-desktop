using Google.Solutions.Common.Security;
using Google.Solutions.Settings.Registry;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using static Google.Solutions.Settings.Test.Registry.TestRegistryValueAccessor.TestEnumRegistryValueAccessor;

namespace Google.Solutions.Settings.Test.Registry
{
    public static class TestRegistryValueAccessor
    {
        [TestFixture]
        public class TestBoolRegistryValueAccessor : TestRegistryValueAccessorBase<bool>
        {
            protected override bool SampleData => true;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }

        [TestFixture]
        public class TestDwordRegistryValueAccessor : TestRegistryValueAccessorBase<int>
        {
            protected override int SampleData => int.MinValue;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }

        [TestFixture]
        public class TestQwordRegistryValueAccessor : TestRegistryValueAccessorBase<long>
        {
            protected override long SampleData => long.MinValue;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }

        [TestFixture]
        public class TestStringRegistryValueAccessor : TestRegistryValueAccessorBase<string>
        {
            protected override string SampleData => "some text";

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, 1, RegistryValueKind.DWord);
            }
        }

        [TestFixture]
        public class TestSecureStringRegistryValueAccessor : TestRegistryValueAccessorBase<SecureString>
        {
            protected override SecureString SampleData
                => SecureStringExtensions.FromClearText("some text");

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, 1, RegistryValueKind.DWord);
            }

            [Test]
            public override void WhenValueSet_ThenTryReadReturnsTrue()
            {
                using (var key = CreateKey())
                {
                    var accessor = CreateAccessor("test");
                    accessor.Write(key, this.SampleData);

                    Assert.IsTrue(accessor.TryRead(key, out var read));
                    Assert.AreEqual(this.SampleData.AsClearText(), read.AsClearText());
                }
            }
        }

        [TestFixture]
        public class TestEnumRegistryValueAccessor : TestRegistryValueAccessorBase<Drink>
        {
            public enum Drink
            {
                Coffee,
                Tea,
                Water
            }

            protected override Drink SampleData => Drink.Water;

            protected override void WriteIncompatibleValue(RegistryKey key, string name)
            {
                key.SetValue(name, "some data", RegistryValueKind.String);
            }
        }
    }
}
