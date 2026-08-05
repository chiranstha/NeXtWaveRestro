using System.Collections.Generic;
using NextWave.Erp.Editions.Dto;
using NextWave.Erp.Security;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Tenants;

public class CreateTenantViewModel
{
    public IReadOnlyList<SubscribableEditionComboboxItemDto> EditionItems { get; set; }

    public PasswordComplexitySetting PasswordComplexitySetting { get; set; }

    public CreateTenantViewModel(IReadOnlyList<SubscribableEditionComboboxItemDto> editionItems)
    {
        EditionItems = editionItems;
    }
}

