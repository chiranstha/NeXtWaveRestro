using Abp.Dependency;
using NextWave.Erp.Inventory.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Importing
{
    public interface IUnitListExcelDataReader : ITransientDependency
    {
        List<GetUnitForViewDto> GetUnitFromExcel(byte[] fileBytes);
    }
}
