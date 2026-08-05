using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class TaxesDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal? Rate { get; set; }
        public string Description { get; set; }
        public Guid LedgerId { get; set; }
    }
}
