using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BytecodeDecoder
{
    public struct PredefinedVariable
    {
        public string Name;
        public int Depth; // long*{count}
        public string InitExpression;

        public PredefinedVariable(string name, int depth, string initExpression)
        {
            Name = name;
            Depth = depth;
            InitExpression = initExpression;
        }
    }

    public static class Decoder
    {
        private const string tlong = "long";

        private static int Take(ref byte b, int count)
        {
            int mask = (1 << count) - 1;
            int result = b & mask;
            b >>= count;
            return result;
        }

        private static PredefinedVariable[] SelectPredefs(IEnumerable<PredefinedVariable> predef, Func<int, bool> depthCondition)
            => predef.Where(pd => depthCondition(pd.Depth)).ToArray();

        private static PredefinedVariable[] SelectLowering(IEnumerable<PredefinedVariable> predef, int depth)
        {
            PredefinedVariable[] result;
            do
            {
                result = SelectPredefs(predef, d => d == depth);
                depth--;
            } while(result.Length == 0);
            return result;
        }

        private static object ReadStatement(BinStream bs, List<PredefinedVariable> predef)
        {
            const int SIG_IF = 0;
            const int SIG_WHILE = 1;
            const int SIG_IF_ELSE = 2;
            const int SIG_ASSIGN = 3;

            object readST(int count)
            {
                StringTree r = new();
                for(int i = 0; i < count; i++)
                    r.Add(ReadStatement(bs, predef));
                return r;
            }

            object readRV(int ptrdepth, bool conditionBoosted)
            {
                return ReadRvalue(bs, ptrdepth, predef,
                    conditionBoosted ? RvalueDistribution.CreateConditionBoosted(ptrdepth) : RvalueDistribution.CreateDefault(ptrdepth));
            }

            object readLV(int ptrdepth)
            {
                return ReadLvalue(bs, ptrdepth, predef);
            }

            bool forceAssign = bs.Randomized;
            byte b = bs.GetNext();
            if (forceAssign) b |= SIG_ASSIGN;

            StringTree result = new();
            int maincount;
            int extracount;
            int permutation;
            object a1res = "";
            object a2res = "";
            object a3res = "";
            PredefinedVariable[] ok;

            switch (Take(ref b, 2))
            {
                case SIG_IF:
                    maincount = Take(ref b, 2) + 1;
                    permutation = Take(ref b, 1);
                    extracount = Take(ref b, 3);
                    if (extracount == 0b111)
                    {
                        ok = SelectPredefs(predef, d => d > 0);
                        int index = bs.GetNext() % ok.Length;
                        result.Add("delete ", ok[index].Name, ";\n");
                        break;
                    }
                    bs.Feed(extracount, 0b111);
                    if (permutation == 0)
                    {
                        a1res = readRV(0, true);
                        a2res = readST(maincount);
                    }
                    else
                    {
                        a2res = readST(maincount);
                        a1res = readRV(0, true);
                    }
                    result.Add(
                        "if(", a1res, ")\n{\n", 
                        a2res, 
                        "}\n");
                    break;
                case SIG_WHILE:
                    maincount = Take(ref b, 2) + 1;
                    permutation = Take(ref b, 1);
                    extracount = Take(ref b, 3);
                    bs.Feed(extracount, 0b111);
                    if (permutation == 0)
                    {
                        a1res = readRV(0, true);
                        a2res = readST(maincount);
                    }
                    else
                    {
                        a2res = readST(maincount);
                        a1res = readRV(0, true);
                    }
                    result.Add(
                        "while(", a1res, ")\n{\n", 
                        a2res, 
                        "}\n");
                    break;
                case SIG_IF_ELSE:
                    maincount = Take(ref b, 2) + 1;
                    extracount = Take(ref b, 2) + 1;
                    permutation = Take(ref b, 2);

                    switch (permutation)
                    {
                        case 0:
                            a1res = readRV(0, true);
                            a2res = readST(maincount);
                            a3res = readST(extracount);
                            break;
                        case 1:
                            a1res = readRV(0, true);
                            a3res = readST(extracount);
                            a2res = readST(maincount);
                            break;
                        case 2:
                            a2res = readST(maincount);
                            a3res = readST(extracount);
                            a1res = readRV(0, true);
                            break;
                        case 3:
                            a3res = readST(extracount);
                            a2res = readST(maincount);
                            a1res = readRV(0, true);
                            break;
                    }

                    result.Add(
                        "if(", a1res, ")\n{\n", 
                        a2res, 
                        "}\n", 
                        "else\n{\n", 
                        a3res, 
                        "}\n");
                    break;
                case SIG_ASSIGN:
                    const int maxdepth = 5;
                    permutation = Take(ref b, 1);
                    maincount = Take(ref b, maxdepth);
                    int currdepth = 0;
                    for (int i = 0; i < maxdepth; i++)
                        if ((maincount & (1 << i)) != 0)
                            currdepth++;
                        else
                            break;

                    ok = SelectLowering(predef, currdepth);
                    currdepth = ok[0].Depth;

                    if (permutation == 0)
                    {
                        a1res = readLV(currdepth);
                        a2res = readRV(currdepth, false);
                    }
                    else
                    {
                        a2res = readRV(currdepth, false);
                        a1res = readLV(currdepth);
                    }

                    result.Add("(", a1res, ") = (", a2res, ");\n");
                    break;
            }

            return result;
        }

        private static object Default(BinStream bs, int ptrdepth)
        {
            if(ptrdepth == 0)
            {
                const string result = $"(new {tlong}[1])[0]";
                return result;
            }
            else
            {
                StringTree result = new();
                result.Add("(new ", tlong);
                for (int i = 0; i < ptrdepth; i++)
                    result.Add("*");
                result.Add("[", bs.GetNext(), "])[0]");
                return result;
            }
        }

        private static object ReadLvalue(BinStream bs, int ptrdepth, List<PredefinedVariable> predef)
        {
            const int SIG_VAR = 0;
            const int SIG_INDEX = 1;
            const int SIG_DEREF = 2;
            
            int decision;
            bool forceVar = bs.Randomized;
            byte b = bs.GetNext();
            int bytevar = Take(ref b, 1);
            int byteindex = Take(ref b, 1);
            int bytederef = Take(ref b, 1);
            if (forceVar || bytevar == 1)
                decision = SIG_VAR;
            else if (byteindex == 1)
                decision = SIG_INDEX;
            else if (bytederef == 1)
                decision = SIG_DEREF;
            else
                decision = SIG_VAR;

            StringTree result;

            RvalueDistribution makedist(int newdepth) => RvalueDistribution.CreateDefault(newdepth);

            switch (decision)
            {
                case SIG_VAR:
                    var vars = SelectPredefs(predef, d => d == ptrdepth);
                    if (vars.Length == 0)
                        return Default(bs, ptrdepth);
                    int index = Take(ref b, 5) % vars.Length;
                    return vars[index].Name;
                case SIG_INDEX:
                    int permutation = Take(ref b, 1);
                    bs.Feed(b, 0b1111);
                    object arrayres;
                    object indexres;
                    if(permutation == 0)
                    {
                        arrayres = ReadRvalue(bs, ptrdepth + 1, predef, makedist(ptrdepth + 1));
                        indexres = ReadRvalue(bs, 0, predef, makedist(0));
                    }
                    else
                    {
                        indexres = ReadRvalue(bs, 0, predef, makedist(0));
                        arrayres = ReadRvalue(bs, ptrdepth + 1, predef, makedist(ptrdepth + 1));
                    }
                    result = new();
                    result.Add("(", arrayres, ")[", indexres, "]");
                    return result;
                default:
                    bs.Feed(b, 0b11111);
                    result = new();
                    result.Add("*(", ReadRvalue(bs, ptrdepth + 1, predef, makedist(ptrdepth + 1)), ")");
                    return result;
            }
        }

        private static object ReadRvalue(
            BinStream bs, 
            int ptrdepth, 
            List<PredefinedVariable> predef,
            RvalueDistribution distribution)
        {
            bool forceLeaf = bs.Randomized;
            //11:5
            byte major = bs.GetNext();
            byte minor = bs.GetNext();
            
            if(forceLeaf)
            {
                if((major & 1) == 0)
                {
                    var ok = SelectPredefs(predef, d => d == ptrdepth);
                    if (ok.Length == 0)
                    {
                        if (ptrdepth == 0)
                            return minor;
                        else
                            return Default(bs, ptrdepth);
                    }
                    else
                        return ok[minor % ok.Length].Name;
                }
                else
                {
                    if(ptrdepth == 0)
                        return minor;
                    else
                        return Default(bs, ptrdepth);
                }
            }
            else
            {
                const int MAX_PROB = 1 << 11;
                int probabilty = major | (Take(ref minor, 3) << 8);
                var decision = distribution.Select(probabilty, MAX_PROB);

                StringTree doUnary(string sign, bool lvalue, int newdepth)
                {
                    StringTree result = new();
                    result.Add(
                        sign, "(",
                        lvalue 
                        ? ReadLvalue(bs, newdepth, predef)
                        : ReadRvalue(bs, newdepth, predef, distribution.Next(newdepth)),
                        ")");
                    return result;
                }

                StringTree doBinary(string sign, bool leftlvalue, int ldepth, int rdepth, byte minor)
                {
                    object leftres, rightres;

                    if((minor & 1) == 0)
                    {
                        leftres = leftlvalue
                        ? ReadLvalue(bs, ldepth, predef)
                        : ReadRvalue(bs, ldepth, predef, distribution.Next(ldepth));
                        rightres = ReadRvalue(bs, rdepth, predef, distribution.Next(rdepth));
                    }
                    else
                    {
                        rightres = ReadRvalue(bs, rdepth, predef, distribution.Next(rdepth));
                        leftres = leftlvalue
                        ? ReadLvalue(bs, ldepth, predef)
                        : ReadRvalue(bs, ldepth, predef, distribution.Next(ldepth));
                    }

                    StringTree result = new();
                    result.Add("(", leftres, ") ", sign, " (", rightres, ")");
                    return result;
                }

                int minor2depth()
                {
                    int result = 0;
                    for (int i = 0; i < 5; i++)
                        if ((minor & (1 << i)) != 0)
                            result++;
                        else
                            break;
                    return result;
                }

                StringTree localres;
                switch(decision)
                {
                    case "var":
                        var ok = SelectPredefs(predef, d => d == ptrdepth);
                        if (ok.Length == 0)
                        {
                            if (ptrdepth == 0)
                                return minor;
                            else
                                return Default(bs, ptrdepth);
                        }
                        else
                            return ok[minor % ok.Length].Name;
                    case "const":
                        int depth = minor2depth();
                        if(depth == 0)
                        {
                            minor >>= 1;
                            int d2 = minor2depth();
                            if (d2 == 0) return 0;
                            return 1L << (d2 - 1);
                        }
                        int bytecount = 1 << (depth - 1);
                        long result = 0;
                        for (int i = 0; i < bytecount; i++)
                            result = (result << 8) + bs.GetNext();
                        return result;
                    case "new":
                        localres = new();
                        localres.Add("new ", tlong);
                        for (int i = 1; i < ptrdepth; i++)
                            localres.Add("*");
                        localres.Add(
                            "[",
                            ReadRvalue(bs, 0, predef, distribution.Next(0)),
                            "]");
                        return localres;
                    case "cast":
                        localres = new();
                        localres.Add("<", tlong);
                        for (int i = 0; i < ptrdepth; i++)
                            localres.Add("*");
                        localres.Add(">(");
                        int newdepth = minor2depth();
                        localres.Add(
                            ReadRvalue(bs, newdepth, predef, distribution.Next(newdepth)),
                            ")");
                        return localres;
                    case "[]":
                        localres = new();
                        object arrayres;
                        object indexres;
                        if((minor & 1) == 0)
                        {
                            arrayres = ReadRvalue(bs, ptrdepth + 1, predef, distribution.Next(ptrdepth + 1));
                            indexres = ReadRvalue(bs, 0, predef, distribution.Next(0));
                        }
                        else
                        {
                            indexres = ReadRvalue(bs, 0, predef, distribution.Next(0));
                            arrayres = ReadRvalue(bs, ptrdepth + 1, predef, distribution.Next(ptrdepth + 1));
                        }
                        localres.Add("(", arrayres, ")[", indexres, "]");
                        return localres;
                    case "*u":
                        return doUnary("*", false, ptrdepth + 1);
                    case "&u":
                        return doUnary("&", true, ptrdepth - 1);
                    case "!":
                        return doUnary("!", false, ptrdepth);
                    case "~":
                        return doUnary("~", false, ptrdepth);
                    case "+u":
                        return doUnary("+", false, ptrdepth);
                    case "-u":
                        return doUnary("-", false, ptrdepth);
                    case "+b":
                        return doBinary("+", false, ptrdepth, 0, minor);
                    case "-b":
                        return doBinary("-", false, ptrdepth, 0, minor);
                    case "*b":
                        return doBinary("*", false, ptrdepth, ptrdepth, minor);
                    case "/":
                        return doBinary("/", false, ptrdepth, ptrdepth, minor);
                    case "%":
                        return doBinary("%", false, ptrdepth, ptrdepth, minor);
                    case "==":
                        int opdepth = minor2depth();
                        return doBinary("==", false, opdepth, opdepth, minor);
                    case "!=":
                        opdepth = minor2depth();
                        return doBinary("!=", false, opdepth, opdepth, minor);
                    case "<":
                        return doBinary("<", false, ptrdepth, ptrdepth, minor);
                    case ">":
                        return doBinary(">", false, ptrdepth, ptrdepth, minor);
                    case "<=":
                        return doBinary("<=", false, ptrdepth, ptrdepth, minor);
                    case ">=":
                        return doBinary(">=", false, ptrdepth, ptrdepth, minor);
                    case "&&":
                        return doBinary("&&", false, ptrdepth, ptrdepth, minor);
                    case "||":
                        return doBinary("||", false, ptrdepth, ptrdepth, minor);
                    case "&b":
                        return doBinary("&", false, ptrdepth, ptrdepth, minor);
                    case "|":
                        return doBinary("|", false, ptrdepth, ptrdepth, minor);
                    case "^":
                        return doBinary("^", false, ptrdepth, ptrdepth, minor);
                    case "<<":
                        return doBinary("<<", false, ptrdepth, ptrdepth, minor);
                    case ">>":
                        return doBinary(">>", false, ptrdepth, ptrdepth, minor);
                    case "=":
                        return doBinary("=", true, ptrdepth, ptrdepth, minor);
                    default:
                        throw new Exception("Internal error");
                }
            }
        }

        public static string Decode(byte[] input, List<PredefinedVariable> predef)
        {
            if (!predef.Any(pv => pv.Depth == 0) || !predef.Any(pv => pv.Depth == 1))
                throw new BadPredefException();
            BinStream bs = new(input);
            StringTree result = new();
            result.Add("void main()\n{\n");
            foreach(var v in predef)
            {
                result.Add("var ", tlong);
                for (int i = 0; i < v.Depth; i++)
                    result.Add("*");
                result.Add(" ", v.Name, " = ", v.InitExpression, ";\n");
            }
            result.Add(ReadStatement(bs, predef), "}\n");
            return result.ToString() ?? "";
        }
    }
}
