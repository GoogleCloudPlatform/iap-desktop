using Google.Solutions.CloudIap.IapDesktop.Application.Settings;
using Microsoft.Win32;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.CloudIap.IapDesktop.Application.Test.Settings
{
    [TestFixture]
    public class TestInventorySettingsRepository
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);


        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
        }

        [Test]
        public void WhenBaseKeyIsEmpty_SettingsAreEmpty()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var settings = repository.GetSettings();

            Assert.IsNull(settings.Username);
            Assert.IsNull(settings.Password);
            Assert.IsNull(settings.Domain);
        }

        [Test]
        public void WhenSettingsSaved_GetSettingsReturnsData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new InventorySettings()
            {
                Username = "user"
            };

            repository.SetSettings(originalSettings);

            var settings = repository.GetSettings();

            Assert.AreEqual(originalSettings.Username, settings.Username);
        }


        //---------------------------------------------------------------------
        // Projects.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBaseKeyDoesNotExist_ListProjectSettingsReturnsEmptyList()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var projects = repository.ListProjectSettings();

            Assert.IsFalse(projects.Any());
        }

        [Test]
        public void WhenProjectSettingsSaved_ProjectReturnedInListProjects()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);

            var projects = repository.ListProjectSettings();

            Assert.AreEqual(1, projects.Count());
        }

        [Test]
        public void WhenProjectSettingsSaved_GetProjectSettingsReturnsData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(originalSettings.ProjectId);

            Assert.AreEqual(originalSettings.ProjectId, settings.ProjectId);
            Assert.AreEqual(originalSettings.Username, settings.Username);
        }

        [Test]
        public void WhenProjectSettingsSavedTwice_GetProjectSettingsReturnsLatestData()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);

            originalSettings.Username = "new-user";
            repository.SetProjectSettings(originalSettings);

            var settings = repository.GetProjectSettings(originalSettings.ProjectId);

            Assert.AreEqual(originalSettings.ProjectId, settings.ProjectId);
            Assert.AreEqual(originalSettings.Username, settings.Username);
        }

        [Test]
        public void WhenProjectSettingsDeleted_GetProjectSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            var originalSettings = new ProjectSettings()
            {
                ProjectId = "pro-1",
                Username = "user"
            };

            repository.SetProjectSettings(originalSettings);
            repository.DeleteProjectSettings(originalSettings.ProjectId);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings(originalSettings.ProjectId);
            });
        }

        [Test]
        public void WhenIdDoesNotExist_GetProjectSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetProjectSettings("nonexisting");
            });
        }


        //---------------------------------------------------------------------
        // Regions.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_GetRegionSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetRegionSettings("nonexisting-project", "region-id");
            });
        }

        [Test]
        public void WhenRegionIdDoesNotExist_GetRegionSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetRegionSettings("pro-1", "nonexisting-region");
            });
        }

        [Test]
        public void WhenSetValidRegionSettings_GetRegionSettingsReturnSameValues()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetRegionSettings("pro-1", new RegionSettings()
            {
                RegionId = "region-1",
                Username = "user-1"
            });

            Assert.AreEqual("user-1", repository.GetRegionSettings("pro-1", "region-1").Username);
        }

        [Test]
        public void WhenProjectSettingsDeleted_RegionSettingsAreDeletedToo()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetRegionSettings("pro-1", new RegionSettings()
            {
                RegionId = "region-1",
                Username = "user-1"
            });
            repository.DeleteProjectSettings("pro-1");

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetRegionSettings("pro-1", "region-1");
            });
        }


        //---------------------------------------------------------------------
        // Zones.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_GetZoneSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings("nonexisting-project", "zone-id");
            });
        }

        [Test]
        public void WhenZoneIdDoesNotExist_GetZoneSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings("pro-1", "nonexisting-zone");
            });
        }

        [Test]
        public void WhenSetValidZoneSettings_GetZoneSettingsReturnSameValues()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetZoneSettings("pro-1", new ZoneSettings()
            {
                ZoneId = "zone-1",
                Username = "user-1"
            });

            Assert.AreEqual("user-1", repository.GetZoneSettings("pro-1", "zone-1").Username);
        }

        [Test]
        public void WhenProjectSettingsDeleted_ZoneSettingsAreDeletedToo()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetZoneSettings("pro-1", new ZoneSettings()
            {
                ZoneId = "zone-1",
                Username = "user-1"
            });
            repository.DeleteProjectSettings("pro-1");

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetZoneSettings("pro-1", "zone-1");
            });
        }

        //---------------------------------------------------------------------
        // VirtualMachines.
        //---------------------------------------------------------------------

        [Test]
        public void WhenProjectIdDoesNotExist_GetVirtualMachineSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetVirtualMachineSettings("nonexisting-project", "vm-id");
            });
        }

        [Test]
        public void WhenVirtualMachineIdDoesNotExist_GetVirtualMachineSettingsThrowsKeyNotFoundException()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetVirtualMachineSettings("pro-1", "nonexisting-vm");
            });
        }

        [Test]
        public void WhenSetValidVirtualMachineSettings_GetVirtualMachineSettingsReturnSameValues()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetVirtualMachineSettings("pro-1", new VirtualMachineSettings()
            {
                InstanceName = "vm-1",
                Username = "user-1"
            });

            Assert.AreEqual("user-1", repository.GetVirtualMachineSettings("pro-1", "vm-1").Username);
        }

        [Test]
        public void WhenProjectSettingsDeleted_VirtualMachineSettingsAreDeletedToo()
        {
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new InventorySettingsRepository(baseKey);

            repository.SetProjectSettings(new ProjectSettings()
            {
                ProjectId = "pro-1"
            });
            repository.SetVirtualMachineSettings("pro-1", new VirtualMachineSettings()
            {
                InstanceName = "vm-1",
                Username = "user-1"
            });
            repository.DeleteProjectSettings("pro-1");

            Assert.Throws<KeyNotFoundException>(() =>
            {
                repository.GetVirtualMachineSettings("pro-1", "vm-1");
            });
        }
    }
}
