using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class CreateProductMergeDto
    {
        public Guid OldProductId { get; set; }
        public Guid NewProductId { get; set; }
    }
}
