using NextWave.Erp.Maui.Models.NavigationMenu;

namespace NextWave.Erp.Maui.Services.Navigation;

public interface IMenuProvider
{
    List<NavigationMenuItem> GetAuthorizedMenuItems(Dictionary<string, string> grantedPermissions);
}