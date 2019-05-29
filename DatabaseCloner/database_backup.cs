using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data.SQLite;

namespace DatabaseCloner {
    public class database_backup {
        public string name = string.Empty;

        public List<backup_settings> backup_settings = new List<backup_settings>();

        public event EventHandler<string> updateStatus;

        private readonly Dictionary<string, database_table> table = new Dictionary<string, database_table>();
        private readonly Dictionary<string, database_view> view = new Dictionary<string, database_view>();
        private readonly Dictionary<string, database_function> function = new Dictionary<string, database_function>();
        private readonly Dictionary<string, database_trigger> trigger = new Dictionary<string, database_trigger>();

        private string message = string.Empty;
        private StreamWriter sw;
        private readonly proGEDIA.utilities.LogWriter log = new proGEDIA.utilities.LogWriter();
        private readonly proGEDIA.utilities.database db;
        private readonly int row_per_insert;

        public database_backup( proGEDIA.utilities.database db, string name, int row_per_insert ) {
            this.db = db;
            this.name = name;
            this.row_per_insert = row_per_insert;

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    db.mssqlCon.ChangeDatabase( name );
                break;

                case "mysql":
                    db.mysqlCon.ChangeDatabase( name );
                break;

                case "sqlite":

                break;
            }
        }

        public bool getDatabase( ) {
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
                        if( getTableSchema( table[ bs.name ] ) == false ) {
                            return false;
                        }
                    }

