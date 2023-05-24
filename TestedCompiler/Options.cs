using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal static class Options
    {
        public static string[] BaseTypes =
        {
            "byte",
            "short",
            "int",
            "long"
        };

        public static Dictionary<string, int> Size = new()
        {
            {"byte", 1 },
            {"short", 2 },
            {"int", 4 },
            {"long", 8 },
            {"ptr", 8 }
        };

        public static string[] VMTypes =
        {
            "byte",
            "short",
            "int",
            "long",
            "ptr"
        };
    }
}
