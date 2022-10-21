using Google.Apis.Util;
using Google.Solutions.Common.Util;
using Google.Solutions.Mvvm.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser : UserControl
    {
        private bool bound = false;
        private readonly FileTypeCache fileTypeCache = new FileTypeCache();

        //---------------------------------------------------------------------
        // Privates.
        //---------------------------------------------------------------------

        private int GetImageIndex(FileType fileType)
        {
            var imageIndex = this.fileIconsList.Images.IndexOfKey(fileType.TypeName);
            if (imageIndex == -1)
            {
                //
                // Image not added to list yet.
                //
                this.fileIconsList.Images.Add(fileType.TypeName, fileType.FileIcon);
                imageIndex = this.fileIconsList.Images.IndexOfKey(fileType.TypeName);

                Debug.Assert(imageIndex != -1);
            }

            return imageIndex;
        }

        public FileBrowser()
        {
            InitializeComponent();

            this.Disposed += (s, e) =>
            {
                this.fileTypeCache.Dispose();
            };
        }


        //---------------------------------------------------------------------
        // Events.
        //---------------------------------------------------------------------

        public EventHandler<ExceptionEventArgs> NavigationFailed;

        protected void OnNavigationFailed(Exception e)
            => this.NavigationFailed?.Invoke(this, new ExceptionEventArgs(e));

        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        public IEnumerable<IFileItem> SelectedItems => this.fileList.SelectedModelItems;
        public IFileItem SelectedItem => this.fileList.SelectedModelItem;

        //---------------------------------------------------------------------
        // Data Binding.
        //---------------------------------------------------------------------

        public void Bind(
            IFileItem root,
            Func<IFileItem, Task<ObservableCollection<IFileItem>>> listFiles)
        {
            root.ThrowIfNull(nameof(root));
            listFiles.ThrowIfNull(nameof(listFiles));

            if (this.bound)
            {
                throw new InvalidOperationException("Control is already bound");
            }
            else
            {
                this.bound = true;
            }

            //
            // Bind directory tree.
            //
            this.directoryTree.BindIsLeaf(i => i.Type.IsFile);
            this.directoryTree.BindText(i => i.Name);
            this.directoryTree.BindIsExpanded(i => i.IsExpanded);

            // TODO: Project to filter out files
            this.directoryTree.BindChildren(listFiles);
            this.directoryTree.Bind(root);

            // TODO: Change to accept Func<> also
            //
            //this.directoryTree.BindImageIndex(i => i.ImageIndex);
            //this.directoryTree.BindSelectedImageIndex(i => i.SelectedImageIndex);


            //
            // Bind file list.
            //
            this.fileList.BindImageIndex(i => GetImageIndex(i.Type));
            this.fileList.BindColumn(0, i => i.Name);
            this.fileList.BindColumn(1, i => i.LastModified.ToString()); // TODO: Formatting?
            this.fileList.BindColumn(2, i => i.Type.TypeName);
            this.fileList.BindColumn(3, i => i.Size.ToString()); // TODO: Use ByteSizeFormatter

            this.directoryTree.SelectedModelNodeChanged += async (s, _) =>
            {
                try
                {
                    var files = await listFiles(this.directoryTree.SelectedModelNode)
                        .ConfigureAwait(true);
                    this.fileList.BindCollection(files);
                }
                catch (Exception e)
                {
                    OnNavigationFailed(e);
                }
            };
            this.directoryTree.LoadingChildrenFailed += (s, args) => OnNavigationFailed(args.Exception);
        }

        private void directoryTree_LoadingChildrenFailed(object sender, ExceptionEventArgs e)
        {

        }

        private void directoryTree_SelectedModelNodeChanged(object sender, EventArgs e)
        {

        }
    }
}
