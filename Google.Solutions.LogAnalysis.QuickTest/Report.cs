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

        private DateTime selectionStartDate;
        private DateTime selectionEndDate;

        public Report(InstanceSetHistory instanceSet, NodeSetHistory nodeSet)
        {
            this.instanceSet = instanceSet;
            this.nodeSet = nodeSet;
            this.selectionEndDate = instanceSet.StartDate;
            this.selectionEndDate = instanceSet.EndDate;

            InitializeComponent();

            // Remove space between bars.
            this.nodesByDay.Series[0]["PointWidth"] = "1";

            Repopulate();
        }



        //---------------------------------------------------------------------
        // Data population.
        //---------------------------------------------------------------------

        private void Repopulate()
        {
            if (this.tabControl.SelectedTab == this.instancesTabPage)
            {
                RepopulateChart(Enumerable.Empty<History.DataPoint>());
                RepopulateNodeList(Enumerable.Empty<NodeHistory>());
                RepopulateInstancesList(Enumerable.Empty<NodePlacement>());

            }
            else if (this.tabControl.SelectedTab == this.nodesTabPage)
            {
                RepopulateChart(nodeSet.MaxNodesByDay);
                RepopulateNodeList(this.nodeSet.Nodes);
                RepopulateInstancesList(Enumerable.Empty<NodePlacement>());
            }
        }

        private void RepopulateChart(IEnumerable<History.DataPoint> dataPoints)
        {
            this.nodesByDay.Series[0].Points.Clear();
            foreach (var dp in dataPoints)
            {
                this.nodesByDay.Series[0].Points.AddXY(dp.Timestamp, dp.Value);
            }
        }

        private void RepopulateNodeList(IEnumerable<NodeHistory> nodes)
        {
            this.nodePlacementsList.Items.Clear();
            this.nodesList.Items.Clear();
            this.nodesList.Items.AddRange(
                nodes.Select(n => new ListViewItem(new[]
                {
                    n.ServerId,
                    n.Zone,
                    n.ProjectId,
                    n.FirstUse.ToString(),
                    n.LastUse.ToString(),
                    Math.Ceiling((n.LastUse - n.FirstUse).TotalDays).ToString(),
                    n.PeakConcurrentPlacements.ToString()
                })
                {
                    Tag = n
                })
                .ToArray());
        }

        private void RepopulateInstancesList(IEnumerable<NodePlacement> placements)
        {
            this.nodePlacementsList.Items.Clear();
            this.nodePlacementsList.Items.AddRange(
                placements.Select(p => new ListViewItem(new[]
                {
                    p.Instance.InstanceId.ToString(),
                    p.Instance.Reference != null ? p.Instance.Reference.InstanceName : string.Empty,
                    p.Instance.Reference != null ? p.Instance.Reference.Zone : string.Empty,
                    p.Instance.Reference != null ? p.Instance.Reference.ProjectId : string.Empty,
                    p.From.ToString(),
                    p.To.ToString()
                })
                {
                    Tag = p
                })
                .ToArray());
        }

        //---------------------------------------------------------------------
        // Window event handlers.
        //---------------------------------------------------------------------

        private void nodesByDay_SelectionRangeChanged(object sender, CursorEventArgs e)
        {
            var start = DateTime.FromOADate(Math.Min(e.NewSelectionStart, e.NewSelectionEnd));
            var end = DateTime.FromOADate(Math.Max(e.NewSelectionStart, e.NewSelectionEnd));

            if (start == end)
            {
                // reset.
                this.selectionStartDate = this.instanceSet.StartDate;
                this.selectionEndDate = this.instanceSet.EndDate;
            }
            else
            {
                this.selectionStartDate = start;
                this.selectionEndDate = end;
            }


            var nodes = this.nodeSet.Nodes.Where(
                n => n.FirstUse <= this.selectionEndDate && n.LastUse >= this.selectionStartDate);
            RepopulateNodeList(nodes);
        }

        private void nodesList_Click(object sender, EventArgs e)
        {
            var selectedItem = this.nodesList.SelectedItems
                .Cast<ListViewItem>()
                .FirstOrDefault();
            if (selectedItem == null)
            {
                return;
            }

            var node = (NodeHistory)selectedItem.Tag;

            RepopulateInstancesList(
                node.Placements.Where(
                    p => p.From <= this.selectionEndDate && p.To >= this.selectionStartDate));
        }

        private void nodesList_SelectedIndexChanged(object sender, EventArgs e) 
            => nodesList_Click(sender, e);

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            Repopulate();
        }
    }
}
