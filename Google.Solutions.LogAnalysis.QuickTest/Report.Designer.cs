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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.nodesByDay = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label1 = new System.Windows.Forms.Label();
            this.nodesList = new System.Windows.Forms.ListView();
            this.serverId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.firstUse = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lastUse = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.daysUsed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.peakInstances = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            ((System.ComponentModel.ISupportInitialize)(this.nodesByDay)).BeginInit();
            this.SuspendLayout();
            // 
            // nodesByDay
            // 
            this.nodesByDay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodesByDay.BackColor = System.Drawing.SystemColors.Control;
            chartArea1.AxisX.MajorGrid.Enabled = false;
            chartArea1.AxisX.ScaleView.Zoomable = false;
            chartArea1.AxisY.IntervalType = System.Windows.Forms.DataVisualization.Charting.DateTimeIntervalType.Number;
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.SystemColors.ControlDarkDark;
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea1.BackColor = System.Drawing.SystemColors.Control;
            chartArea1.BorderColor = System.Drawing.Color.DimGray;
            chartArea1.CursorX.IsUserSelectionEnabled = true;
            chartArea1.Name = "mainArea";
            this.nodesByDay.ChartAreas.Add(chartArea1);
            this.nodesByDay.Location = new System.Drawing.Point(0, 34);
            this.nodesByDay.Name = "nodesByDay";
            this.nodesByDay.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Grayscale;
            series1.ChartArea = "mainArea";
            series1.Name = "Series1";
            this.nodesByDay.Series.Add(series1);
            this.nodesByDay.Size = new System.Drawing.Size(832, 300);
            this.nodesByDay.TabIndex = 0;
            this.nodesByDay.SelectionRangeChanged += new System.EventHandler<System.Windows.Forms.DataVisualization.Charting.CursorEventArgs>(this.nodesByDay_SelectionRangeChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.label1.Location = new System.Drawing.Point(23, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Nodes";
            // 
            // nodesList
            // 
            this.nodesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nodesList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.serverId,
            this.firstUse,
            this.lastUse,
            this.daysUsed,
            this.peakInstances});
            this.nodesList.FullRowSelect = true;
            this.nodesList.HideSelection = false;
            this.nodesList.Location = new System.Drawing.Point(38, 354);
            this.nodesList.Name = "nodesList";
            this.nodesList.Size = new System.Drawing.Size(765, 209);
            this.nodesList.TabIndex = 2;
            this.nodesList.UseCompatibleStateImageBehavior = false;
            this.nodesList.View = System.Windows.Forms.View.Details;
            // 
            // serverId
            // 
            this.serverId.Text = "Server ID";
            this.serverId.Width = 250;
            // 
            // firstUse
            // 
            this.firstUse.Text = "First use";
            this.firstUse.Width = 130;
            // 
            // lastUse
            // 
            this.lastUse.Text = "Last Use";
            this.lastUse.Width = 130;
            // 
            // daysUsed
            // 
            this.daysUsed.Text = "Days used";
            this.daysUsed.Width = 70;
            // 
            // peakInstances
            // 
            this.peakInstances.Text = "Peak # instances";
            this.peakInstances.Width = 100;
            // 
            // Report
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.AutoScrollMinSize = new System.Drawing.Size(800, 600);
            this.ClientSize = new System.Drawing.Size(832, 762);
            this.Controls.Add(this.nodesList);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nodesByDay);
            this.Name = "Report";
            this.Text = "Report";
            ((System.ComponentModel.ISupportInitialize)(this.nodesByDay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart nodesByDay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListView nodesList;
        private System.Windows.Forms.ColumnHeader serverId;
        private System.Windows.Forms.ColumnHeader firstUse;
        private System.Windows.Forms.ColumnHeader lastUse;
        private System.Windows.Forms.ColumnHeader peakInstances;
        private System.Windows.Forms.ColumnHeader daysUsed;
    }
}