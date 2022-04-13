using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    public class SshSftpChannel : IDisposable
    {
        internal readonly SshSftpHandle channelHandle;
        private bool disposed = false;

        internal SshSftpChannel(
            SshSftpHandle channelHandle)
        {
            this.channelHandle = channelHandle;
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.channelHandle.Dispose();
                this.disposed = true;
            }
        }
    }
}
