using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class GetUnitsOfEdit
    {
        public Guid UnitId { get; set; }
        public List<ProductUnitConversionDto> UnitConversion { get; set; }
    }
}
