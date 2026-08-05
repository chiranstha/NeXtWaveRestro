using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class TemporaryUnitsDto
    {
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public string UnitName { get; set; }
    }
}
