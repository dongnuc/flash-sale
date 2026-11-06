using System.Security.Cryptography.X509Certificates;

namespace Movie_StructureCode.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public string Title { get; }
        protected DomainException(string title, string message): base(message) => Title = title;

        
    }
}
