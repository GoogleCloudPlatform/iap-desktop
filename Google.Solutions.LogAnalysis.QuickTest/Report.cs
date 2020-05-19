using Google.Solutions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Google.Solutions.LogAnalysis.QuickTest
{
    public partial class Report : Form
    {
        private readonly InstanceSetHistory instanceSet;
        private readonly NodeSetHistory nodeSet;

        public Report(InstanceSetHistory instanceSet, NodeSetHistory nodeSet)
        {
            this.instanceSet = instanceSet;
            this.nodeSet = nodeSet;

            InitializeComponent();

            // Remove space between bars.
            this.nodesByDay.Series[0]["PointWidth"] = "1";

            foreach (var dp in nodeSet.MaxNodesByDay)
            {
                this.nodesByDay.Series[0].Points.AddXY(dp.Timestamp, dp.Value);
            }
        }
    }
}
