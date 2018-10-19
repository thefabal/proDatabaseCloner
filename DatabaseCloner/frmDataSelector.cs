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
using MySql.Data.MySqlClient;

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
            switch( database.server_type.ToLower() ) {
                case "mssql":
                    database.mssqlCon.ChangeDatabase( db_name );
                
                    /**
                     * Tables
                    **/
                    SqlCommand mssqlCom = database.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT s.name, t.name, object_id FROM sys.tables AS t INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id WHERE t.type = 'U' ORDER BY t.name";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            dgvTableList.Rows.Add( "table", mssqlReader.GetString( 0 ), mssqlReader.GetString( 1 ), true, true );
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get table list.\r\n" + ex.Message );
                    }

                   /**
                     * Views
                    **/
                    mssqlCom.CommandText = "SELECT name FROM sys.views";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            dgvTableList.Rows[ dgvTableList.Rows.Add( "view", "", mssqlReader.GetString( 0 ), true, false ) ].Cells[ 4 ].ReadOnly = true;
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get view list.\r\n" + ex.Message );
                    }

                    /**
                     * Functions
                    **/
                    mssqlCom.CommandText = "SELECT s.name, o.name FROM sys.all_objects AS o INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id WHERE type = 'FN' AND is_ms_shipped = 0";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            dgvTableList.Rows[ dgvTableList.Rows.Add( "function", mssqlReader.GetString( 0 ), mssqlReader.GetString( 1 ), true, false ) ].Cells[ 4 ].ReadOnly = true;
                        }
                    mssqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get function list.\r\n" + ex.Message );
                    }

                    /**
                     * Database Triggers
                    **/
                    mssqlCom.CommandText = "SELECT name FROM sys.triggers WHERE type = 'TR'";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            dgvTableList.Rows[ dgvTableList.Rows.Add( "trigger", "", mssqlReader.GetString( 0 ), true, false ) ].Cells[ 4 ].ReadOnly = true;
                        }
                    mssqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get view list.\r\n" + ex.Message );
                    }
                break;

                case "mysql":
                    database.mysqlCon.ChangeDatabase( db_name );

                    /**
                    * Tables
                    **/                    
                    MySqlCommand mysqlCom = database.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = '" + db_name + "' ORDER BY table_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            dgvTableList.Rows.Add( "table", "", mysqlReader.GetString(0), true, true );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get table list.\r\n" + ex.Message );
                    }
                    
                    /**
                     * Views
                    **/
                    mysqlCom.CommandText = "SELECT table_name FROM information_schema.views WHERE table_schema = '" + db_name + "' ORDER BY table_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            dgvTableList.Rows[ dgvTableList.Rows.Add( "view", "", mysqlReader.GetString( 0 ), true, false ) ].Cells[ 4 ].ReadOnly = true;
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get view list.\r\n" + ex.Message );
                    }

                    /**
                     * Functions
                    **/
                    mysqlCom.CommandText = "SELECT name FROM mysql.proc WHERE db = '" + db_name + "' ORDER BY name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            dgvTableList.Rows[ dgvTableList.Rows.Add( "function", "", mysqlReader.GetString( 0 ), true, false ) ].Cells[ 4 ].ReadOnly = true;
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get function list.\r\n" + ex.Message );
                    }

                    /**
                     * Database Triggers
                    **/                    
                    mysqlCom.CommandText = "SELECT trigger_name FROM information_schema.triggers WHERE TRIGGER_SCHEMA = '" + db_name + "' ORDER BY trigger_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            dgvTableList.Rows[ dgvTableList.Rows.Add( "trigger", "", mysqlReader.GetString( 0 ), true, false ) ].Cells[ 4 ].ReadOnly = true;
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get view list.\r\n" + ex.Message );
                    }
                    
                break;
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
                        Convert.ToBoolean( row.Cells[ 3 ].Value ) == true,
                        Convert.ToBoolean( row.Cells[ 4 ].Value ) == true
                ) );
            }

            this.DialogResult = DialogResult.OK;
        }
    }
}
