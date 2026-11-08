using System.Security.Cryptography;

namespace FlashSaleStore.Domain.Entities;

/// <summary>
/// Base for entities whose tables have a Guid primary key and CreatedAt/UpdatedAt columns.
/// Timestamps are UTC; UUIDs use the RFC 9562 version 7 layout.
/// </summary>
public abstract class BaseEntity
{
    protected BaseEntity()
    {
        var now = DateTime.UtcNow;
        Id = NewId(now);
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Creates a UUIDv7 for entities that have a Guid key but do not have audit columns.</summary>
    public static Guid NewId() => NewId(DateTime.UtcNow);

    protected void MarkUpdated(DateTime? utcNow = null)
    {
        var value = utcNow ?? DateTime.UtcNow;
        UpdatedAt = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static Guid NewId(DateTime utcNow)
    {
        Span<byte> rfcBytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(rfcBytes);

        var unixTimeMilliseconds = new DateTimeOffset(utcNow).ToUnixTimeMilliseconds();
        rfcBytes[0] = (byte)(unixTimeMilliseconds >> 40);
        rfcBytes[1] = (byte)(unixTimeMilliseconds >> 32);
        rfcBytes[2] = (byte)(unixTimeMilliseconds >> 24);
        rfcBytes[3] = (byte)(unixTimeMilliseconds >> 16);
        rfcBytes[4] = (byte)(unixTimeMilliseconds >> 8);
        rfcBytes[5] = (byte)unixTimeMilliseconds;

        // UUID version 7 and RFC variant 10xx.
        rfcBytes[6] = (byte)((rfcBytes[6] & 0x0F) | 0x70);
        rfcBytes[8] = (byte)((rfcBytes[8] & 0x3F) | 0x80);

        // Guid(byte[]) uses little-endian layout for its first three fields.
        var guidBytes = new byte[]
        {
            rfcBytes[3], rfcBytes[2], rfcBytes[1], rfcBytes[0],
            rfcBytes[5], rfcBytes[4],
            rfcBytes[7], rfcBytes[6],
            rfcBytes[8], rfcBytes[9], rfcBytes[10], rfcBytes[11],
            rfcBytes[12], rfcBytes[13], rfcBytes[14], rfcBytes[15]
        };

        return new Guid(guidBytes);
    }
}
