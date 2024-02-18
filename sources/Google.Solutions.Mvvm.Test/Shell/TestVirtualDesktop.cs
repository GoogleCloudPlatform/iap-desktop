using Google.Solutions.Mvvm.Shell;
using Google.Solutions.Testing.Apis;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Shell
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestVirtualDesktop
    {
        //---------------------------------------------------------------------
        // IsOnCurrentVirtualDesktop.
        //---------------------------------------------------------------------

        [WindowsFormsTest]
        public void WhenFormValid_ThenIsOnCurrentVirtualDesktopReturnsTrue()
        {
            using (var f = new Form())
            {
                f.Show();

                Assert.IsTrue(f.IsOnCurrentVirtualDesktop());

                f.Close();
            }
        }

        [WindowsFormsTest]
        public void WhenWindowInvalid_ThenIsOnCurrentVirtualDesktopThrowsException()
        {
            var window = new Mock<IWin32Window>();

            Assert.Throws<ArgumentException>(
                () => window.Object.IsOnCurrentVirtualDesktop());
        }

        //---------------------------------------------------------------------
        // GetVirtualDesktopId.
        //---------------------------------------------------------------------

        [WindowsFormsTest]
        public void WhenFormValid_ThenGetVirtualDesktopIdReturnsGuid()
        {
            using (var f = new Form())
            {
                f.Show();

                var id = f.GetVirtualDesktopId();
                Assert.IsNotNull(id);
                Assert.AreNotEqual(Guid.Empty, id);

                f.Close();
            }
        }

        [WindowsFormsTest]
        public void WhenWindowInvalid_ThenGetVirtualDesktopIdReturnsGuidThrowsException()
        {
            var window = new Mock<IWin32Window>();

            Assert.Throws<ArgumentException>(
                () => window.Object.GetVirtualDesktopId());
        }

        //---------------------------------------------------------------------
        // MoveToVirtualDesktop.
        //---------------------------------------------------------------------

        [WindowsFormsTest]
        public void WhenDesktopNotFound_ThenMoveToVirtualDesktopThrowsException()
        {
            using (var f = new Form())
            {
                f.Show();

                Assert.Throws<ArgumentException>(
                    () => f.MoveToVirtualDesktop(Guid.Empty));

                f.Close();
            }
        }
    }
}
