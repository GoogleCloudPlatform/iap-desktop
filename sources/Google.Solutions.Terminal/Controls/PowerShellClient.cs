using Google.Solutions.Platform.Dispatch;
using Google.Solutions.Platform.IO;
using System;
using System.Diagnostics;

namespace Google.Solutions.Terminal.Controls
{
    /// <summary>
    /// Client for a local Powershell process.
    /// </summary>
    public class PowerShellClient : PseudoTerminalClientBase // TODO: test
    {
        private IWin32Process? process = null;

        protected override IPseudoTerminal ConnectCore(
            PseudoTerminalSize initialSize)
        {
            Debug.Assert(this.process == null);

            var processFactory = new Win32ProcessFactory();
            this.process = processFactory.CreateProcessWithPseudoConsole(
                "powershell.exe",
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
            Debug.Assert(this.process != null);
            this.process?.Dispose();
            this.process = null;
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
