using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class HsCodeDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string HsCode { get; set; }
    }
}
