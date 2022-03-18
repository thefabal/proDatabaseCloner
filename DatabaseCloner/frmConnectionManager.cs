using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DatabaseCloner {
    public partial class frmConnectionManager : Form {
        public frmConnectionManager( frmMain frmMain, ref proGEDIA.Utilities.Database database ) {
            this.frmMain = frmMain;
            this.database = database;

            InitializeComponent();
        }

        private readonly frmMain frmMain;
        private readonly proGEDIA.Utilities.Database database;

        private void frmConnectionManager_Load( object sender, EventArgs e ) {
            cbServerType.SelectedIndex = 0;
        }

        private void cbServerType_SelectedIndexChanged( object sender, EventArgs e ) {
            cbAuthentication.SelectedIndexChanged -= cbAuthentication_SelectedIndexChanged;

            switch(cbServerType.SelectedItem.ToString()) {
                case "MsSQL":
                    tbServerPort.Enabled = false;
                    tbServerPort.Text = "1433";

                    lblServiceName.Enabled = false;
                    tbServiceName.Enabled = false;
                    lblDatabaseName.Enabled = false;
                    lblDatabaseFile.Enabled = false;
                    tbDatabaseFile.Enabled = false;
                    btnBrowse.Enabled = false;
                    tbDatabaseName.Enabled = false;
                    cbAuthentication.Enabled = true;
                    lblAuthentication.Enabled = true;

                    cbAuthentication_SelectedIndexChanged( null, null );
                    cbAuthentication.SelectedIndexChanged += cbAuthentication_SelectedIndexChanged;
                break;

                case "MySQL":
                    tbServerPort.Enabled = true;
                    tbServerPort.Text = "3306";

                    lblServiceName.Enabled = false;
                    tbServiceName.Enabled = false;
                    lblDatabaseName.Enabled = true;
                    tbDatabaseName.Enabled = true;
                    lblDatabaseFile.Enabled = false;
                    tbDatabaseFile.Enabled = false;
                    btnBrowse.Enabled = false;
                    cbAuthentication.Enabled = false;
                    lblAuthentication.Enabled = false;
                    lblUserName.Enabled = true;
                    tbUserName.Enabled = true;
                    lblUserPassword.Enabled = true;
                    tbUserPassword.Enabled = true;
                break;

                case "Oracle":
                    tbServerPort.Enabled = true;
                    tbServerPort.Text = "1521";

                    lblServiceName.Enabled = true;
                    tbServiceName.Enabled = true;
                    lblDatabaseName.Enabled = false;
                    tbDatabaseName.Enabled = false;
                    lblDatabaseFile.Enabled = false;
                    tbDatabaseFile.Enabled = false;
                    btnBrowse.Enabled = false;
                    cbAuthentication.Enabled = false;
                    lblAuthentication.Enabled = false;
                    lblUserName.Enabled = true;
                    tbUserName.Enabled = true;
                    lblUserPassword.Enabled = true;
                    tbUserPassword.Enabled = true;
                break;

                case "SQLite":
                    tbServerPort.Enabled = false;
                    tbServerPort.Text = "";

                    lblServiceName.Enabled = false;
                    tbServiceName.Enabled = false;
                    lblDatabaseName.Enabled = false;
                    tbDatabaseName.Enabled = false;
                    lblDatabaseFile.Enabled = true;
                    tbDatabaseFile.Enabled = true;
                    btnBrowse.Enabled = true;
                    cbAuthentication.Enabled = false;
                    lblAuthentication.Enabled = false;
                    lblUserName.Enabled = false;
                    tbUserName.Enabled = false;
                    lblUserPassword.Enabled = false;
                    tbUserPassword.Enabled = false;
                break;

                default:
                    MessageBox.Show( "Please select a database engine." );

                    return;
            }

            cbServerName.Items.Clear();
            cbServerName.Text = "";
            foreach( proGEDIA.Utilities.Database item in frmMain.settings.database ) {
                if( item.serverType == cbServerType.Text ) {
                    cbServerName.Items.Add( item );
                }
            }

            if( cbServerName.Items.Count > 0 ) {
                cbServerName.SelectedIndex = 0;
            } else {
                cbAuthentication.SelectedIndex = 0;

                tbServerPort.Text = string.Empty;
                tbServiceName.Text = string.Empty;
                tbDatabaseName.Text = string.Empty;
                tbUserName.Text = string.Empty;
                tbUserPassword.Text = string.Empty;

                cbRememberPassword.Checked = true;
            }

            cbServerName.Focus();
        }

        private void cbServerName_SelectedIndexChanged( object sender, EventArgs e ) {
            proGEDIA.Utilities.Database item = (proGEDIA.Utilities.Database) cbServerName.SelectedItem;

            tbServerPort.Text = item.serverPort;
            tbServiceName.Text = item.serviceName;
            tbDatabaseName.Text = item.databaseName;
            tbDatabaseFile.Text = item.databaseFile;
            cbAuthentication.SelectedIndex = item.userAuth ? 0 : 1;
            tbUserName.Text = item.userName;
            tbUserPassword.Text = item.userPass;
            cbRememberPassword.Checked = item.rememberPassword;
        }

        private void cbAuthentication_SelectedIndexChanged( object sender, EventArgs e ) {
            if(cbAuthentication.SelectedIndex == 0) {
                lblUserName.Enabled = false;
                tbUserName.Enabled = false;
                lblUserPassword.Enabled = false;
                tbUserPassword.Enabled = false;
            } else {
                lblUserName.Enabled = true;
                tbUserName.Enabled = true;
                lblUserPassword.Enabled = true;
                tbUserPassword.Enabled = true;
            }
        }

        private void btnCancel_Click( object sender, EventArgs e ) {
            this.DialogResult = DialogResult.Cancel;
        }

        private void btnConnect_Click( object sender, EventArgs e ) {
            switch(cbServerType.SelectedItem.ToString()) {
                case "MsSQL":
                case "MySQL":
                case "Oracle":
                case "SQLite":

                break;

                default:
                    MessageBox.Show( "Please select a database engine." );

                    return;
            }

            database.Set( cbServerType.Text, cbServerName.Text, tbServerPort.Text, tbServiceName.Text, tbDatabaseName.Text, tbDatabaseFile.Text, cbAuthentication.SelectedIndex == 0 ? false : true, tbUserName.Text, tbUserPassword.Text, cbRememberPassword.Checked );
            frmMain.settings.Add( database );

            this.DialogResult = DialogResult.OK;
        }

        private void btnDelete_Click( object sender, EventArgs e ) {
            frmMain.settings.Remove( cbServerType.Text, cbServerName.Text, tbDatabaseFile.Text, tbUserName.Text );
        }

        private void btnBrowse_Click( object sender, EventArgs e ) {
            OpenFileDialog openFileDialog = new OpenFileDialog() {
                CheckFileExists = true,
                Multiselect = false
            };

            if( openFileDialog.ShowDialog() == DialogResult.OK ) {
                tbDatabaseFile.Text = openFileDialog.FileName;

                cbServerName.Text = System.IO.Path.GetFileNameWithoutExtension( openFileDialog.FileName );
            }
        }
    }
}
