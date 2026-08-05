using System.Collections.Generic;
using System.Threading.Tasks;
using NextWave.Erp.Authorization.Users.Dto;
using NextWave.Erp.Dto;

namespace NextWave.Erp.Authorization.Users.Exporting
{
    public interface IUserListExcelExporter
    {
        Task<FileDto> ExportToFile(List<UserListDto> userListDtos, List<string> selectedColumns);
    }
}