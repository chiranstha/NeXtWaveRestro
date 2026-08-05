namespace NextWave.Erp.Transaction.Dtos
{
    public class GetAllPDCPayablesForExcelInput
    {
        public string Filter { get; set; }

        public string BranchNameFilter { get; set; }

        public string FinancialYearFromMitiFilter { get; set; }
    }
}