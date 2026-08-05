using System;

namespace NextWave.Erp.WebHooks.Dto;

public class ActivateWebhookSubscriptionInput
{
    public Guid SubscriptionId { get; set; }

    public bool IsActive { get; set; }
}

