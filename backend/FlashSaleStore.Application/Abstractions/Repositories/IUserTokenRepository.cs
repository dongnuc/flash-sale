using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public sealed record UserTokenKey(Guid UserId, string LoginProvider, string Name);

public interface IUserTokenRepository : IEntityRepository<UserToken, UserTokenKey>
{
}
