using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseTrigger {
        public string name;
        public string table;
        public string actionTiming;
        public string eventManupilation;
        public string schema;
        public string actionOrientation;

        public DatabaseTrigger() {

        }
    }
}
