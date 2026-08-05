using NextWave.Erp.Common.Dto;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.Dto
{
    public class BookReportLookupDto
    {
        public List<UniversalDropdownDto> Ledgers { get; set; } = new();

        public List<BookReportVoucherTypeDto> VoucherTypes { get; set; } = new();

        public CurrentFinancialYearDto FinancialYear { get; set; }
    }

    public class BookReportVoucherTypeDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }

        public string TypeOfVoucher { get; set; }
    }

    public class BookReportResultDto
    {
        public string ReportType { get; set; }

        public string ReportTitle { get; set; }

        public bool RequiresLedger { get; set; }

        public List<BookReportRowDto> Rows { get; set; } = new();

        public BookReportSummaryDto Summary { get; set; } = new();
    }

    public class BookReportRowDto
    {
        public int Sn { get; set; }

        public DateTime Date { get; set; }

        public string DateMiti { get; set; }

        public string VoucherType { get; set; }

        public string VoucherNo { get; set; }

        public Guid? LedgerId { get; set; }

        public string LedgerName { get; set; }

        public string GroupName { get; set; }

        public int LineCount { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public decimal InAmount { get; set; }

        public decimal OutAmount { get; set; }

        public decimal Difference { get; set; }

        public decimal ClosingBalance { get; set; }

        public int AgeDays { get; set; }

        public decimal CurrentAmount { get; set; }

        public decimal Age1To30 { get; set; }

        public decimal Age31To60 { get; set; }

        public decimal Age61To90 { get; set; }

        public decimal AgeAbove90 { get; set; }

        public string Status { get; set; }

        public string Remarks { get; set; }
    }

    public class BookReportSummaryDto
    {
        public int TotalRows { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal TotalDebit { get; set; }

        public decimal TotalCredit { get; set; }

        public decimal TotalIn { get; set; }

        public decimal TotalOut { get; set; }

        public decimal Difference { get; set; }

        public decimal ClosingBalance { get; set; }
    }

    public class BookReportExcelDto
    {
        public string ReportTitle { get; set; }

        public string FromMiti { get; set; }

        public string ToMiti { get; set; }

        public List<BookReportRowDto> Rows { get; set; } = new();

        public BookReportSummaryDto Summary { get; set; } = new();
    }
}
