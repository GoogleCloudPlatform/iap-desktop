using Google.Apis.Util;
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
        private readonly FileTypeCache fileTypeCache = new FileTypeCache();

        private FileType GetFileType(IFileItem item, FileType.IconFlags iconFlags)
        {
            var fileType = this.fileTypeCache.Lookup(
                item.Name,
                item.Attributes,
                iconFlags);
            Debug.Assert(fileType != null);

            return fileType;
        }

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

        private int GetImageIndex(IFileItem item, FileType.IconFlags iconFlags)
        {
            return GetImageIndex(GetFileType(item, iconFlags));
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

            //
            // Bind directory tree.
            //
            this.directoryTree.Bind(root);
            this.directoryTree.BindIsLeaf(i => i.IsFile);
            this.directoryTree.BindText(i => i.Name);

            // TODO: Project to filter out files
            this.directoryTree.BindChildren(listFiles);

            // TODO: Change to accept Func<> also
            //this.directoryTree.BindImageIndex(i => i.ImageIndex);
            //this.directoryTree.BindSelectedImageIndex(i => i.SelectedImageIndex);


            //
            // Bind file list.
            //
            this.fileList.BindImageIndex(i => GetImageIndex(i, FileType.IconFlags.Small));
            this.fileList.BindColumn(0, i => i.Name);
            this.fileList.BindColumn(1, i => i.LastModified.ToString()); // TODO: Formatting?
            this.fileList.BindColumn(2, i => GetFileType(i, FileType.IconFlags.Small).TypeName);
            this.fileList.BindColumn(3, i => i.Size.ToString()); // TODO: Use ByteSizeFormatter

            this.directoryTree.SelectedModelNodeChanged += async (s, e) =>
            {
                var files = await listFiles(this.directoryTree.SelectedModelNode)
                    .ConfigureAwait(true);
                this.fileList.BindCollection(files);
            };
        }
    }
}
