using Google.Solutions.IapDesktop.Application.Settings;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Settings
{
    [TestFixture]
    public class TestRegistryStringSetting
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser, 
            RegistryView.Default);

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        //---------------------------------------------------------------------
        // Load.
        //---------------------------------------------------------------------

        [Test]
        public void WhenRegistryValueDoesNotExist_ThenFromKeyUsesDefaults()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("blue", setting.Value);
                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenRegistryValueExists_ThenFromKeyUsesValue()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", "red");

                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                Assert.AreEqual("test", setting.Key);
                Assert.AreEqual("title", setting.Title);
                Assert.AreEqual("description", setting.Description);
                Assert.AreEqual("category", setting.Category);
                Assert.AreEqual("red", setting.Value);
                Assert.IsFalse(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        //---------------------------------------------------------------------
        // Save.
        //---------------------------------------------------------------------

        [Test]
        public void WhenSettingIsNonNull_ThenSaveUpdatesRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "green";
                setting.Save(key);

                Assert.AreEqual("green", key.GetValue("test"));
            }
        }

        [Test]
        public void WhenSettingIsNull_ThenSaveResetsRegistry()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                key.SetValue("test", "red");

                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = null;
                setting.Save(key);

                Assert.IsNull(key.GetValue("test"));
            }
        }

        //---------------------------------------------------------------------
        // Get/set value.
        //---------------------------------------------------------------------

        [Test]
        public void WhenValueEqualsDefault_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "blue";

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueAndDefaultAreNull_ThenSetValueSucceedsAndSettingIsNotDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    null,
                    key,
                    _ => true);

                setting.Value = null;

                Assert.IsTrue(setting.IsDefault);
                Assert.IsFalse(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueDiffersFromDefault_ThenSetValueSucceedsAndSettingIsDirty()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                setting.Value = "yellow";

                Assert.IsFalse(setting.IsDefault);
                Assert.IsTrue(setting.IsDirty);
            }
        }

        [Test]
        public void WhenValueIsOfWrongType_ThenSetValueRaisesInvalidCastException()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var setting = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "blue",
                    key,
                    _ => true);

                Assert.Throws<InvalidCastException>(() => setting.Value = 1);
            }
        }

        [Test]
        public void WhenValueIsInvalid_ThenSetValueRaisesArgumentOutOfRangeException()
        {
            Assert.Inconclusive();
        }

        [Test]
        public void WhenValueIsUnparsable_ThenSetValueRaisesFormatException()
        {
            Assert.Inconclusive();
        }

        //---------------------------------------------------------------------
        // Overlay.
        //---------------------------------------------------------------------

        [Test]
        public void WhenParentIsDefault_ThenOverlayByAppliesParentValueUsedAsNewDefault()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);

                Assert.AreEqual("black", effective.DefaultValue);
                Assert.AreEqual("black", effective.Value);
                Assert.IsTrue(effective.IsDefault);
            }

        }

        [Test]
        public void WhenParentIsNonDefault_ThenOverlayByAppliesParentValueUsedAsNewDefault()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                parent.Value = "red";
                Assert.IsFalse(parent.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);


                Assert.AreEqual("red", effective.Value);
                Assert.AreEqual("red", effective.DefaultValue);
                Assert.IsTrue(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildIsNonDefault_ThenOverlayByAppliesParentValueUsedAsNewDefault()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(parent.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                child.Value = null;
                Assert.IsFalse(child.IsDefault);

                var effective = parent.OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, child);


                Assert.IsNull(effective.Value);
                Assert.AreEqual("black", effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }

        [Test]
        public void WhenParentIsNonDefaultAndChildSetToOriginalDefault_ThenIsDefaultReturnsFalse()
        {
            using (var key = this.hkcu.CreateSubKey(TestKeyPath))
            {
                var parent = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                parent.Value = "red";
                Assert.IsFalse(parent.IsDefault);

                var intermediate = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);
                Assert.IsTrue(intermediate.IsDefault);

                var child = RegistryStringSetting.FromKey(
                    "test",
                    "title",
                    "description",
                    "category",
                    "black",
                    key,
                    _ => true);

                var effective = parent
                    .OverlayBy(intermediate)
                    .OverlayBy(child);
                Assert.AreNotSame(effective, parent);
                Assert.AreNotSame(effective, intermediate);
                Assert.AreNotSame(effective, child);

                effective.Value = "black";

                Assert.AreEqual("black", effective.Value);
                Assert.AreEqual("red", effective.DefaultValue);
                Assert.IsFalse(effective.IsDefault);
            }
        }
    }
}
