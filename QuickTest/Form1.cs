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

            this.treeView.OnControlPropertyChange(
                t => t.SelectedModelNode,
                (SampleNodeViewModel n) => this.label1.Text = n?.Name);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.root.Name += "1";
            this.root.ImageIndex = 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.treeView.SelectedModelNode.AddMore();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.treeView.SelectedModelNode.RemoveFirst();
        }
    }


    class SampleNodeViewModel : ViewModelBase
    {
        private int imageIndex;
        private string name;
        private bool expanded = false;


        private readonly ObservableCollection<SampleNodeViewModel> coll 
            = new ObservableCollection<SampleNodeViewModel>();

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
            coll.Add(new SampleNodeViewModel()
            {
                Name = "sub " + Guid.NewGuid().ToString().Substring(0, 4)
            });
            coll.Add(new SampleNodeViewModel()
            {
                Name = "sub " + Guid.NewGuid().ToString().Substring(0, 4)
            });
            return coll;
        }

        public void AddMore()
        {

            coll.Add(new SampleNodeViewModel()
            {
                Name = "sub " + Guid.NewGuid().ToString().Substring(0, 5)
            });
        }

        public void RemoveFirst()
        {
            this.coll.Remove(this.coll.First());
        }
    }

    class SampleTreeView : BindableTreeView<SampleNodeViewModel>
    { }
}
