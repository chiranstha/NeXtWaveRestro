using System;
using System.Collections.Generic;

namespace NextWave.Erp.Inventory.Dtos
{
    public class MultipleProductCreate
    {
        public Guid ProductGroupId { get; set; }
        public Guid UnitId { get; set; }
        public List<MultipleProductListCreateDto> ProductList { get; set; }
    }
}
