using System;
using Abp.Notifications;
using NextWave.Erp.Dto;

namespace NextWave.Erp.Notifications.Dto;

public class GetUserNotificationsInput : PagedInputDto
{
    public UserNotificationState? State { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

