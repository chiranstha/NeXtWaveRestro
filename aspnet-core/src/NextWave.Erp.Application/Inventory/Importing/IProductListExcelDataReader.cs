using Abp.Dependency;
using NextWave.Erp.Inventory.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Inventory.Importing
{
    public interface IProductListExcelDataReader : ITransientDependency
    {
        List<ImportProductDto> GetProductsFromExcel(byte[] fileBytes);
    }
}
