using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class Interruption : Exception
    {
        public Interruption(string errorCode) : base("Interpretation interrupted")
        {
            try
            {
                Log.AddRecord("error", errorCode);
            }
            catch(LimitReachedException)
            {
                //NOTHING
            }
        }
    }
}
