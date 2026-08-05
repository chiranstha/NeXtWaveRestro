using System.Collections.Generic;
using NextWave.Erp.Editions.Dto;
using NextWave.Erp.MultiTenancy.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Tenants;

public class EditTenantViewModel
{
    public TenantEditDto Tenant { get; set; }

    public IReadOnlyList<SubscribableEditionComboboxItemDto> EditionItems { get; set; }

    public EditTenantViewModel(TenantEditDto tenant, IReadOnlyList<SubscribableEditionComboboxItemDto> editionItems)
    {
        Tenant = tenant;
        EditionItems = editionItems;
    }
}

