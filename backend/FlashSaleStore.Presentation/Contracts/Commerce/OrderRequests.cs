using FlashSaleStore.Application.UseCases.Orders;

namespace FlashSaleStore.Presentation.Contracts.Commerce;

public sealed record DeliveryAddressRequest(string RecipientName, string Phone, string? Province, string? District, string? Ward, string AddressLine)
{
    public DeliveryAddressInput ToInput(string? note) => new(RecipientName, Phone, Province, District, Ward, AddressLine, note);
}
public sealed record CreateOrderRequest(string PaymentMethod, DeliveryAddressRequest? DeliveryAddress, string? Note = null);
