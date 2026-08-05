using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Dtos
{
    public class GetPurchaseMasterExcelExportMasterDto
    {
        public string Branch { get; set; }
        public string PhoneNo { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Pan { get; set; }
        public string Address { get; set; }
        public List<GetPurchaseMasterExcelExportDetailDto> Details { get; set; }
    }
}
