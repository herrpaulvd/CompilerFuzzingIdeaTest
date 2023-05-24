using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytecodeDecoder
{
    internal class StringTree
    {
        private readonly List<object> children = new();

        public void Add(params object[] s)
        {
            foreach(var e in s)
                children.Add(e);
        }

        private void Release(StringBuilder result)
        {
            foreach (var e in children)
                if (e is StringTree st)
                    st.Release(result);
                else
                    result.Append(e);
        }

        public override string ToString()
        {
            StringBuilder result = new();
            Release(result);
            return result.ToString();
        }
    }
}
