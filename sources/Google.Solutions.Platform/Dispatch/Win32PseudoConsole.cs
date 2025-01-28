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

using Google.Solutions.Common.Runtime;
using Google.Solutions.Platform.Interop;
using Google.Solutions.Platform.IO;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable VSTHRD004 // Return task

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// A Win32 pseudo-console for interacting with a process.
    /// 
    /// Note: Pseudo consoles don't work properly in NUnit tests!
    /// </summary>
    public class Win32PseudoConsole : DisposableBase, IPseudoTerminal
    {
        /// <summary>
        /// Encoding used by terminal, which is always UTF-8 (not UCS-2!) 
        /// without BOM.
        /// </summary>
        internal static Encoding Encoding = new UTF8Encoding(false);

        private readonly Task pumpOutputTask;
        private readonly TextReader outputReader;
        private readonly TextWriter inputWriter;

        internal AnonymousPipe InputPipe { get; }
        internal AnonymousPipe OutputPipe { get; }
        internal PseudoConsoleHandle Handle { get; }

        public Win32PseudoConsole(PseudoTerminalSize size)
        {
            var stdin = new AnonymousPipe();
            var stdout = new AnonymousPipe();

            try
            {
                try
                {
                    var hresult = NativeMethods.CreatePseudoConsole(
                        new NativeMethods.COORD
                        {
                            X = (short)size.Width,
                            Y = (short)size.Height
                        },
                        stdin.ReadSideHandle,
                        stdout.WriteSideHandle,
                        0,
                        out var handle);
                    if (hresult.Failed())
                    {
                        throw PseudoTerminalException.FromHresult(
                            hresult,
                            "Failed to create pseudo console");
                    }

                    stdout.CloseWriteSide();
                    stdin.CloseReadSide();

                    this.Handle = handle;
                    this.InputPipe = stdin;
                    this.OutputPipe = stdout;

                    this.inputWriter = new StreamWriter(stdin.WriteSide, Encoding)
                    {
                        AutoFlush = true
                    };
                    this.outputReader = new StreamReader(stdout.ReadSide, Encoding);
                    this.pumpOutputTask = PumpEventsAsync();
                }
                catch (EntryPointNotFoundException)
                {
                    throw new PseudoTerminalException(
                        "This feature requires Windows 10 version 1809 or newer");
                }
            }
            catch
            {
                stdin.Dispose();
                stdout.Dispose();

                throw;
            }
        }

        private async Task PumpEventsAsync()
        {
            var buffer = new char[1024];
            while (!this.IsDisposed)
            {
                try
                {
                    var charsRead = await this.outputReader
                        .ReadAsync(buffer, 0, buffer.Length)
                        .ConfigureAwait(false);
                    if (charsRead == 0)
                    {
                        //
                        // EOF reached.
                        //
                        this.Disconnected?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                    else
                    {
                        this.OutputAvailable?.Invoke(
                            this,
                            new PseudoTerminalDataEventArgs(
                                new string(buffer, 0, charsRead)));
                    }
                }
                catch (Exception) when (this.IsDisposed)
                {
                    //
                    // The pseudo console was closed or disposed while the
                    // async read was pending. 
                    //
                }
                catch (Exception e)
                {
                    this.FatalError?.Invoke(
                        this,
                        new PseudoTerminalErrorEventArgs(e));
                    return;
                }
            }
        }

        private void ExpectNotClosed()
        {
            if (this.IsClosed)
            {
                throw new InvalidOperationException("Pseudo-console is closed");
            }
        }

        //---------------------------------------------------------------------
        // IPseudoTerminal.
        //---------------------------------------------------------------------

        public event EventHandler<PseudoTerminalDataEventArgs>? OutputAvailable;

        public event EventHandler<PseudoTerminalErrorEventArgs>? FatalError;

        public event EventHandler<EventArgs>? Disconnected;

        public bool IsClosed { get; private set; }

        public Task ResizeAsync(
            PseudoTerminalSize size,
            CancellationToken cancellationToken)
        {
            ExpectNotClosed();

            return Task.Run(() =>
            {
                var hresult = NativeMethods.ResizePseudoConsole(
                    this.Handle,
                    new NativeMethods.COORD()
                    {
                        X = (short)size.Width,
                        Y = (short)size.Height
                    });
                if (hresult.Failed())
                {
                    throw PseudoTerminalException.FromHresult(
                        hresult,
                        "Failed to resize pseudo console");
                }
            },
            cancellationToken);
        }

        public Task WriteAsync(
            string data,
            CancellationToken cancellationToken)
        {
            ExpectNotClosed();

            return this.inputWriter.WriteAsync(data);
        }

        public Task DrainAsync()
        {
            return this.pumpOutputTask;
        }

        public async Task CloseAsync()
        {
            this.Handle.Close();
            this.IsClosed = true;

            //
            // Drain all pending output.
            //
            await DrainAsync().ConfigureAwait(false);
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.inputWriter.Dispose();
            this.InputPipe.Dispose();

            this.outputReader.Dispose();
            this.OutputPipe.Dispose();

            if (!this.IsClosed)
            {
                this.Handle.Close();
                this.IsClosed = true;
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        internal class PseudoConsoleHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private PseudoConsoleHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                //
                // NB. This not only ends the pseudo console session,
                // but also terminates the attached process.
                //
                NativeMethods.ClosePseudoConsole(this.handle);
                return true;
            }
        }

        private static class NativeMethods
        {
            internal struct COORD
            {
                public short X;
                public short Y;
            }

            [DllImport("kernel32.dll", SetLastError = false)]
            internal static extern HRESULT CreatePseudoConsole(
                COORD size,
                SafeFileHandle hInput,
                SafeFileHandle hOutput,
                uint dwFlags,
                out PseudoConsoleHandle handle);

            [DllImport("kernel32.dll", SetLastError = false)]
            internal static extern HRESULT ResizePseudoConsole(
                PseudoConsoleHandle handle,
                COORD size);

            [DllImport("kernel32.dll", SetLastError = false)]
            internal static extern void ClosePseudoConsole(IntPtr hPC);
        }
    }
}