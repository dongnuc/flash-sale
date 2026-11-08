using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.FlashSales;

/// <summary>Maps to dbo.FlashSales.</summary>
public sealed class FlashSale : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public FlashSaleStatus Status { get; private set; } = FlashSaleStatus.Draft;

    public static FlashSale Create(string name, DateTime startsAt, DateTime endsAt)
    {
        var flashSale = new FlashSale();
        flashSale.UpdateDetails(name, startsAt, endsAt);
        flashSale.MarkUpdated(flashSale.CreatedAt);
        return flashSale;
    }

    public void UpdateDetails(string name, DateTime startsAt, DateTime endsAt)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainRuleViolationException("FlashSale.Name.Required", "Tên FlashSale không được để trống.");
        }

        if (normalizedName.Length > 200)
        {
            throw new DomainRuleViolationException("FlashSale.Name.MaxLength", "Tên FlashSale không được vượt quá 200 ký tự.");
        }

        ValidatePeriod(startsAt, endsAt);

        if (Status is FlashSaleStatus.Ended or FlashSaleStatus.Cancelled)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Immutable", "Không thể sửa lịch của FlashSale đã kết thúc hoặc bị hủy.");
        }

        Name = normalizedName;
        StartsAt = startsAt;
        EndsAt = endsAt;
        MarkUpdated();
    }

    public void SetStartTime(DateTime startsAt)
    {
        if (Status is FlashSaleStatus.Ended or FlashSaleStatus.Cancelled)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Immutable", "Không thể sửa lịch của FlashSale đã kết thúc hoặc bị hủy.");
        }

        if (startsAt >= EndsAt)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Invalid", "Thời gian bắt đầu phải trước thời gian kết thúc.");
        }

        StartsAt = startsAt;
        MarkUpdated();
    }

    public bool IsEffectiveAt(DateTime utcNow) =>
        Status == FlashSaleStatus.Active && StartsAt <= utcNow && utcNow < EndsAt;

    public void ChangeStatus(FlashSaleStatus targetStatus, DateTime utcNow)
    {
        if (Status == targetStatus) return;

        if (targetStatus == FlashSaleStatus.Cancelled && Status is not (FlashSaleStatus.Ended or FlashSaleStatus.Cancelled))
        {
            Status = targetStatus;
            MarkUpdated(utcNow);
            return;
        }

        var canTransition = (Status, targetStatus) switch
        {
            (FlashSaleStatus.Draft, FlashSaleStatus.Scheduled) => StartsAt > utcNow,
            (FlashSaleStatus.Draft, FlashSaleStatus.Active) => StartsAt <= utcNow && utcNow < EndsAt,
            (FlashSaleStatus.Scheduled, FlashSaleStatus.Active) => StartsAt <= utcNow && utcNow < EndsAt,
            (FlashSaleStatus.Scheduled, FlashSaleStatus.Ended) => utcNow >= EndsAt,
            (FlashSaleStatus.Active, FlashSaleStatus.Ended) => utcNow >= EndsAt,
            _ => false
        };

        if (!canTransition)
        {
            throw new DomainRuleViolationException("FlashSale.Status.TransitionInvalid", "Không thể chuyển FlashSale sang trạng thái đích trong thời điểm hiện tại.");
        }

        Status = targetStatus;
        MarkUpdated(utcNow);
    }

    private static void ValidatePeriod(DateTime startsAt, DateTime endsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainRuleViolationException("FlashSale.Schedule.Invalid", "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }
    }
}
