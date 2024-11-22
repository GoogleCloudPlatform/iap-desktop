//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    /// <summary>
    /// Explorer-style progress dialog for coping items.
    /// </summary>
    public interface IOperationProgressDialog
    {
        /// <summary>
        /// Show a Shell progress dialog for a file copy operation.
        /// </summary>
        IOperation StartCopyOperation(
            IWin32Window owner,
            ulong totalItems,
            ulong totalSize);
    }

    /// <summary>
    /// Represents an active operation. Dispose to end the
    /// operation.
    /// </summary>
    public interface IOperation : IDisposable
    {
        /// <summary>
        /// Token indicating whether the user has cancelled
        /// the operation.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Report that an item has been completed.
        /// </summary>
        void OnItemCompleted();

        /// <summary>
        /// Report that a chunk of bytes has been completed.
        /// </summary>
        void OnBytesCompleted(ulong delta);
    }

    [SkipCodeCoverage("UI code")]
    public class OperationProgressDialog : IOperationProgressDialog
    {
        public IOperation StartCopyOperation(
            IWin32Window owner,
            ulong totalItems,
            ulong totalSize)
        {
            var flags =
                PROGDLG.OPDONTDISPLAYSOURCEPATH |
                PROGDLG.OPDONTDISPLAYDESTPATH |
                PROGDLG.OPDONTDISPLAYLOCATIONS |
                PROGDLG.MODAL;

            return new Operation(
                owner,
                totalItems,
                totalSize,
                SPACTION.COPYING,
                PDMODE.RUN,
                flags);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private sealed class Operation : IOperation
        {
            private const string CLSID_ProgressDialog = "{F8383852-FCD3-11d1-A6B9-006097DF5BD4}";

            private readonly CancellationTokenSource cancellationTokenSource;
            private readonly IOperationsProgressDialog dialog;
            private readonly ulong totalItems;
            private readonly ulong totalSize;
            private ulong itemsCompleted = 0;
            private ulong sizeCompleted = 0;

            public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

            private void UpdateProgress()
            {
                if (this.dialog.GetOperationStatus() == PDOPSTATUS.CANCELLED)
                {
                    this.cancellationTokenSource.Cancel();
                }

                this.dialog.UpdateProgress(
                    this.sizeCompleted,
                    this.totalSize,
                    this.sizeCompleted,
                    this.totalSize,
                    this.itemsCompleted,
                    this.totalItems);
            }

            public Operation(
                IWin32Window owner,
                ulong totalItems,
                ulong totalSize,
                SPACTION operation,
                PDMODE mode,
                PROGDLG flags)
            {
                this.totalItems = totalItems;
                this.totalSize = totalSize;
                this.dialog = (IOperationsProgressDialog)
                    Activator.CreateInstance(
                        Type.GetTypeFromCLSID(new Guid(CLSID_ProgressDialog)));

                this.dialog.StartProgressDialog(
                    owner.Handle,
                    flags);
                this.dialog.SetOperation(operation);
                this.dialog.SetMode(mode);

                this.cancellationTokenSource = new CancellationTokenSource();
            }

            public void OnItemCompleted()
            {
                this.itemsCompleted++;
                UpdateProgress();
            }

            public void OnBytesCompleted(ulong delta)
            {
                this.sizeCompleted += delta;
                UpdateProgress();
            }

            public void Dispose()
            {
                this.dialog.StopProgressDialog();
                Marshal.ReleaseComObject(this.dialog);
            }
        }

        //---------------------------------------------------------------------
        // Interop declarations.
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
