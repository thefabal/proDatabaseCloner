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
        public frmConnectionManager( frmMain frmMain, ref proGEDIA.utilities.database database ) {
            this.frmMain = frmMain;
            this.database = database;

            InitializeComponent();
        }

        private readonly frmMain frmMain;
        private readonly proGEDIA.utilities.database database;

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
            foreach( proGEDIA.utilities.database item in frmMain.settings.database ) {
                if( item.server_type == cbServerType.Text ) {
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
            proGEDIA.utilities.database item = (proGEDIA.utilities.database) cbServerName.SelectedItem;

            tbServerPort.Text = item.server_port;
            tbServiceName.Text = item.service_name;
            tbDatabaseName.Text = item.database_name;
            tbDatabaseFile.Text = item.database_file;
            cbAuthentication.SelectedIndex = item.authentication;
            tbUserName.Text = item.user_name;
            tbUserPassword.Text = item.user_pass;
            cbRememberPassword.Checked = item.remember_password;
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

            database.Set( cbServerType.Text, cbServerName.Text, tbServerPort.Text, tbServiceName.Text, tbDatabaseName.Text, tbDatabaseFile.Text, cbAuthentication.SelectedIndex, tbUserName.Text, tbUserPassword.Text, cbRememberPassword.Checked );
            frmMain.settings.add( database );

            this.DialogResult = DialogResult.OK;
        }

        private void btnDelete_Click( object sender, EventArgs e ) {
            frmMain.settings.remove( cbServerType.Text, cbServerName.Text, tbDatabaseFile.Text, tbUserName.Text );
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
