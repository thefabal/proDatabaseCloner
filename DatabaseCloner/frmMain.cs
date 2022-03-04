using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.IO;
using System.Threading;

using System.Data.SqlClient;
using System.Data.SQLite;
using MySql.Data.MySqlClient;

namespace DatabaseCloner {
    public partial class frmMain: Form {
        public frmSettings frmSettings;

        public databaseBackup backup;
        public settings settings = new settings();
        public proGEDIA.utilities.database db_source = new proGEDIA.utilities.database();
        public proGEDIA.utilities.database db_destination = new proGEDIA.utilities.database();
        public proGEDIA.utilities.LogWriter log = new proGEDIA.utilities.LogWriter();

        public frmMain() {
            InitializeComponent();

            frmSettings = new frmSettings(this);
        }

        private void frmMain_Load( object sender, EventArgs e ) {
            settings.get();
        }

        private void frmMain_FormClosing( object sender, FormClosingEventArgs e ) {
            settings.save();
        }

        private void btnGenerateSource_Click( object sender, EventArgs e ) {
            frmConnectionManager manager = new frmConnectionManager(this, ref db_source );
            if( manager.ShowDialog() == DialogResult.OK ) {
                tbConnectionSource.Text = db_source.getConnectionString();

                loadDatabases( db_source, cbDatabaseSource );
            }
        }

        private void loadDatabases( proGEDIA.utilities.database db, ComboBox cb ) {
            cb.Items.Clear();
            switch( db.serverType.ToLower() ) {
                case "mssql":
                    if( db_source.mssqlCon != null && db_source.mssqlCon.State == ConnectionState.Open ) {
                        db_source.mssqlCon.Close();
                    }

                    if( db_source.mssqlCon == null )
                        db_source.mssqlCon = new SqlConnection();

                    db_source.mssqlCon.ConnectionString = db.getConnectionString();
                    try {
                        db_source.mssqlCon.Open();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not connect to database.\r\n" + ex.Message );

                        return;
                    }

                    SqlCommand mssqlCom = db_source.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT name FROM sys.databases WHERE name NOT IN('master', 'tempdb', 'model', 'msdb') ORDER BY name";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            cb.Items.Add( mssqlReader[ 0 ].ToString() );
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not get database list.\r\n" + ex.Message );
                    }
                break;

                case "mysql":
                    if( db_source.mysqlCon != null && db_source.mysqlCon.State == ConnectionState.Open ) {
                        db_source.mysqlCon.Close();
                    }

                    if( db_source.mysqlCon == null )
                        db_source.mysqlCon = new MySqlConnection();

                    db_source.mysqlCon.ConnectionString = db.getConnectionString();
                    try {
                        db_source.mysqlCon.Open();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not connect to database.\r\n" + ex.Message );

                        return;
                    }

                    MySqlCommand mysqlCom = db_source.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT LIKE '%schema' ORDER BY schema_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            cb.Items.Add( mysqlReader[ 0 ].ToString() );
                        }
                        mysqlReader.Close();
                    } catch(Exception ex) {
                        MessageBox.Show( "Could not get database list.\r\n" + ex.Message );
                    }
                break;

                case "sqlite":
                    if( db_source.sqliteCon != null && db_source.sqliteCon.State == ConnectionState.Open ) {
                        db_source.sqliteCon.Close();
                    }

                    if( db_source.sqliteCon == null )
                        db_source.sqliteCon = new SQLiteConnection();

                    db_source.sqliteCon.ConnectionString = db.getConnectionString();
                    try {
                        db_source.sqliteCon.Open();
                    } catch( Exception ex ) {
                        MessageBox.Show( "Could not connect to database.\r\n" + ex.Message );

                        return;
                    }

