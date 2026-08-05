using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class SalesProductTableDto
    {
        public Guid ProductId { get; set; }
        public Guid ProductGroupId { get; set; }

        public string Name { get; set; }
        public decimal Rate { get; set; }
        public Guid UnitId { get; set; }
    }
}
