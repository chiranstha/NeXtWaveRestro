using Abp.Dependency;
using NextWave.Erp.Sales.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Sales.Importing
{
    public interface ISalesMasterListExcelDataReader : ITransientDependency
    {
        List<GetSalesMasterForImport> GetSalesMasterFromExcel(byte[] fileBytes);
    }
}
