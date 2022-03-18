using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class DatabaseFunction {
        public bool isDeterministic;
        public string name;
        public string returns;
        public string schema;
        public string definer;
        public List<FunctionParameterList> parameterList = new List<FunctionParameterList>();

        public DatabaseFunction() {

        }
    }
}
