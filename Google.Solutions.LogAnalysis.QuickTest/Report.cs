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
                this.chartLabel.Text = "Active VM instances";

                this.currentNodeSet = NodeSetHistory.FromInstancyHistory(
                    this.instanceSet.Instances,
                    this.includeFleetVmInstancesMenuItem.Checked,
                    this.includeSoleTenantVmInstancesMenuItem.Checked);

                RepopulateChart(this.currentNodeSet.MaxInstancePlacementsByDay);
                //RepopulateNodeList();
                RepopulateInstancesList(this.currentNodeSet.Nodes.SelectMany(n => n.Placements));
            }
            else if (this.tabControl.SelectedTab == this.nodesTabPage)
            {
                this.chartLabel.Text = "Active number of sole-tenant nodes";

                this.currentNodeSet = NodeSetHistory.FromInstancyHistory(
                    this.instanceSet.Instances, 
                    false, 
                    true);

                RepopulateChart(this.currentNodeSet.MaxNodesByDay);
                RepopulateNodeList();
            }

            this.splitContainer.Panel1Collapsed = (this.tabControl.SelectedTab == this.instancesTabPage);
        }

        private void RepopulateChart(IEnumerable<History.DataPoint> dataPoints)
        {
            this.chart.Series[0].Points.Clear();
            foreach (var dp in dataPoints)
            {
                Debug.WriteLine(dp.Value);
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
                RepopulateInstancesList(nodes.First().Placements);
            }
            else
            {
                RepopulateInstancesList(Enumerable.Empty<NodePlacement>());
            }
        }

        private void RepopulateInstancesList(IEnumerable<NodePlacement> nodePlacements)
        {
            this.nodePlacementsList.Items.Clear();

            var placements = nodePlacements.Where(
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

            Repopulate();
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

            RepopulateInstancesList(node.Placements);
        }

        private void nodesList_SelectedIndexChanged(object sender, EventArgs e) 
            => nodesList_Click(sender, e);

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            Repopulate();
        }

        private void includeInstancesCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Repopulate();
        }

        private void chart_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            switch (e.HitTestResult.ChartElementType)
            {
                case ChartElementType.DataPoint:
                    var dataPoint = e.HitTestResult.Series.Points[e.HitTestResult.PointIndex];
                    e.Text = $"{DateTime.FromOADate(dataPoint.XValue)}: {dataPoint.YValues[0]}";
                    break;
            }
        }

        private void includeInstancesMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = !menuItem.Checked;
            }

            Repopulate();
        }

        private void chartMenu_Opening(object sender, CancelEventArgs e)
        {
            this.includeSoleTenantVmInstancesMenuItem.Visible =
                this.includeFleetVmInstancesMenuItem.Visible =
                (this.tabControl.SelectedTab == this.instancesTabPage);
        }
    }
}
