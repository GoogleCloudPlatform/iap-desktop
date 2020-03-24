using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Windows;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestSettingsEditor : WindowTestFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";

        private IServiceProvider serviceProvider;
        private IMainForm mainForm;

        [SetUp]
        public void SetUp()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var registry = new ServiceRegistry();
            registry.AddSingleton(new InventorySettingsRepository(hkcu.CreateSubKey(TestKeyPath)));

            var mainForm = new MockMainForm();
            registry.AddSingleton<IMainForm>(mainForm);
            registry.AddSingleton<IEventService>(new EventService(mainForm));

            this.mainForm = mainForm;
            this.serviceProvider = registry;

            mainForm.Show();
        }

        [TearDown]
        public void TearDown()
        {
            this.mainForm.Close();
        }

        [Test]
        public void WhenObjectHasNoBrowsableProperties_ThenNoPropertiesShown()
        {
            var settings = new EmptyMockSettingsObject();

            var window = new SettingsEditorWindow(this.serviceProvider);
            window.ShowWindow(settings);

            var grid = window.GetChild<PropertyGrid>("propertyGrid");
            Assert.IsNull(grid.SelectedGridItem.Parent);
        }

        [Test]
        public void WhenObjectHasBrowsableProperties_ThenPropertiyIsShown()
        {
            var settings = new MockSettingsObject();

            var window = new SettingsEditorWindow(this.serviceProvider);
            window.ShowWindow(settings);

            var grid = window.GetChild<PropertyGrid>("propertyGrid");
            Assert.AreEqual(1, grid.SelectedGridItem.Parent.GridItems.Count);
        }

        class EmptyMockSettingsObject : ISettingsObject
        {
            public int SaveChangesCalls { get; private set; } = 0;

            public void SaveChanges()
            {
                this.SaveChangesCalls++;
            }
        }

        class MockSettingsObject : ISettingsObject
        {
            public int SaveChangesCalls { get; private set; } = 0;

            public void SaveChanges()
            {
                this.SaveChangesCalls++;
            }

            [BrowsableSetting]
            [Browsable(true)]
            public string SampleProperty { get; set; }
        }
    }
}
