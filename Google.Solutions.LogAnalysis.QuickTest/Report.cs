using Google.Solutions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

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

            RepopulateNodeList(nodeSet.Nodes);
        }

        private void nodesByDay_SelectionRangeChanged(object sender, CursorEventArgs e)
        {
            var start = DateTime.FromOADate(Math.Min(e.NewSelectionStart, e.NewSelectionEnd));
            var end = DateTime.FromOADate(Math.Max(e.NewSelectionStart, e.NewSelectionEnd));

            if (start == end)
            {
                // Reset.
                RepopulateNodeList(nodeSet.Nodes);
            }
            else
            { 
                RepopulateNodeList(this.nodeSet.Nodes
                    .Where(n => n.FirstUse <= end && n.LastUse >= start));
            }
        }

        private void RepopulateNodeList(IEnumerable<NodeHistory> nodes)
        {
            this.nodesList.Items.Clear();
            this.nodesList.Items.AddRange(
                nodes.Select(n => new ListViewItem(new[]
                {
                    n.ServerId,
                    n.FirstUse.ToString(),
                    n.LastUse.ToString(),
                    Math.Ceiling((n.LastUse - n.FirstUse).TotalDays).ToString(),
                    n.PeakConcurrentPlacements.ToString()
                }))
                .ToArray());
        }
    }
}
