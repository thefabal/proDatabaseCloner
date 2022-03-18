using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseColumn {
        public bool isNullable;
        public bool isIdentity;
        public Int32? maxLength;
        public string name;
        public string type;
        public string characterSet;
        public string collationName;
        public string columnDefault;

        public DatabaseColumn() {

        }

        public DatabaseColumn( string name, string type, Int32? maxLength, bool isNullable, bool isIdentity ) {
            this.name = name;
            this.type = type;
            this.maxLength = maxLength;
            this.isNullable = isNullable;
            this.isIdentity = isIdentity;
        }
    }
}