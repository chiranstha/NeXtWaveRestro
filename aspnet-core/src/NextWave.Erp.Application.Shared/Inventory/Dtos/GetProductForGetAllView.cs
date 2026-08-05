using System;
using NextWave.Erp.Enums;

namespace NextWave.Erp.Inventory.Dtos
{
    public class GetProductForGetAllView
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public string HSCode { get; set; }
        public string Name { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public string ProductGroupName { get; set; }
    }
}
