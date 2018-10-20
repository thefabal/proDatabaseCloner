using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Globalization;

namespace proGEDIA.utilities {
    public static class Convert {
        public static Double toDouble( string value ) {
            return System.Convert.ToDouble( value, CultureInfo.InvariantCulture );
        }

        public static DateTime toDateTime( string value, bool reserverOrder = false ) {
            string dateformat = string.Empty;
            DateTime dt = new DateTime( 1900, 1, 1 );

            if( value.Length == 6 ) {
                dateformat = "yyMMdd";
            } else if( value.Length == 10 ) {
                if( reserverOrder ) {
                    dateformat = "ddMMyyHHmm";
                } else {
                    dateformat = "yyMMddHHmm";
                }
            } else if( value.Length == 14 ) {
                if( value == "00-00-00,00:00" ) {
                    return dt;
                }
                dateformat = "yy-MM-dd,HH:mm";
            } else if( value.Length == 16 ) {
                if( value == "00.00.0000 00:00" ) {
                    return dt;
                }
                dateformat = "dd.MM.yyyy HH:mm";
            } else if( value.Length == 19 ) {
                dateformat = "yyyy-MM-dd HH:mm:ss";
            }

            try {
                if( DateTime.TryParseExact( value, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt ) == false ) {
                    return new DateTime( 1, 1, 1 );
                }
            } catch {

            }

            return dt;
        }

        public static DateTime toDate( string value ) {
            string dateformat = string.Empty;
            DateTime dt = new DateTime( 1900, 1, 1 );

            if( value.Length == 6 ) {
                dateformat = "ddMMyy";
            } else if( value.Length == 8 ) {
                dateformat = "yy-MM-dd";
            } else if( value.Length == 10 ) {
                dateformat = "dd.MM.yyyy";
            }

            try {
                if( DateTime.TryParseExact( value, dateformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt ) == false ) {
                    return new DateTime( 1, 1, 1 );
                }
            } catch {

            }

            return dt;
        }

        public static TimeSpan toTime( string value ) {
            string dateformat = string.Empty;
            TimeSpan ts = new TimeSpan();

            if( value.Length == 6 ) {
                dateformat = "hhmmss";
            } else if( value.Length == 8 ) {
                dateformat = "hh\\:mm\\:ss";
            }

            try {
                TimeSpan.TryParseExact( value, dateformat, CultureInfo.InvariantCulture, out ts );
            } catch {

            }

            return ts;
        }
    }

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

        public SqlConnection mssqlCon;
        public MySqlConnection mysqlCon;

        public database( ) {

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

        public string getConnectionString( ) {
            string connectionString = string.Empty;
            switch( server_type.ToLower() ) {
                case "mssql":
                // MultipleActiveResultSets=true
                if( authentication == 0 )
                    connectionString = "Data Source=" + server_name + ";Integrated Security=SSPI;";
                else
                    connectionString = "Data Source=" + server_name + ";User id=" + user_name + ";Password=" + user_pass + ";";
                break;

                case "mysql":
                connectionString = "Server=" + server_name + ";";
                if( server_port.Length != 0 ) {
                    connectionString += "Port=" + server_port + ";";
                }
                // connectionString += "Database=myDataBase;";
                if( user_name.Length != 0 || user_pass.Length != 0 ) {
                    connectionString += "Uid=" + user_name + ";Pwd=" + user_pass + ";";
                }
                connectionString += "SslMode=none";
                break;
            }

            return connectionString;
        }

        public bool Compare( string server_type, string server_name, string user_name ) {
            if( this.server_type == server_type && this.service_name == service_name && this.user_name == user_name ) {
                return true;
            } else {
                return false;
            }
        }

        public bool Compare( database set ) {
            if( server_type == set.server_type && service_name == set.service_name && user_name == set.user_name ) {
                return true;
            } else {
                return false;
            }
        }

        public override string ToString( ) {
            return this.server_name;
        }
    }
}
