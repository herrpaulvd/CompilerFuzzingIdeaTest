using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicAnalyzer
{
    internal class Frame
    {
        private readonly SortedDictionary<string, int> sizes = new();
        private readonly SortedDictionary<string, long> values = new();
        private readonly SortedDictionary<string, bool> corrupted = new();

        public Frame(VariableDeclaration[] variables)
        {
            foreach (var v in variables)
            {
                sizes.Add(v.Name, v.Size);
                values.Add(v.Name, 0);
                corrupted.Add(v.Name, false);
            }
        }

        private static long GetMask(int size)
        {
            return (1L << (size * 8)) - 1;
        }

        private static bool IsNegative(long value, int size)
        {
            return (value & (1L << (size * 8 - 1))) != 0;
        }

        public long GetValue(string name)
        {
            int size = sizes[name];
            long value = values[name];
            if(IsNegative(value, size)) value |= ((-1) ^ GetMask(size));
            return value;
        }

        public int GetSize(string name) => sizes[name];

        // return true, if the variable becomes corrupted
        public void SetValue(string name, long value)
        {
            values[name] = value & GetMask(sizes[name]);
        }

        private void SetCritical(out bool isCritical, string name)
        {
            isCritical = name.StartsWith("v") && !name.StartsWith("v_tmp");
        }

        public bool Exists(string variable) => values.ContainsKey(variable);

        public bool IsCorrupted(out bool isCritical, string name)
        {
            SetCritical(out isCritical, name);
            return corrupted[name];
        }

        public void MarkCorrupted(out bool isCritical, string name)
        {
            SetCritical(out isCritical, name);
            corrupted[name] = true;
        }

        public bool InheritCorruption(out bool isCritical, string result, IEnumerable<string> operands)
        {
            SetCritical(out isCritical, result);
            return corrupted[result] = operands.Any(v => corrupted[v]);
        }
    }
}
