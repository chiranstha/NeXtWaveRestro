using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class ProductTaxDto
    {
        public Guid TaxId { get; set; }
        public string TaxName { get; set; }
        public decimal? TaxRate { get; set; }
    }
}
