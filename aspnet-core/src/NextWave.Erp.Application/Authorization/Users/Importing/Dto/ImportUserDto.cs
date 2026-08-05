using NextWave.Erp.DataImporting.Excel;

namespace NextWave.Erp.Authorization.Users.Importing.Dto;

public class ImportUserDto : ImportFromExcelDto
{
    public string Name { get; set; }

    public string Surname { get; set; }

    public string UserName { get; set; }

    public string EmailAddress { get; set; }

    public string PhoneNumber { get; set; }

    public string Password { get; set; }

    /// <summary>
    /// comma separated list
    /// </summary>
    public string[] Roles { get; set; }
}
