using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseMasterTaxTableDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal? Rate { get; set; }
    }
}
