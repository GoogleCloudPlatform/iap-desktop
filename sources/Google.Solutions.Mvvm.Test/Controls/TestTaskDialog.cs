using Google.Solutions.Common.Interop;
using Google.Solutions.Mvvm.Controls;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Google.Solutions.Mvvm.Controls.TaskDialog;

namespace Google.Solutions.Mvvm.Test.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class TestTaskDialog
    {
        [Test]
        public void WhenButtonsEmpty_ThenShowDialogThrowsException()
        {
            var parameters = new TaskDialogParameters();
            Assert.Throws<InvalidOperationException>(
                () => new TaskDialog().ShowDialog(null, parameters));
        }

        //---------------------------------------------------------------------
        // Standard buttons.
        //---------------------------------------------------------------------

        [Test]
        public void WhenOkClicked_ThenShowDialogReturnsOk()
        {
            var parameters = new TaskDialogParameters();
            parameters.Buttons.Add(TaskDialogStandardButton.OK);
            parameters.Buttons.Add(TaskDialogStandardButton.OK);
            parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            HRESULT taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.AreEqual(0x0009, config.dwCommonButtons);
                Assert.IsNull(config.pszVerificationText);

                buttonPressed = TaskDialogStandardButton.OK.CommandId;
                radioButtonPressed = -1;
                verificationFlagChecked = false;
                return HRESULT.S_OK;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            Assert.AreEqual(DialogResult.OK, dialog.ShowDialog(null, parameters));
        }

        [Test]
        public void WhenCancelClicked_ThenShowDialogReturnsCancel()
        {
            var parameters = new TaskDialogParameters();
            parameters.Buttons.Add(TaskDialogStandardButton.Yes);
            parameters.Buttons.Add(TaskDialogStandardButton.No);
            parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            HRESULT taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.AreEqual(0x000E, config.dwCommonButtons);
                Assert.IsNull(config.pszVerificationText);

                buttonPressed = TaskDialogStandardButton.Cancel.CommandId;
                radioButtonPressed = -1;
                verificationFlagChecked = false;
                return HRESULT.S_OK;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            Assert.AreEqual(DialogResult.Cancel, dialog.ShowDialog(null, parameters));
        }

        //---------------------------------------------------------------------
        // Command link buttons.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCommandLinkButtonClicked_ThenShowDialogInvokesHandler()
        {
            var parameters = new TaskDialogParameters();
            parameters.Buttons.Add(TaskDialogStandardButton.Cancel);

            var yes = new TaskDialogCommandLinkButton("Yes", DialogResult.Yes);
            var no = new TaskDialogCommandLinkButton("No", DialogResult.No);
            
            parameters.Buttons.Add(yes);
            parameters.Buttons.Add(no);

            int clicks = 0;
            no.Click += (s, e) => clicks++;

            HRESULT taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.IsNull(config.pszVerificationText);
                Assert.IsNotNull(config.cButtons);
                Assert.AreEqual(2, config.cButtons);

                buttonPressed = TaskDialog.CommandLinkIdOffset + 1; // No
                radioButtonPressed = -1;
                verificationFlagChecked = false;
                return HRESULT.S_OK;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            Assert.AreEqual(DialogResult.No, dialog.ShowDialog(null, parameters));
            Assert.AreEqual(1, clicks);
        }

        //---------------------------------------------------------------------
        // Verification checkbox.
        //---------------------------------------------------------------------

        [Test]
        public void WhenVerificationCheckboxChecked_ThenShowDialogUpdatesParameters()
        {
            var parameters = new TaskDialogParameters()
            {
                VerificationCheckBox = new TaskDialogVerificationCheckBox()
                {
                    Text = "check me"
                }
            };

            parameters.Buttons.Add(TaskDialogStandardButton.OK);

            HRESULT taskDialogIndirect(
                ref TASKDIALOGCONFIG config,
                out int buttonPressed,
                out int radioButtonPressed,
                out bool verificationFlagChecked)
            {
                Assert.IsNotNull(config.pszVerificationText);

                buttonPressed = TaskDialogStandardButton.OK.CommandId;
                radioButtonPressed = -1;
                verificationFlagChecked = true;
                return HRESULT.S_OK;
            }

            var dialog = new TaskDialog()
            {
                TaskDialogIndirect = taskDialogIndirect
            };

            dialog.ShowDialog(null, parameters);

            Assert.IsTrue(parameters.VerificationCheckBox.Checked);
        }
    }
}
