using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestedCompiler
{
    internal class TypeExpression
    {
        public string BaseTypeName { get; }
        public int PointerDepth { get; }

        public bool IsPointer => PointerDepth > 0;
        public int Size => Options.Size[IsPointer ? "long" : BaseTypeName];

        public TypeExpression(string baseTypeName, int pointerDepth = 0)
        {
            BaseTypeName = baseTypeName;
            PointerDepth = pointerDepth;
        }

        public TypeExpression MakePointer() => new(BaseTypeName, PointerDepth + 1);
        public TypeExpression Dereference() => 
            PointerDepth > 0
            ? new(BaseTypeName, PointerDepth - 1)
            : throw new ArgumentException("this");

        public string VMType => IsPointer ? "ptr" : BaseTypeName;

        public bool IsAssignableTo(TypeExpression other)
        {
            if(IsPointer)
                return PointerDepth == other.PointerDepth;
            return !other.IsPointer && Size <= other.Size;
        }
    }
}
