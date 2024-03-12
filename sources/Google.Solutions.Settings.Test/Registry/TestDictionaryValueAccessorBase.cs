using Google.Solutions.Settings.Registry;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Solutions.Settings.Test.Registry
{
    [TestFixture]
    public abstract class TestDictionaryValueAccessorBase<T>
    {
        private protected DictionaryValueAccessor<T> CreateAccessor(string valueName)
        {
            return DictionaryValueAccessor.Create<T>(valueName);
        }

        protected abstract T SampleData { get; }

        //---------------------------------------------------------------------
        // Read.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueNotSet_ThenTryReadReturnsFalse()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");

            Assert.IsFalse(accessor.TryRead(dictionary, out var _));
        }

        [Test]
        public virtual void WhenValueSet_ThenTryReadReturnsTrue()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");
            accessor.Write(dictionary, this.SampleData);

            Assert.IsTrue(accessor.TryRead(dictionary, out var read));
            Assert.AreEqual(this.SampleData, read);
        }

        //---------------------------------------------------------------------
        // Delete.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueNotSet_ThenDeleteReturns()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");
         
            accessor.Delete(dictionary);
            accessor.Delete(dictionary);
        }

        [Test]
        public void WhenValueSet_ThenDeleteDeletesValue()
        {
            var dictionary = new Dictionary<string, string>();
            var accessor = CreateAccessor("test");

            accessor.Write(dictionary, this.SampleData);
            accessor.Delete(dictionary);

            Assert.IsFalse(accessor.TryRead(dictionary, out var _));
        }
    }
}
