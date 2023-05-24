using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytecodeDecoder
{
    public class BadPredefException : Exception
    {
        public BadPredefException() : base("Predefined variables should contain at least one long var and one long* var") { }
    }
}
