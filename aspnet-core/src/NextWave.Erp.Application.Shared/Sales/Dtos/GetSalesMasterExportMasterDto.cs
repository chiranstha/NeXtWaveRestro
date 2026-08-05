using System.Collections.Generic;

namespace NextWave.Erp.Sales.Dtos
{
    public class GetSalesMasterExportMasterDto
    {
        public string Company { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public List<GetSalesMasterExportDto> Details { get; set; }
    }
}
