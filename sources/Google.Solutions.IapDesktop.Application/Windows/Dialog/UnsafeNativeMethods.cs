//
// Copyright 2019 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    internal static class UnsafeNativeMethods
    {
        //---------------------------------------------------------------------
        // Task Dialog definitions.
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
        public enum TASKDIALOG_COMMON_BUTTON_FLAGS : uint
        {
            TDCBF_OK_BUTTON = 0x0001,
            TDCBF_YES_BUTTON = 0x0002,
            TDCBF_NO_BUTTON = 0x0004,
            TDCBF_CANCEL_BUTTON = 0x0008,
            TDCBF_RETRY_BUTTON = 0x0010,
            TDCBF_CLOSE_BUTTON = 0x0020,
        }

        public enum TASKDIALOG_NOTIFICATIONS : uint
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

        public const int IDOK = 1;
        public const int IDCANCEL = 2;
        public const int IDABORT = 3;
        public const int IDRETRY = 4;
        public const int IDIGNORE = 5;
        public const int IDYES = 6;
        public const int IDNO = 7;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOGCONFIG
        {
            public uint cbSize;
            public IntPtr hwndParent;
            public IntPtr hInstance;
            public TASKDIALOG_FLAGS dwFlags;
            public TASKDIALOG_COMMON_BUTTON_FLAGS dwCommonButtons;

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
            public string? pszVerificationText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pszExpandedInformation;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszExpandedControlText;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCollapsedControlText;

            public IntPtr FooterIcon;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszFooter;

            public TaskDialogCallback pfCallback;
            public IntPtr lpCallbackData;
            public uint cxWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        internal struct TASKDIALOG_BUTTON_RAW
        {
            public int nButtonID;

            public IntPtr pszButtonText;
        }

        internal delegate int TaskDialogCallback(
            [In] IntPtr hwnd,
            [In] TASKDIALOG_NOTIFICATIONS msg,
            [In] UIntPtr wParam,
            [In] IntPtr lParam,
            [In] IntPtr refData);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1400:PInvokeEntryPointsShouldExist")]
        [DllImport("ComCtl32", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void TaskDialogIndirect(
            [In] ref TASKDIALOGCONFIG pTaskConfig,
            [Out] out int pnButton,
            [Out] out int pnRadioButton,
            [Out] out bool pfVerificationFlagChecked);

        //---------------------------------------------------------------------
        // Operations Progress Dialog definitions.
        //---------------------------------------------------------------------


        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        internal interface IShellItem
        {
            void BindToHandler(IntPtr pbc,
                [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
                [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

            void Compare(IShellItem psi, uint hint, out int piOrder);
        };

        [ComImport]
        [Guid("0C9FB851-E5C9-43EB-A370-F0677B13874C")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IOperationsProgressDialog
        {
            void StartProgressDialog(IntPtr hwndOwner, PROGDLG flags);
            void StopProgressDialog();
            void SetOperation(SPACTION action);
            void SetMode(PDMODE mode);
            void UpdateProgress(ulong ullPointsCurrent, ulong ullPointsTotal, ulong ullSizeCurrent, ulong ullSizeTotal, ulong ullItemsCurrent, ulong ullItemsTotal);
            void UpdateLocations(IShellItem psiSource, IShellItem psiTarget, IShellItem psiItem);
            void ResetTimer();
            void PauseTimer();
            void ResumeTimer();
            void GetMilliseconds(ulong pullElapsed, ulong pullRemaining);
            PDOPSTATUS GetOperationStatus();
        }

        internal enum SIGDN : uint
        {
            NORMALDISPLAY = 0,
            PARENTRELATIVEPARSING = 0x80018001,
            PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            PARENTRELATIVEEDITING = 0x80031001,
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            FILESYSPATH = 0x80058000,
            URL = 0x80068000
        }


        internal enum PDMODE : uint
        {
            DEFAULT = 0x00000000,
            RUN = 0x00000001,
            PREFLIGHT = 0x00000002,
            UNDOING = 0x00000004,
            ERRORSBLOCKING = 0x00000008,
            INDETERMINATE = 0x00000010
        }

        internal enum SPACTION : uint
        {
            NONE = 0,
            MOVING,
            COPYING,
            RECYCLING,
            APPLYINGATTRIBS,
            DOWNLOADING,
            SEARCHING_INTERNET,
            CALCULATING,
            UPLOADING,
            SEARCHING_FILES,
            DELETING,
            RENAMING,
            FORMATTING,
            COPY_MOVING
        }

        internal enum PDOPSTATUS : uint
        {
            RUNNING = 1,
            PAUSED = 2,
            CANCELLED = 3,
            STOPPED = 4,
            ERRORS = 5
        }

        internal enum PROGDLG : uint
        {
            NORMAL = 0x00000000,
            MODAL = 0x00000001,
            AUTOTIME = 0x00000002,
            NOTIME = 0x00000004,
            NOMINIMIZE = 0x00000008,
            NOPROGRESSBAR = 0x00000010,
            MARQUEEPROGRESS = 0x00000020,
            NOCANCEL = 0x00000040,
            OPDEFAULT = 0x00000000,
            OPENABLEPAUSE = 0x00000080,
            OPALLOWUNDO = 0x00000100,
            OPDONTDISPLAYSOURCEPATH = 0x00000200,
            OPDONTDISPLAYDESTPATH = 0x00000400,
            OPNOMULTIDAYESTIMATES = 0x00000800,
            OPDONTDISPLAYLOCATIONS = 0x00001000
        }
    }
}
