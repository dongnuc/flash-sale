using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Services;

/// <summary>A read model supplied by Application/Persistence for schedule validation.</summary>
public sealed record FlashSaleSchedule(
    Guid FlashSaleId,
    Guid ProductId,
    DateTime StartsAt,
    DateTime EndsAt,
    FlashSaleStatus Status);

/// <summary>Pure policy for checking product schedule overlaps; it does not query storage.</summary>
public sealed class FlashSaleSchedulePolicy
{
    public void EnsureNoOverlap(FlashSaleSchedule candidate, IEnumerable<FlashSaleSchedule> existingSchedules)
    {
        if (candidate is null || existingSchedules is null)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Invalid", "Thông tin lịch FlashSale không hợp lệ.");
        }

        if (candidate.EndsAt <= candidate.StartsAt)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Invalid", "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        if (candidate.Status == FlashSaleStatus.Cancelled) return;

        var hasConflict = existingSchedules.Any(existing =>
            existing is not null &&
            existing.FlashSaleId != candidate.FlashSaleId &&
            existing.ProductId == candidate.ProductId &&
            existing.Status != FlashSaleStatus.Cancelled &&
            Overlaps(candidate.StartsAt, candidate.EndsAt, existing.StartsAt, existing.EndsAt));

        if (hasConflict)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Overlap", "Sản phẩm đã có FlashSale khác trong khoảng thời gian này.", DomainFailureKind.Conflict);
        }
    }

    public static bool Overlaps(DateTime startsAt, DateTime endsAt, DateTime otherStartsAt, DateTime otherEndsAt) =>
        startsAt < otherEndsAt && otherStartsAt < endsAt;
}
