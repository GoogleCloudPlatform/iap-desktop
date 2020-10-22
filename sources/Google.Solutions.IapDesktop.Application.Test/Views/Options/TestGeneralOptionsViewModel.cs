using Google.Solutions.IapDesktop.Application.Services.Persistence;
using Google.Solutions.IapDesktop.Application.Services.Settings;
using Google.Solutions.IapDesktop.Application.Util;
using Google.Solutions.IapDesktop.Application.Views.Options;
using Microsoft.Win32;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Views.Options
{
    [TestFixture]
    public class TestGeneralOptionsViewModel : FixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";
        private readonly RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        private GeneralOptionsViewModel viewModel;
        private Mock<IAppProtocolRegistry> protocolRegistryMock;

        [SetUp]
        public void SetUp()
        {
            hkcu.DeleteSubKeyTree(TestKeyPath, false);
            var baseKey = hkcu.CreateSubKey(TestKeyPath);
            var repository = new ApplicationSettingsRepository(baseKey);

            this.protocolRegistryMock = new Mock<IAppProtocolRegistry>();
            this.viewModel = new GeneralOptionsViewModel(
                repository,
                this.protocolRegistryMock.Object);
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUpdateCheckChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsUpdateCheckEnabled = !viewModel.IsUpdateCheckEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        //---------------------------------------------------------------------
        // Update check.
        //---------------------------------------------------------------------

        [Test]
        public void WhenBrowserIntegrationChanged_ThenIsDirtyIsTrueUntilApplied()
        {
            Assert.IsFalse(viewModel.IsDirty);

            viewModel.IsBrowserIntegrationEnabled = !viewModel.IsBrowserIntegrationEnabled;

            Assert.IsTrue(viewModel.IsDirty);
        }

        [Test]
        public void WhenBrowserIntegrationEnabled_ThenApplyChangesRegistersProtocol()
        {
            viewModel.IsBrowserIntegrationEnabled = true;
            viewModel.ApplyChanges();

            this.protocolRegistryMock.Verify(r => r.Register(
                    It.Is<string>(s => s == IapRdpUrl.Scheme),
                    It.Is<string>(s => s == GeneralOptionsViewModel.FriendlyName),
                    It.IsAny<string>()), 
                Times.Once);
        }

        [Test]
        public void WhenBrowserIntegrationDisabled_ThenApplyChangesUnregistersProtocol()
        {
            viewModel.IsBrowserIntegrationEnabled = false;
            viewModel.ApplyChanges();

            this.protocolRegistryMock.Verify(r => r.Unregister(
                    It.Is<string>(s => s == IapRdpUrl.Scheme)), 
                Times.Once);
        }
    }
}
