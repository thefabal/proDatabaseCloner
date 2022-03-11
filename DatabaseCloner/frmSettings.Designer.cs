namespace DatabaseCloner
{
    partial class frmSettings
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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.nudRowPerInsert = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbColumnNamesNo = new System.Windows.Forms.RadioButton();
            this.rbColumnNamesYes = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.nudRowPerInsert)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(191, 103);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(110, 103);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Row per Insert";
            // 
            // nudRowPerInsert
            // 
            this.nudRowPerInsert.Location = new System.Drawing.Point(174, 20);
            this.nudRowPerInsert.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nudRowPerInsert.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudRowPerInsert.Name = "nudRowPerInsert";
            this.nudRowPerInsert.Size = new System.Drawing.Size(92, 20);
            this.nudRowPerInsert.TabIndex = 1;
            this.nudRowPerInsert.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbColumnNamesNo);
            this.groupBox1.Controls.Add(this.rbColumnNamesYes);
            this.groupBox1.Location = new System.Drawing.Point(15, 46);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(251, 51);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Include Column Names in INSERT";
            // 
            // rbColumnNamesNo
            // 
            this.rbColumnNamesNo.AutoSize = true;
            this.rbColumnNamesNo.Location = new System.Drawing.Point(148, 19);
            this.rbColumnNamesNo.Name = "rbColumnNamesNo";
            this.rbColumnNamesNo.Size = new System.Drawing.Size(39, 17);
            this.rbColumnNamesNo.TabIndex = 1;
            this.rbColumnNamesNo.TabStop = true;
            this.rbColumnNamesNo.Text = "No";
            this.rbColumnNamesNo.UseVisualStyleBackColor = true;
            // 
            // rbColumnNamesYes
            // 
            this.rbColumnNamesYes.AutoSize = true;
            this.rbColumnNamesYes.Location = new System.Drawing.Point(6, 19);
            this.rbColumnNamesYes.Name = "rbColumnNamesYes";
            this.rbColumnNamesYes.Size = new System.Drawing.Size(43, 17);
            this.rbColumnNamesYes.TabIndex = 0;
            this.rbColumnNamesYes.TabStop = true;
            this.rbColumnNamesYes.Text = "Yes";
            this.rbColumnNamesYes.UseVisualStyleBackColor = true;
            // 
            // frmSettings
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(278, 135);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.nudRowPerInsert);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSettings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.frmSettings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudRowPerInsert)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudRowPerInsert;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbColumnNamesNo;
        private System.Windows.Forms.RadioButton rbColumnNamesYes;
    }
}