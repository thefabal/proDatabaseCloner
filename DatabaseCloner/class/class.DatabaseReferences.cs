using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseReferences {
        public string table;
        public string column;
        public string onUpdate;
        public string onDelete;
    }
}