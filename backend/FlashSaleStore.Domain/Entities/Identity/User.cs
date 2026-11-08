using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Identity;

/// <summary>Maps to dbo.Users. PasswordHash is managed by ASP.NET Identity.</summary>
public sealed class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; private set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public int AccessFailedCount { get; set; }

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        var normalizedName = fullName?.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName.Length > 200)
        {
            throw new DomainRuleViolationException("User.FullName.Invalid", "Họ tên phải có từ 1 đến 200 ký tự.");
        }

        if (normalizedPhone?.Length > 30)
        {
            throw new DomainRuleViolationException("User.PhoneNumber.Invalid", "Số điện thoại không được vượt quá 30 ký tự.");
        }

        FullName = normalizedName;
        PhoneNumber = normalizedPhone;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;

        IsActive = isActive;
        MarkUpdated();
    }
}
