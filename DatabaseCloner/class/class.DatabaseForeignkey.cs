using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseForeignkey {
        public string name;
        public string schema;
        public string pschema;
        public string ptable;
        public string rschema;
        public string rtable;
        public string onUpdate;
        public string onDelete;

        public List<string> columns;
        public List<string> rcolumns;

        /**
         * SQLite
         **/
        public DatabaseReferences references;

        public DatabaseForeignkey() {

        }
    }
}
