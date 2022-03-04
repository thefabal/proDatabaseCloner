using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Data.SqlClient;
using System.Data.SQLite;
using MySql.Data.MySqlClient;

using System.Text.RegularExpressions;

namespace DatabaseCloner {
    public class databaseBackup {
        public string databaseName = string.Empty;

        public List<backup_settings> backupSettings = new List<backup_settings>();

        public event EventHandler<string> updateStatus;

        private readonly Dictionary<string, database_table> table = new Dictionary<string, database_table>();
        private readonly Dictionary<string, database_view> view = new Dictionary<string, database_view>();
        private readonly Dictionary<string, database_function> function = new Dictionary<string, database_function>();
        private readonly Dictionary<string, database_trigger> trigger = new Dictionary<string, database_trigger>();
        private readonly Dictionary<string, database_procedure> procedure = new Dictionary<string, database_procedure>();

        private StreamWriter sw;
        private readonly proGEDIA.utilities.LogWriter log = new proGEDIA.utilities.LogWriter();
        private readonly proGEDIA.utilities.database db;
        private readonly int rowPerInsert;

        public databaseBackup( proGEDIA.utilities.database db, string databaseName, int rowPerInsert ) {
            this.db = db;
            this.databaseName = databaseName;
            this.rowPerInsert = rowPerInsert;

            switch( db.serverType.ToLower() ) {
                case "mssql": { db.mssqlCon.ChangeDatabase( databaseName ); } break;
                case "mysql": { db.mysqlCon.ChangeDatabase( databaseName ); } break;
            }
        }

        public bool getDatabase( ) {
            updateStatus( this, "Generating Table's Schema" );

            if( getSchemaTable() && getSchemaView() && getSchemaFunction() && getSchemaTrigger() && getSchemaProcedure() ) {

            } else {
                updateStatus( this, "enableForm" );

                return false;
            }

            return true;
        }

        public bool getSchema( StreamWriter sw ) {
            this.sw = sw;

            foreach( backup_settings bs in backupSettings ) {
                if( bs.type == "table" ) {
                    if( bs.schema ) {
                        if( writeSchemaTable( table[ bs.name ] ) == false ) {
                            return false;
                        }
                    }

                    if( bs.table ) {
                        if( writeTableData( table[ bs.name ] ) == false ) {
                            return false;
                        }
                    }

                    if( bs.schema ) {
                        if( writeSchemaTableAutoIncrement( table[ bs.name ]  ) == false ) {
                            return false;
                        }
                    }
                } else if( bs.type == "view" && bs.schema ) {
                    if( writeSchemaView( view[ bs.name ] ) == false ) {
                        return false;
                    }
                } else if( bs.type == "function" && bs.schema ) {
                    if( writeSchemaFunction( function[ bs.name ] ) == false ) {
                        return false;
                    }
                } else if( bs.type == "trigger" && bs.schema ) {
                    if( writeSchemaTrigger( trigger[ bs.name ] ) == false ) {
                        return false;
                    }
                } else if( bs.type == "procedure" && bs.schema ) {
                    if( writeSchemaProcedure( procedure[ bs.name ] ) == false ) {
                        return false;
                    }
                }
            }

            updateStatus( this, "enableForm" );

            return true;
        }

