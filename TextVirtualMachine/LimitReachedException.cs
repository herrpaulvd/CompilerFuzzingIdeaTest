using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    public class LimitReachedException : Exception
    {
        public LimitReachedException() 
            : base("Limit of records reached") { }
    }
}
