using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class ImportProductDto
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public string ProductGroupId { get; set; }
        public string ProductGroupName { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public bool IsOpeningStock { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal SalesRate { get; set; }
        public decimal Mrp { get; set; }
        public decimal OpeningQty { get; set; }
        public string HsCode { get; set; }
        public int IsTaxable { get; set; }
    }
}
