using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales
{
    [Table("tbl_SalesMaster")]
    public class SalesMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public int? TenantId { get; set; }

        public virtual string VoucherNo { get; set; }

        public virtual Guid SalesAccountId { get; set; }
        public virtual DateTime Date { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual int CreditPeriod { get; set; }

        public virtual DateTime CreditDate { get; set; }

        public virtual DateTime CreatedDate { get; set; } = DateTime.Now;


        public virtual string Description { get; set; }

        public virtual decimal TaxAmount { get; set; }
        public virtual decimal BillDiscount { get; set; }

        public virtual decimal GrandTotal { get; set; }

        public virtual decimal GrossAmount { get; set; }

        public virtual decimal TaxableAmount { get; set; }

        public virtual bool IsPrint { get; set; }

        public virtual int NoOfPrint { get; set; }

        public virtual decimal NetAmount { get; set; }

        public virtual bool SyncwithIrd { get; set; }
        public virtual DateTime? IrdSyncDateTime { get; set; }


        public virtual string PrintedTime { get; set; }

        public virtual bool IsRealTime { get; set; }

        public virtual PaymentMethod PaymentMethod { get; set; }
        public virtual Guid? PaymentMethodLedgerId { get; set; }

        public virtual bool IsDelete { get; set; }

        public virtual decimal? VatRefundAmount { get; set; }

        public virtual string LrNo { get; set; }

        public virtual string VehicleNo { get; set; }

        public virtual string AgainstId { get; set; }
        public virtual SalesModeType SalesModeType { get; set; }
        public virtual string AgainstVoucherNo { get; set; }
        public virtual string PINumber { get; set; }

        public virtual InvoiceTypeEnum InvoiceType { get; set; }
        public virtual SalesType SalesType { get; set; } = 0;
        public virtual int? PrintUserId { get; set; }


        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }


        public virtual string LedgerName { get; set; }
        public virtual string CustomerAddress { get; set; }
        public virtual string CustomerPhoneNo { get; set; }
        public virtual string VatNo { get; set; }

        public virtual Guid? LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }


        public virtual string ServiceDeliveryId { get; set; }

        public virtual string SourceModule { get; set; }

        public virtual Guid? SourceDocumentId { get; set; }

        public virtual bool IsErrorFixed { get; set; } = true;

        public virtual Guid FinancialYearId { get; set; }

        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public virtual long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }
        public virtual decimal? PostingNumbering { get; set; }

        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }
    }
}
