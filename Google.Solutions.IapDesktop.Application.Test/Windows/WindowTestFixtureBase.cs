using Google.Solutions.IapDesktop.Application.ObjectModel;
using Google.Solutions.IapDesktop.Application.Services;
using Google.Solutions.IapDesktop.Application.Settings;
using Google.Solutions.IapDesktop.Application.SettingsEditor;
using Google.Solutions.IapDesktop.Application.Windows;
using Google.Solutions.IapDesktop.Windows;
using Microsoft.Win32;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Google.Solutions.IapDesktop.Application.Test.Windows
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WindowTestFixtureBase
    {
        private const string TestKeyPath = @"Software\Google\__Test";

        protected ServiceRegistry serviceRegistry;
        protected IServiceProvider serviceProvider;
        protected IMainForm mainForm;
        protected MockExceptionDialog exceptionDialog;

        protected void PumpWindowMessages()
            => System.Windows.Forms.Application.DoEvents();

        [SetUp]
        public void SetUp()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
            hkcu.DeleteSubKeyTree(TestKeyPath, false);

            var registry = new ServiceRegistry();
            registry.AddSingleton(new InventorySettingsRepository(hkcu.CreateSubKey(TestKeyPath)));
            registry.AddTransient<ProjectInventoryService>();

            var mainForm = new MockMainForm();
            registry.AddSingleton<IMainForm>(mainForm);
            registry.AddSingleton<IJobHost>(mainForm);
            registry.AddSingleton<IAuthorizationService>(mainForm);
            registry.AddSingleton<IEventService>(new EventService(mainForm));
            registry.AddSingleton<JobService>();

            this.exceptionDialog = new MockExceptionDialog();
            registry.AddSingleton<IExceptionDialog>(this.exceptionDialog);

            this.mainForm = mainForm;
            this.serviceRegistry = registry;
            this.serviceProvider = registry;

            mainForm.Show();

            PumpWindowMessages();
        }

        [TearDown]
        public void TearDown()
        {
            this.mainForm.Close();
        }

        protected class MockExceptionDialog : IExceptionDialog
        {
            public Exception ExceptionShown { get; private set; }

            public void Show(IWin32Window parent, string caption, Exception e)
            {
                this.ExceptionShown = e;
            }
        }
    }

    internal static class ControlTestExtensions
    {
        public static T GetChild<T>(this Control control, string name) where T : Control
        {
            if (control.Controls.ContainsKey(name))
            {
                return (T)control.Controls[name];
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Control {control.Name} does not have a child control named {name}");
            }
        }
    }
}
