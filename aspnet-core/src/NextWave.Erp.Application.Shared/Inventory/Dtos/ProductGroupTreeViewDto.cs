using System;
using System.Collections.Generic;

namespace NextWave.Erp.Inventory.Dtos
{
    public class ProductGroupTreeViewDto
    {
        public Guid Id { get; set; }
        public ProductGroupTreeViewDataDto Data { get; set; }
        public List<ProductGroupTreeViewDto> Children { get; set; }
    }

    public class ProductGroupTreeViewDataDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsGroup { get; set; }
    }
}
