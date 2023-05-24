using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextVirtualMachine
{
    internal class Log
    {
        private static List<string[]> records = new();
        private static int RecordsLimit;

        public static void Initialize(int reclimit)
        {
            records.Clear();
            RecordsLimit = reclimit;
        }

        public static void AddRecord(params string[] record)
        {
            if (RecordsLimit == 0)
            {
                records.Add(new string[] { "limit" });
                throw new LimitReachedException();
            }
            if (RecordsLimit > 0)
                RecordsLimit--;
            records.Add(record);
        }

        public static string Release()
        {
            return string.Join('\n', records.Select(r => string.Join(' ', r)));
        }
    }
}
