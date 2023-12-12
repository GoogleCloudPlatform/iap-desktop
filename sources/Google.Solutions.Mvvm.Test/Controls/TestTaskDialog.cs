using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Controls
{

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestTaskDialog
    {
        [SetUp]
        public void SetUp()
        {
            Application.EnableVisualStyles();
        }

        [InteractiveTest]
        [Test]
        public void WhenButtonsEmpty_ThenShowDialogThrowsException()
        {
            using (var dialog = new TaskDialog())
            {
                Assert.Throws<InvalidOperationException>(
                    () => dialog.ShowDialog(null));
            }
        }

        [InteractiveTest]
        [Test]
        public void OkCancelDialog()
        {
            using (var dialog = new TaskDialog())
            {
                dialog.Buttons.Add(TaskDialogStandardButton.OK);
                dialog.Buttons.Add(TaskDialogStandardButton.OK);
                dialog.Buttons.Add(TaskDialogStandardButton.Cancel);

                dialog.ShowDialog(null);
            }
        }
    }
}
