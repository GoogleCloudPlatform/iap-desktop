using Google.Solutions.IapDesktop.Application.Controls;
using Google.Solutions.IapDesktop.Application.ObjectModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuickTest
{
    public partial class Form1 : Form
    {
        private SampleNodeViewModel root;

        public Form1()
        {
            InitializeComponent();

            this.root = new SampleNodeViewModel()
            {
                Name = "root",
                ImageIndex = 0
            };

            this.treeView.BindImageIndex(m => m.ImageIndex);
            this.treeView.BindSelectedImageIndex(m => m.ImageIndex);
            this.treeView.BindText(m => m.Name);
            this.treeView.BindIsLeaf(m => m.IsLeaf);
            this.treeView.BindIsExpanded(m => m.IsExpanded);
            this.treeView.BindChildren(m => m.GetChildren());
            this.treeView.Bind(this.root);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.root.Name += "1";
            this.root.ImageIndex = 1;
        }
    }


    class SampleNodeViewModel : ViewModelBase
    {
        private int imageIndex;
        private string name;
        private bool expanded = false;

        public bool IsLeaf => false;

        public string Name
        {
            get => this.name + (expanded ? " (expanded)" : "");
            set
            {
                this.name = value;
                RaisePropertyChange();
            }
        }

        public int ImageIndex
        {
            get => this.imageIndex;
            set
            {
                this.imageIndex = value;
                RaisePropertyChange();
            }
        }

        public bool IsExpanded
        {
            get => this.expanded;
            set
            {
                this.expanded = value;
                RaisePropertyChange();
                RaisePropertyChange("Name");
            }
        }

        public async Task<ObservableCollection<SampleNodeViewModel>> GetChildren()
        {
            await Task.Delay(1000);
            var coll = new ObservableCollection<SampleNodeViewModel>();
            coll.Add(new SampleNodeViewModel()
            {
                Name = "sub"
            });
            return coll;
        }
    }

    class SampleTreeView : BindableTreeView<SampleNodeViewModel>
    { }
}
