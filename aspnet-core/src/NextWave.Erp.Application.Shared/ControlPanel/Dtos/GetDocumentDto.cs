using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.ControlPanel.Dtos
{
    public class GetDocumentDto
    {
        public Guid Id { get; set; }
        public virtual Guid VoucherTypeId { get; set; }
        public virtual string VoucherNo { get; set; }
        public virtual string Name { get; set; }
        public byte[] Image { get; set; }
    }
}
