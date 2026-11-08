using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Settings;

/// <summary>Maps to dbo.SystemSettings.</summary>
public sealed class SystemSetting
{
    public string SettingKey { get; private set; } = string.Empty;
    public string? SettingValue { get; private set; }
    public string? Description { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static SystemSetting Create(string settingKey, string? value, string? description,
        Guid? updatedByUserId = null)
    {
        var normalizedKey = settingKey?.Trim();
        Validate(normalizedKey, value, description, updatedByUserId);

        return new SystemSetting
        {
            SettingKey = normalizedKey!,
            SettingValue = value,
            Description = description,
            UpdatedByUserId = updatedByUserId,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? value, string? description, Guid? updatedByUserId, DateTime? utcNow = null)
    {
        Validate(SettingKey, value, description, updatedByUserId);

        SettingValue = value;
        Description = description;
        UpdatedByUserId = updatedByUserId;
        var now = utcNow ?? DateTime.UtcNow;
        UpdatedAt = now.Kind switch
        {
            DateTimeKind.Utc => now,
            DateTimeKind.Local => now.ToUniversalTime(),
            _ => DateTime.SpecifyKind(now, DateTimeKind.Utc)
        };
    }

    private static void Validate(string? key, string? value, string? description, Guid? updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
        {
            throw new DomainRuleViolationException("SystemSetting.Key.Invalid", "Khóa cấu hình phải có từ 1 đến 100 ký tự.");
        }

        if (value?.Length > 2000 || description?.Length > 500)
        {
            throw new DomainRuleViolationException("SystemSetting.Value.Invalid", "Giá trị hoặc mô tả cấu hình vượt quá độ dài cho phép.");
        }

        if (updatedByUserId == Guid.Empty)
        {
            throw new DomainRuleViolationException("SystemSetting.UpdatedBy.Invalid", "Người cập nhật cấu hình không hợp lệ.");
        }
    }
}
