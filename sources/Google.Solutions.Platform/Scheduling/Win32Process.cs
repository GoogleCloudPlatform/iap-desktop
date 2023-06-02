using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Scheduling
{

    public interface IWin32Process : IDisposable
    {
        /// <summary>
        /// Image name, without path.
        /// </summary>
        string ImageName { get; }

        uint Id { get; }

        /// <summary>
        /// Process handle.
        /// </summary>
        SafeProcessHandle Handle { get; }

        /// <summary>
        /// Resume the process.
        /// </summary>
        void Resume();

        /// <summary>
        /// Request an orderly close by sending a a WM_CLOSE
        /// message to the process. This only works for GUI
        /// processes.
        /// 
        /// Returns true if at least one window was found.
        /// </summary>
        bool Close();

        /// <summary>
        /// Forcefully terminate the process.
        /// </summary>
        void Terminate(uint exitCode);

        /// <summary>
        /// Indicates whether the process is running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Numer of top-level windows owned by this process.
        /// </summary>
        int WindowCount { get; }
    }

}
