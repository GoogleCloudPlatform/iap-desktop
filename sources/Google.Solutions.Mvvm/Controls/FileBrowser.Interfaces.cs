using System;
using System.IO;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser
    {
        /// <summary>
        /// A file or directory.
        /// </summary>
        public interface IFileItem
        {
            /// <summary>
            /// Unqualified name of file.
            /// </summary>
            string Name { get; }

            FileAttributes Attributes { get; }

            DateTime LastModified { get; }
        }
    }
}
