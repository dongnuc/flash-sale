using MediatR;

namespace Movie_StructureCode.Contract.Abstractions.Message
{
    public interface IDomainEvent : INotification
    {
        public Guid EventId { get; init; }
    }
}
