using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseConstraint {
        public string name;
        public string type;
        public Dictionary<string, bool> column;
        public bool allowPageLocks;
        public bool allowRowLocks;
        public bool clustered;
        public bool ignoreDuplicateKey;
        public bool isDescendingKey;
        public bool isPadded;
        public bool isUnique;
        public bool staticticsNoreCompute;

        public DatabaseConstraint() {
            clustered = false;
            isDescendingKey = false;
            isPadded = false;
            staticticsNoreCompute = false;
            ignoreDuplicateKey = false;
            allowRowLocks = false;
            allowPageLocks = false;
            isUnique = false;
        }
    }
}
