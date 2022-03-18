using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseTableEntry {
        public string schema;
        public string name;

        public DatabaseTableEntry( string schema, string name ) {
            this.schema = schema;
            this.name = name;
        }
    }
}
