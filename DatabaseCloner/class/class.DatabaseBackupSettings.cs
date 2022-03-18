using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseBackupSettings {
        public bool backupData;
        public bool backupSchema;
        public string name;
        public string schemaName;
        public string type;

        public DatabaseBackupSettings( string type, string schemaName, string name, bool backupSchema, bool backupData ) {
            this.name = name;
            this.schemaName = schemaName;
            this.type = type;
            this.backupData = backupData;
            this.backupSchema = backupSchema;
        }
    }
}