        private bool getSchemaTable( ) {
            string tables = string.Empty;
            foreach( backup_settings entry in backupSettings ) {
                if( entry.type == "table" ) {
                    tables += ", '" + entry.name + "'";
                }
            }

            if( tables.Length == 0 ) {
                return true;
            } else {
                tables = tables.Substring( 2 );
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, engine, table_collation, character_set_name, auto_increment FROM information_schema.tables AS t LEFT JOIN information_schema.collation_character_set_applicability AS c ON t.table_collation = c.collation_name WHERE table_type = 'BASE TABLE' AND table_schema = '" + databaseName + "' AND table_name IN(" + tables + ") ORDER BY table_name";

                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        if( mysqlReader.HasRows ) {
                            while( mysqlReader.Read() ) {
                                table.Add( mysqlReader.GetString( 0 ), new database_table( "", mysqlReader.GetString( 0 ) ) {
                                    db_engine = mysqlReader.GetString( 1 ),
                                    db_collation = mysqlReader.GetString( 2 ),
                                    db_character_set = mysqlReader.GetString( 3 ),
                                    auto_increment = mysqlReader.GetInt32( 4 )
                                } );
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTable" );
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT name FROM sqlite_master";

                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        if( sqliteReader.HasRows ) {
                            while( sqliteReader.Read() ) {
                                table.Add( sqliteReader.GetString( 0 ), new database_table( "", sqliteReader.GetString( 0 ) ) );
                            }
                        }
                        sqliteReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTable" );
                        log.LogWrite( sqliteCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;
            }

            if( getSchemaTableColumn() == false || getSchemaTableConstraint() == false || getSchemaTableUniqueKey() == false || getSchemaTableForeignKey() == false ) {
                return false;
            }

            return true;
        }

        private bool getSchemaTableColumn() {
            updateStatus( this, "Generating Table's Column Info" );

            if( table.Count == 0 ) {
                return true;
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT ta.name, c.name, t.name, COLUMNPROPERTY(c.object_id, c.name, 'charmaxlen') AS max_length, c.is_nullable, c.is_identity FROM sys.columns AS c INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id INNER JOIN sys.tables AS ta ON c.object_id = ta.object_id WHERE ta.name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "') ORDER BY ta.name, c.column_id";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            table[ mssqlReader.GetString( 0 ) ].columns.Add( new database_column() {
                                name = mssqlReader.GetString( 1 ),
                                type = mssqlReader.GetString( 2 ),
                                maxlen = ( mssqlReader.IsDBNull( 3 ) ) ? ( null ) : ( ( Int32? )mssqlReader.GetInt32( 3 ) ),
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, column_name, column_default, is_nullable, column_type, character_set_name, collation_name, extra FROM information_schema.columns WHERE table_schema = '" + databaseName + "' AND table_name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "') ORDER BY table_name, ordinal_position";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            table[ mysqlReader.GetString( 0 ) ].columns.Add( new database_column() {
                                name = mysqlReader.GetString( 1 ),
                                column_default = ( mysqlReader.IsDBNull( 2 ) ) ? ( "NULL" ) : ( mysqlReader.GetString( 2 ) ),
                                type = mysqlReader.GetString( 4 ).Replace( " unsigned", " UNSIGNED" ),
                                is_nullable = ( mysqlReader.GetString( 3 ) == "YES" ) ? ( true ) : ( false ),
                                character_set = (mysqlReader.IsDBNull( 5 )) ? ("") : (mysqlReader.GetString( 5 )),
                                collation_name = ( mysqlReader.IsDBNull( 6 ) ) ? ( "" ) : ( mysqlReader.GetString( 6 ) ),
                                is_identity = ( mysqlReader.GetString( 7 ).ToLower() == "auto_increment" ) ? ( true ) : ( false )
                            } );

                            if( mysqlReader.GetString( 7 ).ToLower() == "auto_increment" ) {
                                table[ mysqlReader.GetString( 0 ) ].is_identity = true;
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getColumn" );
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    foreach( string key in table.Keys.ToArray() ) {
                        sqliteCom.CommandText = "PRAGMA table_info('" + key + "')";
                        try {
                            SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                            while( sqliteReader.Read() ) {
                                Match match = ( new Regex(@"([a-zA-Z]{1,})(\(([0-9]{1,})\))?") ).Match( sqliteReader.GetString( 2 ) );

                                if( match.Success ) {
                                    table[ key ].columns.Add( new database_column() {
                                        name = sqliteReader.GetString( 1 ),
                                        column_default = ( sqliteReader.IsDBNull( 4 ) ) ? ( "NULL" ) : ( sqliteReader.GetString( 4 ) ),
                                        type = match.Groups[ 1 ].Value,
                                        maxlen = ( match.Groups[ 3 ].Value.Length != 0 ) ? ( Convert.ToInt32( match.Groups[ 3 ].Value ) ) : ( 0 ),
                                        is_nullable = ( sqliteReader.GetInt32( 3 ) == 0 ) ? ( true ) : ( false ),
                                        is_identity = ( sqliteReader.GetInt32( 5 ) == 1 ) ? ( true ) : ( false )
                                    } );
                                }
                            }
                            sqliteReader.Close();
                        } catch( Exception ex ) {
                            log.LogWrite( "getColumn" );
                            log.LogWrite( sqliteCom.CommandText );
                            log.LogWrite( ex.Message );

                            updateStatus( this, "Error : " + ex.Message );
                            updateStatus( this, "enableForm" );

                            return false;
                        }
                    }
                }
                break;
            }

            return true;
        }

        private bool getSchemaTableConstraint() {
            updateStatus( this, "Generating Table's Constraint Info" );

            if( table.Count == 0 ) {
                return true;
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT t.name, k.name AS key_name, k.type AS key_type, c.name AS column_name, i.type, ic.is_descending_key, i.is_padded, st.no_recompute, i.ignore_dup_key, i.allow_row_locks, i.allow_page_locks FROM sys.key_constraints AS k INNER JOIN sys.tables AS t ON k.parent_object_id = t.object_id INNER JOIN sys.indexes AS i ON i.object_id = t.object_id AND i.name = k.name INNER JOIN sys.schemas AS s ON k.schema_id = s.schema_id INNER JOIN sys.index_columns ic ON ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id INNER JOIN sys.columns AS c ON ic.column_id = c.column_id AND t.object_id = c.object_id INNER JOIN sys.stats AS st ON i.object_id = st.object_id AND i.index_id = st.stats_id WHERE k.is_ms_shipped = 0 AND t.name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "') ORDER BY t.name, k.type, k.name";
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, non_unique, index_name, column_name FROM information_schema.statistics WHERE table_schema = '" + databaseName + "' AND table_name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "') ORDER BY table_name";
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
                                    is_unique = ( mysqlReader.GetInt32( 1 ) == 1 ) ? ( false ) : ( true )
                                } );

                                pre = mysqlReader.GetString( 0 ) + "|" + mysqlReader.GetString( 2 );
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getConstraint" );
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    try {
                        sqliteCom.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'table' AND name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "')";

                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            Match match = ( new Regex( @"CONSTRAINT ([^\s]{1,}) PRIMARY KEY([\s]{1,})\(([^\s]{1,})\)" ) ).Match( proGEDIA.utilities.StringExtensions.cleanSQL( sqliteReader.GetString( 1 ) ) );

                            if( match.Success ) {
                                table[ sqliteReader.GetString( 0 ) ].constraint.Add( new database_constraint() {
                                    name = match.Groups[ 1 ].Value,                                    
                                    column = new Dictionary<string, bool>() {
                                            { match.Groups[3].Value, true }
                                        }
                                } );
                            }
                        }

                        sqliteReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getConstraint" );
                        log.LogWrite( sqliteCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;
            }

            return true;
        }

        private bool getSchemaTableUniqueKey() {
            updateStatus( this, "Generating Table's Unique Keys" );

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT t.name, c.name AS column_name, i.name, s.name, ic.is_descending_key, i.type, i.is_unique, i.is_padded, st.no_recompute, i.ignore_dup_key, i.allow_row_locks, i.allow_page_locks FROM sys.indexes AS i INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id INNER JOIN sys.tables AS t ON i.object_id = t.object_id INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id INNER JOIN sys.columns AS c ON ic.column_id = c.column_id AND t.object_id = c.object_id INNER JOIN sys.stats AS st ON i.object_id = st.object_id AND i.index_id = st.stats_id WHERE i.is_primary_key = 0 AND i.is_unique_constraint = 0 AND t.name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "') ORDER BY t.name, i.name";
                    try {
                        string pre = string.Empty;

                        SqlDataReader sqlReader = mssqlCom.ExecuteReader();
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "mysql": {

                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    try {
                        sqliteCom.CommandText = "SELECT name, tbl_name, sql FROM sqlite_master WHERE type = 'index' AND name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "')";

                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            if( sqliteReader.IsDBNull( 2 ) == false ) {
                                Match match = ( new Regex( @"CREATE INDEX([\s]{1,})([^\s]{1,})([\s]{1,})ON([\s]{1,})([^\s]{1,})([\s]{1,})\(([^\s]{1,})\)" ) ).Match( proGEDIA.utilities.StringExtensions.cleanSQL( sqliteReader.GetString( 2 ) ) );

                                if( match.Success ) {
                                    table[ sqliteReader.GetString( 1 ) ].uniquekey.Add( new database_uniquekey() {
                                        name = match.Groups[ 2 ].Value,
                                        table = match.Groups[ 5 ].Value,
                                        column = new Dictionary<string, bool>() {
                                            { match.Groups[ 7 ].Value, true }
                                        }
                                    } );
                                }
                            }
                        }

                        sqliteReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getUniqueKey" );
                        log.LogWrite( sqliteCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;
            }

            return true;
        }

        private bool getSchemaTableForeignKey() {
            updateStatus( this, "Generating Table's Unique Keys" );

            if( table.Count == 0 ) {
                return true;
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT pc.name AS parent_tname, sf.name AS key_schema, f.name AS key_name, spc.name AS parent_tschema, sc.name AS parent_cname, spr.name AS reference_tschema, pr.name AS reference_tname, sr.name AS reference_cname FROM sys.foreign_keys f INNER JOIN sys.schemas AS sf ON f.schema_id = sf.schema_id INNER JOIN sys.foreign_key_columns k ON k.constraint_object_id = f.object_id INNER JOIN sys.columns AS sc ON k.parent_object_id = sc.object_id AND k.parent_column_id = sc.column_id INNER JOIN sys.columns AS sr ON k.referenced_object_id = sr.object_id AND k.referenced_column_id = sr.column_id INNER JOIN sys.tables pc ON pc.object_id = f.parent_object_id INNER JOIN sys.tables pr ON pr.object_id = f.referenced_object_id INNER JOIN sys.schemas AS spc ON pc.schema_id = spc.schema_id INNER JOIN sys.schemas AS spr ON pr.schema_id = spr.schema_id WHERE pc.name IN('" + string.Join( "', '", table.Keys.ToArray() ) + "') ORDER BY pc.name, f.name, k.constraint_column_id";

                    try {
                        string pre = string.Empty;

                        SqlDataReader sqlReader = mssqlCom.ExecuteReader();
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "mysql": {
                        MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                        mysqlCom.CommandText = "SELECT t1.ID, t1.FOR_NAME, t1.REF_NAME, t2.FOR_COL_NAME, t2.REF_COL_NAME, t3.UPDATE_RULE, t3.DELETE_RULE FROM INFORMATION_SCHEMA.INNODB_FOREIGN AS t1 INNER JOIN INFORMATION_SCHEMA.INNODB_FOREIGN_COLS AS t2 ON t1.ID = t2.ID INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS t3 ON t1.ID = CONCAT( t3.CONSTRAINT_SCHEMA, '/', t3.CONSTRAINT_NAME) WHERE t1.FOR_NAME IN('" + databaseName + "/" + string.Join( "', '" + databaseName + "/", table.Keys.ToArray() ) + "')";
                        try {
                            MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                            if( mysqlReader.HasRows ) {
                                string tableName = string.Empty;
                                string fkName = string.Empty;
                                string fkReferences = string.Empty;

                                while( mysqlReader.Read() ) {                                    
                                    tableName = mysqlReader.GetString( 1 );
                                    tableName = tableName.Substring( tableName.IndexOf( "/" ) + 1 );

                                    fkName = mysqlReader.GetString( 0 );
                                    fkName = fkName.Substring( fkName.IndexOf( "/" ) + 1 );

                                    fkReferences = mysqlReader.GetString( 2 );
                                    fkReferences = fkReferences.Substring( fkReferences.IndexOf( "/" ) + 1 );

                                    table[ tableName ].foreignkey.Add( new database_foreignkey() {
                                        name = fkName,
                                        column = new List<string>() { mysqlReader.GetString( 3 ) },
                                        ptable = tableName,
                                        rtable = fkReferences,
                                        rcolumn = new List<string>() { mysqlReader.GetString( 4 ) },
                                        on_update = mysqlReader.GetString( 5 ),
                                        on_delete = mysqlReader.GetString( 6 )
                                    } );
                                }
                            }
                            mysqlReader.Close();
                        } catch( Exception ex ) {
                            log.LogWrite( "getView" );
                            log.LogWrite( mysqlCom.CommandText );
                            log.LogWrite( ex.Message );

                            return false;
                        }

                    }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    foreach( string key in table.Keys.ToArray() ) {
                        try {
                            sqliteCom.CommandText = "PRAGMA foreign_key_list('" + key + "')";

                            SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                            while( sqliteReader.Read() ) {
                                table[ key ].foreignkey.Add( new database_foreignkey() {
                                    column = new List<string>() { sqliteReader.GetString( 3 ) },
                                    references = new database_references() {
                                        table = sqliteReader.GetString( 2 ),
                                        column = sqliteReader.GetString( 4 ),
                                        on_update = sqliteReader.GetString( 5 ),
                                        on_delete = sqliteReader.GetString( 6 )
                                    }
                                } );
                            }

                            sqliteReader.Close();
                        } catch( Exception ex ) {
                            log.LogWrite( "getForeignKey" );
                            log.LogWrite( sqliteCom.CommandText );
                            log.LogWrite( ex.Message );

                            updateStatus( this, "Error : " + ex.Message );
                            updateStatus( this, "enableForm" );

                            return false;
                        }
                    }
                }
                break;
            }

            return true;
        }

        private bool getSchemaView( ) {
            string views = string.Empty;
            foreach( backup_settings entry in backupSettings ) {
                if( entry.type == "view" && entry.schema ) {
                    views += "'" + entry.name + "', ";
                }
            }

            if( views.Length == 0 ) {
                return true;
            } else {
                views = proGEDIA.utilities.StringExtensions.Cut( views, 2 );
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name, view_definition, definer, security_type FROM information_schema.views WHERE table_schema = '" + databaseName + "' AND table_name IN(" + views + ")";

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
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    try {
                        sqliteCom.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'view' and name IN(" + views + ")";

                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            view.Add( sqliteReader.GetString( 0 ), new database_view() {
                                name = sqliteReader.GetString( 0 ),
                                schema = sqliteReader.GetString( 1 )
                            });
                        }

                        sqliteReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getView" );
                        log.LogWrite( sqliteCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;
            }

            return true;
        }

        private bool getSchemaFunction( ) {
            string functions = string.Empty;
            foreach( backup_settings entry in backupSettings ) {
                if( entry.type == "function" && entry.schema ) {
                    functions += "'" + entry.name + "', ";
                }
            }

            if( functions.Length == 0 ) {
                return true;
            } else {
                functions = proGEDIA.utilities.StringExtensions.Cut( functions, 2 );
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT t1.specific_name, t1.routine_schema, t1.routine_name, t1.data_type, t1.routine_definition, t1.definer, t1.is_deterministic, t2.parameter_name, t2.data_type FROM information_schema.routines AS t1 LEFT JOIN information_schema.PARAMETERS AS t2 ON t1.routine_schema = t2.specific_schema AND t1.specific_name = t2.specific_name WHERE t1.routine_schema = '" + databaseName + "' AND t1.specific_name IN(" + functions + ") ORDER BY t1.specific_name, t2.ordinal_position";

                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        if( mysqlReader.HasRows ) {
                            while( mysqlReader.Read() ) {
                                if( function.ContainsKey( mysqlReader.GetString( 0 ) ) == false ) {
                                    function.Add( mysqlReader.GetString( 0 ), new database_function() {
                                        name = mysqlReader.GetString( 0 ),
                                        returns = mysqlReader.GetString( 3 ),
                                        schema = mysqlReader.GetString( 4 ),
                                        definer = mysqlReader.GetString( 5 ),
                                        is_deterministic = (mysqlReader.GetString( 6 ) == "YES") ?( true ):( false )
                                    } );
                                }

                                if( mysqlReader.IsDBNull( 7 ) == false ) {
                                    function[ mysqlReader.GetString( 0 ) ].param_list.Add( new param_list( mysqlReader.GetString( 7 ), mysqlReader.GetString( 8 ) ) );
                                }                                
                            }
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getFunction" );
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "sqlite": {

                }
                break;
            }

            return true;
        }

        private bool getSchemaTrigger( ) {
            string triggers = string.Empty;
            foreach( backup_settings entry in backupSettings ) {
                if( entry.type == "trigger" && entry.schema ) {
                    triggers += "'" + entry.name + "', ";
                }
            }

            if( triggers.Length == 0 ) {
                return true;
            } else {
                triggers = proGEDIA.utilities.StringExtensions.Cut( triggers, 2 );
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT trigger_name, event_object_table, action_timing, event_manipulation, action_statement, action_orientation FROM information_schema.triggers WHERE trigger_schema = '" + databaseName + "' AND trigger_name IN(" + triggers + ")";
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
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    try {
                        sqliteCom.CommandText = "SELECT name, tbl_name, sql FROM sqlite_master WHERE type = 'trigger' and name IN(" + triggers + ")";

                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            trigger.Add( sqliteReader.GetString( 0 ), new database_trigger() {
                                name = sqliteReader.GetString( 0 ),
                                table = sqliteReader.GetString( 1 ),
                                schema = sqliteReader.GetString( 2 )
                            } );
                        }

                        sqliteReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTrigger" );
                        log.LogWrite( sqliteCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;
            }

            return true;
        }

        private bool getSchemaProcedure() {
            string functions = string.Empty;
            foreach( backup_settings entry in backupSettings ) {
                if( entry.type == "procedure" && entry.schema ) {
                    functions += "'" + entry.name + "', ";
                }
            }

            if( functions.Length == 0 ) {
                return true;
            } else {
                functions = proGEDIA.utilities.StringExtensions.Cut( functions, 2 );
            }

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT v.name, s.definition FROM sys.procedures AS v INNER JOIN sys.sql_modules AS s ON v.object_id = s.object_id WHERE v.name IN(" + functions + ")";

                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        if( mssqlReader.HasRows ) {
                            while( mssqlReader.Read() ) {
                                procedure.Add( mssqlReader.GetString( 0 ), new database_procedure() {
                                    name = mssqlReader.GetString( 0 ),
                                    schema = mssqlReader.GetString( 1 )
                                } );
                            }
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getProcedures" );
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    /** TODO **/
                }
                break;

                case "sqlite": {
                    /** TODO **/
                }
                break;
            }

            return true;
        }

        private bool writeSchemaTable( database_table entry_table ) {
            string schema = string.Empty;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    updateStatus( this, "Generating Table's Schema ([" + entry_table.schema + "][" + entry_table.name + "])" );

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
                    schema += writeSchemaTableConstraint( entry_table.constraint );
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );

                    schema += "\r\n) ON [PRIMARY]";
                    if( entry_table.is_textimage_on ) {
                        schema += " TEXTIMAGE_ON [PRIMARY]";
                    }
                    schema += ";\r\n\r\n";

                    /**
                     * Unique and Foreign Keys
                    **/
                    schema += writeSchemaTableUniqueKey( entry_table.uniquekey );
                    schema += writeSchemaTableForeignKey( entry_table.foreignkey );
                }
                break;

                case "mysql": {
                    updateStatus( this, "Generating Table's Schema (" + entry_table.name + ")" );

                    schema = "CREATE TABLE " + entry_table.name + " (";

                    /**
                     * Columns
                    */
                    foreach( database_column entry_column in entry_table.columns ) {
                        schema += "\r\n\t" + entry_column.name + " " + entry_column.type + "";

                        if( entry_column.character_set != "" ) {
                            schema += " CHARACTER SET " + entry_column.character_set;
                        }

                        if( entry_column.collation_name != "" ) {
                            schema += " COLLATE " + entry_column.collation_name;
                        }

                        if( entry_column.is_nullable == false ) {
                            schema += " NOT NULL";
                        }

                        if( entry_column.is_nullable == true || entry_column.column_default != "NULL" ) {
                            schema += " DEFAULT ";

                            if( entry_column.column_default == "NULL" ) {
                                { schema += "NULL"; }
                            } else {
                                string type = entry_column.type.ToLower();
                                if( type.IndexOf( " " ) >= 0 ) {
                                    type = type.Substring( 0, type.IndexOf( " " ) );
                                }

                                if( type.IndexOf("(") >= 0 ) {
                                    type = type.Substring( 0, type.IndexOf("(") );
                                }

                                switch( type ) {
                                    case "varchar":
                                    case "char":
                                    case "longtext":
                                    case "mediumtext":
                                    case "tinytext":
                                    case "text":
                                    case "binary":
                                    case "varbinary":
                                    case "tinyblob":
                                    case "blob":
                                    case "mediumblob":
                                    case "longblob":
                                    case "enum":
                                    case "date":
                                    case "datetime":
                                    case "time": { schema += "'" + entry_column.column_default + "'"; } break;


                                    case "real":
                                    case "double":
                                    case "float":
                                    case "decimal":
                                    case "bigint":
                                    case "int":
                                    case "tinyint":
                                    case "smallint":
                                    case "mediumint":
                                    case "boolean":
                                    case "bit":
                                    case "timestamp":
                                    case "year": { schema += "" + entry_column.column_default + ""; } break;

                                    default: { schema += "'#" + entry_column.column_default + "#'"; } break;
                                }
                            }
                        }

                        schema += ",";
                    }

                    /**
                     * Primary and Unique Constraints
                    **/
                    schema += writeSchemaTableConstraint( entry_table.constraint );
                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );
                    schema += "\r\n) ENGINE=" + entry_table.db_engine + " DEFAULT CHARSET=" + entry_table.db_character_set + " COLLATE=" + entry_table.db_collation + ";\r\n\r\n";
                    schema += writeSchemaTableForeignKey( entry_table.foreignkey );
                }
                break;

                case "sqlite": {
                    updateStatus( this, "Generating Table's Schema (" + entry_table.name + ")" );

                    schema = "CREATE TABLE " + entry_table.name + " (";

                    /**
                     * Columns
                    */
                    foreach( database_column entry_column in entry_table.columns ) {
                        schema += "\r\n\t" + entry_column.name + " " + entry_column.type + "";

                        if( entry_column.maxlen != 0 ) {
                            schema += "(" + entry_column.maxlen + ")";
                        }

                        if( entry_column.is_nullable == false ) {
                            schema += " NOT NULL";
                        }

                        schema += ",";
                    }

                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );

                    /**
                     * Primary Constraints
                    **/
                    schema += writeSchemaTableConstraint( entry_table.constraint );

                    /**
                     * Foreign Keys
                    **/
                    schema += writeSchemaTableForeignKey( entry_table.foreignkey );

                    schema = proGEDIA.utilities.StringExtensions.Cut( schema, 1 );
                    schema += "\r\n);\r\n";

                    /**
                     * Unique Constraints
                    **/
                    schema += writeSchemaTableUniqueKey( entry_table.uniquekey );
                    schema += "\r\n";
                }
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private bool writeSchemaTableAutoIncrement( database_table entry_table ) {
            string schema = string.Empty;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    
                }
                break;

                case "mysql": {
                    foreach( database_column entry_column in entry_table.columns ) {
                        if( entry_column.is_identity ) {
                            schema += "ALTER TABLE " + entry_table.name;
                            schema += "\r\n\tMODIFY " + entry_column.name + " " + entry_column.type + "";

                            if( entry_column.collation_name != "" ) {
                                schema += " COLLATE " + entry_column.collation_name;
                            }

                            if( entry_column.is_nullable == false ) {
                                schema += " NOT NULL";
                            }

                            if( entry_column.is_identity ) {
                                schema += " AUTO_INCREMENT, AUTO_INCREMENT = " + entry_table.auto_increment;
                            }

                            schema += ";\r\n\r\n\r\n";

                            break;
                        }
                    }
                }
                break;

                case "sqlite": {

                }
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private string writeSchemaTableConstraint( List<database_constraint> constraint ) {
            string schema = string.Empty;

            if( constraint.Count > 0 ) {
                switch( db.serverType.ToLower() ) {
                    case "mssql": {
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
                    }
                    break;

                    case "mysql": {
                        foreach( database_constraint entry_constraint in constraint ) {
                            if( entry_constraint.name == "PRIMARY" ) {
                                schema += "\r\n\tPRIMARY KEY (";

                                foreach( KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
                                    schema += entry_column.Key + ", ";
                                }

                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                                schema += "),";
                            } else {
                                if( entry_constraint.is_unique ) {
                                    schema += "\r\n\tUNIQUE KEY " + entry_constraint.name + " (";
                                } else {
                                    schema += "\r\n\tKEY " + entry_constraint.name + " (";
                                }

                                foreach( KeyValuePair<string, bool> entry_column in entry_constraint.column ) {
                                    schema += entry_column.Key + ", ";
                                }

                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                                schema += "),";
                            }
                        }
                    }
                    break;

                    case "sqlite": {
                        foreach( database_constraint entry_constraint in constraint ) {
                            schema += "\r\n\tCONSTRAINT " + entry_constraint.name + " PRIMARY KEY (";

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

        private string writeSchemaTableUniqueKey( List<database_uniquekey> uniquekey ) {
            string schema = string.Empty;

            if( uniquekey.Count > 0 ) {
                switch( db.serverType.ToLower() ) {
                    case "mssql": {
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
                    break;

                    case "mysql": {

                    }
                    break;

                    case "sqlite": {
                        foreach( database_uniquekey entry_uniquekey in uniquekey ) {
                            schema += "\r\nCREATE INDEX " + entry_uniquekey.name + " ON " + entry_uniquekey.table + " (";
                            foreach( KeyValuePair<string, bool> entry_column in entry_uniquekey.column ) {
                                schema += entry_column.Key + ", ";
                            }
                            schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                            schema += ");";
                        }

                        if( schema.Length > 0 ) {
                            schema += "\r\n\r\n";
                        }
                    }
                    break;
                }
            }

            return schema;
        }

        private string writeSchemaTableForeignKey( List<database_foreignkey> foreignkey ) {
            string schema = string.Empty;

            if( foreignkey.Count > 0 ) {
                switch( db.serverType.ToLower() ) {
                    case "mssql": {
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
                    break;

                    case "mysql": {
                        schema += "\r\nALTER TABLE " + foreignkey[ 0 ].ptable + "";

                        foreach( database_foreignkey entry_foreignkey in foreignkey ) {
                            schema += "\r\n\tADD CONSTRAINT " + entry_foreignkey.name + " FOREIGN KEY (";
                            foreach( string column in entry_foreignkey.column ) {
                                schema += "" + column + ", ";
                            }
                            schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                            schema += ") REFERENCES " + entry_foreignkey.rtable + " (";
                            foreach( string column in entry_foreignkey.rcolumn ) {
                                schema += "" + column + ", ";
                            }
                            schema = proGEDIA.utilities.StringExtensions.Cut( schema, 2 );
                            schema += ") ON DELETE CASCADE ON UPDATE RESTRICT";
                        }

                        schema += ";\r\n\r\n";
                    }
                    break;

                    case "sqlite": {                       
                        foreach( database_foreignkey entry_foreignkey in foreignkey ) {
                            schema += "\r\n\tFOREIGN KEY (" + entry_foreignkey.column[0] + ") REFERENCES " + entry_foreignkey.references.table +  " (" + entry_foreignkey.references.column + ")";
                            schema += "\r\n\t\tON DELETE " + entry_foreignkey.references.on_delete + " ON UPDATE " + entry_foreignkey.references.on_update + ",";
                        }
                    }
                    break;
                }
            }

            return schema;
        }

        private bool writeTableData( database_table entry_table ) {
            string schema = string.Empty;
            Type[ ] tof;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    updateStatus( this, "Generating Table's Data ([" + entry_table.schema + "][" + entry_table.name + "])" );

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
                                if( j % rowPerInsert == 0 ) {
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
                                            case "String":      { schema += "'" + proGEDIA.utilities.StringExtensions.sqlText( mssqlReader.GetString( i ) ) + "'"; } break;
                                            case "Date":        { schema += "'" + mssqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd" ) + "'"; } break;
                                            case "DateTime":    { schema += "'" + mssqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd HH:mm:ss" ) + "'"; } break;
                                            case "Boolean":     { schema += ( mssqlReader.GetBoolean( i ) ) ? ( "1" ) : ( "0" ); } break;
                                            case "Int16":       { schema += "" + mssqlReader.GetInt16( i ) + ""; } break;
                                            case "Int32":       { schema += "" + mssqlReader.GetInt32( i ) + ""; } break;
                                            case "Double":      { schema += "" + mssqlReader.GetDouble( i ) + ""; } break;
                                            case "Single":      { schema += "'" + mssqlReader.GetFloat( i ) + "'"; } break;
                                            case "Decimal":     { schema += "" + mssqlReader.GetDecimal( i ) + ""; } break;
                                            case "Byte":        { schema += "'" + mssqlReader.GetByte( i ) + "'"; } break;
                                            case "Byte[]":      {
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
                                            }
                                            break;

                                            default:            { schema += "'#" + tof[ i ].Name + "#'"; } break;
                                        }
                                    }
                                }

                                schema += ")";
                                j++;
                                if( j % rowPerInsert == 0 ) {
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
                    } catch( Exception ex ) {
                        log.LogWrite( "getTableData" );
                        log.LogWrite( mssqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "mysql": {
                    updateStatus( this, "Generating Table's Data (" + entry_table.name + "]" );

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
                                if( j % rowPerInsert == 0 ) {
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
                                            case "String":      { schema += "'" + proGEDIA.utilities.StringExtensions.sqlText( mysqlReader.GetString( i ) ) + "'"; } break;
                                            case "DateTime":    { schema += "'" + mysqlReader.GetDateTime( i ).ToString( "yyyy-MM-dd HH:mm:ss" ) + "'"; } break;
                                            case "UInt16":
                                            case "Int16":       { schema += "" + mysqlReader.GetInt16( i ) + ""; } break;
                                            case "UInt32":
                                            case "Int32":       { schema += "" + mysqlReader.GetInt32( i ) + ""; } break;
                                            case "Decimal":     { schema += "" + mysqlReader.GetDecimal( i ) + ""; } break;
                                            case "Double":      { schema += "" + mysqlReader.GetDouble( i ) + ""; } break;
                                            case "SByte":       { schema += "'" + mysqlReader.GetSByte( i ) + "'"; } break;
                                            case "Byte":        { schema += "" + mysqlReader.GetByte( i ) + ""; } break;
                                            case "Boolean":     { schema += ( mysqlReader.GetBoolean( i ) ) ? ( "1" ) : ( "0" ); } break;
                                            default:            { schema += "'#" + tof[ i ].Name + "#'"; } break;
                                        }
                                    }
                                }

                                schema += ")";
                                j++;
                                if( j % rowPerInsert == 0 ) {
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

                            sw.Write( "\r\n" );
                            sw.Flush();
                        }

                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTableData" );
                        log.LogWrite( mysqlCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;

                case "sqlite": {
                    updateStatus( this, "Generating Table's Data (" + entry_table.name + "]" );

                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT * FROM " + entry_table.name + "";

                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();

                        if( sqliteReader.HasRows ) {
                            string columns = string.Empty;

                            sqliteReader.Read();

                            tof = new Type[ sqliteReader.FieldCount ];
                            for( int i = 0; i < sqliteReader.FieldCount; i++ ) {
                                if( i != 0 ) {
                                    columns += ", ";
                                }
                                columns += sqliteReader.GetName( i );

                                tof[ i ] = sqliteReader.GetFieldType( i );
                            }

                            int j = 0;
                            do {
                                if( j % rowPerInsert == 0 ) {
                                    schema = "INSERT INTO " + entry_table.name + " (" + columns + ") VALUES\r\n";
                                }

                                schema += "\t(";

                                for( int i = 0; i < sqliteReader.FieldCount; i++ ) {
                                    if( i != 0 ) {
                                        schema += ", ";
                                    }

                                    if( sqliteReader.IsDBNull( i ) ) {
                                        schema += "NULL";
                                    } else {
                                        switch( tof[ i ].Name ) {
                                            case "String":      { schema += "'" + proGEDIA.utilities.StringExtensions.sqlText( sqliteReader.GetString( i ) ) + "'"; } break;
                                            case "DateTime":    { schema += "'" + sqliteReader.GetDateTime( i ).ToString( "yyyy-MM-dd HH:mm:ss" ) + "'"; } break;
                                            case "UInt16":
                                            case "Int16":       { schema += "" + sqliteReader.GetInt16( i ) + ""; } break;
                                            case "UInt32":
                                            case "Int32":       { schema += "" + sqliteReader.GetInt32( i ) + ""; } break;
                                            case "UInt64":
                                            case "Int64":       { schema += "" + sqliteReader.GetInt64( i ) + ""; } break;
                                            case "Double":      { schema += "" + sqliteReader.GetDouble( i ) + ""; } break;
                                            case "Decimal":     { schema += "" + sqliteReader.GetDecimal( i ) + ""; } break;
                                            case "Byte":        { schema += "" + sqliteReader.GetByte( i ) + ""; } break;
                                            case "Boolean":     { schema += ( sqliteReader.GetBoolean( i ) ) ? ( "1" ) : ( "0" ); } break;
                                            default:            { schema += "'#" + tof[ i ].Name + "#'"; } break;
                                        }
                                    }
                                }

                                schema += ")";
                                j++;
                                if( j % rowPerInsert == 0 ) {
                                    schema += ";\r\n";

                                    sw.Write( schema );
                                    sw.Flush();

                                    schema = string.Empty;
                                } else {
                                    schema += ",\r\n";
                                }
                            } while( sqliteReader.Read() );

                            if( schema.Length > 0 ) {
                                schema = proGEDIA.utilities.StringExtensions.Cut( schema, 3 ) + ";\r\n";

                                sw.Write( schema );
                                sw.Flush();
                            }

                            sw.Write( "\r\n\r\n" );
                            sw.Flush();
                        }

                        sqliteReader.Close();
                    } catch( Exception ex ) {
                        log.LogWrite( "getTableData" );
                        log.LogWrite( sqliteCom.CommandText );
                        log.LogWrite( ex.Message );

                        updateStatus( this, "Error : " + ex.Message );
                        updateStatus( this, "enableForm" );

                        return false;
                    }
                }
                break;
            }

            return true;
        }

        private bool writeSchemaView( database_view entry_view ) {
            string schema = string.Empty;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    schema = entry_view.schema + ";\r\n\r\n";
                }
                break;

                case "mysql": {
                    schema = "CREATE ALGORITHM=UNDEFINED DEFINER=" + entry_view.definer + " SQL SECURITY " + entry_view.security_type + " VIEW " + entry_view.name + " AS " + entry_view.schema + ";\r\n\r\n";
                }
                break;

                case "sqlite": {
                    schema = entry_view.schema + ";\r\n\r\n";
                }
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private bool writeSchemaFunction( database_function entry_function ) {
            string schema = string.Empty;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    schema = entry_function.schema + ";\r\n\r\n";
                }
                break;

                case "mysql": {
                    schema += "DELIMITER $$\r\n";
                    schema += "CREATE DEFINER=" + entry_function.definer + " FUNCTION " + entry_function.name + "(";

                    if( entry_function.param_list.Count > 0 ) {
                        foreach( param_list item in entry_function.param_list ) {
                            schema += item.name + " " + item.type + ", ";
                        }

                        schema = schema.Substring( 0, schema.Length - 2 );
                    }
                    
                    schema += ") RETURNS " + entry_function.returns + "\r\n";
                    
                    if( entry_function.is_deterministic ) {
                        schema += "\tDETERMINISTIC\r\n";
                    }

                    schema += entry_function.schema;
                    schema += "$$\r\n";
                    schema += "DELIMITER;\r\n\r\n";
                }
                break;

                case "sqlite":

                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private bool writeSchemaTrigger( database_trigger entry_trigger ) {
            string schema = string.Empty;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    schema = entry_trigger.schema + ";\r\n\r\n";
                }
                break;

                case "mysql": {
                    schema += "DELIMITER $$\r\n";
                    schema += "CREATE TRIGGER " + entry_trigger.name + " " + entry_trigger.action_timing + " " + entry_trigger.event_manupilation + " ON " + entry_trigger.table + "";
                    schema += "\r\n\tFOR EACH " + entry_trigger.action_orientation + "";
                    schema += "\r\n" + entry_trigger.schema;
                    schema += "\r\n$$";
                    schema += "\r\nDELIMITER;\r\n\r\n";
                }
                break;

                case "sqlite": {
                    schema = entry_trigger.schema + ";\r\n\r\n";
                }
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        private bool writeSchemaProcedure( database_procedure entry_procedure ) {
            string schema = string.Empty;

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    schema = entry_procedure.schema + ";\r\n\r\n";
                }
                break;

                case "mysql": {
                    /** TODO **/
                }
                break;

                case "sqlite": {
                    /** TODO **/
                }
                break;
            }

            sw.Write( schema );
            sw.Flush();

            return true;
        }

        public List<table_entry> getListTable() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT s.name, t.name, object_id FROM sys.tables AS t INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id WHERE t.type = 'U' ORDER BY t.name";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            tableList.Add( new table_entry( mssqlReader.GetString( 0 ), mssqlReader.GetString( 1 ) ) );
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = '" + databaseName + "' ORDER BY table_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                }
                break;

                case "sqlite": {
                    SQLiteCommand sqliteCom = db.sqliteCon.CreateCommand();
                    sqliteCom.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name != 'sqlite_sequence' ORDER BY name";
                    try {
                        SQLiteDataReader sqliteReader = sqliteCom.ExecuteReader();
                        while( sqliteReader.Read() ) {
                            tableList.Add( new table_entry( "", sqliteReader.GetString( 0 ) ) );
                        }
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get table list.\r\n" + ex.Message );
                    }
                }
                break;
            }

            return tableList;
        }

        public List<table_entry> getListView() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT table_name FROM information_schema.views WHERE table_schema = '" + databaseName + "' ORDER BY table_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get view list.\r\n" + ex.Message );
                    }
                }
                break;

                case "sqlite": {
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
                }
                break;
            }

            return tableList;
        }

        public List<table_entry> getListFunction() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT routine_name FROM information_schema.routines WHERE routine_type = 'FUNCTION' AND routine_schema = '" + databaseName + "' ORDER BY routine_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get function list.\r\n" + ex.Message );
                    }
                }
                break;
            }

