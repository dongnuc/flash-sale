using System.Reflection;

namespace Movie_StructureCode.Domain
{
    public class AssemblyReference
    {
        public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
    }
}
