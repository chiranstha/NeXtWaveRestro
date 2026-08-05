using Abp.Dependency;
using NextWave.Erp.Accounting.Importing.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Accounting.Importing
{
    public interface IAccountGroupListExcelDataReader : ITransientDependency
    {
        List<ImportAccountGroupDto> GetAccountGroupsFromExcel(byte[] fileBytes);
    }
}
