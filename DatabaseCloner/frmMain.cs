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
using System.Security.Cryptography;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace DatabaseCloner {
    public partial class frmMain: Form {
        public static SqlConnection sqlCon = new SqlConnection();
        public frmSettings frmSettings;

        public database_backup backup;
        public settings settings = new settings();
        public proGEDIA.utilities.database db_source = new proGEDIA.utilities.database();
        public proGEDIA.utilities.database db_destination = new proGEDIA.utilities.database();
        public proGEDIA.utilities.LogWriter log = new proGEDIA.utilities.LogWriter();

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
                tbConnectionSource.Text = db_source.getConnectionString();

                cbDatabaseSource.Items.Clear();
                if( sqlCon.State == ConnectionState.Open ) {
                    sqlCon.Close();
                }
                sqlCon.ConnectionString = tbConnectionSource.Text;
                try {
                    sqlCon.Open();
                } catch(Exception ex) {
                    MessageBox.Show( "Could not connect to database.\r\n" + ex.Message );

                    return;
                }

                SqlCommand sqlCom = sqlCon.CreateCommand();
                sqlCom.CommandText = "SELECT name FROM sys.databases WHERE name NOT IN('master', 'tempdb', 'model', 'msdb')";
                try {
                    SqlDataReader sqlReader = sqlCom.ExecuteReader();
                    while( sqlReader.Read() ) {
                        cbDatabaseSource.Items.Add( sqlReader[0].ToString() );
                    }
                    sqlReader.Close();
                } catch(Exception ex) {
                    MessageBox.Show( "Could not get database list.\r\n" + ex.Message );
                }
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
                backup = new database_backup( sqlCon, cbDatabaseSource.SelectedItem.ToString(), settings.row_per_insert );
                backup.updateStatus += new EventHandler<string>( updateStatus );

                frmDataSelector frmDataSelector = new frmDataSelector( db_source, cbDatabaseSource.SelectedItem.ToString(), ref backup );

                if( frmDataSelector.ShowDialog() == DialogResult.OK ) {
                    string schema = string.Empty;

                    SaveFileDialog sfd = new SaveFileDialog();
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

    public class database_backup {
        public string name = string.Empty;

        public List<backup_settings> backup_settings = new List<backup_settings>();

        public event EventHandler<string> updateStatus;

        private Dictionary<string, database_table> table = new Dictionary<string, database_table>();
        private Dictionary<string, string> view = new Dictionary<string, string>();
        private Dictionary<string, string> function = new Dictionary<string, string>();
        private Dictionary<string, string> trigger = new Dictionary<string, string>();

        private string message = string.Empty;
        private StreamWriter sw;
        private proGEDIA.utilities.LogWriter log = new proGEDIA.utilities.LogWriter();
        private SqlConnection sqlCon;
        private int row_per_insert;

        public database_backup( SqlConnection sqlCon, string name, int row_per_insert ) {
            this.sqlCon = sqlCon;
            this.name = name;
            this.row_per_insert = row_per_insert;
        }

        public bool getDatabase() {
            this.updateStatus( this, "Generating Table's Schema" );

            if( getTable() && getView() && getFunction() && getTrigger() ) {

            } else {
                this.updateStatus( this, "enableForm" );

                return false;
            }

            return true;
        }

        public bool getSchema( StreamWriter sw ) {
            this.sw = sw;

            foreach( backup_settings bs in backup_settings ) {
                if( bs.type == "table" ) {
                    if( bs.schema ) {
                        getTableSchema( table[ bs.name ] );
                    }

                    if( bs.table ) {
                        getTableData( table[ bs.name ] );
                    }
                } else if( bs.type == "view" && bs.schema ) {
                    sw.Write( view[ bs.name ] );
                    sw.Flush();
                } else if( bs.type == "function" && bs.schema ) {
                    sw.Write( function[ bs.name ] );
                    sw.Flush();
                } else if( bs.type == "trigger" && bs.schema ) {
                    sw.Write( trigger[ bs.name ] );
                    sw.Flush();
                }
            }

            this.updateStatus( this, "enableForm" );
            return true;
        }

        private bool getTableSchema( database_table entry_table ) {
            this.updateStatus( this, "Generating Table's Schema ([" + entry_table.schema + "][" + entry_table.name + "])" );

            string schema = string.Empty;

            schema = "CREATE TABLE [" + entry_table.schema + "].[" + entry_table.name + "] (";
            /**
             * Columns
            */
            foreach( database_column entry_column in entry_table.columns ) {
                schema += "\r\n\t[" + entry_column.name + "] [" + entry_column.type + "]";
                schema += ( entry_column.is_identity ) ? ( " IDENTITY(1, 1)" ) : ( "" );
                if( entry_column.maxlen != null ) {
                    if( entry_column.maxlen == -1 ) {
                        schema += "(max)";
                    } else {
                        schema += "(" + entry_column.maxlen + ")";
                    }
                }
                schema += ( entry_column.is_nullable ) ? ( " NULL" ) : ( " NOT NULL" );
                schema += ",";
            }

            /**
             * Primary and Unique Constraints
            **/
            schema += getConstraintSchema( entry_table.constraint );
            schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );

            schema += "\r\n) ON [PRIMARY]";
            if( entry_table.is_textimage_on ) {
                schema += " TEXTIMAGE_ON [PRIMARY]";
            }
            schema += ";\r\n\r\n";

            /**
             * Unique and Foreign Keys
            **/
            schema += getUniqueKey( entry_table.uniquekey );
            schema += getForeignKey( entry_table.foreignkey );

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private string getConstraintSchema( List<database_constraint> constraint ) {
            string schema = string.Empty;

            if( constraint.Count > 0 ) {
                foreach(database_constraint entry_constraint in constraint ) {
                    schema = "\r\n\tCONSTRAINT [" + entry_constraint.name + "] ";
                    schema += ( entry_constraint.type == "PK" ) ? ( "PRIMARY KEY " ) : ( "UNIQUE " );
                    if( entry_constraint.clustered ) {
                        schema += "CLUSTERED";
                    } else {
                        schema += "NONCLUSTERED";
                    }
                    schema += " (";
                    foreach(KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
                        schema += "[" + entry_column.Key + "] ";
                        schema += ( entry_column.Value ) ? ( "DESC" ) : ( "ASC" );
                        schema += ", ";
                    }
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                    schema += ") WITH (";
                    schema += ( entry_constraint.is_padded ) ? ( "PAD_INDEX = ON, " ) : ( "PAD_INDEX = OFF, " );
                    schema += ( entry_constraint.statictics_norecompute ) ? ( "STATISTICS_NORECOMPUTE = ON, " ) : ( "STATISTICS_NORECOMPUTE = OFF, " );
                    schema += ( entry_constraint.ignore_dup_key ) ? ( "IGNORE_DUP_KEY = ON, " ) : ( "IGNORE_DUP_KEY = OFF, " );
                    schema += ( entry_constraint.allow_row_locks ) ? ( "ALLOW_ROW_LOCKS = ON, " ) : ( "ALLOW_ROW_LOCKS = OFF, " );
                    schema += ( entry_constraint.allow_page_locks ) ? ( "ALLOW_PAGE_LOCKS = ON" ) : ( "ALLOW_PAGE_LOCKS = OFF" );
                    schema += ") ON[PRIMARY],";
                }
            }

            return schema;
        }

        private string getUniqueKey( List<database_uniquekey> uniquekey ) {
            string schema = string.Empty;

            if( uniquekey.Count > 0 ) {
                foreach(database_uniquekey entry_uniquekey in uniquekey ) {
                    schema += "\r\nCREATE ";
                    if( entry_uniquekey.is_unique ) {
                        schema += "UNIQUE ";
                    }
                    if( entry_uniquekey.clustered ) {
                        schema += "CLUSTERED ";
                    } else {
                        schema += "NONCLUSTERED ";
                    }
                    schema += "INDEX [" + entry_uniquekey.name + "] ON [" + entry_uniquekey.schema + "].[" + entry_uniquekey.table + "](";
                    foreach( KeyValuePair<string, bool> entry_column in entry_uniquekey.column ) {
                        schema += "[" + entry_column.Key + "] ";
                        schema += ( entry_column.Value ) ? ( "DESC" ) : ( "ASC" );
                        schema += ", ";
                    }
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                    schema += ")\r\n\tWITH (";
                    schema += ( entry_uniquekey.is_padded ) ? ( "PAD_INDEX = ON, " ) : ( "PAD_INDEX = OFF, " );
                    schema += ( entry_uniquekey.statictics_norecompute ) ? ( "STATISTICS_NORECOMPUTE = ON, " ) : ( "STATISTICS_NORECOMPUTE = OFF, " );
                    schema += ( entry_uniquekey.sort_in_tempdb ) ? ( "SORT_IN_TEMPDB = ON, " ) : ( "SORT_IN_TEMPDB = OFF, " );
                    schema += ( entry_uniquekey.ignore_dup_key ) ? ( "IGNORE_DUP_KEY = ON, " ) : ( "IGNORE_DUP_KEY = OFF, " );
                    schema += ( entry_uniquekey.drop_existing ) ? ( "DROP_EXISTING = ON, " ) : ( "DROP_EXISTING = OFF, " );
                    schema += ( entry_uniquekey.allow_row_locks ) ? ( "ALLOW_ROW_LOCKS = ON, " ) : ( "ALLOW_ROW_LOCKS = OFF, " );
                    schema += ( entry_uniquekey.allow_page_locks ) ? ( "ALLOW_PAGE_LOCKS = ON" ) : ( "ALLOW_PAGE_LOCKS = OFF" );
                    schema += ") ON[PRIMARY];\r\n\r\n";
                }
            }

            return schema;
        }

        private string getForeignKey( List<database_foreignkey> foreignkey ) {
            string schema = string.Empty;

            if( foreignkey.Count > 0 ) {
                schema += "\r\nALTER TABLE [" + foreignkey[ 0 ].pschema + "].[" + foreignkey[ 0 ].ptable + "] ADD ";
                foreach( database_foreignkey entry_foreignkey in foreignkey ) {
                    schema += "\r\n\tCONSTRAINT [" + entry_foreignkey.name + "] FOREIGN KEY (";
                    foreach(string column in entry_foreignkey.column ) {
                        schema += "[" + column +"], ";
                    }
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                    schema += ") REFERENCES [" + entry_foreignkey.rschema + "].[" + entry_foreignkey.rtable + "] (";
                    foreach( string column in entry_foreignkey.rcolumn ) {
                        schema += "[" + column + "], ";
                    }
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                    schema += ")";
                }
                schema += ";\r\n\r\n";
            }

            return schema;
        }

        private bool getTable() {
            string tables = string.Empty;
            foreach( backup_settings entry in backup_settings ) {
                if( entry.type == "table" ) {
                    tables += "'" + entry.name + "', ";
                }
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT s.name, t.name, t.lob_data_space_id FROM sys.tables AS t INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id WHERE t.name IN(" + tables + ") ORDER BY t.name";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                if( sqlReader.HasRows ) {
                    while( sqlReader.Read()) {
                        table.Add( sqlReader.GetString( 1 ), new database_table( sqlReader.GetString( 0 ), sqlReader.GetString( 1 ), ( sqlReader.GetInt32( 2 ) == 1 ) ?( true ):( false ) ) );
                    }
                }
                sqlReader.Close();
            } catch(Exception ex) {
                log.LogWrite( "getTable" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;
                this.updateStatus( this, "Error : " + ex.Message );
                this.updateStatus( this, "enableForm" );

                return false;
            }

            if( getColumn() == false || getConstraint() == false || getUniqueKey() == false || getForeignKey() == false ) {
                return false;
            }

            return true;
        }

        private bool getView() {
            string views = string.Empty;
            foreach( backup_settings entry in backup_settings ) {
                if( entry.type == "view" && entry.schema ) {
                    views += "'" + entry.name + "', ";
                }
            }

            if( views.Length == 0 ) {
                return true;
            } else {
                views = proGEDIA.utilities.StringExtensions.Cut( views, 2 );
            }

            SqlCommand sqlCom = sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT v.name, s.definition FROM sys.views AS v INNER JOIN sys.sql_modules AS s ON v.object_id = s.object_id WHERE v.name IN(" + views + ")";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                if( sqlReader.HasRows ) {
                    while( sqlReader.Read() ) {
                        view.Add( sqlReader.GetString( 0 ), sqlReader.GetString( 1 ) );
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getView" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                return false;
            }

            return true;
        }

        private bool getFunction() {
            string functions = string.Empty;
            foreach( backup_settings entry in backup_settings ) {
                if( entry.type == "function" && entry.schema ) {
                    functions += "'" + entry.name + "', ";
                }
            }

            if( functions.Length == 0 ) {
                return true;
            } else {
                functions = proGEDIA.utilities.StringExtensions.Cut( functions, 2 );
            }

            SqlCommand sqlCom = sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT o.name, m.definition FROM sys.all_objects AS o INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id INNER JOIN sys.sql_modules AS m ON o.object_id = m.object_id WHERE type = 'FN' AND is_ms_shipped = 0 AND o.name IN(" + functions + ")";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                if( sqlReader.HasRows ) {
                    while( sqlReader.Read() ) {
                        function.Add( sqlReader.GetString( 0 ), sqlReader.GetString( 1 ) );
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getFunction" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                return false;
            }

            return true;
        }

        private bool getTrigger() {
            string triggers = string.Empty;
            foreach( backup_settings entry in backup_settings ) {
                if( entry.type == "trigger" && entry.schema ) {
                    triggers += "'" + entry.name + "', ";
                }
            }

            if( triggers.Length == 0 ) {
                return true;
            } else {
                triggers = proGEDIA.utilities.StringExtensions.Cut( triggers, 2 );
            }

            SqlCommand sqlCom = sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT t.name, s.definition FROM sys.triggers AS t INNER JOIN sys.sql_modules AS s ON t.object_id = s.object_id WHERE t.type = 'TR' AND t.name IN(" + triggers + ")";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                if( sqlReader.HasRows ) {
                    while( sqlReader.Read() ) {
                        trigger.Add( sqlReader.GetString( 0 ), sqlReader.GetString( 1 ) );
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getTrigger" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                return false;
            }

            return true;
        }

        private bool getColumn() {
            this.updateStatus( this, "Generating Table's Column Info" );

            string tables = string.Empty;
            foreach( KeyValuePair<string, database_table> entry in table ) {
                tables += "'" + entry.Key + "', ";
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = frmMain.sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT ta.name, c.name, t.name, COLUMNPROPERTY(c.object_id, c.name, 'charmaxlen') AS max_length, c.is_nullable, c.is_identity FROM sys.columns AS c INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id INNER JOIN sys.tables AS ta ON c.object_id = ta.object_id WHERE ta.name IN( " + tables + " ) ORDER BY ta.name, c.column_id";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    table[ sqlReader.GetString( 0 ) ].columns.Add( new database_column() {
                        name = sqlReader.GetString( 1 ),
                        type = sqlReader.GetString( 2 ),
                        maxlen = ( sqlReader.IsDBNull( 3 ) ) ? ( null ) : ( (Int32?)sqlReader.GetInt32( 3 ) ),
                        is_nullable = sqlReader.GetBoolean( 4 ),
                        is_identity = sqlReader.GetBoolean( 5 )
                    } );

                    if( sqlReader.GetBoolean( 5 ) ) {
                        table[ sqlReader.GetString( 0 ) ].is_identity = true;
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getColumn" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                this.updateStatus( this, "Error : " + ex.Message );
                this.updateStatus( this, "enableForm" );

                return false;
            }

            return true;
        }

        private bool getConstraint() {
            this.updateStatus( this, "Generating Table's Constraint Info" );

            string tables = string.Empty;
            foreach( KeyValuePair<string, database_table> entry in table ) {
                tables += "'" + entry.Key + "', ";
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = frmMain.sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT t.name, k.name AS key_name, k.type AS key_type, c.name AS column_name, i.type, ic.is_descending_key, i.is_padded, st.no_recompute, i.ignore_dup_key, i.allow_row_locks, i.allow_page_locks FROM sys.key_constraints AS k INNER JOIN sys.tables AS t ON k.parent_object_id = t.object_id INNER JOIN sys.indexes AS i ON i.object_id = t.object_id AND i.name = k.name INNER JOIN sys.schemas AS s ON k.schema_id = s.schema_id INNER JOIN sys.index_columns ic ON ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id INNER JOIN sys.columns AS c ON ic.column_id = c.column_id AND t.object_id = c.object_id INNER JOIN sys.stats AS st ON i.object_id = st.object_id AND i.index_id = st.stats_id WHERE k.is_ms_shipped = 0 AND t.name IN(" + tables + ") ORDER BY t.name, k.type, k.name";
            try {
                string pre = string.Empty;

                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    if( pre == sqlReader.GetString( 0 ) + "|" + sqlReader.GetString( 1 ) ) {
                        table[ sqlReader.GetString( 0 ) ].constraint[ table[ sqlReader.GetString( 0 ) ].constraint.Count - 1 ].column[ sqlReader.GetString( 3 ) ] = sqlReader.GetBoolean( 5 );
                    } else {
                        table[ sqlReader.GetString( 0 ) ].constraint.Add( new database_constraint() {
                            name = sqlReader.GetString( 1 ),
                            type = sqlReader.GetString( 2 ),
                            column = new Dictionary<string, bool>() { { sqlReader.GetString( 3 ), sqlReader.GetBoolean( 5 ) } },
                            clustered = ( sqlReader.GetByte( 4 ) == 1 ) ? ( true ) : ( false ),
                            is_descending_key = sqlReader.GetBoolean( 5 ),
                            is_padded = sqlReader.GetBoolean( 6 ),
                            statictics_norecompute = sqlReader.GetBoolean( 7 ),
                            ignore_dup_key = sqlReader.GetBoolean( 8 ),
                            allow_row_locks = sqlReader.GetBoolean( 9 ),
                            allow_page_locks = sqlReader.GetBoolean( 10 )
                        } );

                        pre = sqlReader.GetString( 0 ) + "|" + sqlReader.GetString( 1 );
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getConstraint" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                this.updateStatus( this, "Error : " + ex.Message );
                this.updateStatus( this, "enableForm" );

                return false;
            }

            return true;
        }

        private bool getUniqueKey() {
            this.updateStatus( this, "Generating Table's Unique Keys" );

            string tables = string.Empty;
            foreach( KeyValuePair<string, database_table> entry in table ) {
                tables += "'" + entry.Key + "', ";
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = frmMain.sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT t.name, c.name AS column_name, i.name, s.name, ic.is_descending_key, i.type, i.is_unique, i.is_padded, st.no_recompute, i.ignore_dup_key, i.allow_row_locks, i.allow_page_locks FROM sys.indexes AS i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.tables AS t ON i.object_id = t.object_id INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id INNER JOIN sys.columns AS c ON ic.column_id = c.column_id AND t.object_id = c.object_id INNER JOIN sys.stats AS st ON i.object_id = st.object_id AND i.index_id = st.stats_id WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND t.name IN(" + tables + ") ORDER BY t.name, i.name";
            try {
                string pre = string.Empty;

                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    if( pre == sqlReader.GetString( 0 ) + "|" + sqlReader.GetString( 2 ) ) {
                        table[ sqlReader.GetString( 0 ) ].uniquekey[ table[ sqlReader.GetString( 0 ) ].uniquekey.Count - 1 ].column[ sqlReader.GetString( 1 ) ] = sqlReader.GetBoolean( 4 );
                    } else {
                        table[ sqlReader.GetString( 0 ) ].uniquekey.Add( new database_uniquekey() {
                            name = sqlReader.GetString( 2 ),
                            schema = sqlReader.GetString( 3 ),
                            table = sqlReader.GetString( 0 ),
                            column = new Dictionary<string, bool>() { { sqlReader.GetString( 1 ), sqlReader.GetBoolean( 4 ) } },
                            clustered = (sqlReader.GetByte( 5 ) == 1)?(true):(false),
                            is_unique = sqlReader.GetBoolean( 6 ),
                            is_padded = sqlReader.GetBoolean( 7 ),
                            statictics_norecompute = sqlReader.GetBoolean( 8 ),
                            ignore_dup_key = sqlReader.GetBoolean( 9 ),
                            allow_row_locks = sqlReader.GetBoolean( 10 ),
                            allow_page_locks = sqlReader.GetBoolean( 11 )
                        } );

                        pre = sqlReader.GetString( 0 ) + "|" + sqlReader.GetString( 2 );
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getUniqueKey" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                this.updateStatus( this, "Error : " + ex.Message );
                this.updateStatus( this, "enableForm" );

                return false;
            }

            return true;
        }

        private bool getForeignKey() {
            this.updateStatus( this, "Generating Table's Unique Keys" );

            string tables = string.Empty;
            foreach( KeyValuePair<string, database_table> entry in table ) {
                tables += "'" + entry.Key + "', ";
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = frmMain.sqlCon.CreateCommand();
            sqlCom.CommandText = "SELECT pc.name AS parent_tname, sf.name AS key_schema, f.name AS key_name, spc.name AS parent_tschema, sc.name AS parent_cname, spr.name AS reference_tschema, pr.name AS reference_tname, sr.name AS reference_cname FROM sys.foreign_keys f INNER JOIN sys.schemas AS sf ON f.schema_id = sf.schema_id INNER JOIN sys.foreign_key_columns k ON k.constraint_object_id = f.object_id INNER JOIN sys.columns AS sc ON k.parent_object_id = sc.object_id AND k.parent_column_id = sc.column_id INNER JOIN sys.columns AS sr ON k.referenced_object_id = sr.object_id AND k.referenced_column_id = sr.column_id INNER JOIN sys.tables pc ON pc.object_id = f.parent_object_id INNER JOIN sys.tables pr ON pr.object_id = f.referenced_object_id INNER JOIN sys.schemas AS spc ON pc.schema_id = spc.schema_id INNER JOIN sys.schemas AS spr ON pr.schema_id = spr.schema_id WHERE pc.name IN(" + tables + ") ORDER BY pc.name, f.name, k.constraint_column_id";
            try {
                string pre = string.Empty;

                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                while( sqlReader.Read() ) {
                    if( pre == sqlReader.GetString( 0 ) + "|" + sqlReader.GetString( 2 ) ) {
                        if( table[ sqlReader.GetString( 0 ) ].foreignkey[ table[ sqlReader.GetString( 0 ) ].foreignkey.Count - 1 ].column.Contains( sqlReader.GetString( 4 ) ) == false ) {
                            table[ sqlReader.GetString( 0 ) ].foreignkey[ table[ sqlReader.GetString( 0 ) ].foreignkey.Count - 1 ].column.Add( sqlReader.GetString( 4 ) );
                        }

                        if( table[ sqlReader.GetString( 0 ) ].foreignkey[ table[ sqlReader.GetString( 0 ) ].foreignkey.Count - 1 ].rcolumn.Contains( sqlReader.GetString( 7 ) ) == false ) {
                            table[ sqlReader.GetString( 0 ) ].foreignkey[ table[ sqlReader.GetString( 0 ) ].foreignkey.Count - 1 ].rcolumn.Add( sqlReader.GetString( 7 ) );
                        }
                    } else {
                        table[ sqlReader.GetString( 0 ) ].foreignkey.Add( new database_foreignkey() {
                            name = sqlReader.GetString( 2 ),
                            schema = sqlReader.GetString( 3 ),
                            column = new List<string>() { sqlReader.GetString( 4 ) },
                            pschema = sqlReader.GetString( 1 ),
                            ptable = sqlReader.GetString( 0 ),
                            rschema = sqlReader.GetString( 5 ),
                            rtable = sqlReader.GetString( 6 ),
                            rcolumn = new List<string>() { sqlReader.GetString( 7 ) }
                        } );

                        pre = sqlReader.GetString( 0 ) + "|" + sqlReader.GetString( 2 );
                    }
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                log.LogWrite( "getForeignKey" );
                log.LogWrite( ex.Message );

                this.message = ex.Message;

                this.updateStatus( this, "Error : " + ex.Message );
                this.updateStatus( this, "enableForm" );

                return false;
            }

            return true;
        }

        private bool getTableData( database_table entry_table ) {
            this.updateStatus( this, "Generating Table's Data ([" + entry_table.schema + "][" + entry_table.name + "])" );

            string schema = string.Empty;
            SqlCommand sqlCom = sqlCon.CreateCommand();
            Type[] tof;

            sqlCom.CommandText = "SELECT * FROM [" + entry_table.schema + "].[" + entry_table.name + "]";
            try {
                SqlDataReader sqlReader = sqlCom.ExecuteReader();
                if( sqlReader.HasRows ) {
                    string columns = string.Empty;

                    sqlReader.Read();

                    tof = new Type[ sqlReader.FieldCount ];
                    for( int i = 0; i < sqlReader.FieldCount; i++ ) {
                        if( i!= 0) {
                            columns += ", ";
                        }
                        columns += sqlReader.GetName( i );

                        tof[ i ] = sqlReader.GetFieldType( i );
                    }

                    if( entry_table.is_identity ) {
                        sw.Write( "SET IDENTITY_INSERT [" + entry_table.schema + "].[" + entry_table.name + "] ON;\r\n" );
                    }

                    int j = 0;
                    do {
                        if( j % row_per_insert == 0 ) {
                            schema = "INSERT INTO [" + entry_table.schema + "].[" + entry_table.name + "] (" + columns + ") VALUES\r\n";
                        }

                        schema += "\t(";
                        for( int i = 0; i < sqlReader.FieldCount; i++ ) {
                            if( i != 0 ) {
                                schema += ", ";
                            }

                            if( sqlReader.IsDBNull(i) ) {
                                schema += "NULL";
                            } else if( tof[ i ].Name == "String" ) {
                                schema += "'" + sqlReader.GetString( i ) + "'";
                            } else if( tof[ i ].Name == "DateTime" ) {
                                schema += "'" + sqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd HH:mm:ss" ) + "'";
                            } else if( tof[ i ].Name == "Int16" ) {
                                schema += "" + sqlReader.GetInt16( i ) + "";
                            } else if( tof[ i ].Name == "Int32" ) {
                                schema += "" + sqlReader.GetInt32( i ) + "";
                            } else if( tof[ i ].Name == "Double" ) {
                                schema += "" + sqlReader.GetDouble( i ) + "";
                            } else if( tof[ i ].Name == "Byte" ) {
                                schema += "'" + sqlReader.GetByte( i ) + "'";
                            } else {
                                schema += "'#" + tof[ i ].Name + "#'";
                            }
                        }

                        schema += ")";
                        j++;
                        if( j % row_per_insert == 0 ) {
                            schema += ";\r\n";

                            sw.Write( schema );
                            sw.Flush();

                            schema = string.Empty;
                        } else {
                            schema += ",\r\n";
                        }
                    } while( sqlReader.Read() );

                    if( schema.Length > 0 ) {
                        schema = proGEDIA.utilities.StringExtensions.Cut(schema, 3) + ";\r\n";

                        sw.Write( schema );
                        sw.Flush();
                    }

                    if( entry_table.is_identity ) {
                        sw.Write( "SET IDENTITY_INSERT [" + entry_table.schema + "].[" + entry_table.name + "] OFF;\r\n" );
                    }

                    sw.Write( "\r\n\r\n" );
                    sw.Flush();
                }
                sqlReader.Close();
            } catch( Exception ex ) {
                this.message = ex.Message;

                return false;
            }

            return true;
        }
    }

    public class database_table {
        public string schema { get; set; }
        public string name { get; set; }
        public bool is_textimage_on { get; set; }
        public bool is_identity { get; set; }
        public List<database_column> columns = new List<database_column>();
        public List<database_constraint> constraint = new List<database_constraint>();
        public List<database_uniquekey> uniquekey = new List<database_uniquekey>();
        public List<database_foreignkey> foreignkey = new List<database_foreignkey>();

        public database_table( string schema, string name, bool is_textimage_on ) {
            this.schema = schema;
            this.name = name;
            this.is_textimage_on = is_textimage_on;
        }
    }

    public class database_column {
        public string name { get; set; }
        public string type { get; set; }
        public Int32? maxlen { get; set; } = null;
        public bool is_nullable { get; set; }
        public bool is_identity { get; set; }

        public database_column() {

        }

        public database_column( string name, string type, Int32? maxlen, bool is_nullable, bool is_identity ) {
            this.name = name;
            this.type = type;
            this.maxlen = maxlen;
            this.is_nullable = is_nullable;
            this.is_identity = is_identity;
        }
    }

    public class database_constraint {
        public string name { get; set; }
        public string type { get; set; }
        public Dictionary<string, bool> column { get; set; }
        public bool clustered { get; set; } = false;
        public bool is_descending_key { get; set; } = false;
        public bool is_padded { get; set; } = false;
        public bool statictics_norecompute { get; set; } = false;
        public bool ignore_dup_key { get; set; } = false;
        public bool allow_row_locks { get; set; } = false;
        public bool allow_page_locks { get; set; } = false;

        public database_constraint() {

        }
    }

    public class database_uniquekey {
        public string name { get; set; }
        public string schema { get; set; }
        public string table { get; set; }
        public Dictionary<string, bool> column { get; set; }
        public bool clustered { get; set; } = false;
        public bool is_unique { get; set; } = false;
        public bool is_padded { get; set; } = false;
        public bool statictics_norecompute { get; set; } = false;
        public bool sort_in_tempdb { get; set; } = false;
        public bool ignore_dup_key { get; set; } = false;
        public bool drop_existing { get; set; } = false;
        public bool online { get; set; } = false;
        public bool allow_row_locks { get; set; } = false;
        public bool allow_page_locks { get; set; } = false;

        public database_uniquekey() {

        }
    }

    public class database_foreignkey {
        public string name { get; set; }
        public string schema { get; set; }
        public List<string> column { get; set; }
        public string pschema { get; set; }
        public string ptable { get; set; }
        public string rschema { get; set; }
        public string rtable { get; set; }
        public List<string> rcolumn { get; set; }

        public database_foreignkey() {

        }
    }

    public class backup_settings {
        public string type { get; set; }
        public string schema_name { get; set; }
        public string name { get; set; }
        public bool schema { get; set; } = true;
        public bool table { get; set; } = true;

        public backup_settings( string type, string schema_name, string name, Boolean schema, Boolean table ) {
            this.type = type;
            this.schema_name = schema_name;
            this.name = name;
            this.schema = ( schema == true ) ? ( true ) : ( false );
            this.table = ( table == true ) ? ( true ) : ( false );
        }
    }

    public class settings {
        public int row_per_insert = 1;
        public List<proGEDIA.utilities.database> database = new List<proGEDIA.utilities.database>();

        public settings() {

        }

        public void Add( proGEDIA.utilities.database set ) {
            foreach( var item in database.Select( ( value, i ) => new { i, value } ) ) {
                if( item.value.ToString() == set.ToString() && item.value.server_type == set.server_type ) {
                    database.RemoveAt( item.i );

                    break;
                }
            }

            database.Add( set );
        }

        public void Remove( string server_type, string server_name, string user_name ) {
            foreach( var item in database.Select( ( value, i ) => new { i, value } ) ) {
                if( item.value.Compare( server_type, server_name, user_name ) ) {
                    database.RemoveAt( item.i );

                    return;
                }
            }
        }

        public void Get() {
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

        public void Save() {
            for( int i = 0; i < database.Count; i++ ) {
                if( database[ i ].remember_password == false ) {
                    database[ i ].user_pass = string.Empty;
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

namespace proGEDIA.utilities {
    public static class StringExtensions {
        public static string Right( this string str, int length ) {
            if( string.IsNullOrEmpty( str ) ) {
                str = string.Empty;
            } else if( str.Length > length ) {
                str = str.Substring( str.Length - length );
            }

            return str;
        }

        public static string Right( this string str, int length, char repeat ) {
            if( string.IsNullOrEmpty( str ) ) {
                str = string.Empty;
            } else if( str.Length > length ) {
                str = str.Substring( str.Length - length );
            } else if( str.Length > length ) {
                str = ( new string( repeat, length - str.Length ) ) + str;
            }

            return str;
        }

        public static string Left( this string str, int length ) {
            if( string.IsNullOrEmpty( str ) ) {
                str = string.Empty;
            } else if( str.Length > length ) {
                str = str.Substring( 0, str.Length - length );
            }

            return str;
        }

        public static string Cut( this string str, int length ) {
            if( string.IsNullOrEmpty( str) ) {
                str = string.Empty;
            } else if( str.Length > length ) {
                str = str.Substring( 0, str.Length - length );
            }

            return str;
        }
    }

    public static class encryption {
        public static string EncryptPassword( string password ) {
            byte[] data = Encoding.UTF8.GetBytes( password );
            byte[] encrypted_data = ProtectedData.Protect( data, null, DataProtectionScope.CurrentUser );

            return Convert.ToBase64String( encrypted_data );
        }

        public static string DecryptPassword( string password ) {
            if( password.Length > 0 ) {
                byte[] encrypted_data = Convert.FromBase64String( password );
                byte[] data = ProtectedData.Unprotect( encrypted_data, null, DataProtectionScope.CurrentUser );

                return Encoding.UTF8.GetString( data );
            }

            return "";
        }
    }

    public class LogWriter {
        private string log_path = string.Empty;
        private string prefix = string.Empty;

        public LogWriter( string prefix = "" ) {
            log_path = AppDomain.CurrentDomain.BaseDirectory + "\\log\\";
            if( prefix.Length > 0 ) {
                this.prefix = prefix + "_";
            }
        }

        public void LogWrite( string log_message ) {
            int iNumOfTry = 0;

            while( iNumOfTry < 10 ) {
                try {
                    using( StreamWriter sw = File.AppendText( log_path + "log_" + prefix + DateTime.Now.ToString( "yyyyMMdd" ) + ".txt" ) ) {
                        sw.Write( "\r\n[{0}]\t{1}", DateTime.Now.ToString( "dd.MM.yyyy HH:mm:ss.fff" ), log_message );
                    }

                    break;
                } catch {
                    iNumOfTry++;

                    Thread.Sleep( 50 );
                }
            }
        }
    }

    public class database {
        public string server_type { get; set; } = "";
        public string server_name { get; set; } = "";
        public string server_port { get; set; } = "";
        public string service_name { get; set; } = "";
        public string database_name { get; set; } = "";
        public int authentication { get; set; } = 0;
        public string user_name { get; set; } = "";
        public string user_pass { get; set; } = "";
        public bool remember_password { get; set; } = true;

        public database() {

        }

        public void Set( string server_type, string server_name, string server_port, string service_name, string database_name, int authentication, string user_name, string user_pass, bool remember_password ) {
            this.server_type = server_type;
            this.server_name = server_name;
            this.server_port = server_port;
            this.service_name = service_name;
            this.database_name = database_name;
            this.authentication = authentication;
            this.user_name = user_name;
            this.user_pass = user_pass;
            this.remember_password = remember_password;
        }

        public string getConnectionString() {
            string connectionString = string.Empty;
            switch( server_type ) {
                case "MsSQL":
                    // MultipleActiveResultSets=true
                    if( authentication == 0 )
                        connectionString = "Data Source=" + server_name + ";Integrated Security=SSPI;";
                    else
                        connectionString = "Data Source=" + server_name + ";User id=" + user_name + ";Password=" + user_pass + ";";
                    break;
            }

            return connectionString;
        }

        public bool Compare( string server_type, string server_name, string user_name) {
            if( this.server_type == server_type && this.service_name == service_name && this.user_name == user_name ) {
                return true;
            } else {
                return false;
            }
        }

        public override string ToString() {
            return this.server_name;
        }
    }
}