using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.SharedDtos
{
    public class UnitConversionAddParamsDto
    {
        public Guid ProductId { get; set; }
        public List<UnitConversionAddParamDetails> Details { get; set; }
    }

    public class UnitConversionAddParamDetails
    {
        public Guid UnitId { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
