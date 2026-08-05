
using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.FinancialStatement
{
    /// Represents the hierarchical structure of a cash flow statement
    /// </summary>
    public class CashFlowStatementDto : EntityDto<Guid>
    {
        /// <summary>
        /// The data for this cash flow statement item
        /// </summary>
        public CashFlowStatementDetail Data { get; set; }

        /// <summary>
        /// Child items in the cash flow hierarchy
        /// </summary>
        public List<CashFlowStatementDto> Children { get; set; }
    }

    /// <summary>
    /// Represents the details of a cash flow statement item
    /// </summary>
    public class CashFlowStatementDetail : EntityDto<Guid>
    {
        /// <summary>
        /// Name of the cash flow item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Amount for the current period
        /// </summary>
        public decimal CurrentAmount { get; set; }

        /// <summary>
        /// Amount for the previous period (for comparison)
        /// </summary>
        public decimal PreviousAmount { get; set; }

        /// <summary>
        /// Type of the cash flow group
        /// </summary>
        public CashFlowGroupEnum GroupType { get; set; }

        /// <summary>
        /// Level of indentation for display purposes
        /// </summary>
        public int Level { get; set; }
    }

    /// <summary>
    /// Enum representing different types of cash flow groups
    /// </summary>
    public enum CashFlowGroupEnum
    {
        None = 0,
        OperatingActivities = 1,
        InvestingActivities = 2,
        FinancingActivities = 3,
        NetChange = 4,
        BeginningBalance = 5,
        EndingBalance = 6,
        AccountGroup = 7,
        AccountLedger = 8
    }

    /// <summary>
    /// DTO for cash flow PDF report generation
    /// </summary>
    public class CashFlowPdfDto
    {
        /// <summary>
        /// Company information for the report header
        /// </summary>
        public GetBranchForViewDto CompanyInfo { get; set; }

        /// <summary>
        /// List of cash flow statement details
        /// </summary>
        public List<CashFlowStatementDetail> CashFlowDetails { get; set; }

        /// <summary>
        /// Report parameters
        /// </summary>
        public ReportParametersDto ReportParameters { get; set; }
    }

    /// <summary>
    /// DTO for parameter information in the report
    /// </summary>
    public class ReportParametersDto
    {
        /// <summary>
        /// Start date of the report period
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// End date of the report period
        /// </summary>
        public string EndDate { get; set; }

        /// <summary>
        /// Branch ID for the report
        /// </summary>


        /// <summary>
        /// Comparison period type (previous-year, previous-quarter, etc.)
        /// </summary>
        public string ComparisonPeriod { get; set; }
    }

    /// <summary>
    /// DTO for ledger posting data used in reports
    /// </summary>


    /// <summary>
    /// DTO for account ledger information used in reports
    /// </summary>


    /// <summary>
    /// Interface for cash flow excel export
    /// </summary>
    public interface ICashFlowExcelExporter
    {
        /// <summary>
        /// Export cash flow data to Excel file
        /// </summary>
        Task<FileDto> ExportToFile(List<CashFlowStatementDetail> items);
    }

    /// <summary>
    /// Interface for cash flow PDF export
    /// </summary>
    public interface ICashFlowPdfExporter
    {
        /// <summary>
        /// Generate PDF from cash flow data
        /// </summary>
        Task<byte[]> GeneratePdf(CashFlowPdfDto data);
    }
}