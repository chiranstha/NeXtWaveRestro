using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class Above1LakhMasterDto
    {
        public string Date { get; set; }
        public string BillNo { get; set; }
        public string PartyName { get; set; }
        public string Address { get; set; }
        public string Pan { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal NonTaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string ProductName { get; set; }
    }
}
