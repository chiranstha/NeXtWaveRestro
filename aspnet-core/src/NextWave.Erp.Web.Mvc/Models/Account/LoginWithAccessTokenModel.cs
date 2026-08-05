using System.ComponentModel.DataAnnotations;
using Abp.Auditing;

namespace NextWave.Erp.Web.Models.Account;

public class LoginWithAccessTokenModel
{
    public string AccessToken { get; set; }
}

