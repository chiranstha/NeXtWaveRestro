using Abp.AutoMapper;
using NextWave.Erp.Authorization.Users.Dto;

namespace NextWave.Erp.Maui.Models.User;

[AutoMapFrom(typeof(UserListDto))]
public class UserListModel : UserListDto
{
    public string Photo { get; set; }

    public string FullName => Name + " " + Surname;
}