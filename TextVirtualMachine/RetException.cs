using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class RetException : Exception
    {
        public readonly bool Correct;

        public RetException(bool correct) : base(correct ? "Executed RET" : "Reached end of function")
        {
            Correct = correct;
        }
    }
}
