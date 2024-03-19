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

namespace Google.Solutions.IapDesktop.Application.Windows.Dialog
{
    public interface IOperationProgressDialog
    {
        /// <summary>
        /// Show a Shell progress dialog for a file copy operation.
        /// </summary>
        IOperation ShowCopyDialog(
            IWin32Window owner,
            ulong totalItems,
            ulong totalSize);
    }

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
        /// <param name="delta"></param>
        void OnBytesCompleted(ulong delta);
    }


    [SkipCodeCoverage("UI code")]
    public class OperationProgressDialog : IOperationProgressDialog
    {
        public IOperation ShowCopyDialog(
            IWin32Window owner,
            ulong totalItems,
            ulong totalSize)
        {
            var flags =
                UnsafeNativeMethods.PROGDLG.OPDONTDISPLAYSOURCEPATH |
                UnsafeNativeMethods.PROGDLG.OPDONTDISPLAYSOURCEPATH |
                UnsafeNativeMethods.PROGDLG.OPDONTDISPLAYLOCATIONS |
                UnsafeNativeMethods.PROGDLG.MODAL;

            return new Operation(
                owner,
                totalItems,
                totalSize,
                UnsafeNativeMethods.SPACTION.COPYING,
                UnsafeNativeMethods.PDMODE.RUN,
                flags);
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        private sealed class Operation : IOperation
        {
            private const string CLSID_ProgressDialog = "{F8383852-FCD3-11d1-A6B9-006097DF5BD4}";

            private readonly CancellationTokenSource cancellationTokenSource;
            private readonly UnsafeNativeMethods.IOperationsProgressDialog dialog;
            private readonly ulong totalItems;
            private readonly ulong totalSize;
            private ulong itemsCompleted = 0;
            private ulong sizeCompleted = 0;

            public CancellationToken CancellationToken => this.cancellationTokenSource.Token;

            private void UpdateProgress()
            {
                if (this.dialog.GetOperationStatus() == UnsafeNativeMethods.PDOPSTATUS.CANCELLED)
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
                UnsafeNativeMethods.SPACTION operation,
                UnsafeNativeMethods.PDMODE mode,
                UnsafeNativeMethods.PROGDLG flags)
            {
                this.totalItems = totalItems;
                this.totalSize = totalSize;
                this.dialog = (UnsafeNativeMethods.IOperationsProgressDialog)
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
    }
}
