using System;

namespace NextWave.Erp.Dto
{
    public class DocumentDetailsDto
    {
        public Guid Id { get; set; }
        public string VoucherNo { get; set; }
        public string VoucherType { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public virtual byte[] Image { get; set; }
    }
}
