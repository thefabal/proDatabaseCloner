using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseTable {
        public string schema;
        public string name;

        /* MsSQL */
        public bool isTextimageOn;
        public bool isIdentity;

        /* MySQL */
        public int autoIncrement;
        public string dbEngine;
        public string dbCollation;
        public string dbCharacterSet;

        public List<DatabaseColumn> columns = new List<DatabaseColumn>();
        public List<DatabaseConstraint> constraints = new List<DatabaseConstraint>();
        public List<DatabaseUniqueKey> uniqueKeys = new List<DatabaseUniqueKey>();
        public List<DatabaseForeignkey> foreignKeys = new List<DatabaseForeignkey>();

        public DatabaseTable( string schema, string name ) {
            this.schema = schema;
            this.name = name;
        }
    }
}