                    cb.Items.Add( db_source.serverName );
                    cb.SelectedIndex = 0;
                break;
            }
        }

        private void btnGenerateDestination_Click( object sender, EventArgs e ) {
            frmConnectionManager manager = new frmConnectionManager(this, ref db_destination );
            if( manager.ShowDialog() == DialogResult.OK ) {
                tbConnectionDestination.Text = db_destination.getConnectionString();
            }
        }

        private void btnSelectData_Click( object sender, EventArgs e ) {
            if( tbConnectionSource.Text.Length > 0 && cbDatabaseSource.SelectedIndex != -1 ) {
                backup = new databaseBackup( db_source, cbDatabaseSource.SelectedItem.ToString(), settings.row_per_insert );
                backup.updateStatus += new EventHandler<string>( updateStatus );

                frmDataSelector frmDataSelector = new frmDataSelector( db_source, cbDatabaseSource.SelectedItem.ToString(), ref backup );

                if( frmDataSelector.ShowDialog() == DialogResult.OK ) {
                    string schema = string.Empty;

                    SaveFileDialog sfd = new SaveFileDialog {
                        FileName = cbDatabaseSource.SelectedItem.ToString(),
                        Filter = "SQL (*.sql)|*.sql|Text (*.txt)|*.txt"
                    };

                    if( sfd.ShowDialog() == DialogResult.OK ) {
                        disableForm();

                        if( backup.getDatabase() ) {
                            StreamWriter sw = File.CreateText( sfd.FileName );
#if DEBUG
                            backup.getSchema( sw );
#else
                            new Thread( () => { backup.getSchema( sw ); } ).Start();
#endif
                        }
                    }
                }
            }
        }

        public void disableForm() {
            if( btnSelectData.InvokeRequired ) {
                btnSelectData.BeginInvoke( (MethodInvoker)delegate () { disableForm(); } );
            } else {
                btnGenerateSource.Enabled = false;
                btnGenerateDestination.Enabled = false;
                cbDatabaseDestination.Enabled = false;
                cbDatabaseSource.Enabled = false;
                btnSelectData.Enabled = false;
            }
        }

        public void enableForm() {
            if( btnSelectData.InvokeRequired ) {
                btnSelectData.BeginInvoke( (MethodInvoker)delegate () { enableForm(); } );
            } else {
                MessageBox.Show( "All is done.\r\n\r\nThank you for using proGEDIA Database Cloner.\r\nWe hope you like it. Feel free to share it." );

                btnGenerateSource.Enabled = true;
                btnGenerateDestination.Enabled = true;
                cbDatabaseDestination.Enabled = true;
                cbDatabaseSource.Enabled = true;
                btnSelectData.Enabled = true;
            }
        }

        public void updateStatus( object s, string message ) {
            if( message == "enableForm" ) {
                enableForm();

                return;
            }

            if( statusStrip1.InvokeRequired ) {
                statusStrip1.BeginInvoke( (MethodInvoker)delegate () { updateStatus( s, message ); } );
            } else {
                slInfo.Text = message;
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            frmSettings.ShowDialog();
        }
    }

    public class settings {
        public int row_per_insert = 1;
        public List<proGEDIA.utilities.database> database = new List<proGEDIA.utilities.database>();

        public settings() {

        }

        public void add( proGEDIA.utilities.database set ) {
            foreach( var item in database.Select( ( value, i ) => new { i, value } ) ) {
                if( item.value.serverType == set.serverType && item.value.serverName == set.serverName && item.value.serverPort == set.serverPort ) {
                    database.RemoveAt( item.i );

                    break;
                }
            }

            database.Add( set );
        }

        public void remove( string server_type, string server_name, string database_file, string user_name ) {
            foreach( var item in database.Select( ( value, i ) => new { i, value } ) ) {
                if( item.value.Compare( server_type, server_name, database_file, user_name ) ) {
                    database.RemoveAt( item.i );

                    return;
                }
            }
        }

        public void get() {
            string database_setting = Properties.Settings.Default.database_setting.ToString();
            if( database_setting.Length > 0 ) {
                database_setting = proGEDIA.utilities.encryption.DecryptPassword( database_setting );
                JavaScriptSerializer serializer = new JavaScriptSerializer();

                try {
                    this.database = (List<proGEDIA.utilities.database>)serializer.Deserialize( database_setting, typeof( List<proGEDIA.utilities.database> ) );
                } catch( Exception e ) {
                    throw new Exception( e.Message );
                }
            }

            row_per_insert = Properties.Settings.Default.row_per_insert;
        }

        public void save() {
            for( int i = 0; i < database.Count; i++ ) {
                if( database[ i ].rememberPassword == false ) {
                    database[ i ].userPass = string.Empty;
                }

                if( database[ i ].mssqlCon != null ) {
                    database[ i ].mssqlCon = null;
                }

                if( database[ i ].mysqlCon != null ) {
                    database[ i ].mysqlCon = null;
                }

                if( database[ i ].sqliteCon != null ) {
                    database[ i ].sqliteCon = null;
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string settings = serializer.Serialize( this.database );

            Properties.Settings.Default.database_setting = proGEDIA.utilities.encryption.EncryptPassword( settings );
            Properties.Settings.Default.row_per_insert = row_per_insert;
            Properties.Settings.Default.Save();
        }
    }
}