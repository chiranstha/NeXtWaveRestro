using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    public interface IPaymentMastersExcelExporter
    {
        FileDto ExportToFile(List<GetPaymentMasterForViewDto> paymentMasters);

    }
}
