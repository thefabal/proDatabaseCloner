using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace DatabaseCloner {
    public partial class frmDataSelector: Form {
        public database_backup backup_settings;
        private proGEDIA.utilities.database database;
        private string db_name;

        public frmDataSelector( proGEDIA.utilities.database database, string db_name, ref database_backup backup_settings ) {
            this.database = database;
            this.db_name = db_name;
            this.backup_settings = backup_settings;

            InitializeComponent();

            for(int i = 0; i < dgvTableList.Columns.Count; i++ ) {
                dgvTableList.Columns[ i ].HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
            }
            dgvTableList.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvTableList.AllowUserToResizeRows = false;

            this.MinimumSize = this.Size;
        }

        private void frmDataSelector_Load( object sender, EventArgs e ) {
            frmMain.sqlCon.ChangeDatabase( db_name );

            /**
             * Tables
            **/
            SqlCommand sqlCom = frmMain.sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT s.name, t.name, object_id FROM sys.tables AS t INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id WHERE t.type = 'U' ORDER BY t.name";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    dgvTableList.Rows.Add( "table", sqlReader[ 0 ].ToString(), sqlReader[ 1 ].ToString(), true, true );
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                MessageBox.Show( "Could not get table list.\r\n" + ex.Message );
            }

            /**
             * Views
            **/
            sqlCom.CommandText = "SELECT name FROM sys.views";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read()) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "view", "", sqlReader[ 0 ].ToString(), true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
                sqlReader.Close();
            } catch(Exception ex) {
                MessageBox.Show( "Could not get view list.\r\n" + ex.Message );
            }

            /**
             * Functions
            **/
            sqlCom.CommandText = "SELECT s.name, o.name FROM sys.all_objects AS o INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id WHERE type = 'FN' AND is_ms_shipped = 0";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "function", sqlReader[ 0 ].ToString(), sqlReader[ 1 ].ToString(), true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                MessageBox.Show( "Could not get function list.\r\n" + ex.Message );
            }

            /**
             * Database Triggers
            **/
            sqlCom.CommandText = "SELECT name FROM sys.triggers WHERE type = 'TR'";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    dgvTableList.Rows[ dgvTableList.Rows.Add( "trigger", "", sqlReader[ 0 ].ToString(), true, false ) ].Cells[ 4 ].ReadOnly = true;
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                MessageBox.Show( "Could not get view list.\r\n" + ex.Message );
            }

            arrangeCheckboxes();
        }

        private void arrangeCheckboxes() {
            if( dgvTableList.Rows.Count == 0 ) {
                cbData.Visible = false;
                cbSchema.Visible = false;
            } else {
                cbData.Location = new Point( dgvTableList.GetCellDisplayRectangle( 4, 0, true ).Location.X + dgvTableList.Location.X + dgvTableList.Columns[ 4 ].Width / 2 - cbData.Width / 2, dgvTableList.Location.Y + 5 );
                cbSchema.Location = new Point( dgvTableList.GetCellDisplayRectangle( 3, 0, true ).Location.X + dgvTableList.Location.X + dgvTableList.Columns[ 3 ].Width / 2 - cbSchema.Width / 2, dgvTableList.Location.Y + 5 );
            }
        }

        private void cbSchema_CheckedChanged( object sender, EventArgs e ) {
            dgvTableList.BeginEdit(false);
            foreach( DataGridViewRow row in dgvTableList.Rows ) {
                if( ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value == ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).FalseValue || ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value == null ) {
                    ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value = true;
                } else {
                    ( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value = null;
                }
            }
            dgvTableList.EndEdit();
        }

        private void cbData_CheckedChanged( object sender, EventArgs e ) {
            dgvTableList.BeginEdit( false );
            foreach( DataGridViewRow row in dgvTableList.Rows ) {
                if( (string)row.Cells[ 0 ].Value == "table" ) {
                    if( ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value == ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).FalseValue || ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value == null ) {
                        ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value = true;
                    } else {
                        ( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value = null;
                    }
                }
            }
            dgvTableList.EndEdit();
        }

        private void btnOK_Click( object sender, EventArgs e ) {
            backup_settings.backup_settings.Clear();

            foreach( DataGridViewRow row in dgvTableList.Rows ) {
                backup_settings.backup_settings.Add( 
                    new backup_settings( 
                        row.Cells[ 0 ].Value.ToString(), 
                        row.Cells[ 1 ].Value.ToString(), 
                        row.Cells[ 2 ].Value.ToString(),
                        (bool)(( (DataGridViewCheckBoxCell)row.Cells[ 3 ] ).Value) == true,
                        (bool)(( (DataGridViewCheckBoxCell)row.Cells[ 4 ] ).Value) == true
                ) );
            }

            this.DialogResult = DialogResult.OK;
        }
    }
}
