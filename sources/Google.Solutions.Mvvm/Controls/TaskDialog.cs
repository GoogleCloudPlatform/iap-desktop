using Google.Solutions.Common.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// A "Vista" style task dialog.
    /// </summary>
    public class TaskDialog : IDisposable
    {
        /// <summary>
        /// Icon for the task dialog.
        /// </summary>
        public TaskDialogIcon Icon { get; set; }

        /// <summary>
        /// Caption for title bar.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Main instruction.
        /// </summary>
        public string Heading { get; set; }

        /// <summary>
        /// Text content.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Footnote text.
        /// </summary>
        public string Footnote { get; set; }

        /// <summary>
        /// Command buttons to show.
        /// </summary>
        public IList<TaskDialogButton> Buttons { get; } = new List<TaskDialogButton>();

        /// <summary>
        /// Verification text box in footer.
        /// </summary>
        public TaskDialogVerificationCheckBox VerificationCheckBox { get; set; }

        public EventHandler LinkClicked { get; }

        public void Dispose()
        {
        }

        public TaskDialogButton ShowDialog(IWin32Window parent)
        {
            const int CommandButtonIdOffset = 1000;

            if (!this.Buttons.Any())
            {
                throw new InvalidOperationException
                    ("The dialog must contain at least one button");
            }

            var standardButtons = this.Buttons
                .OfType<TaskDialogStandardButton>()
                .ToList();
            var commandButtons = this.Buttons
                .OfType<TaskDialogCommandLinkButton>()
                .ToList();

            //
            // Prepare native struct for command buttons.
            //
            var commandButtonTexts = commandButtons
                .Select(b =>
                {
                    //
                    // Text up to the first new line character is treated as the
                    // command link's main text, the remainder is treated as the
                    // command link's note. 
                    //
                    var text = b.Text?.Replace('\n', ' ') ?? string.Empty;
                    
                    if (b.Details != null)
                    {
                        text += $"\n{b.Details}";
                    }

                    return b;
                })
                .Select(b => Marshal.StringToHGlobalUni(b.Text))
                .ToArray();
            var commandButtonsPtr = Marshal.AllocHGlobal(
                Marshal.SizeOf<TASKDIALOG_BUTTON_RAW>() * commandButtons.Count);

            for (var i = 0; i < commandButtons.Count; i++)
            {
                Marshal.StructureToPtr<TASKDIALOG_BUTTON_RAW>(
                    new TASKDIALOG_BUTTON_RAW()
                    {
                        //
                        // Add ID offset to avoid conflict with IDOK/IDCANCEL.
                        //
                        nButtonID = CommandButtonIdOffset + i,
                        pszButtonText = commandButtonTexts[i]
                    },
                    commandButtonsPtr + i * Marshal.SizeOf<TASKDIALOG_BUTTON_RAW>(),
                    false);
            }

            try
            {
                var flags = 
                    TASKDIALOG_FLAGS.TDF_EXPAND_FOOTER_AREA |
                    TASKDIALOG_FLAGS.TDF_ENABLE_HYPERLINKS;
                if (commandButtons.Any())
                {
                    flags |= TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS;
                }

                var config = new TASKDIALOGCONFIG()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(TASKDIALOGCONFIG)),
                    hwndParent = parent?.Handle ?? IntPtr.Zero,
                    dwFlags = flags,
                    dwCommonButtons = standardButtons
                        .Select(b => b.Flag)
                        .Aggregate((f1, f2) => f1 | f2),
                    pszWindowTitle = this.Caption,
                    MainIcon = this.Icon?.Handle ?? IntPtr.Zero,
                    pszMainInstruction = this.Heading,
                    pszContent = this.Text,
                    pButtons = commandButtonsPtr,
                    cButtons = (uint)commandButtons.Count,
                    pszExpandedInformation = this.Footnote,
                    pszVerificationText = this.VerificationCheckBox?.Text,
                    pfCallback = (hwnd, notification, wParam, lParam, refData) =>
                    {
                        if (notification == TASKDIALOG_NOTIFICATIONS.TDN_HYPERLINK_CLICKED)
                        {
                            this.LinkClicked?.Invoke(this, EventArgs.Empty);
                        }

                        return HRESULT.S_OK;
                    }
                };
                
                var hr = NativeMethods.TaskDialogIndirect(
                    ref config,
                    out var buttonIdPressed,
                    out var radioButtonPressed,
                    out var verificationFlagPressed);
                if (hr.Failed())
                {
                    throw new InvalidOperationException($"The TaskDialog failed: {hr:X}");
                }

                if (this.VerificationCheckBox != null)
                {
                    this.VerificationCheckBox.Checked = verificationFlagPressed;
                }

                //
                // Map the result back to the right button.
                //
                if (buttonIdPressed >= CommandButtonIdOffset &&
                    buttonIdPressed < CommandButtonIdOffset + commandButtons.Count)
                {
                    var pressedCommandButton = commandButtons[buttonIdPressed];
                    pressedCommandButton.PerformClick();
                    return pressedCommandButton;
                }
                else if (standardButtons.FirstOrDefault(b => b.CommandId == buttonIdPressed)
                    is var pressedStandardButton &&
                    pressedStandardButton != null)
                {
                    return pressedStandardButton;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"The TaskDialog returned an unexpected result: {buttonIdPressed}");
                }
            }
            finally
            {
                foreach (var commandButtonText in commandButtonTexts)
                {
                    Marshal.FreeHGlobal(commandButtonText);
                }

                Marshal.FreeHGlobal(commandButtonsPtr);
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        [Flags]
        internal enum TASKDIALOG_FLAGS : uint
        {
            TDF_ENABLE_HYPERLINKS = 0x0001,
            TDF_USE_HICON_MAIN = 0x0002,
            TDF_USE_HICON_FOOTER = 0x0004,
            TDF_ALLOW_DIALOG_CANCELLATION = 0x0008,
            TDF_USE_COMMAND_LINKS = 0x0010,
            TDF_USE_COMMAND_LINKS_NO_ICON = 0x0020,
            TDF_EXPAND_FOOTER_AREA = 0x0040,
            TDF_EXPANDED_BY_DEFAULT = 0x0080,
            TDF_VERIFICATION_FLAG_CHECKED = 0x0100,
            TDF_SHOW_PROGRESS_BAR = 0x0200,
            TDF_SHOW_MARQUEE_PROGRESS_BAR = 0x0400,
            TDF_CALLBACK_TIMER = 0x0800,
            TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000,
            TDF_RTL_LAYOUT = 0x2000,
            TDF_NO_DEFAULT_RADIO_BUTTON = 0x4000,
            TDF_CAN_BE_MINIMIZED = 0x8000
        }

        [Flags]
        internal enum TASKDIALOG_COMMON_BUTTON_FLAGS : uint
        {
            TDCBF_OK_BUTTON = 0x0001,
            TDCBF_YES_BUTTON = 0x0002,
            TDCBF_NO_BUTTON = 0x0004,
            TDCBF_CANCEL_BUTTON = 0x0008,
            TDCBF_RETRY_BUTTON = 0x0010,
            TDCBF_CLOSE_BUTTON = 0x0020,
        }

        internal enum TASKDIALOG_NOTIFICATIONS : uint
        {
            TDN_CREATED = 0,
            TDN_NAVIGATED = 1,
            TDN_BUTTON_CLICKED = 2,
            TDN_HYPERLINK_CLICKED = 3,
            TDN_TIMER = 4,
            TDN_DESTROYED = 5,
            TDN_RADIO_BUTTON_CLICKED = 6,
            TDN_DIALOG_CONSTRUCTED = 7,
            TDN_VERIFICATION_CLICKED = 8,
            TDN_HELP = 9,
            TDN_EXPANDO_BUTTON_CLICKED = 10
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOGCONFIG
        {
            public uint cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TASKDIALOG_FLAGS dwFlags;
            public uint dwCommonButtons;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszWindowTitle;

            public IntPtr MainIcon;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMainInstruction;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszContent;

            public uint cButtons;

            public IntPtr pButtons;

            public int nDefaultButton;
            public uint cRadioButtons;
            public IntPtr pRadioButtons;
            public int nDefaultRadioButton;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszVerificationText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedInformation;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedControlText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCollapsedControlText;

            public IntPtr FooterIcon;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszFooter;

            public NativeMethods.TaskDialogCallback pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOG_BUTTON_RAW
        {
            public int nButtonID;

            public IntPtr pszButtonText;
        }


        internal static class NativeMethods
        {
            internal delegate HRESULT TaskDialogCallback(
                [In] IntPtr hwnd,
                [In] TASKDIALOG_NOTIFICATIONS msg,
                [In] UIntPtr wParam,
                [In] IntPtr lParam,
                [In] IntPtr refData);

            [DllImport("ComCtl32", CharSet = CharSet.Unicode, PreserveSig = false)]
            internal static extern HRESULT TaskDialogIndirect(
                [In] ref TASKDIALOGCONFIG pTaskConfig,
                [Out] out int pnButton,
                [Out] out int pnRadioButton,
                [Out] out bool pfVerificationFlagChecked);
        }
    }

    public class TaskDialogVerificationCheckBox
    {
        /// <summary>
        /// Text to show next to checkbox.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Checkbox state.
        /// </summary>
        public bool Checked { get; set; }
    }

    public abstract class TaskDialogButton
    {
    }

    /// <summary>
    /// Standard dialog button.
    /// </summary>
    public class TaskDialogStandardButton : TaskDialogButton
    {
        private const uint TDCBF_OK_BUTTON = 0x0001;
        private const uint TDCBF_YES_BUTTON = 0x0002;
        private const uint TDCBF_NO_BUTTON = 0x0004;
        private const uint TDCBF_CANCEL_BUTTON = 0x0008;
        private const uint TDCBF_RETRY_BUTTON = 0x0010;
        private const uint TDCBF_CLOSE_BUTTON = 0x0020;

        private const uint IDOK = 1;
        private const uint IDCANCEL = 2;
        private const uint IDABORT = 3;
        private const uint IDRETRY = 4;
        private const uint IDIGNORE = 5;
        private const uint IDYES = 6;
        private const uint IDNO = 7;

        public static readonly TaskDialogStandardButton OK =
            new TaskDialogStandardButton(DialogResult.OK, IDOK, TDCBF_OK_BUTTON);

        public static readonly TaskDialogStandardButton Cancel
            = new TaskDialogStandardButton(DialogResult.Cancel, IDCANCEL, TDCBF_CANCEL_BUTTON);

        public static readonly TaskDialogStandardButton Yes =
            new TaskDialogStandardButton(DialogResult.Yes, IDYES, TDCBF_YES_BUTTON);

        public static readonly TaskDialogStandardButton No =
            new TaskDialogStandardButton(DialogResult.No, IDNO, TDCBF_NO_BUTTON);

        public static readonly TaskDialogStandardButton Retry =
            new TaskDialogStandardButton(DialogResult.Retry, IDRETRY, TDCBF_RETRY_BUTTON);

        public static readonly TaskDialogStandardButton Abort =
            new TaskDialogStandardButton(DialogResult.Abort, IDABORT, TDCBF_CLOSE_BUTTON);

        internal TaskDialogStandardButton(
            DialogResult result,
            uint commandId,
            uint flag)
        {
            this.Result = result;
            this.CommandId = commandId;
            this.Flag = flag;
        }

        public DialogResult Result { get; }

        internal uint CommandId { get; }

        internal uint Flag { get; }
    }

    /// <summary>
    /// Custom command link.
    /// </summary>
    public class TaskDialogCommandLinkButton : TaskDialogButton
    {
        public EventHandler Click;

        /// <summary>
        /// Command text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Command text.
        /// </summary>
        public string Details { get; set; }

        public void PerformClick()
        {
            this.Click?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Icon for the task dialog.
    /// </summary>
    public abstract class TaskDialogIcon : IDisposable
    {
        internal IntPtr Handle { get; }

        protected TaskDialogIcon(IntPtr handle)
        {
            this.Handle = handle;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        //---------------------------------------------------------------------
        // Stock icons.
        //
        // These icons don't need disposal.
        //---------------------------------------------------------------------

        public static readonly TaskDialogIcon Warning = new StockIcon(65535);
        public static readonly TaskDialogIcon Error = new StockIcon(65534);
        public static readonly TaskDialogIcon Information = new StockIcon(65533);
        public static readonly TaskDialogIcon Shield = new StockIcon(65532);
        public static readonly TaskDialogIcon ShieldGrayBackground = new StockIcon(65527);
        public static readonly TaskDialogIcon ShieldGreenBackground = new StockIcon(65528);
        public static readonly TaskDialogIcon ShieldInfoBackground = new StockIcon(65531);
        public static readonly TaskDialogIcon ShieldWarningBackground = new StockIcon(65530);

        private class StockIcon : TaskDialogIcon
        {
            public StockIcon(int id) : base(new IntPtr(id))
            {
            }
        }
    }
}
