using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Identity;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class UserRepository : SqlRepositoryBase, IUserRepository
{
    public UserRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<User>(UserSelect + " WHERE Id = @Id;", new { Id = id }, cancellationToken);

    public Task<User?> GetByNormalizedUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<User>(UserSelect + " WHERE NormalizedUserName = @NormalizedUserName;", new { NormalizedUserName = normalizedUserName }, cancellationToken);

    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<User>(UserSelect + " WHERE NormalizedEmail = @NormalizedEmail;", new { NormalizedEmail = normalizedEmail }, cancellationToken);

    public Task<int> InsertAsync(User entity, CancellationToken cancellationToken = default) => ExecuteAsync("""
        INSERT INTO dbo.Users
            (Id, FullName, IsActive, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
             PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
             LockoutEnd, LockoutEnabled, AccessFailedCount, CreatedAt, UpdatedAt)
        VALUES
            (@Id, @FullName, @IsActive, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed,
             @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled,
             @LockoutEnd, @LockoutEnabled, @AccessFailedCount, @CreatedAt, @UpdatedAt);
        """, entity, cancellationToken);

    public Task<int> UpdateAsync(User entity, CancellationToken cancellationToken = default) => ExecuteAsync("""
        UPDATE dbo.Users
        SET FullName = @FullName, IsActive = @IsActive, UserName = @UserName, NormalizedUserName = @NormalizedUserName,
            Email = @Email, NormalizedEmail = @NormalizedEmail, EmailConfirmed = @EmailConfirmed,
            PasswordHash = @PasswordHash, SecurityStamp = @SecurityStamp, ConcurrencyStamp = @ConcurrencyStamp,
            PhoneNumber = @PhoneNumber, PhoneNumberConfirmed = @PhoneNumberConfirmed, TwoFactorEnabled = @TwoFactorEnabled,
            LockoutEnd = @LockoutEnd, LockoutEnabled = @LockoutEnabled, AccessFailedCount = @AccessFailedCount, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """, entity, cancellationToken);

    private const string UserSelect = """
        SELECT Id, FullName, IsActive, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
               PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
               LockoutEnd, LockoutEnabled, AccessFailedCount, CreatedAt, UpdatedAt
        FROM dbo.Users
        """;
}

internal sealed class RoleRepository : SqlRepositoryBase, IRoleRepository
{
    public RoleRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Role>(RoleSelect + " WHERE Id = @Id;", new { Id = id }, cancellationToken);

    public Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Role>(RoleSelect + " WHERE NormalizedName = @NormalizedName;", new { NormalizedName = normalizedName }, cancellationToken);

    public Task<int> InsertAsync(Role entity, CancellationToken cancellationToken = default) => ExecuteAsync("""
        INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp);
        """, entity, cancellationToken);

    public Task<int> UpdateAsync(Role entity, CancellationToken cancellationToken = default) => ExecuteAsync("""
        UPDATE dbo.Roles SET Name = @Name, NormalizedName = @NormalizedName, ConcurrencyStamp = @ConcurrencyStamp
        WHERE Id = @Id;
        """, entity, cancellationToken);

    private const string RoleSelect = "SELECT Id, Name, NormalizedName, ConcurrencyStamp FROM dbo.Roles";
}

internal sealed class UserClaimRepository : SqlRepositoryBase, IUserClaimRepository
{
    public UserClaimRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public Task<UserClaim?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<UserClaim>("SELECT Id, UserId, ClaimType, ClaimValue FROM dbo.UserClaims WHERE Id = @Id;", new { Id = id }, cancellationToken);

    public async Task<IReadOnlyList<UserClaim>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await QueryAsync<UserClaim>("SELECT Id, UserId, ClaimType, ClaimValue FROM dbo.UserClaims WHERE UserId = @UserId ORDER BY Id;", new { UserId = userId }, cancellationToken);

    public async Task<int> InsertAsync(UserClaim entity, CancellationToken cancellationToken = default)
    {
        var id = await ExecuteScalarAsync<int>("""
            INSERT INTO dbo.UserClaims (UserId, ClaimType, ClaimValue)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @ClaimType, @ClaimValue);
            """, entity, cancellationToken);
        entity.Id = id;
        return id;
    }

