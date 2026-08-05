using Abp.Events.Bus;

namespace NextWave.Erp.MultiTenancy.Subscription;

public class RecurringPaymentsEnabledEventData : EventData
{
    public int TenantId { get; set; }
}

