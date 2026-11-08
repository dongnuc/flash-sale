using MediatR;

namespace FlashSaleStore.Contract.Abstractions.Message
{
    public interface IDomainEvent : INotification
    {
        public Guid EventId { get; init; }
    }
}
