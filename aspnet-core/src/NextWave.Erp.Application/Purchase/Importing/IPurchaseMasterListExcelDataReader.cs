using Abp.Dependency;
using NextWave.Erp.Purchase.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Importing
{
    public interface IPurchaseMasterListExcelDataReader : ITransientDependency
    {
        List<GetPurchaseMasterForImportDto> GetpurchaseMasterFromExcel(byte[] fileBytes);
    }
}
