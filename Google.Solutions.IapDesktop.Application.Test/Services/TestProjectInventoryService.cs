using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.Test.ServiceModel;
using Microsoft.Win32;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services
{
    [TestFixture]
    public class TestProjectInventoryService
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private ProjectInventoryService inventory = null;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            this.inventory = new ProjectInventoryService(
                new InventorySettingsRepository(baseKey),
                new MockEventService());
        }

        [Test]
        public void WhenNoProjectsAdded_ListProjectsReturnsEmptyList()
        {
            var projects = this.inventory.ListProjectsAsync().Result;

            Assert.IsFalse(projects.Any());
        }

        [Test]
        public async Task WhenProjectsAddedTwice_ListProjectsReturnsProjectOnce()
        {
            await inventory.AddProjectAsync("test-123");
            await inventory.AddProjectAsync("test-123");

            var projects = this.inventory.ListProjectsAsync().Result;

            Assert.AreEqual(1, projects.Count());
            Assert.AreEqual("test-123", projects.First().Name);
        }

        [Test]
        public async Task WhenProjectsDeleted_ListProjectsExcludesProject()
        {
            await inventory.AddProjectAsync("test-123");
            await inventory.AddProjectAsync("test-456");
            await inventory.DeleteProjectAsync("test-456");

            var projects = this.inventory.ListProjectsAsync().Result;

            Assert.AreEqual(1, projects.Count());
            Assert.AreEqual("test-123", projects.First().Name);
        }
    }
}
