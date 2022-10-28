using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using Google.Solutions.Mvvm.Shell;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    internal class SftpFileDialogViewModel : ViewModelBase
    {
        public SftpFileDialogViewModel()
        {
            this.SelectedFiles = ObservableProperty.Build(Enumerable.Empty<FileBrowser.IFileItem>());
            this.TargetDirectory = ObservableProperty.Build(KnownFolders.Downloads);
            this.IsDownloadButtonEnabled = ObservableProperty.Build(
                this.SelectedFiles,
                files => files.Any() && files.All(f => f.Type.IsFile));
        }

        //---------------------------------------------------------------------
        // Observable properties.
        //---------------------------------------------------------------------

        public ObservableProperty<IEnumerable<FileBrowser.IFileItem>> SelectedFiles { get; }
        public ObservableProperty<string> TargetDirectory { get; }
        public ObservableFunc<bool> IsDownloadButtonEnabled { get; }
    }
}