                    if( bs.table ) {
                        if( getTableData( table[ bs.name ] ) == false ) {
                            return false;
                        }
                    }
                } else if( bs.type == "view" && bs.schema ) {
                    if( getTableView( view[ bs.name ] ) == false ) {
                        return false;
                    }
                } else if( bs.type == "function" && bs.schema ) {
                    if( getTableFunction( function[ bs.name ] ) == false ) {
                        return false;
                    }
                } else if( bs.type == "trigger" && bs.schema ) {
                    if( getTableTrigger( trigger[ bs.name ] ) == false ) {
                        return false;
                    }
                }
            }

            this.updateStatus( this, "enableForm" );
            return true;
        }

        private string getUniqueKey( List<database_uniquekey> uniquekey ) {
            string schema = string.Empty;
            if( db.server_type.ToLower() != "mssql" ) {
                return "";
            }

            if( uniquekey.Count > 0 ) {
                foreach( database_uniquekey entry_uniquekey in uniquekey ) {
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
            if( db.server_type.ToLower() != "mssql" ) {
                return "";
            }

            if( foreignkey.Count > 0 ) {
                schema += "\r\nALTER TABLE [" + foreignkey[ 0 ].pschema + "].[" + foreignkey[ 0 ].ptable + "] ADD ";
                foreach( database_foreignkey entry_foreignkey in foreignkey ) {
                    schema += "\r\n\tCONSTRAINT [" + entry_foreignkey.name + "] FOREIGN KEY (";
                    foreach( string column in entry_foreignkey.column ) {
                        schema += "[" + column + "], ";
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

        private bool getTable( ) {
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

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT s.name, t.name, t.lob_data_space_id FROM sys.tables AS t INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id WHERE t.name IN(" + tables + ") ORDER BY t.name";

                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        if( mssqlReader.HasRows ) {
                            while( mssqlReader.Read() ) {
                                table.Add( mssqlReader.GetString( 1 ), new database_table( mssqlReader.GetString( 0 ), mssqlReader.GetString( 1 ) ) {
                                    is_textimage_on = ( mssqlReader.GetInt32( 2 ) == 1 ) ? ( true ) : ( false )
                                } );
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTable" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;
                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, engine, table_collation, character_set_name FROM information_schema.tables AS t LEFT JOIN information_schema.collation_character_set_applicability AS c ON t.table_collation = c.collation_name WHERE table_type = 'BASE TABLE' AND table_schema = '" + name + "' AND table_name IN(" + tables + ") ORDER BY table_schema";

                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        if( mysqlReader.HasRows ) {
                            while( mysqlReader.Read() ) {
                                table.Add( mysqlReader.GetString( 0 ), new database_table( "", mysqlReader.GetString( 0 ) ) {
                                    db_engine = mysqlReader.GetString( 1 ),
                                    db_collation = mysqlReader.GetString( 2 ),
                                    db_character_set = mysqlReader.GetString( 3 )
                                } );
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTable" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;
                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;

                case "sqlite":
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT name FROM sqlite_master";

                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        if( sqliteReader.HasRows ) {
                            while( sqliteReader.Read()) {
                                table.Add( sqliteReader.GetString( 0 ), new database_table( "", sqliteReader.GetString( 0 ) ) );
                            }
                        }
                        sqliteReader.Close();
                    } catch(Exception ex) {
                        log.LogWrite( "getTable" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;
                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;
            }

            if( getColumn() == false || getConstraint() == false || getUniqueKey() == false || getForeignKey() == false ) {
                return false;
            }

            return true;
        }

        private bool getView( ) {
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

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT v.name, s.definition FROM sys.views AS v INNER JOIN sys.sql_modules AS s ON v.object_id = s.object_id WHERE v.name IN(" + views + ")";

                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        if( mssqlReader.HasRows ) {
                            while( mssqlReader.Read() ) {
                                view.Add( mssqlReader.GetString( 0 ), new database_view() {
                                    name = mssqlReader.GetString( 0 ),
                                    schema = mssqlReader.GetString( 1 )
                                } );
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getView" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;
                        
                        return false;
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, view_definition, definer, security_type FROM information_schema.views WHERE table_schema = '" + name + "' AND table_name IN(" + views + ")";

                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        if( mysqlReader.HasRows ) {
                            while( mysqlReader.Read() ) {
                                view.Add( mysqlReader.GetString( 0 ), new database_view() {
                                    name = mysqlReader.GetString( 0 ),
                                    schema = mysqlReader.GetString( 1 ),
                                    definer = mysqlReader.GetString( 2 ),
                                    security_type = mysqlReader.GetString( 3 )
                                } );
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getView" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        return false;
                    }
                break;
            }

            return true;
        }

        private bool getFunction( ) {
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

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT o.name, m.definition FROM sys.all_objects AS o INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id INNER JOIN sys.sql_modules AS m ON o.object_id = m.object_id WHERE type = 'FN' AND is_ms_shipped = 0 AND o.name IN(" + functions + ")";

                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        if( mssqlReader.HasRows ) {
                            while( mssqlReader.Read() ) {
                                function.Add( mssqlReader.GetString( 0 ), new database_function() {
                                    name = mssqlReader.GetString( 0 ),
                                    schema = mssqlReader.GetString( 1 )
                                } );
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getFunction" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        return false;
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT name, param_list, returns, body, definer FROM mysql.proc WHERE db = '" + name + "' AND name IN(" + functions + ")";

                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        if( mysqlReader.HasRows ) {
                            while( mysqlReader.Read() ) {
                                function.Add( mysqlReader.GetString( 0 ), new database_function() {
                                    name = mysqlReader.GetString( 0 ),
                                    param_list = mysqlReader.GetString( 1 ),
                                    returns = mysqlReader.GetString( 2 ),
                                    schema = mysqlReader.GetString( 3 ),
                                    definer = mysqlReader.GetString( 4 )
                                } );
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getFunction" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        return false;
                    }
                break;
            }

            return true;
        }

        private bool getTrigger( ) {
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

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT t.name, s.definition FROM sys.triggers AS t INNER JOIN sys.sql_modules AS s ON t.object_id = s.object_id WHERE t.type = 'TR' AND t.name IN(" + triggers + ")";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        if( mssqlReader.HasRows ) {
                            while( mssqlReader.Read() ) {
                                trigger.Add( mssqlReader.GetString( 0 ), new database_trigger() {
                                    name = mssqlReader.GetString( 0 ),
                                    schema = mssqlReader.GetString( 1 )
                                } );
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTrigger" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        return false;
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT trigger_name, event_object_table, action_timing, event_manipulation, action_statement, action_orientation FROM information_schema.triggers WHERE trigger_schema = '" + name + "' AND trigger_name IN(" + triggers + ")";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        if( mysqlReader.HasRows ) {
                            while( mysqlReader.Read() ) {
                                trigger.Add( mysqlReader.GetString( 0 ), new database_trigger() {
                                    name = mysqlReader.GetString( 0 ),
                                    table = mysqlReader.GetString( 1 ),
                                    action_timing = mysqlReader.GetString( 2 ),
                                    event_manupilation = mysqlReader.GetString( 3 ),
                                    schema = mysqlReader.GetString( 4 ),
                                    action_orientation = mysqlReader.GetString( 5 )
                                } );
                            }
                        }
                    mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTrigger" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        return false;
                    }
                break;
            }

            return true;
        }

        private bool getColumn( ) {
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

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT ta.name, c.name, t.name, COLUMNPROPERTY(c.object_id, c.name, 'charmaxlen') AS max_length, c.is_nullable, c.is_identity FROM sys.columns AS c INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id INNER JOIN sys.tables AS ta ON c.object_id = ta.object_id WHERE ta.name IN( " + tables + " ) ORDER BY ta.name, c.column_id";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            table[ mssqlReader.GetString( 0 ) ].columns.Add( new database_column() {
                                name = mssqlReader.GetString( 1 ),
                                type = mssqlReader.GetString( 2 ),
                                maxlen = ( mssqlReader.IsDBNull( 3 ) ) ? ( null ) : ( (Int32?)mssqlReader.GetInt32( 3 ) ),
                                is_nullable = mssqlReader.GetBoolean( 4 ),
                                is_identity = mssqlReader.GetBoolean( 5 )
                            } );

                            if( mssqlReader.GetBoolean( 5 ) ) {
                                table[ mssqlReader.GetString( 0 ) ].is_identity = true;
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getColumn" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }            
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, column_name, column_default, is_nullable, column_type, collation_name, extra FROM information_schema.columns WHERE table_schema = '" + name + "' AND table_name IN(" + tables + ") ORDER BY table_name, ordinal_position";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            table[ mysqlReader.GetString( 0 ) ].columns.Add( new database_column() {
                                name = mysqlReader.GetString( 1 ),
                                column_default = ( mysqlReader.IsDBNull( 2 ) )?( "NULL" ):( mysqlReader.GetString( 2 ) ),
                                type = mysqlReader.GetString( 4 ),
                                is_nullable = ( mysqlReader.GetString( 3 ) == "YES") ?( true ):( false ),
                                collation_name = ( mysqlReader.IsDBNull( 5 ) )?( "" ):( mysqlReader.GetString( 5 ) ),
                                is_identity = ( mysqlReader.GetString( 6 ).ToLower() == "auto_increment" ) ?( true ):( false )
                            } );

                            if( mysqlReader.GetString( 6 ).ToLower() == "auto_increment" ) {
                                table[ mysqlReader.GetString( 0 ) ].is_identity = true;
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getColumn" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;
            }

            return true;
        }

        private bool getConstraint( ) {
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

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT t.name, k.name AS key_name, k.type AS key_type, c.name AS column_name, i.type, ic.is_descending_key, i.is_padded, st.no_recompute, i.ignore_dup_key, i.allow_row_locks, i.allow_page_locks FROM sys.key_constraints AS k INNER JOIN sys.tables AS t ON k.parent_object_id = t.object_id INNER JOIN sys.indexes AS i ON i.object_id = t.object_id AND i.name = k.name INNER JOIN sys.schemas AS s ON k.schema_id = s.schema_id INNER JOIN sys.index_columns ic ON ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id INNER JOIN sys.columns AS c ON ic.column_id = c.column_id AND t.object_id = c.object_id INNER JOIN sys.stats AS st ON i.object_id = st.object_id AND i.index_id = st.stats_id WHERE k.is_ms_shipped = 0 AND t.name IN(" + tables + ") ORDER BY t.name, k.type, k.name";
                    try {
                        string pre = string.Empty;

                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            if( pre == mssqlReader.GetString( 0 ) + "|" + mssqlReader.GetString( 1 ) ) {
                                table[ mssqlReader.GetString( 0 ) ].constraint[ table[ mssqlReader.GetString( 0 ) ].constraint.Count - 1 ].column[ mssqlReader.GetString( 3 ) ] = mssqlReader.GetBoolean( 5 );
                            } else {
                                table[ mssqlReader.GetString( 0 ) ].constraint.Add( new database_constraint() {
                                    name = mssqlReader.GetString( 1 ),
                                    type = mssqlReader.GetString( 2 ),
                                    column = new Dictionary<string, bool>() { { mssqlReader.GetString( 3 ), mssqlReader.GetBoolean( 5 ) } },
                                    clustered = ( mssqlReader.GetByte( 4 ) == 1 ) ? ( true ) : ( false ),
                                    is_descending_key = mssqlReader.GetBoolean( 5 ),
                                    is_padded = mssqlReader.GetBoolean( 6 ),
                                    statictics_norecompute = mssqlReader.GetBoolean( 7 ),
                                    ignore_dup_key = mssqlReader.GetBoolean( 8 ),
                                    allow_row_locks = mssqlReader.GetBoolean( 9 ),
                                    allow_page_locks = mssqlReader.GetBoolean( 10 )
                                } );

                                pre = mssqlReader.GetString( 0 ) + "|" + mssqlReader.GetString( 1 );
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getConstraint" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, non_unique, index_name, column_name FROM information_schema.statistics WHERE table_schema = '" + name + "' ORDER BY table_name";
                    try {
                        string pre = string.Empty;

                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            if( pre == mysqlReader.GetString( 0 ) + "|" + mysqlReader.GetString( 2 ) ) {
                                table[ mysqlReader.GetString( 0 ) ].constraint[ table[ mysqlReader.GetString( 0 ) ].constraint.Count - 1 ].column.Add( mysqlReader.GetString( 3 ), true );
                            } else {
                                table[ mysqlReader.GetString( 0 ) ].constraint.Add( new database_constraint() {
                                    name = mysqlReader.GetString( 2 ),
                                    column = new Dictionary<string, bool>() { { mysqlReader.GetString( 3 ), true } },
                                    is_unique = ( mysqlReader.GetInt32( 1 ) == 1) ? (false) : (true)
                                } );

                                pre = mysqlReader.GetString( 0 ) + "|" + mysqlReader.GetString( 2 );
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getConstraint" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;
            }

            return true;
        }

        private bool getUniqueKey( ) {
            this.updateStatus( this, "Generating Table's Unique Keys" );
            if( db.server_type.ToLower() != "mssql" ) {
                return true;
            }

            string tables = string.Empty;
            foreach( KeyValuePair<string, database_table> entry in table ) {
                tables += "'" + entry.Key + "', ";
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = db.mssqlCon.CreateCommand();
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
                            clustered = ( sqlReader.GetByte( 5 ) == 1 ) ? ( true ) : ( false ),
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

        private bool getForeignKey( ) {
            this.updateStatus( this, "Generating Table's Unique Keys" );
            if( db.server_type.ToLower() != "mssql" ) {
                return true;
            }

            string tables = string.Empty;
            foreach( KeyValuePair<string, database_table> entry in table ) {
                tables += "'" + entry.Key + "', ";
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = proGEDIA.utilities.StringExtensions.Cut( tables, 2 );
            }

            SqlCommand sqlCom = db.mssqlCon.CreateCommand();
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

        private bool getTableSchema( database_table entry_table ) {
            string schema = string.Empty;

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    this.updateStatus( this, "Generating Table's Schema ([" + entry_table.schema + "][" + entry_table.name + "])" );

                    schema = "CREATE TABLE [" + entry_table.schema + "].[" + entry_table.name + "] (";

                    /**
                     * Columns
                    */
                    foreach( database_column entry_column in entry_table.columns ) {
                        schema += "\r\n\t[" + entry_column.name + "] [" + entry_column.type + "]";
                        if( entry_column.is_identity ) {
                            schema += " IDENTITY(1, 1)";
                        }

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
                break;

                case "mysql":
                    this.updateStatus( this, "Generating Table's Schema (" + entry_table.name + ")" );

                    schema = "CREATE TABLE " + entry_table.name + " (";
                
                    /**
                     * Columns
                    */
                    foreach( database_column entry_column in entry_table.columns ) {
                        schema += "\r\n\t" + entry_column.name + " " + entry_column.type + "";

                        if( entry_column.collation_name != "" ) {
                            schema += "COLLATE " + entry_column.collation_name;
                        }

                        if( entry_column.column_default != "NULL" ) {
                            schema += " DEFAULT " + entry_column.column_default;
                        }

                        if( entry_column.is_nullable == false ) {
                            schema += " NOT NULL";
                        }

                        if( entry_column.is_identity ) {
                            schema += " auto_increment";
                        }

                        schema += ",";
                    }

                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );

                    /**
                     * Primary and Unique Constraints
                    **/
                    schema += getConstraintSchema( entry_table.constraint );
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );
                    schema += "\r\n) ENGINE=" + entry_table.db_engine + " DEFAULT CHARSET=" + entry_table.db_character_set + " COLLATE=" + entry_table.db_collation + ";\r\n\r\n";
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private string getConstraintSchema( List<database_constraint> constraint ) {
            string schema = string.Empty;

            if( constraint.Count > 0 ) {
                switch( db.server_type.ToLower() ) {
                    case "mssql":
                        foreach( database_constraint entry_constraint in constraint ) {
                            schema += "\r\n\tCONSTRAINT [" + entry_constraint.name + "] ";
                            schema += ( entry_constraint.type == "PK" ) ? ( "PRIMARY KEY " ) : ( "UNIQUE " );

                            if( entry_constraint.clustered ) {
                                schema += "CLUSTERED";
                            } else {
                                schema += "NONCLUSTERED";
                            }

                            schema += " (";
                            foreach( KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
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
                    break;

                    case "mysql":
                        foreach( database_constraint entry_constraint in constraint ) {
                            if( entry_constraint.name == "PRIMARY" ) {
                                schema += "\r\n\tPRIMARY KEY (";

                                foreach( KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
                                    schema += entry_column.Key + ", ";
                                }

                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                                schema += "),";
                            } else if( entry_constraint.name.IndexOf( "UNIQUE" ) == 0 ) {
                                schema += " UNIQUE " + entry_constraint.name.Substring( 7 ) + " (";

                                foreach( KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
                                    schema += entry_column.Key + ", ";
                                }

                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                                schema += "),";
                            } else {
                                schema += " KEY " + entry_constraint.name + " (";

                                foreach( KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
                                    schema += entry_column.Key + ", ";
                                }

                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                                schema += "),";
                            }
                        }
                    break;
                }
            }

            return schema;
        }

        private bool getTableData( database_table entry_table ) {
            string schema = string.Empty;
            Type[ ] tof;

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    this.updateStatus( this, "Generating Table's Data ([" + entry_table.schema + "][" + entry_table.name + "])" );

                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT * FROM [" + entry_table.schema + "].[" + entry_table.name + "]";

                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();

                        if( mssqlReader.HasRows ) {
                            string columns = string.Empty;

                            mssqlReader.Read();

                            tof = new Type[ mssqlReader.FieldCount ];
                            for( int i = 0; i < mssqlReader.FieldCount; i++ ) {
                                if( i != 0 ) {
                                    columns += ", ";
                                }
                                columns += mssqlReader.GetName( i );

                                tof[ i ] = mssqlReader.GetFieldType( i );
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

                                for( int i = 0; i < mssqlReader.FieldCount; i++ ) {
                                    if( i != 0 ) {
                                        schema += ", ";
                                    }

                                    if( mssqlReader.IsDBNull( i ) ) {
                                        schema += "NULL";
                                    } else {
                                        switch( tof[ i ].Name ) {
                                            case "String":
                                                schema += "'" + proGEDIA.utilities.StringExtensions.sqlText( mssqlReader.GetString( i ) ) + "'";
                                            break;

                                            case "Date":
                                                schema += "'" + mssqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd" ) + "'";
                                            break;

                                            case "DateTime":
                                                schema += "'" + mssqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd HH:mm:ss" ) + "'";
                                            break;

                                            case "Boolean":
                                                schema += ( mssqlReader.GetBoolean( i ) ) ? ("1") : ("0");
                                            break;

                                            case "Int16":
                                                schema += "" + mssqlReader.GetInt16( i ) + "";
                                            break;

                                            case "Int32":
                                                schema += "" + mssqlReader.GetInt32( i ) + "";
                                            break;

                                            case "Double":
                                                schema += "" + mssqlReader.GetDouble( i ) + "";
                                            break;

                                            case "Single":
                                                schema += "'" + mssqlReader.GetFloat( i ) + "'";
                                            break;

                                            case "Decimal":
                                                schema += "" + mssqlReader.GetDecimal( i ) + "";
                                            break;

                                            case "Byte":
                                                schema += "'" + mssqlReader.GetByte( i ) + "'";
                                            break;

                                            case "Byte[]":
                                                long size = mssqlReader.GetBytes( i, 0, null, 0, 0 );
                                                byte[ ] result = new byte[ size ];
                                                int bufferSize = 1024;
                                                long bytesRead = 0;
                                                int curPos = 0;

                                                while( bytesRead < size ) {
                                                    bytesRead += mssqlReader.GetBytes( i, curPos, result, curPos, bufferSize );
                                                    curPos += bufferSize;
                                                }

                                                schema += "'0x" + proGEDIA.utilities.StringExtensions.ByteArrayToString( result ) + "'";
                                            break;

                                            default:
                                                schema += "'#" + tof[ i ].Name + "#'";
                                            break;
                                        }
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
                            } while( mssqlReader.Read() );

                            if( schema.Length > 0 ) {
                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 3 ) + ";\r\n";

                                sw.Write( schema );
                                sw.Flush();
                            }

                            if( entry_table.is_identity ) {
                                sw.Write( "SET IDENTITY_INSERT [" + entry_table.schema + "].[" + entry_table.name + "] OFF;\r\n" );
                            }

                            sw.Write( "\r\n\r\n" );
                            sw.Flush();
                        }

                        mssqlReader.Close();
                    } catch(Exception ex) {
                        log.LogWrite( "getTableData" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;

                case "mysql":
                    this.updateStatus( this, "Generating Table's Data (" + entry_table.name + "]" );

                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT * FROM " + entry_table.name + "";

                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();

                        if( mysqlReader.HasRows ) {
                            string columns = string.Empty;

                            mysqlReader.Read();

                            tof = new Type[ mysqlReader.FieldCount ];
                            for( int i = 0; i < mysqlReader.FieldCount; i++ ) {
                                if( i != 0 ) {
                                    columns += ", ";
                                }
                                columns += mysqlReader.GetName( i );

                                tof[ i ] = mysqlReader.GetFieldType( i );
                            }

                            int j = 0;
                            do {
                                if( j % row_per_insert == 0 ) {
                                    schema = "INSERT INTO " + entry_table.name + " (" + columns + ") VALUES\r\n";
                                }

                                schema += "\t(";

                                for( int i = 0; i < mysqlReader.FieldCount; i++ ) {
                                    if( i != 0 ) {
                                        schema += ", ";
                                    }

                                    if( mysqlReader.IsDBNull( i ) ) {
                                        schema += "NULL";
                                    } else {
                                        switch( tof[ i ].Name ) {
                                            case "String":
                                                schema += "'" + proGEDIA.utilities.StringExtensions.sqlText( mysqlReader.GetString( i ) ) + "'";
                                            break;

                                            case "DateTime":
                                                schema += "'" + mysqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd HH:mm:ss" ) + "'";
                                            break;

                                            case "UInt16":
                                            case "Int16":
                                                schema += "" + mysqlReader.GetInt16( i ) + "";
                                            break;

                                            case "UInt32":
                                            case "Int32":
                                                schema += "" + mysqlReader.GetInt32( i ) + "";
                                            break;

                                            case "Double":
                                                schema += "" + mysqlReader.GetDouble( i ) + "";
                                            break;

                                            case "SByte":
                                                schema += "'" + mysqlReader.GetSByte( i ) + "'";
                                            break;

                                            case "Byte":
                                                schema += "'" + mysqlReader.GetByte( i ) + "'";
                                            break;

                                            case "Boolean":
                                                schema += ( mysqlReader.GetBoolean( i ) ) ? ( "1" ) : ( "0" );
                                            break;

                                            default:
                                                schema += "'#" + tof[ i ].Name + "#'";
                                            break;
                                        }
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
                            } while( mysqlReader.Read() );

                            if( schema.Length > 0 ) {
                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 3 ) + ";\r\n";

                                sw.Write( schema );
                                sw.Flush();
                            }

                            sw.Write( "\r\n\r\n" );
                            sw.Flush();
                        }

                        mysqlReader.Close();
                    } catch(Exception ex) {
                        log.LogWrite( "getTableData" );
                        log.LogWrite( ex.Message );

                        this.message = ex.Message;

                        this.updateStatus( this, "Error : " + ex.Message );
                        this.updateStatus( this, "enableForm" );

                        return false;
                    }
                break;
            }

            return true;
        }

        private bool getTableView( database_view entry_view ) {
            string schema = string.Empty;

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    schema = entry_view.schema + ";\r\n\r\n";
                break;

                case "mysql":
                    schema = "CREATE ALGORITHM=UNDEFINED DEFINER=" + entry_view.definer + " SQL SECURITY " + entry_view.security_type + " VIEW " + entry_view.name + " AS " + entry_view.schema + ";\r\n\r\n";
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private bool getTableFunction( database_function entry_function ) {
            string schema = string.Empty;

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    schema = entry_function.schema + ";\r\n\r\n";
                break;

                case "mysql":
                    schema += "DELIMITER $$\r\n";
                    schema += "CREATE DEFINER=" + entry_function.definer + " FUNCTION " + entry_function.name + "(" + entry_function.param_list + ") RETURNS " + entry_function.returns + "\r\n";
                    schema += entry_function.schema;
                    schema += "$$\r\n";
                    schema += "DELIMITER;\r\n\r\n";
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private bool getTableTrigger( database_trigger entry_trigger ) {
            string schema = string.Empty;

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    schema = entry_trigger.schema + ";\r\n\r\n";
                break;

                case "mysql":
                    schema += "DELIMITER $$\r\n";
                    schema += "CREATE TRIGGER " + entry_trigger.name + " " + entry_trigger.action_timing + " " + entry_trigger.event_manupilation + " ON " + entry_trigger.table + "";
                    schema += "\r\n\tFOR EACH " + entry_trigger.action_orientation + "";
                    schema += "\r\n" + entry_trigger.schema;
                    schema += "\r\n$$";
                    schema += "\r\nDELIMITER;\r\n\r\n";
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        public List<table_entry> getTableList() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT s.name, t.name, object_id FROM sys.tables AS t INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id WHERE t.type = 'U' ORDER BY t.name";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            tableList.Add( new table_entry( mssqlReader.GetString( 0 ), mssqlReader.GetString( 1 ) ));
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = '" + name + "' ORDER BY table_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                break;

                case "sqlite":
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            tableList.Add( new table_entry( "", sqliteReader.GetString(0) ) );
                        }
                    } catch(Exception ex) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                break;
            }

            return tableList;
        }

        public List<table_entry> getViewList() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT name FROM sys.views";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mssqlReader.GetString( 0 ) ) );
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get view list.\r\n" + ex.Message );
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT name FROM sys.views ORDER BY name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get view list.\r\n" + ex.Message );
                    }
                break;

                case "sqlite":
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT name FROM sqlite_master WHERE type = 'view' ORDER BY name";
                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            tableList.Add( new table_entry( "", sqliteReader.GetString( 0 ) ) );
                        }
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                break;
            }

            return tableList;
        }

        public List<table_entry> getFunctionList() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT s.name, o.name FROM sys.all_objects AS o INNER JOIN sys.schemas AS s ON o.schema_id = s.schema_id WHERE type = 'FN' AND is_ms_shipped = 0 ORDER BY s.name, o.name";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            tableList.Add( new table_entry( mssqlReader.GetString( 0 ), mssqlReader.GetString( 1 ) ) );
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get function list.\r\n" + ex.Message );
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT name FROM mysql.proc WHERE db = '" + name + "' ORDER BY name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get function list.\r\n" + ex.Message );
                    }
                break;

                case "sqlite":

                break;
            }

            return tableList;
        }

        public List<table_entry> getTriggerList() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.server_type.ToLower() ) {
                case "mssql":
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT name FROM sys.triggers WHERE type = 'TR' ORDER BY name";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mssqlReader.GetString( 0 ) ) );
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get trigger list.\r\n" + ex.Message );
                    }
                break;

                case "mysql":
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT trigger_name FROM information_schema.triggers WHERE TRIGGER_SCHEMA = '" + name + "' ORDER BY trigger_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get trigger list.\r\n" + ex.Message );
                    }
                break;

                case "sqlite":
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT name FROM sqlite_master WHERE type = 'trigger' ORDER BY name";
                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            tableList.Add( new table_entry( "", sqliteReader.GetString( 0 ) ) );
                        }
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get trigger list.\r\n" + ex.Message );
                    }
                break;
            }

            return tableList;
        }
    }

    public class database_table {
        public string schema { get; set; }
        public string name { get; set; }

        /* MsSQL */
        public bool is_textimage_on { get; set; }
        public bool is_identity { get; set; }

        /* MySQL */
        public string db_engine { get; set; }
        public string db_collation { get; set; }
        public string db_character_set { get; set; }

        public List<database_column> columns = new List<database_column>();
        public List<database_constraint> constraint = new List<database_constraint>();
        public List<database_uniquekey> uniquekey = new List<database_uniquekey>();
        public List<database_foreignkey> foreignkey = new List<database_foreignkey>();

        public database_table( string schema, string name ) {
            this.schema = schema;
            this.name = name;
        }
    }

    public class database_column {
        public string name { get; set; }
        public string type { get; set; }
        public Int32? maxlen { get; set; } = null;
        public bool is_nullable { get; set; }
        public bool is_identity { get; set; }
        public string collation_name { get; set; }
        public string column_default { get; set; }

        public database_column( ) {

        }

        public database_column( string name, string type, Int32? maxlen, bool is_nullable, bool is_identity ) {
            this.name = name;
            this.type = type;
            this.maxlen = maxlen;
            this.is_nullable = is_nullable;
            this.is_identity = is_identity;
        }
    }

    public class database_view {
        public string name { get; set; }
        public string schema { get; set; }
        public string definer { get; set; }
        public string security_type { get; set; }

        public database_view( ) {

        }
    }

    public class database_function {
        public string name { get; set; }
        public string param_list { get; set; }
        public string returns { get; set; }
        public string schema { get; set; }
        public string definer { get; set; }

        public database_function() {

        }
    }

    public class database_trigger {
        public string name { get; set; }
        public string table { get; set; }
        public string action_timing { get; set; }
        public string event_manupilation { get; set; }
        public string schema { get; set; }
        public string action_orientation { get; set; }

        public database_trigger() {

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
        public bool is_unique { get; set; } = false;

        public database_constraint( ) {

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

        public database_uniquekey( ) {

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

        public database_foreignkey( ) {

        }
    }

    public class table_entry {
        public string schema { get; set; } = "";
        public string name { get; set; } = "";

        public table_entry( string schema, string name) {
            this.schema = schema;
            this.name = name;
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
}