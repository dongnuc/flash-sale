using System.Reflection;

namespace Movie_StructureCode.Contract
{
    public class AssemblyReference
    {
        public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
    }
}
