namespace DatabaseCloner {
    partial class frmMain {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing ) {
            if( disposing && ( components != null ) ) {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tbConnectionSource = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnGenerateSource = new System.Windows.Forms.Button();
            this.btnGenerateDestination = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbConnectionDestination = new System.Windows.Forms.TextBox();
            this.btnSelectData = new System.Windows.Forms.Button();
            this.cbDatabaseSource = new System.Windows.Forms.ComboBox();
            this.cbDatabaseDestination = new System.Windows.Forms.ComboBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.slInfo = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbConnectionSource
            // 
            this.tbConnectionSource.BackColor = System.Drawing.SystemColors.Window;
            this.tbConnectionSource.Location = new System.Drawing.Point(7, 55);
            this.tbConnectionSource.Margin = new System.Windows.Forms.Padding(2);
            this.tbConnectionSource.Name = "tbConnectionSource";
            this.tbConnectionSource.ReadOnly = true;
            this.tbConnectionSource.Size = new System.Drawing.Size(254, 20);
            this.tbConnectionSource.TabIndex = 9999;
            this.tbConnectionSource.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 38);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(177, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Source Database Connection String";
            // 
            // btnGenerateSource
            // 
            this.btnGenerateSource.Location = new System.Drawing.Point(413, 53);
            this.btnGenerateSource.Margin = new System.Windows.Forms.Padding(2);
            this.btnGenerateSource.Name = "btnGenerateSource";
            this.btnGenerateSource.Size = new System.Drawing.Size(75, 24);
            this.btnGenerateSource.TabIndex = 0;
            this.btnGenerateSource.Text = "Generate";
            this.btnGenerateSource.UseVisualStyleBackColor = true;
            this.btnGenerateSource.Click += new System.EventHandler(this.btnGenerateSource_Click);
            // 
            // btnGenerateDestination
            // 
            this.btnGenerateDestination.Location = new System.Drawing.Point(413, 112);
            this.btnGenerateDestination.Margin = new System.Windows.Forms.Padding(2);
            this.btnGenerateDestination.Name = "btnGenerateDestination";
            this.btnGenerateDestination.Size = new System.Drawing.Size(75, 24);
            this.btnGenerateDestination.TabIndex = 1;
            this.btnGenerateDestination.Text = "Generate";
            this.btnGenerateDestination.UseVisualStyleBackColor = true;
            this.btnGenerateDestination.Click += new System.EventHandler(this.btnGenerateDestination_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 98);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(196, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Destination Database Connection String";
            // 
            // tbConnectionDestination
            // 
            this.tbConnectionDestination.BackColor = System.Drawing.SystemColors.Window;
            this.tbConnectionDestination.Location = new System.Drawing.Point(7, 114);
            this.tbConnectionDestination.Margin = new System.Windows.Forms.Padding(2);
            this.tbConnectionDestination.Name = "tbConnectionDestination";
            this.tbConnectionDestination.ReadOnly = true;
            this.tbConnectionDestination.Size = new System.Drawing.Size(254, 20);
            this.tbConnectionDestination.TabIndex = 9999;
            this.tbConnectionDestination.TabStop = false;
            // 
            // btnSelectData
            // 
            this.btnSelectData.Location = new System.Drawing.Point(413, 163);
            this.btnSelectData.Margin = new System.Windows.Forms.Padding(2);
            this.btnSelectData.Name = "btnSelectData";
            this.btnSelectData.Size = new System.Drawing.Size(75, 24);
            this.btnSelectData.TabIndex = 2;
            this.btnSelectData.Text = "Next";
            this.btnSelectData.UseVisualStyleBackColor = true;
            this.btnSelectData.Click += new System.EventHandler(this.btnSelectData_Click);
            // 
            // cbDatabaseSource
            // 
            this.cbDatabaseSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDatabaseSource.FormattingEnabled = true;
            this.cbDatabaseSource.Location = new System.Drawing.Point(264, 55);
            this.cbDatabaseSource.Margin = new System.Windows.Forms.Padding(2);
            this.cbDatabaseSource.Name = "cbDatabaseSource";
            this.cbDatabaseSource.Size = new System.Drawing.Size(145, 21);
            this.cbDatabaseSource.TabIndex = 10000;
            // 
            // cbDatabaseDestination
            // 
            this.cbDatabaseDestination.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDatabaseDestination.FormattingEnabled = true;
            this.cbDatabaseDestination.Location = new System.Drawing.Point(264, 114);
            this.cbDatabaseDestination.Margin = new System.Windows.Forms.Padding(2);
            this.cbDatabaseDestination.Name = "cbDatabaseDestination";
            this.cbDatabaseDestination.Size = new System.Drawing.Size(145, 21);
            this.cbDatabaseDestination.TabIndex = 10001;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.slInfo});
            this.statusStrip1.Location = new System.Drawing.Point(0, 189);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(499, 22);
            this.statusStrip1.TabIndex = 10002;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // slInfo
            // 
            this.slInfo.Name = "slInfo";
            this.slInfo.Size = new System.Drawing.Size(0, 17);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(499, 24);
            this.menuStrip1.TabIndex = 10003;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(499, 211);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.cbDatabaseDestination);
            this.Controls.Add(this.cbDatabaseSource);
            this.Controls.Add(this.btnSelectData);
            this.Controls.Add(this.btnGenerateDestination);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbConnectionDestination);
            this.Controls.Add(this.btnGenerateSource);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbConnectionSource);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Database Cloner";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbConnectionSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnGenerateSource;
        private System.Windows.Forms.Button btnGenerateDestination;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbConnectionDestination;
        private System.Windows.Forms.Button btnSelectData;
        private System.Windows.Forms.ComboBox cbDatabaseSource;
        private System.Windows.Forms.ComboBox cbDatabaseDestination;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel slInfo;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
    }
}

