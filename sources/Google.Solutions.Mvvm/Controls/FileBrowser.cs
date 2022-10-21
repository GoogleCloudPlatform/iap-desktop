using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        //---------------------------------------------------------------------
        // Selection properties.
        //---------------------------------------------------------------------

        public IEnumerable<IFileItem> SelectedItems => throw new NotImplementedException();

        //---------------------------------------------------------------------
        // Data Binding.
        //---------------------------------------------------------------------

        public void Bind(
            Func<IFileItem, Task<ObservableCollection<IFileItem>>> listFiles)
        {
            throw new NotImplementedException();
        }
    }
}
