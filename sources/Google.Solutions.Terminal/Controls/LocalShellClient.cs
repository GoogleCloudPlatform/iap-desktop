//
// Copyright 2024 Google LLC
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

using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Platform.IO;
using System;
using System.Diagnostics;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Client for a local shell.
    /// </summary>
    public class LocalShellClient : PseudoTerminalClientBase
    {
        private IWin32Process? process = null;
        private readonly string shellProgram;

        public LocalShellClient(string shellProgram)
        {
            this.shellProgram = shellProgram;
        }

        protected override IPseudoTerminal ConnectCore(
            PseudoTerminalSize initialSize)
        {
            Debug.Assert(this.process == null);

            var processFactory = new Win32ProcessFactory();
            this.process = processFactory.CreateProcessWithPseudoConsole(
                this.shellProgram,
                null,
                initialSize);

            //
            // The process is now attached to a pseudo-terminal,
            // but is still suspended.
            //

            Debug.Assert(this.process.PseudoTerminal != null);
            return this.process.PseudoTerminal!;
        }

        private void CloseProcess()
        {
            //
            // Close handle to process.
            //
            if (this.process != null)
            {
                this.process?.Dispose();
                this.process = null;
            }
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        protected override void OnAfterConnect()
        {
            base.OnAfterConnect();
            
            Debug.Assert(this.process != null);
            this.process!.Resume();
        }

        protected override void OnConnectionClosed(DisconnectReason reason)
        {
            CloseProcess();

            base.OnConnectionClosed(reason);
        }

        protected override void OnConnectionFailed(Exception e)
        {
            CloseProcess();

            base.OnConnectionFailed(e);
        }

        protected override bool IsCausedByConnectionTimeout(Exception e)
        {
            //
            // No such thing as a connection timeout for a local process.
            //
            return false;
        }
    }
}