            return tableList;
        }

        public List<table_entry> getListTrigger() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.serverType.ToLower() ) {
                case "mssql": {
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
                }
                break;

                case "mysql": {
                    MySqlCommand mysqlCom = db.mysqlCon.CreateCommand();
                    mysqlCom.CommandText = "SELECT trigger_name FROM information_schema.triggers WHERE TRIGGER_SCHEMA = '" + databaseName + "' ORDER BY trigger_name";
                    try {
                        MySqlDataReader mysqlReader = mysqlCom.ExecuteReader();
                        while( mysqlReader.Read() ) {
                            tableList.Add( new table_entry( "", mysqlReader.GetString( 0 ) ) );
                        }
                        mysqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get trigger list.\r\n" + ex.Message );
                    }
                }
                break;

                case "sqlite": {
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
                }
                break;
            }

            return tableList;
        }

        public List<table_entry> getListProcedures() {
            List<table_entry> tableList = new List<table_entry>();

            switch( db.serverType.ToLower() ) {
                case "mssql": {
                    List<string> msShipped = new List<string>() {
                        "sp_dropdiagram",
                        "sp_alterdiagram",
                        "sp_renamediagram",
                        "sp_creatediagram",
                        "sp_helpdiagramdefinition",
                        "sp_helpdiagrams",
                        "sp_upgraddiagrams"
                    };

                    SqlCommand mssqlCom = db.mssqlCon.CreateCommand();
                    mssqlCom.CommandText = "SELECT name FROM sys.procedures";
                    try {
                        SqlDataReader mssqlReader = mssqlCom.ExecuteReader();
                        while( mssqlReader.Read() ) {
                            if( msShipped.Contains( mssqlReader.GetString( 0 ) ) == false ) {
                                tableList.Add( new table_entry( "", mssqlReader.GetString( 0 ) ) );
                            }                            
                        }
                        mssqlReader.Close();
                    } catch( Exception ex ) {
                        throw new Exception( "Could not get view list.\r\n" + ex.Message );
                    }
                }
                break;

                case "mysql": {
                    /** TODO **/
                }
                break;

                case "sqlite": {
                    /** TODO **/
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
        public int auto_increment { get; set; }
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
        public string character_set { get; set; }
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

    public class database_procedure {
        public string name { get; set; }
        public string schema { get; set; }

        public database_procedure() {

        }
    }

    public class database_function {
        public string name { get; set; }
        public List<param_list> param_list;
        public string returns { get; set; }
        public string schema { get; set; }
        public string definer { get; set; }
        public bool is_deterministic;

        public database_function() {
            param_list = new List<param_list>();
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
        public string on_update { get; set; }
        public string on_delete { get; set; }

        /**
         * SQLite
         **/
        public database_references references { get; set; }

        public database_foreignkey( ) {

        }
    }

    public class database_references {
        public string table { get; set; }
        public string column { get; set; }
        public string on_update { get; set; }
        public string on_delete { get; set; }
    }

    public class table_entry {
        public string schema { get; set; } = "";
        public string name { get; set; } = "";

        public table_entry( string schema, string name) {
            this.schema = schema;
            this.name = name;
        }
    }

    public class param_list {
        public string name;
        public string type;

        public param_list( string name, string type ) {
            this.name = name;
            this.type = type;
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