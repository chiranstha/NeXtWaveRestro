namespace NextWave.Erp.GeneralSetting.Dtos
{
    public class VoucherNumberingDto
    {
        public int VoucherTypeId { get; set; }

        public string Prefix { get; set; }

        public string Postfix { get; set; }

        public int StartIndex { get; set; }
    }
}
