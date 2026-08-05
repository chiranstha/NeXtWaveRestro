using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetReceiptAgainstMasterDto
    {
        public decimal NewReferenceAmount { get; set; }
        public List<GetReceiptAgainstDto> Details { get; set; }
    }
}