    public Task<int> UpdateAsync(UserClaim entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("UPDATE dbo.UserClaims SET ClaimType = @ClaimType, ClaimValue = @ClaimValue WHERE Id = @Id;", entity, cancellationToken);
}

internal sealed class RoleClaimRepository : SqlRepositoryBase, IRoleClaimRepository
{
    public RoleClaimRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public Task<RoleClaim?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<RoleClaim>("SELECT Id, RoleId, ClaimType, ClaimValue FROM dbo.RoleClaims WHERE Id = @Id;", new { Id = id }, cancellationToken);

    public async Task<IReadOnlyList<RoleClaim>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await QueryAsync<RoleClaim>("SELECT Id, RoleId, ClaimType, ClaimValue FROM dbo.RoleClaims WHERE RoleId = @RoleId ORDER BY Id;", new { RoleId = roleId }, cancellationToken);

    public async Task<int> InsertAsync(RoleClaim entity, CancellationToken cancellationToken = default)
    {
        var id = await ExecuteScalarAsync<int>("""
            INSERT INTO dbo.RoleClaims (RoleId, ClaimType, ClaimValue)
            OUTPUT INSERTED.Id
            VALUES (@RoleId, @ClaimType, @ClaimValue);
            """, entity, cancellationToken);
        entity.Id = id;
        return id;
    }

    public Task<int> UpdateAsync(RoleClaim entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("UPDATE dbo.RoleClaims SET ClaimType = @ClaimType, ClaimValue = @ClaimValue WHERE Id = @Id;", entity, cancellationToken);
}

internal sealed class UserRoleRepository : SqlRepositoryBase, IUserRoleRepository
{
    public UserRoleRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public async Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await QueryAsync<UserRole>("SELECT UserId, RoleId FROM dbo.UserRoles WHERE UserId = @UserId ORDER BY RoleId;", new { UserId = userId }, cancellationToken);

    public async Task<IReadOnlyList<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await QueryAsync<UserRole>("SELECT UserId, RoleId FROM dbo.UserRoles WHERE RoleId = @RoleId ORDER BY UserId;", new { RoleId = roleId }, cancellationToken);

    public Task<int> InsertAsync(UserRole entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);", entity, cancellationToken);

    public Task<int> DeleteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM dbo.UserRoles WHERE UserId = @UserId AND RoleId = @RoleId;", new { UserId = userId, RoleId = roleId }, cancellationToken);
}

internal sealed class UserLoginRepository : SqlRepositoryBase, IUserLoginRepository
{
    public UserLoginRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public Task<UserLogin?> GetByIdAsync(UserLoginKey id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<UserLogin>("""
            SELECT LoginProvider, ProviderKey, ProviderDisplayName, UserId FROM dbo.UserLogins
            WHERE LoginProvider = @LoginProvider AND ProviderKey = @ProviderKey;
            """, id, cancellationToken);

    public async Task<IReadOnlyList<UserLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await QueryAsync<UserLogin>("SELECT LoginProvider, ProviderKey, ProviderDisplayName, UserId FROM dbo.UserLogins WHERE UserId = @UserId;", new { UserId = userId }, cancellationToken);

    public Task<int> InsertAsync(UserLogin entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.UserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId)
            VALUES (@LoginProvider, @ProviderKey, @ProviderDisplayName, @UserId);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(UserLogin entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.UserLogins SET ProviderDisplayName = @ProviderDisplayName, UserId = @UserId
            WHERE LoginProvider = @LoginProvider AND ProviderKey = @ProviderKey;
            """, entity, cancellationToken);
}

internal sealed class UserTokenRepository : SqlRepositoryBase, IUserTokenRepository
{
    public UserTokenRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public Task<UserToken?> GetByIdAsync(UserTokenKey id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<UserToken>("""
            SELECT UserId, LoginProvider, Name, Value FROM dbo.UserTokens
            WHERE UserId = @UserId AND LoginProvider = @LoginProvider AND Name = @Name;
            """, id, cancellationToken);

    public Task<int> InsertAsync(UserToken entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.UserTokens (UserId, LoginProvider, Name, Value)
            VALUES (@UserId, @LoginProvider, @Name, @Value);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(UserToken entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.UserTokens SET Value = @Value
            WHERE UserId = @UserId AND LoginProvider = @LoginProvider AND Name = @Name;
            """, entity, cancellationToken);
}
