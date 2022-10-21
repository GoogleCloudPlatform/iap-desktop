using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Controls
{
    public partial class FileBrowser : UserControl
    {
        public FileBrowser()
        {
            InitializeComponent();
        }

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
