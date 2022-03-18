using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCloner {
    public class FunctionParameterList {
        public string name;
        public string type;

        public FunctionParameterList( string name, string type ) {
            this.name = name;
            this.type = type;
        }
    }
}
