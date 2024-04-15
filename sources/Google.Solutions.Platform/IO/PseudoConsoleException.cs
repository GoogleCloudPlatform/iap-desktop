using Google.Solutions.Common.Interop;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.IO
{
    public class PseudoConsoleException : IOException
    {
        public PseudoConsoleException(string message) : base(message)
        {
        }

        public PseudoConsoleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal static PseudoConsoleException FromHresult(
            HRESULT hresult,
            string message)
        {
            return new PseudoConsoleException(
                $"{message} (HRESULT 0x{hresult:X})",
                new ExternalException(message, (int)hresult));
        }


        internal static PseudoConsoleException FromLastError(string message)
        {
            var lastError = Marshal.GetLastWin32Error();

            return new PseudoConsoleException(
                $"{message} (Error 0x{lastError:X})",
                new Win32Exception(lastError));
        }
    }
}
