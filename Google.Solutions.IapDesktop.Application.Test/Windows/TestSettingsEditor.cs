using Google.Solutions.IapDesktop.Application.SettingsEditor;
using NUnit.Framework;
using System.ComponentModel;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    public class TestSettingsEditor : WindowTestFixtureBase
    {

        [Test]
        public void WhenObjectHasNoBrowsableProperties_ThenNoPropertiesShown()
        {
            var settings = new EmptyMockSettingsObject();

            var window = new SettingsEditorWindow(this.serviceProvider);
            window.ShowWindow(settings);
            PumpWindowMessages();

            var grid = window.GetChild<PropertyGrid>("propertyGrid");
            Assert.IsNull(grid.SelectedGridItem.Parent);
        }

        [Test]
        public void WhenObjectHasBrowsableProperties_ThenPropertiyIsShown()
        {
            var settings = new MockSettingsObject();

            var window = new SettingsEditorWindow(this.serviceProvider);
            window.ShowWindow(settings);
            PumpWindowMessages();

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
