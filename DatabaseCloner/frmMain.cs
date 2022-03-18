using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.IO;

using System.Data.SqlClient;
using System.Data.SQLite;
using MySql.Data.MySqlClient;

namespace DatabaseCloner {
    public partial class frmMain: Form {
        public frmSettings frmSettings;

        public DatabaseBackup backup;
        public Settings settings = new Settings();
        public proGEDIA.Utilities.Database db_source = new proGEDIA.Utilities.Database();
        public proGEDIA.Utilities.Database db_destination = new proGEDIA.Utilities.Database();
        public proGEDIA.Utilities.LogWriter log = new proGEDIA.Utilities.LogWriter();

        public frmMain() {
            InitializeComponent();

            frmSettings = new frmSettings(this);
        }

        private void frmMain_Load( object sender, EventArgs e ) {
            settings.Get();
        }

        private void frmMain_FormClosing( object sender, FormClosingEventArgs e ) {
            settings.Save();
        }

        private void btnGenerateSource_Click( object sender, EventArgs e ) {
            frmConnectionManager manager = new frmConnectionManager(this, ref db_source );
            if( manager.ShowDialog() == DialogResult.OK ) {
                tbConnectionSource.Text = db_source.GetConnectionString();

                loadDatabases( db_source, cbDatabaseSource );
            }
        }

        private void loadDatabases( proGEDIA.Utilities.Database db, ComboBox cb ) {
            cb.Items.Clear();
            switch( db.serverType.ToLower() ) {
                case "mssql":
                    if( db_source.mssqlCon != null && db_source.mssqlCon.State == ConnectionState.Open ) {
                        db_source.mssqlCon.Close();
                    }

                    if( db_source.mssqlCon == null )
                        db_source.mssqlCon = new SqlConnection();

                    db_source.mssqlCon.ConnectionString = db.GetConnectionString();
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

                    db_source.mysqlCon.ConnectionString = db.GetConnectionString();
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

                    db_source.sqliteCon.ConnectionString = db.GetConnectionString();
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
                tbConnectionDestination.Text = db_destination.GetConnectionString();
            }
        }

        private void btnSelectData_Click( object sender, EventArgs e ) {
            if( tbConnectionSource.Text.Length > 0 && cbDatabaseSource.SelectedIndex != -1 ) {
                backup = new DatabaseBackup( db_source, cbDatabaseSource.SelectedItem.ToString(), settings.rowPerInsert, settings.insertColumnName );
                backup.UpdateStatus += new EventHandler<string>( UpdateStatus );

                frmDataSelector frmDataSelector = new frmDataSelector( db_source, cbDatabaseSource.SelectedItem.ToString(), ref backup );

                if( frmDataSelector.ShowDialog() == DialogResult.OK ) {
                    string schema = string.Empty;

                    SaveFileDialog sfd = new SaveFileDialog {
                        FileName = cbDatabaseSource.SelectedItem.ToString(),
                        Filter = "SQL (*.sql)|*.sql|Text (*.txt)|*.txt"
                    };

                    if( sfd.ShowDialog() == DialogResult.OK ) {
                        DisableForm();

                        if( backup.GetDatabase() ) {
                            StreamWriter sw = File.CreateText( sfd.FileName );
#if DEBUG
                            backup.GetSchema( sw );
#else
                            new Thread( () => { backup.getSchema( sw ); } ).Start();
#endif
                        }
                    }
                }
            }
        }

        public void DisableForm() {
            if( btnSelectData.InvokeRequired ) {
                btnSelectData.BeginInvoke( (MethodInvoker)delegate () { DisableForm(); } );
            } else {
                btnGenerateSource.Enabled = false;
                btnGenerateDestination.Enabled = false;
                cbDatabaseDestination.Enabled = false;
                cbDatabaseSource.Enabled = false;
                btnSelectData.Enabled = false;
            }
        }

        public void EnableForm() {
            if( btnSelectData.InvokeRequired ) {
                btnSelectData.BeginInvoke( (MethodInvoker)delegate () { EnableForm(); } );
            } else {
                MessageBox.Show( "All is done.\r\n\r\nThank you for using proGEDIA Database Cloner.\r\nWe hope you like it. Feel free to share it." );

                btnGenerateSource.Enabled = true;
                btnGenerateDestination.Enabled = true;
                cbDatabaseDestination.Enabled = true;
                cbDatabaseSource.Enabled = true;
                btnSelectData.Enabled = true;
            }
        }

        public void UpdateStatus( object s, string message ) {
            if( message == "enableForm" ) {
                EnableForm();

                return;
            }

            if( statusStrip1.InvokeRequired ) {
                statusStrip1.BeginInvoke( (MethodInvoker)delegate () { UpdateStatus( s, message ); } );
            } else {
                slInfo.Text = message;
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) {
            frmSettings.ShowDialog();
        }
    }

    public class Settings {
        public int rowPerInsert = 1;

        public bool insertColumnName = true;

        public List<proGEDIA.Utilities.Database> database = new List<proGEDIA.Utilities.Database>();

        public Settings() {

        }

        public void Add( proGEDIA.Utilities.Database set ) {
            foreach( var item in database.Select( ( value, i ) => new { i, value } ) ) {
                if( item.value.serverType == set.serverType && item.value.serverName == set.serverName && item.value.serverPort == set.serverPort ) {
                    database.RemoveAt( item.i );

                    break;
                }
            }

            database.Add( set );
        }

        public void Remove( string server_type, string server_name, string database_file, string user_name ) {
            foreach( var item in database.Select( ( value, i ) => new { i, value } ) ) {
                if( item.value.Compare( server_type, server_name, database_file, user_name ) ) {
                    database.RemoveAt( item.i );

                    return;
                }
            }
        }

        public void Get() {
            string database_setting = Properties.Settings.Default.database_setting.ToString();
            if( database_setting.Length > 0 ) {
                database_setting = proGEDIA.Utilities.Encryption.DecryptPassword( database_setting );
                JavaScriptSerializer serializer = new JavaScriptSerializer();

                try {
                    this.database = (List<proGEDIA.Utilities.Database>)serializer.Deserialize( database_setting, typeof( List<proGEDIA.Utilities.Database> ) );
                } catch( Exception e ) {
                    throw new Exception( e.Message );
                }
            }

            rowPerInsert = Properties.Settings.Default.rowPerInsert;
            insertColumnName = Properties.Settings.Default.insertColumnName;
        }

        public void Save() {
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
            string settings = serializer.Serialize( database );

            Properties.Settings.Default.database_setting = proGEDIA.Utilities.Encryption.EncryptPassword( settings );
            Properties.Settings.Default.rowPerInsert = rowPerInsert;
            Properties.Settings.Default.insertColumnName = insertColumnName;

            Properties.Settings.Default.Save();
        }
    }
}