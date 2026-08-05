using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    public interface IReceiptMastersExcelExporter
    {
        FileDto ExportToFile(List<GetReceiptMasterForViewDto> receiptMasters);
    }
}
