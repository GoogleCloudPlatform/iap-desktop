using Google.Solutions.LogAnalysis.History;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        private NodeSetHistory currentNodeSet;

        private DateTime selectionStartDate;
        private DateTime selectionEndDate;

        public Report(InstanceSetHistory instanceSet)
        {
            this.instanceSet = instanceSet;

            InitializeComponent();

            // Remove space between bars.
            this.chart.Series[0]["PointWidth"] = "1";

            // Reset selection.
            this.selectionEndDate = instanceSet.StartDate;
            this.selectionEndDate = instanceSet.EndDate;
            Repopulate();
        }



        //---------------------------------------------------------------------
        // Data population.
        //---------------------------------------------------------------------

        private void Repopulate()
        {

            if (this.tabControl.SelectedTab == this.instancesTabPage)
            {
                this.currentNodeSet = NodeSetHistory.FromInstancyHistory(
                    this.instanceSet.Instances,
                    true,
                    false);
                Debug.Assert(this.currentNodeSet.Nodes.Count() <= 1);

                RepopulateChart(this.currentNodeSet.MaxInstancePlacementsByDay);
                RepopulateNodeList();
                RepopulateInstancesList(this.currentNodeSet.Nodes.FirstOrDefault());
            }
            else if (this.tabControl.SelectedTab == this.nodesTabPage)
            {
                this.currentNodeSet = NodeSetHistory.FromInstancyHistory(
                    this.instanceSet.Instances, 
                    false, 
                    true);

                RepopulateChart(this.currentNodeSet.MaxNodesByDay);
                RepopulateNodeList();
                RepopulateInstancesList(null);
            }
        }

        private void RepopulateChart(IEnumerable<History.DataPoint> dataPoints)
        {
            this.chart.Series[0].Points.Clear();
            foreach (var dp in dataPoints)
            {
                this.chart.Series[0].Points.AddXY(dp.Timestamp, dp.Value);
            }
        }

        private void RepopulateNodeList()
        {
            var nodes = this.currentNodeSet.Nodes.Where(
                n => n.FirstUse <= this.selectionEndDate && n.LastUse >= this.selectionStartDate);

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

            if (nodes.Any())
            {
                this.nodesList.Items[0].Selected = true;
                RepopulateInstancesList(nodes.First());
            }
            else
            {
                RepopulateInstancesList(null);
            }
        }

        private void RepopulateInstancesList(NodeHistory node)
        {
            this.nodePlacementsList.Items.Clear();

            if (node != null)
            {
                var placements = node.Placements.Where(
                    p => p.From <= this.selectionEndDate && p.To >= this.selectionStartDate);

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

            RepopulateNodeList();
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

            RepopulateInstancesList(node);
        }

        private void nodesList_SelectedIndexChanged(object sender, EventArgs e) 
            => nodesList_Click(sender, e);

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            Repopulate();
        }
    }
}
