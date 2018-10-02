using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseCloner
{
    public partial class frmSettings : Form
    {
        public frmMain frmMain;

        public frmSettings( frmMain frmMain ) {
            this.frmMain = frmMain;

            InitializeComponent();
        }

        private void frmSettings_Load( object sender, EventArgs e ) {
            nudRowPerInsert.Value = frmMain.settings.row_per_insert;
        }

        private void btnOK_Click( object sender, EventArgs e ) {
            frmMain.settings.row_per_insert = Convert.ToInt32( nudRowPerInsert.Value );

            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click( object sender, EventArgs e ) {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
