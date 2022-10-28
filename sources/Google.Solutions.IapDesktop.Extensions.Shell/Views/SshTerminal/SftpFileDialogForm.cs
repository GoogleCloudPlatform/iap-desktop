using Google.Solutions.Mvvm.Binding;
using Google.Solutions.Mvvm.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Extensions.Shell.Views.SshTerminal
{
    internal partial class SftpFileDialogForm : Form
    {
        private readonly SftpFileDialogViewModel viewModel;

        public SftpFileDialogForm(SftpFileSystem fileSystem)
        {
            InitializeComponent();

            this.viewModel = new SftpFileDialogViewModel();

            this.targetDirectoryTextBox.BindProperty(
                c => c.Text,
                this.viewModel,
                m => m.TargetDirectory,
                this.Container);
            this.fileBrowser.BindProperty(
                c => c.SelectedFiles,
                this.viewModel,
                m => m.SelectedFiles,
                this.Container);
            this.downloadButton.BindProperty(
                c => c.Enabled,
                this.viewModel,
                m => m.IsDownloadButtonEnabled,
                this.Container);
            this.fileBrowser.Bind(fileSystem);

            this.browseButton.Click += (s, args) =>
            {
                using (var dialog = new FolderBrowserDialog()
                {
                    Description = "Select target directory",
                    SelectedPath = this.viewModel.TargetDirectory.Value
                })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        this.viewModel.TargetDirectory.Value = dialog.SelectedPath;
                    }
                }
            };
        }

        public string TargetDirectory => this.targetDirectoryTextBox.Text;

        public IEnumerable<string> SelectedPaths => this.viewModel
            .SelectedFiles
            .Value
            .Cast<SftpFileSystem.ISftpFileItem>()
            .Select(i => i.RemotePath);
    }
}
