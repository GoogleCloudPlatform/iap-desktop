namespace Google.Solutions.LogAnalysis.QuickTest
{
    partial class Report
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.nodesByDay = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.nodesByDayLabel = new System.Windows.Forms.Label();
            this.nodesList = new System.Windows.Forms.ListView();
            this.serverIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodeZoneColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodeProjectIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.firstUseColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lastUseColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.daysUsedColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.peakInstancesColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodePlacementsList = new System.Windows.Forms.ListView();
            this.instanceIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceNameColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceZoneColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.instanceProjectIdColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.placedFromColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.placedUntilColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nodesLabel = new System.Windows.Forms.Label();
            this.nodePlacementsLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nodesByDay)).BeginInit();
            this.SuspendLayout();
            // 
            // nodesByDay
            // 
            this.nodesByDay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodesByDay.BackColor = System.Drawing.SystemColors.Control;
            chartArea2.AxisX.MajorGrid.Enabled = false;
            chartArea2.AxisX.ScaleView.Zoomable = false;
            chartArea2.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea2.AxisY.MajorGrid.LineColor = System.Drawing.SystemColors.ControlDarkDark;
            chartArea2.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea2.BackColor = System.Drawing.SystemColors.Control;
            chartArea2.BorderColor = System.Drawing.Color.DimGray;
            chartArea2.CursorX.IsUserSelectionEnabled = true;
            chartArea2.Name = "mainArea";
            this.nodesByDay.ChartAreas.Add(chartArea2);
            this.nodesByDay.Location = new System.Drawing.Point(0, 34);
            this.nodesByDay.Name = "nodesByDay";
            this.nodesByDay.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Grayscale;
            series2.ChartArea = "mainArea";
            series2.Name = "Series1";
            this.nodesByDay.Series.Add(series2);
            this.nodesByDay.Size = new System.Drawing.Size(834, 300);
            this.nodesByDay.TabIndex = 0;
            this.nodesByDay.SelectionRangeChanged += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.CursorEventArgs>(this.nodesByDay_SelectionRangeChanged);
            // 
            // nodesByDayLabel
            // 
            this.nodesByDayLabel.AutoSize = true;
            this.nodesByDayLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodesByDayLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.nodesByDayLabel.Location = new System.Drawing.Point(35, 13);
            this.nodesByDayLabel.Name = "nodesByDayLabel";
            this.nodesByDayLabel.Size = new System.Drawing.Size(274, 18);
            this.nodesByDayLabel.TabIndex = 1;
            this.nodesByDayLabel.Text = "Active number of sole-tenant nodes";
            // 
            // nodesList
            // 
            this.nodesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodesList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.serverIdColumnHeader,
            this.nodeZoneColumnHeader,
            this.nodeProjectIdColumnHeader,
            this.firstUseColumnHeader,
            this.lastUseColumnHeader,
            this.daysUsedColumnHeader,
            this.peakInstancesColumnHeader});
            this.nodesList.FullRowSelect = true;
            this.nodesList.HideSelection = false;
            this.nodesList.Location = new System.Drawing.Point(38, 354);
            this.nodesList.Name = "nodesList";
            this.nodesList.Size = new System.Drawing.Size(767, 230);
            this.nodesList.TabIndex = 2;
            this.nodesList.UseCompatibleStateImageBehavior = false;
            this.nodesList.View = System.Windows.Forms.View.Details;
            this.nodesList.SelectedIndexChanged += new System.EventHandler(this.nodesList_SelectedIndexChanged);
            this.nodesList.Click += new System.EventHandler(this.nodesList_Click);
            // 
            // serverIdColumnHeader
            // 
            this.serverIdColumnHeader.Text = "Server ID";
            this.serverIdColumnHeader.Width = 150;
            // 
            // nodeZoneColumnHeader
            // 
            this.nodeZoneColumnHeader.Text = "Zone";
            this.nodeZoneColumnHeader.Width = 80;
            // 
            // nodeProjectIdColumnHeader
            // 
            this.nodeProjectIdColumnHeader.Text = "Project Id";
            this.nodeProjectIdColumnHeader.Width = 120;
            // 
            // firstUseColumnHeader
            // 
            this.firstUseColumnHeader.Text = "First use";
            this.firstUseColumnHeader.Width = 130;
            // 
            // lastUseColumnHeader
            // 
            this.lastUseColumnHeader.Text = "Last Use";
            this.lastUseColumnHeader.Width = 130;
            // 
            // daysUsedColumnHeader
            // 
            this.daysUsedColumnHeader.Text = "Days used";
            this.daysUsedColumnHeader.Width = 70;
            // 
            // peakInstancesColumnHeader
            // 
            this.peakInstancesColumnHeader.Text = "Peak VMs";
            this.peakInstancesColumnHeader.Width = 80;
            // 
            // nodePlacementsList
            // 
            this.nodePlacementsList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodePlacementsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.instanceIdColumnHeader,
            this.instanceNameColumnHeader,
            this.instanceZoneColumnHeader,
            this.instanceProjectIdColumnHeader,
            this.placedFromColumnHeader,
            this.placedUntilColumnHeader});
            this.nodePlacementsList.FullRowSelect = true;
            this.nodePlacementsList.HideSelection = false;
            this.nodePlacementsList.Location = new System.Drawing.Point(38, 617);
            this.nodePlacementsList.Name = "nodePlacementsList";
            this.nodePlacementsList.Size = new System.Drawing.Size(767, 230);
            this.nodePlacementsList.TabIndex = 2;
            this.nodePlacementsList.UseCompatibleStateImageBehavior = false;
            this.nodePlacementsList.View = System.Windows.Forms.View.Details;
            // 
            // instanceIdColumnHeader
            // 
            this.instanceIdColumnHeader.Text = "Instance ID";
            this.instanceIdColumnHeader.Width = 120;
            // 
            // instanceNameColumnHeader
            // 
            this.instanceNameColumnHeader.Text = "Instance name";
            this.instanceNameColumnHeader.Width = 130;
            // 
            // instanceZoneColumnHeader
            // 
            this.instanceZoneColumnHeader.Text = "Zone";
            this.instanceZoneColumnHeader.Width = 80;
            // 
            // instanceProjectIdColumnHeader
            // 
            this.instanceProjectIdColumnHeader.Text = "Project Id";
            this.instanceProjectIdColumnHeader.Width = 120;
            // 
            // placedFromColumnHeader
            // 
            this.placedFromColumnHeader.Text = "From";
            this.placedFromColumnHeader.Width = 130;
            // 
            // placedUntilColumnHeader
            // 
            this.placedUntilColumnHeader.Text = "To";
            this.placedUntilColumnHeader.Width = 130;
            // 
            // nodesLabel
            // 
            this.nodesLabel.AutoSize = true;
            this.nodesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodesLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.nodesLabel.Location = new System.Drawing.Point(35, 333);
            this.nodesLabel.Name = "nodesLabel";
            this.nodesLabel.Size = new System.Drawing.Size(102, 18);
            this.nodesLabel.TabIndex = 1;
            this.nodesLabel.Text = "Node details";
            // 
            // nodePlacementsLabel
            // 
            this.nodePlacementsLabel.AutoSize = true;
            this.nodePlacementsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodePlacementsLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.nodePlacementsLabel.Location = new System.Drawing.Point(35, 596);
            this.nodePlacementsLabel.Name = "nodePlacementsLabel";
            this.nodePlacementsLabel.Size = new System.Drawing.Size(227, 18);
            this.nodePlacementsLabel.TabIndex = 1;
            this.nodePlacementsLabel.Text = "Instances scheduled on node";
            // 
            // Report
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(810, 880);
            this.ClientSize = new System.Drawing.Size(834, 911);
            this.Controls.Add(this.nodePlacementsList);
            this.Controls.Add(this.nodesList);
            this.Controls.Add(this.nodePlacementsLabel);
            this.Controls.Add(this.nodesLabel);
            this.Controls.Add(this.nodesByDayLabel);
            this.Controls.Add(this.nodesByDay);
            this.Name = "Report";
            this.Text = "Report";
            ((System.ComponentModel.ISupportInitialize)(this.nodesByDay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart nodesByDay;
        private System.Windows.Forms.Label nodesByDayLabel;
        private System.Windows.Forms.ListView nodesList;
        private System.Windows.Forms.ColumnHeader serverIdColumnHeader;
        private System.Windows.Forms.ColumnHeader firstUseColumnHeader;
        private System.Windows.Forms.ColumnHeader lastUseColumnHeader;
        private System.Windows.Forms.ColumnHeader peakInstancesColumnHeader;
        private System.Windows.Forms.ColumnHeader daysUsedColumnHeader;
        private System.Windows.Forms.ListView nodePlacementsList;
        private System.Windows.Forms.ColumnHeader instanceIdColumnHeader;
        private System.Windows.Forms.ColumnHeader instanceNameColumnHeader;
        private System.Windows.Forms.ColumnHeader instanceZoneColumnHeader;
        private System.Windows.Forms.ColumnHeader instanceProjectIdColumnHeader;
        private System.Windows.Forms.ColumnHeader placedFromColumnHeader;
        private System.Windows.Forms.ColumnHeader placedUntilColumnHeader;
        private System.Windows.Forms.ColumnHeader nodeZoneColumnHeader;
        private System.Windows.Forms.ColumnHeader nodeProjectIdColumnHeader;
        private System.Windows.Forms.Label nodesLabel;
        private System.Windows.Forms.Label nodePlacementsLabel;
    }
}