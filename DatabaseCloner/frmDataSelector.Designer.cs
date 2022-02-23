namespace DatabaseCloner {
    partial class frmDataSelector {
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
            this.btnOK = new System.Windows.Forms.Button();
            this.cbSchema = new System.Windows.Forms.CheckBox();
            this.cbData = new System.Windows.Forms.CheckBox();
            this.dgvTableList = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTableList)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(338, 342);
            this.btnOK.Margin = new System.Windows.Forms.Padding(2);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 24);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // cbSchema
            // 
            this.cbSchema.AutoSize = true;
            this.cbSchema.Checked = true;
            this.cbSchema.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSchema.Location = new System.Drawing.Point(173, 9);
            this.cbSchema.Margin = new System.Windows.Forms.Padding(2);
            this.cbSchema.Name = "cbSchema";
            this.cbSchema.Size = new System.Drawing.Size(15, 14);
            this.cbSchema.TabIndex = 2;
            this.cbSchema.UseVisualStyleBackColor = true;
            this.cbSchema.CheckedChanged += new System.EventHandler(this.cbSchema_CheckedChanged);
            // 
            // cbData
            // 
            this.cbData.AutoSize = true;
            this.cbData.Checked = true;
            this.cbData.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbData.Location = new System.Drawing.Point(244, 9);
            this.cbData.Margin = new System.Windows.Forms.Padding(2);
            this.cbData.Name = "cbData";
            this.cbData.Size = new System.Drawing.Size(15, 14);
            this.cbData.TabIndex = 3;
            this.cbData.UseVisualStyleBackColor = true;
            this.cbData.CheckedChanged += new System.EventHandler(this.cbData_CheckedChanged);
            // 
            // dgvTableList
            // 
            this.dgvTableList.AllowUserToAddRows = false;
            this.dgvTableList.AllowUserToDeleteRows = false;
            this.dgvTableList.AllowUserToResizeColumns = false;
            this.dgvTableList.AllowUserToResizeRows = false;
            this.dgvTableList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvTableList.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvTableList.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Raised;
            this.dgvTableList.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            this.dgvTableList.ColumnHeadersHeight = 40;
            this.dgvTableList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvTableList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5});
            this.dgvTableList.GridColor = System.Drawing.SystemColors.Window;
            this.dgvTableList.Location = new System.Drawing.Point(9, 9);
            this.dgvTableList.Margin = new System.Windows.Forms.Padding(2);
            this.dgvTableList.MultiSelect = false;
            this.dgvTableList.Name = "dgvTableList";
            this.dgvTableList.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.dgvTableList.RowHeadersVisible = false;
            this.dgvTableList.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dgvTableList.RowTemplate.Height = 24;
            this.dgvTableList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvTableList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTableList.Size = new System.Drawing.Size(404, 328);
            this.dgvTableList.TabIndex = 4;
            this.dgvTableList.ColumnWidthChanged += new System.Windows.Forms.DataGridViewColumnEventHandler(this.dgvTableList_ColumnWidthChanged);
            // 
            // Column1
            // 
            this.Column1.FillWeight = 60F;
            this.Column1.Frozen = true;
            this.Column1.HeaderText = "Type";
            this.Column1.Name = "Column1";
            this.Column1.ReadOnly = true;
            this.Column1.Width = 60;
            // 
            // Column2
            // 
            this.Column2.FillWeight = 55F;
            this.Column2.Frozen = true;
            this.Column2.HeaderText = "Schema";
            this.Column2.Name = "Column2";
            this.Column2.ReadOnly = true;
            this.Column2.Width = 55;
            // 
            // Column3
            // 
            this.Column3.FillWeight = 130F;
            this.Column3.Frozen = true;
            this.Column3.HeaderText = "Name";
            this.Column3.Name = "Column3";
            this.Column3.ReadOnly = true;
            this.Column3.Width = 130;
            // 
            // Column4
            // 
            this.Column4.FillWeight = 70F;
            this.Column4.Frozen = true;
            this.Column4.HeaderText = "Schema";
            this.Column4.Name = "Column4";
            this.Column4.Width = 70;
            // 
            // Column5
            // 
            this.Column5.FillWeight = 70F;
            this.Column5.Frozen = true;
            this.Column5.HeaderText = "Data";
            this.Column5.Name = "Column5";
            this.Column5.Width = 70;
            // 
            // frmDataSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(422, 376);
            this.Controls.Add(this.cbSchema);
            this.Controls.Add(this.cbData);
            this.Controls.Add(this.dgvTableList);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmDataSelector";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Table Selector";
            this.Load += new System.EventHandler(this.frmDataSelector_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTableList)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.CheckBox cbSchema;
        private System.Windows.Forms.CheckBox cbData;
        private System.Windows.Forms.DataGridView dgvTableList;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column4;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column5;
    }
}