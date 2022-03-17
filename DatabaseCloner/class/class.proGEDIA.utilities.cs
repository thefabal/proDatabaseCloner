using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Globalization;

using System.Data.SqlClient;
using System.Data.SQLite;
using MySql.Data.MySqlClient;

using System.Text.RegularExpressions;

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
            } else if( str.Length < length ) {
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
            if( string.IsNullOrEmpty( str ) ) {
                str = string.Empty;
            } else if( str.Length > length ) {
                str = str.Substring( 0, str.Length - length );
            }

            return str;
        }

        public static string sqlText( this string str ) {
            return str.Replace( "'", "\\'" ).Replace("\r", "\\r").Replace("\n","\\n");
        }

        public static string ByteArrayToString( byte[ ] ba ) {
            StringBuilder hex = new StringBuilder( ba.Length * 2 );

            foreach( byte b in ba ) {
                hex.AppendFormat( "{0:x2}", b );
            }

            return hex.ToString().ToUpper();
        }

        public static string cleanSQL( string SQL ) {
            return Regex.Replace( SQL.Replace( "[", "" ).Replace( "]", "" ).Replace( "\r", " " ).Replace( "\n", " " ), @"([\s]{2,})", " " );
        }
    }

    public static class encryption {
        public static string EncryptPassword( string password ) {
            byte[ ] data = Encoding.UTF8.GetBytes( password );
            byte[ ] encrypted_data = ProtectedData.Protect( data, null, DataProtectionScope.CurrentUser );

            return System.Convert.ToBase64String( encrypted_data );
        }

        public static string DecryptPassword( string password ) {
            if( password.Length > 0 ) {
                byte[ ] encrypted_data = System.Convert.FromBase64String( password );
                byte[ ] data = ProtectedData.Unprotect( encrypted_data, null, DataProtectionScope.CurrentUser );

                return Encoding.UTF8.GetString( data );
            }

            return "";
        }
    }

    public class LogWriter {
        private readonly string log_path = string.Empty;
        private readonly string prefix = string.Empty;

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
        public string serverType;
        public string serverName;
        public string serverPort;
        public string serviceName;
        public string databaseName;
        public string databaseFile;
        public string userName;
        public string userPass;
        public bool userAuth;
        public bool rememberPassword;

        public SqlConnection mssqlCon;
        public MySqlConnection mysqlCon;
        public SQLiteConnection sqliteCon;

        public database( ) {
            userAuth = false;
            rememberPassword = true;
        }

        public void Set( string serverType, string serverName, string serverPort, string serviceName, string databaseName, string databaseFile, bool userAuth, string userName, string userPass, bool rememberPassword ) {
            this.serverType = serverType;
            this.serverName = serverName;
            this.serverPort = serverPort;
            this.serviceName = serviceName;
            this.databaseName = databaseName;
            this.databaseFile = databaseFile;
            this.userAuth = userAuth;
            this.userName = userName;
            this.userPass = userPass;
            this.rememberPassword = rememberPassword;
        }

        public string GetConnectionString( ) {
            string connectionString = string.Empty;
            switch( serverType.ToLower() ) {
                case "mssql":
                    if( userAuth == false )
                        connectionString = "Data Source=" + serverName + ";Integrated Security=SSPI;";
                    else
                        connectionString = "Data Source=" + serverName + ";User id=" + userName + ";Password=" + userPass + ";";
                break;

                case "mysql":
                    connectionString = "Server=" + serverName + ";";
                    if( serverPort.Length != 0 ) {
                        connectionString += "Port=" + serverPort + ";";
                    }
                    
                    if( userName.Length != 0 || userPass.Length != 0 ) {
                        connectionString += "Uid=" + userName + ";Pwd=" + userPass + ";";
                    }
                    connectionString += "SslMode=none";
                break;

                case "sqlite":
                    connectionString = "Data Source=" + databaseFile + ";Version=3;";
                break;
            }

            return connectionString;
        }

        public bool Compare( string serverType, string serverName, string databaseFile, string userName ) {
            if( this.serverType == serverType && this.serverName == serverName && this.databaseFile == databaseFile && this.userName == userName ) {
                return true;
            } else {
                return false;
            }
        }

        public bool Compare( database set ) {
            if( serverType == set.serverType && serviceName == set.serviceName && databaseFile == set.databaseFile && userName == set.userName ) {
                return true;
            } else {
                return false;
            }
        }

        public override string ToString( ) {
            return this.serverName;
        }
    }
}
