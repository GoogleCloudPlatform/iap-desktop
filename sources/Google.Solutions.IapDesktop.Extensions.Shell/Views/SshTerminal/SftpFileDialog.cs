using Google.Apis.Util;
using Google.Solutions.IapDesktop.Application.Views.Dialog;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Ssh;
using Google.Solutions.Ssh.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    public interface ISftpFileDialog
    {
        DialogResult DownloadFiles(
            IWin32Window owner,
            string caption,
            RemoteFileSystemChannel channel,
            IExceptionDialog exceptionDialog,
            out IEnumerable<string> sourcePaths,
            out DirectoryInfo targetDirectory);
    }

    internal class SftpFileDialog : ISftpFileDialog // TODO: Make generic DownloadDialog -> IFileSystem -> IFileItem
    {
        public DialogResult DownloadFiles(
            IWin32Window owner,
            string caption,
            RemoteFileSystemChannel channel,
            IExceptionDialog exceptionDialog,
            out IEnumerable<string> sourcePaths,
            out DirectoryInfo targetDirectory)
        {
            sourcePaths = null;
            targetDirectory = null;

            using (var fileSystem = new SftpFileSystem(channel))
            using (var form = new SftpFileDialogForm(fileSystem)
            {
                Text = caption
            })
            {
                var result = form.ShowDialog(owner);

                if (result == DialogResult.OK)
                {
                    targetDirectory = new DirectoryInfo(form.TargetDirectory);
                    sourcePaths = form.SelectedPaths;
                }

                return result;
            }
        }
    }
}
