using Abp.Configuration;

namespace NextWave.Erp.Timing.Dto;

public class GetTimezoneComboboxItemsInput
{
    public SettingScopes DefaultTimezoneScope;

    public string SelectedTimezoneId { get; set; }
}

