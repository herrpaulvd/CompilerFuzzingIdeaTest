using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    public class VMException : Exception
    {
        public VMException(string exception) : base("An exception throwed")
        {
            try
            {
                Log.AddRecord("exception", exception);
            }
            catch(LimitReachedException)
            {
                //NOTHING
            }
        }
    }
}
