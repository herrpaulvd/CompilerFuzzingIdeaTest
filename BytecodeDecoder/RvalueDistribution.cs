using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytecodeDecoder
{
    internal class RvalueDistribution
    {
        private const int g1 = 1;
        private const int g2 = 3;
        private const int g3 = 6;
        private const int g4 = 12;
        private const int g5 = 30;
        private const int gconst = 40;
        private const int gvar = 50;

        private readonly SortedDictionary<string, int> distribution = new()
        {
            // basic values
            {"var", gvar},
            {"const", gconst},
            {"new", g3},
            {"cast", g1},
            {"[]", g4},
            {"*u", g3},
            {"&u", g3},
            {"!", g1},
            {"~", g1},
            {"+u", g1},
            {"-u", g1},
            {"+b", g5},
            {"-b", g5},
            {"*b", g2},
            {"/", g2},
            {"%", g2},
            {"==", g2},
            {"!=", g2},
            {"<", g2},
            {">", g2},
            {"<=", g2},
            {">=", g2},
            {"&&", g2},
            {"||", g2},
            {"&b", g1},
            {"|", g1},
            {"^", g1},
            {">>", g1},
            {"<<", g1},
            {"=", g2}
        };

        private List<(int max, string choice)> prefsums = new();

        private void RecalcPrefSums()
        {
            var keys = distribution.Keys;
            int curr = 0;
            foreach (var k in keys)
                prefsums.Add((curr += distribution[k], k));
        }

        private static readonly string[] ptrunallowed =
        {
            "const", "!", "~", "+u", "-u", "*b", "/", "%", "<", ">", "<=", ">=", "==", "!=",
            "&b", "|", "^", "<<", ">>", "&&", "||"
        };

        private static readonly string[] valunallowed =
        {
            "new", "&u"
        };

        private static readonly string[] terminal =
        {
            "var", "const"
        }; // 2

        private static readonly string[] condition =
        {
            "&&", "||", "==", "!=", "<", ">", ">=", "<="
        }; // 8

        public readonly int ConditionBoost;
        public readonly int TerminalBoost;
        public readonly int PtrDepth;

        public RvalueDistribution(
            int conditionBoost,
            int terminalBoost,
            int ptrdepth
            )
        {
            PtrDepth = ptrdepth;
            ConditionBoost = conditionBoost;
            TerminalBoost = terminalBoost;
            foreach (var k in terminal)
                distribution[k] += terminalBoost;
            foreach (var k in condition)
                distribution[k] += conditionBoost;
            foreach (var k in (ptrdepth > 0 ? ptrunallowed : valunallowed))
                distribution.Remove(k);
            RecalcPrefSums();
        }

        public RvalueDistribution Next(int newdepth)
        {
            int nextCond = ConditionBoost / 2;
            int nextTerminal = ConditionBoost == 0 ? (TerminalBoost == 0 ? 1 : TerminalBoost) * 2 : TerminalBoost;
            return new(nextCond, nextTerminal, newdepth);
        }

        public static RvalueDistribution CreateDefault(int ptrdepth)
            => new(0, 0, ptrdepth);

        public static RvalueDistribution CreateConditionBoosted(int ptrdepth)
            => new(32, 0, ptrdepth);

        public string Select(int introll, int intmax)
        {
            double roll = (double)introll / intmax * prefsums[^1].max;
            for (int i = 0; i < prefsums.Count; i++)
                if (roll < prefsums[i].max)
                    return prefsums[i].choice;
            return "var";
        }
    }
}
