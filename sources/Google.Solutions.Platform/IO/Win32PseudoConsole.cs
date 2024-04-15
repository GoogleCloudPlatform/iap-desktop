using Google.Solutions.Common.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.IO
{
    /// <summary>
    /// A Win32 pseudo-console for interacting with a process.
    /// </summary>
    public class Win32PseudoConsole : IPseudoConsole
    {
        /// <summary>
        /// Encoding used by terminal, which is always UTF-8 (not UCS-2!) 
        /// without BOM.
        /// </summary>
        internal static Encoding Encoding = new UTF8Encoding(false);

        private readonly ManualResetEvent eofEvent;
        private readonly TextReader outputWriter;
        private readonly TextWriter inputWriter;

        internal AnonymousPipe InputPipe { get; }
        internal AnonymousPipe OutputPipe { get; }
        internal PseudoConsoleHandle Handle { get; }

        public Win32PseudoConsole(PseudoConsoleSize size)
        {
            var stdin = new AnonymousPipe();
            var stdout = new AnonymousPipe();

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
                    stdin.Dispose();
                    stdout.Dispose();

                    throw PseudoConsoleException.FromHresult(
                        hresult,
                        "Failed to create pseudo console");
                }

                stdout.CloseWriteSide();
                stdin.CloseReadSide();

                this.Handle = handle;
                this.InputPipe = stdin;
                this.OutputPipe = stdout;
                this.eofEvent = new ManualResetEvent(false);

                this.inputWriter = new StreamWriter(stdin.WriteSide, Encoding)
                {
                    AutoFlush = true
                };
                this.outputWriter = new StreamReader(stdout.ReadSide, Encoding);

                //TODO: this.outputWriter.OnDataReceived(OnDataReceived);
            }
            catch (EntryPointNotFoundException)
            {
                stdin.Dispose();
                stdout.Dispose();

                throw new PseudoConsoleException(
                    "This feature requires Windows 10 version 1809 or newer");
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

        public event EventHandler<PseudoConsoleDataEventArgs>? Output;
        public event EventHandler<PseudoConsoleErrorEventArgs>? Error;

        public bool IsClosed { get; private set; }

        public Task ResizeAsync(
            PseudoConsoleSize size,
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
                    throw PseudoConsoleException.FromHresult(
                        hresult,
                        "Failed to resize pseudo console");
                }
            });
        }

        public Task WriteAsync(string data)
        {
            ExpectNotClosed();

            return WriteAsync(data, CancellationToken.None);
        }

        public Task WriteAsync(
            string data,
            CancellationToken cancellationToken)
        {
            ExpectNotClosed();

            return this.inputWriter.WriteAsync(data);
        }

        public async Task DrainAsync()
        {
            ExpectNotClosed();
            
            await this.eofEvent
                .WaitOneAsync()
                .ConfigureAwait(false);
        }

        public async Task CloseAsync()
        {
            this.Handle.Close();

            await DrainAsync().ConfigureAwait(false);

            this.IsClosed = true;
        }

        //---------------------------------------------------------------------
        // IDisposable.
        //---------------------------------------------------------------------

        protected virtual void Dispose(bool disposing)
        {
            this.inputWriter.Dispose();
            this.outputWriter.Dispose();

            if (!this.IsClosed)
            {
                this.Handle.Close();
                this.IsClosed = true;
            }

            this.eofEvent.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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