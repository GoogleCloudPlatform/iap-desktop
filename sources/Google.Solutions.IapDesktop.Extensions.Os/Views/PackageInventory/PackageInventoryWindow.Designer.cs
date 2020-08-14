namespace Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory
{
    partial class PackageInventoryWindow
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
            this.packageList = new Google.Solutions.IapDesktop.Extensions.Os.Views.PackageInventory.PackageList();
            this.SuspendLayout();
            // 
            // packageList
            // 
            this.packageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packageList.Loading = true;
            this.packageList.Location = new System.Drawing.Point(0, 0);
            this.packageList.Name = "packageList";
            this.packageList.SearchTerm = "";
            this.packageList.Size = new System.Drawing.Size(800, 450);
            this.packageList.TabIndex = 0;
            // 
            // PackageInventoryWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.packageList);
            this.Name = "PackageInventoryWindow";
            this.Text = "PackageInventoryWindow";
            this.ResumeLayout(false);

        }

        #endregion

        private PackageList packageList;
    }
}