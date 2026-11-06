using System.Reflection;

namespace Movie_StructureCode.Infracstructure
{
    public class AssemblyReference
    {
        public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
    }
}
