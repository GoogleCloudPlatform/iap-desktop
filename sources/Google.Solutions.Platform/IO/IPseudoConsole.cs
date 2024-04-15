using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.IO
{
    /// <summary>
    /// A pseudo-console that accepts and produces VT/xterm-formatted
    /// input and output.
    /// </summary>
    public interface IPseudoConsole : IDisposable
    {
        /// <summary>
        /// Raised when data is available.
        /// </summary>
        event EventHandler<PseudoConsoleDataEventArgs>? Output;

        /// <summary>
        /// Raised when the device failed.
        /// </summary>
        event EventHandler<PseudoConsoleErrorEventArgs>? Error;

        /// <summary>
        /// Check if the device is closed, indicating that the
        /// session has ended.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Adjust the size of the current session.
        /// </summary>
        Task ResizeAsync(
            PseudoConsoleSize dimensions,
            CancellationToken cancellationToken);

        /// <summary>
        /// Write Xterm-formatted data to the device
        /// </summary>
        Task WriteAsync(
            string data,
            CancellationToken cancellationToken);

        /// <summary>
        /// Drain output until EOF is received.
        /// </summary>
        Task DrainAsync();

        /// <summary>
        /// Drain output and close the session.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }

    /// <summary>
    /// Size of a console, in characters.
    /// </summary>
    public struct PseudoConsoleSize
    {
        public PseudoConsoleSize(ushort width, ushort height)
        {
            Debug.Assert(width > 0 && height > 0);

            this.Width = width;
            this.Height = height;
        }

        public ushort Width { get; }

        public ushort Height { get; }
    }


    /// <summary>
    /// Event data for console data.
    /// </summary>
    public class PseudoConsoleDataEventArgs : EventArgs
    {
        /// <summary>
        /// Xterm-encoded data.
        /// </summary>
        public string Data { get; }

        public bool IsEof { get; private set; }

        internal static PseudoConsoleDataEventArgs Eof = new PseudoConsoleDataEventArgs(string.Empty)
        {
            IsEof = true
        };

        internal PseudoConsoleDataEventArgs(string data)
        {
            this.Data = data;
        }
    }

    /// <summary>
    /// Event data for a console error.
    /// </summary>
    public class PseudoConsoleErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }

        public PseudoConsoleErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }
    }
}
