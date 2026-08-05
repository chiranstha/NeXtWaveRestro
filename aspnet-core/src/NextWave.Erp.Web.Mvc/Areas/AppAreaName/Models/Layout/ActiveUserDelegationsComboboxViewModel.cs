using System.Collections.Generic;
using NextWave.Erp.Authorization.Delegation;
using NextWave.Erp.Authorization.Users.Delegation.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Layout;

public class ActiveUserDelegationsComboboxViewModel
{
    public IUserDelegationConfiguration UserDelegationConfiguration { get; set; }

    public List<UserDelegationDto> UserDelegations { get; set; }

    public string CssClass { get; set; }
}

