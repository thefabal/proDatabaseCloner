using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseUniqueKey {
        public string name;
        public string schema;
        public string table;
        public bool allowPageLocks;
        public bool allowRowLocks;
        public bool clustered;
        public bool dropExisting;
        public bool ignoreDuplicateKey;
        public bool isPadded;
        public bool isUnique;
        public bool staticticsNoreCompute;
        public bool sortInTempDB;
        public bool online;
        public bool optimize_for_sequential_key;

        public Dictionary<string, bool> columns;

        public DatabaseUniqueKey() {
            allowPageLocks = false;
            allowRowLocks = false;
            clustered = false;
            dropExisting = false;
            ignoreDuplicateKey = false;
            isUnique = false;
            isPadded = false;
            online = false;
            staticticsNoreCompute = false;
            sortInTempDB = false;
            online = false;
            optimize_for_sequential_key = false;
        }
    }
}
