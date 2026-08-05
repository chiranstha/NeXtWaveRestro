using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class UnitsByProductIdDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public decimal Rate { get; set; }
    }
}
