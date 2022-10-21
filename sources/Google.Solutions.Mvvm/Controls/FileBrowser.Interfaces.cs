using System;
using System.IO;
using System.ComponentModel;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser
    {
        /// <summary>
        /// A file or directory.
        /// </summary>
        public interface IFileItem : INotifyPropertyChanged
        {
            /// <summary>
            /// Unqualified name of file.
            /// </summary>
            string Name { get; }

            /// <summary>
            /// Check if this is a file (as opposed to a directory).
            /// </summary>
            bool IsFile { get; }

            FileAttributes Attributes { get; }

            DateTime LastModified { get; }

            ulong Size { get; }
        }
    }
}
