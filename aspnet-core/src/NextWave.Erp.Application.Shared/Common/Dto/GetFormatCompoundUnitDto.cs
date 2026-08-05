using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Common.Dto
{
    public class GetFormatCompoundUnitDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
    }
}
