using Abp.Application.Services.Dto;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Authorization.Users.Dto;
using NextWave.Erp.Maui.Models.User;
using NextWave.Erp.Maui.Services.User;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextWave.Erp.Maui.Core;
using NextWave.Erp.Maui.Core.Components;
using NextWave.Erp.Maui.Core.Threading;
using NextWave.Erp.Maui.Services.Navigation;
using NextWave.Erp.Maui.Services.UI;

namespace NextWave.Erp.Maui.Pages.User;

public partial class Index : ErpMainLayoutPageComponentBase
{
    protected IUserAppService UserAppService { get; set; }
    protected IUserProfileService UserProfileService { get; set; }
    
    private ItemsProviderResult<UserListModel> users;

    private readonly GetUsersInput _filter = new GetUsersInput();

    private Virtualize<UserListModel> UserListContainer { get; set; }

    public Index()
    {
        UserAppService = Resolve<IUserAppService>();
        UserProfileService = Resolve<IUserProfileService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("Users"), L("UsersHeaderInfo"),
            [new PageHeaderButton(L("Create"), MauiConsts.Icons.AddIcon, OpenCreatePage)]);
    }

    private async Task RefreshList()
    {
        await UserListContainer.RefreshDataAsync();
        StateHasChanged();
    }

    private async ValueTask<ItemsProviderResult<UserListModel>> LoadUsers(ItemsProviderRequest request)
    {
        _filter.MaxResultCount = request.Count;
        _filter.SkipCount = request.StartIndex;

        await UserDialogsService.Block();

        await WebRequestExecuter.Execute(
            async () => await UserAppService.GetUsers(_filter),
            async (result) =>
            {
                if (result == null)
                {
                    await UserDialogsService.UnBlock();
                    return;
                }

                var userList = ObjectMapper.Map<List<UserListModel>>(result.Items);
                foreach (var user in userList)
                {
                    await SetUserImageSourceAsync(user);
                }

                users = new ItemsProviderResult<UserListModel>(userList, result.TotalCount);

                await UserDialogsService.UnBlock();
            }
        );

        return users;
    }

    private async Task SetUserImageSourceAsync(UserListModel userListModel)
    {
        if (userListModel.Photo != null)
        {
            return;
        }

        if (!userListModel.ProfilePictureId.HasValue)
        {
            userListModel.Photo = UserProfileService.GetDefaultProfilePicture();
            return;
        }

        userListModel.Photo = await UserProfileService.GetProfilePicture(userListModel.Id);
    }

    private Task OpenCreatePage()
    {
        NavigationService.NavigateTo(NavigationUrlConsts.Users_Create_Or_Edit);
        return Task.CompletedTask;
    }

    private void OpenEditPage(long id)
    {
        NavigationService.NavigateTo(NavigationUrlConsts.Users_Create_Or_Edit + $"/{id}");
    }
}