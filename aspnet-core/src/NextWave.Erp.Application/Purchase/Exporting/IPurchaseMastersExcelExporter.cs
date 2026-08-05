using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Exporting
{
    public interface IPurchaseMastersExcelExporter
    {
        FileDto ExportToFile(List<GetPurchaseMasterForViewDto> purchaseMasters);
        FileDto ExportToFileDetails(GetPurchaseMasterExcelExportMasterDto purchaseMasters);
    }
}
