using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class ProductGroupTreeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string GroupUnder { get; set; }
    }
}
