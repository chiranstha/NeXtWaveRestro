using Abp.OpenIddict.Applications;
using Abp.OpenIddict.Authorizations;
using Abp.OpenIddict.EntityFrameworkCore;
using Abp.OpenIddict.Scopes;
using Abp.OpenIddict.Tokens;
using Abp.Zero.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.BranchUser;
using NextWave.Erp.Authorization.Delegation;
using NextWave.Erp.Authorization.Roles;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Chat;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Editions;
using NextWave.Erp.ExtraProperties;
using NextWave.Erp.Friendships;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.MultiTenancy;
using NextWave.Erp.MultiTenancy.Accounting;
using NextWave.Erp.MultiTenancy.Payments;
using NextWave.Erp.Purchase;
using NextWave.Erp.Restaurant;
using NextWave.Erp.Sales;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction;
using System;
using System.Linq;
using System.Text.Json;
using Document = NextWave.Erp.ControlPanel.Document;

namespace NextWave.Erp.EntityFrameworkCore;

public class ErpDbContext : AbpZeroDbContext<Tenant, Role, User, ErpDbContext>, IOpenIddictDbContext
{
    private const int DecimalPrecision = 28;
    private const int DecimalScale = 8;

    private static readonly JsonSerializerOptions ExtraPropertiesJsonSerializerOptions = new()
    {
        WriteIndented = false
    };

    private static readonly ValueComparer<ExtraPropertyDictionary> ExtraPropertiesValueComparer = new(
        (left, right) => SerializeExtraProperties(left) == SerializeExtraProperties(right),
        value => SerializeExtraProperties(value).GetHashCode(),
        value => DeserializeExtraProperties(SerializeExtraProperties(value)));

    /* Define an IDbSet for each entity of the application */

    public virtual DbSet<OpenIddictApplication> Applications { get; }

    public virtual DbSet<OpenIddictAuthorization> Authorizations { get; }

    public virtual DbSet<OpenIddictScope> Scopes { get; }

    public virtual DbSet<OpenIddictToken> Tokens { get; }

    public virtual DbSet<BinaryObject> BinaryObjects { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<SubscribableEdition> SubscribableEditions { get; set; }

    public virtual DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

    public virtual DbSet<SubscriptionPaymentProduct> SubscriptionPaymentProducts { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<UserDelegation> UserDelegations { get; set; }

    public virtual DbSet<RecentPassword> RecentPasswords { get; set; }

    // Added Tables Start

    public virtual DbSet<AccountGroup> AccountGroups { get; set; }
    public virtual DbSet<AccountLedger> AccountLedgers { get; set; }
    public virtual DbSet<FinancialYear> FinancialYears { get; set; }
    public virtual DbSet<Branch> Branches { get; set; }
    public virtual DbSet<FinancialYearSelect> FinancialYearSelects { get; set; }
    public virtual DbSet<Tax> Taxes { get; set; }
    public virtual DbSet<VoucherNumbering> VoucherNumberings { get; set; }
    public virtual DbSet<VoucherType> VoucherTypes { get; set; }
    public virtual DbSet<Bom> Boms { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductGroup> ProductGroups { get; set; }
    public virtual DbSet<StockFifoTable> StockFifoTables { get; set; }
    public virtual DbSet<StockMaintain> StockMaintains { get; set; }
    public virtual DbSet<StockPosting> StockPostings { get; set; }
    public virtual DbSet<Unit> Units { get; set; }
    public virtual DbSet<UnitConversion> UnitConversions { get; set; }
    public virtual DbSet<LedgerPosting> LedgerPostings { get; set; }
    public virtual DbSet<NewPartyBalance> NewPartyBalances { get; set; }
    public virtual DbSet<PartyBalance> PartyBalances { get; set; }
    public virtual DbSet<UserBranch> UserBranches { get; set; }
    public virtual DbSet<Sizes> Sizes { get; set; }
    public virtual DbSet<PurchaseOrderMaster> PurchaseOrderMasters { get; set; }
    public virtual DbSet<PurchaseOrderDetails> PurchaseOrderDetails { get; set; }
    public virtual DbSet<PurchaseMaster> PurchaseMasters { get; set; }
    public virtual DbSet<PurchaseDetail> PurchaseDetails { get; set; }
    public virtual DbSet<PurchaseReturn> PurchaseReturns { get; set; }
    public virtual DbSet<PurchaseReturnDetail> PurchaseReturnDetails { get; set; }

    public virtual DbSet<PurchaseProductCancelMaster> PurchaseProductCancelMasters { get; set; }
    public virtual DbSet<PurchaseProductCancelDetail> PurchaseProductCancelDetails { get; set; }

    public virtual DbSet<SalesMaster> SalesMasters { get; set; }
    public virtual DbSet<SalesDetail> SalesDetails { get; set; }
    public virtual DbSet<SalesReturnMaster> SalesReturnMasters { get; set; }
    public virtual DbSet<SalesReturnDetail> SalesReturnDetails { get; set; }
    public virtual DbSet<SalesProductCancelMaster> SalesProductCancelMasters { get; set; }
    public virtual DbSet<SalesProductCancelDetail> SalesProductCancelDetails { get; set; }


    public virtual DbSet<PaymentMaster> PaymentMasters { get; set; }
    public virtual DbSet<PaymentDetail> PaymentDetails { get; set; }
    public virtual DbSet<ReceiptMaster> ReceiptMasters { get; set; }
    public virtual DbSet<ReceiptDetail> ReceiptDetails { get; set; }
    public virtual DbSet<ContraMaster> ContraMaster { get; set; }
    public virtual DbSet<ContraDetail> ContraDetail { get; set; }
    public virtual DbSet<JournalMaster> JournalMaster { get; set; }
    public virtual DbSet<JournalDetail> JournalDetail { get; set; }
    public virtual DbSet<PDCPayable> PDCPayables { get; set; }
    public virtual DbSet<PDCReceivable> PDCReceivables { get; set; }
    public virtual DbSet<PDCClearance> PDCClearances { get; set; }

    public virtual DbSet<VoucherPhotos> VoucherPhotos { get; set; }
    public virtual DbSet<Document> Documents { get; set; }
    public virtual DbSet<ImportTaxMaster> ImportTaxMasters { get; set; }
    public virtual DbSet<ImportTaxDetails> ImportTaxDetails { get; set; }
    public virtual DbSet<AdditionalCost> AdditionalCost { get; set; }

    public virtual DbSet<Posting> Postings { get; set; }

    public virtual DbSet<RestaurantArea> RestaurantAreas { get; set; }
    public virtual DbSet<RestaurantTable> RestaurantTables { get; set; }
    public virtual DbSet<RestaurantStation> RestaurantStations { get; set; }
    public virtual DbSet<RestaurantMenuCategory> RestaurantMenuCategories { get; set; }
    public virtual DbSet<RestaurantMenuItem> RestaurantMenuItems { get; set; }
    public virtual DbSet<RestaurantMenuVariant> RestaurantMenuVariants { get; set; }
    public virtual DbSet<RestaurantModifierGroup> RestaurantModifierGroups { get; set; }
    public virtual DbSet<RestaurantModifier> RestaurantModifiers { get; set; }
    public virtual DbSet<RestaurantMenuItemModifierGroup> RestaurantMenuItemModifierGroups { get; set; }
    public virtual DbSet<RestaurantMenuItemTag> RestaurantMenuItemTags { get; set; }
    public virtual DbSet<RestaurantTableSession> RestaurantTableSessions { get; set; }
    public virtual DbSet<RestaurantReservation> RestaurantReservations { get; set; }
    public virtual DbSet<RestaurantReservationOtpChallenge> RestaurantReservationOtpChallenges { get; set; }
    public virtual DbSet<RestaurantPrintJob> RestaurantPrintJobs { get; set; }
    public virtual DbSet<RestaurantPrintRoute> RestaurantPrintRoutes { get; set; }
    public virtual DbSet<RestaurantPrintDevice> RestaurantPrintDevices { get; set; }
    public virtual DbSet<RestaurantPrintDeviceRoute> RestaurantPrintDeviceRoutes { get; set; }
    public virtual DbSet<RestaurantPrintDelivery> RestaurantPrintDeliveries { get; set; }
    public virtual DbSet<RestaurantSmsOutbox> RestaurantSmsOutbox { get; set; }
    public virtual DbSet<RestaurantOrder> RestaurantOrders { get; set; }
    public virtual DbSet<RestaurantOrderItem> RestaurantOrderItems { get; set; }
    public virtual DbSet<RestaurantOrderItemModifier> RestaurantOrderItemModifiers { get; set; }
    public virtual DbSet<RestaurantBillLine> RestaurantBillLines { get; set; }
    public virtual DbSet<RestaurantBillPayment> RestaurantBillPayments { get; set; }
    public virtual DbSet<RestaurantBillTender> RestaurantBillTenders { get; set; }
    public virtual DbSet<RestaurantCashShift> RestaurantCashShifts { get; set; }
    public virtual DbSet<RestaurantCashMovement> RestaurantCashMovements { get; set; }
    public virtual DbSet<RestaurantRefund> RestaurantRefunds { get; set; }
    public virtual DbSet<RestaurantRefundLine> RestaurantRefundLines { get; set; }
    public virtual DbSet<RestaurantRefundTender> RestaurantRefundTenders { get; set; }
    public virtual DbSet<RestaurantRefundSettlement> RestaurantRefundSettlements { get; set; }
    public virtual DbSet<RestaurantRefundSettlementTender> RestaurantRefundSettlementTenders { get; set; }
    public virtual DbSet<RestaurantClientOperation> RestaurantClientOperations { get; set; }
    public virtual DbSet<RestaurantSetupAcknowledgement> RestaurantSetupAcknowledgements { get; set; }
    public virtual DbSet<RestaurantSupplierItemMapping> RestaurantSupplierItemMappings { get; set; }
    public virtual DbSet<RestaurantStockAdjustment> RestaurantStockAdjustments { get; set; }
    public virtual DbSet<RestaurantStockAdjustmentLine> RestaurantStockAdjustmentLines { get; set; }
    public virtual DbSet<RestaurantTicket> RestaurantTickets { get; set; }
    public virtual DbSet<RestaurantTicketItem> RestaurantTicketItems { get; set; }
    public virtual DbSet<RestaurantDevice> RestaurantDevices { get; set; }
    public virtual DbSet<RestaurantSyncUpload> RestaurantSyncUploads { get; set; }
    public virtual DbSet<RestaurantChannel> RestaurantChannels { get; set; }
    public virtual DbSet<RestaurantChannelAccount> RestaurantChannelAccounts { get; set; }
    public virtual DbSet<RestaurantChannelItem> RestaurantChannelItems { get; set; }
    public virtual DbSet<RestaurantMenuSyncLog> RestaurantMenuSyncLogs { get; set; }
    public virtual DbSet<RestaurantAggregatorOrder> RestaurantAggregatorOrders { get; set; }
    public virtual DbSet<RestaurantAggregatorPayout> RestaurantAggregatorPayouts { get; set; }
    public virtual DbSet<RestaurantAggregatorPayoutLine> RestaurantAggregatorPayoutLines { get; set; }
    public virtual DbSet<RestaurantPushToken> RestaurantPushTokens { get; set; }
    public virtual DbSet<RestaurantChangeLog> RestaurantChangeLogs { get; set; }
    public virtual DbSet<RestaurantSyncUploadBatch> RestaurantSyncUploadBatches { get; set; }
    public virtual DbSet<RestaurantPayrollDepartment> RestaurantPayrollDepartments { get; set; }
    public virtual DbSet<RestaurantPayrollJobRole> RestaurantPayrollJobRoles { get; set; }
    public virtual DbSet<RestaurantPayrollEmployee> RestaurantPayrollEmployees { get; set; }
    public virtual DbSet<RestaurantPayrollAllowanceHistory> RestaurantPayrollAllowanceHistories { get; set; }
    public virtual DbSet<RestaurantPayrollAttendance> RestaurantPayrollAttendances { get; set; }
    public virtual DbSet<RestaurantPayrollRun> RestaurantPayrollRuns { get; set; }
    public virtual DbSet<RestaurantPayrollLine> RestaurantPayrollLines { get; set; }
    // Added Tables End


    public ErpDbContext(DbContextOptions<ErpDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;

        // Configure self-referencing relationships to use NoAction for delete behavior
        ConfigureSelfReferencingRelationships(modelBuilder);

        // Fix potential circular relationships
        ConfigureCircularRelationships(modelBuilder);
        ConfigureRequiredClientCascadeRelationships(modelBuilder);

        modelBuilder.Entity<BinaryObject>(b => { b.HasIndex(e => new { e.TenantId }); });

        modelBuilder.Entity<AccountGroup>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Id });
            b.HasIndex(e => new { e.TenantId, e.GroupUnder });
        });

        modelBuilder.Entity<SubscriptionPayment>(x =>
        {
            var extraProperties = x.Property(u => u.ExtraProperties)
                .HasConversion(
                    d => SerializeExtraProperties(d),
                    s => DeserializeExtraProperties(s)
                );

            extraProperties.Metadata.SetValueComparer(ExtraPropertiesValueComparer);
        });

        modelBuilder.Entity<SubscriptionPaymentProduct>(x =>
        {
            var extraProperties = x.Property(u => u.ExtraProperties)
                .HasConversion(
                    d => SerializeExtraProperties(d),
                    s => DeserializeExtraProperties(s)
                );

            extraProperties.Metadata.SetValueComparer(ExtraPropertiesValueComparer);
        });

        modelBuilder.Entity<ChatMessage>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId, e.ReadState });
            b.HasIndex(e => new { e.TenantId, e.TargetUserId, e.ReadState });
            b.HasIndex(e => new { e.TargetTenantId, e.TargetUserId, e.ReadState });
            b.HasIndex(e => new { e.TargetTenantId, e.UserId, e.ReadState });
        });

        modelBuilder.Entity<Friendship>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId });
            b.HasIndex(e => new { e.TenantId, e.FriendUserId });
            b.HasIndex(e => new { e.FriendTenantId, e.UserId });
            b.HasIndex(e => new { e.FriendTenantId, e.FriendUserId });
        });

        modelBuilder.Entity<RestaurantSupplierItemMapping>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.SupplierLedgerId });
            b.HasIndex(e => new { e.TenantId, e.ProductId })
                .HasDatabaseName("IX_tbl_RestaurantSupplierItemMapping_PreferredActive")
                .HasFilter("[IsPreferred] = 1 AND [IsActive] = 1 AND [IsDeleted] = 0")
                .IsUnique();
        });

        modelBuilder.Entity<RestaurantCashShift>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.RegisterName }).HasFilter("[IsClosed] = 0").IsUnique();
            b.HasIndex(e => new { e.TenantId, e.OpenedByUserId }).HasFilter("[IsClosed] = 0").IsUnique();
        });

        modelBuilder.Entity<RestaurantCashMovement>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.CashShiftId, e.CreatedAt });
            b.HasOne(e => e.CashShiftFk).WithMany().HasForeignKey(e => e.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantBillTender>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.BillPaymentId });
            b.HasIndex(e => new { e.TenantId, e.CashShiftId });
            b.HasOne(e => e.BillPaymentFk).WithMany().HasForeignKey(e => e.BillPaymentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.CashShiftFk).WithMany().HasForeignKey(e => e.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantBillPayment>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ClientRequestId })
                .HasFilter("[ClientRequestId] IS NOT NULL")
                .IsUnique();
            b.HasOne(e => e.CashShiftFk).WithMany().HasForeignKey(e => e.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantPrintRoute>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
        });

        modelBuilder.Entity<RestaurantPrintDevice>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ClientDeviceId }).IsUnique();
        });

        modelBuilder.Entity<RestaurantPrintDeviceRoute>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.DeviceId, e.RouteName }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.RouteName });
        });

        modelBuilder.Entity<RestaurantPrintDelivery>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.PrintJobId, e.DeviceId }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.DeviceId, e.Status, e.CreatedAtUtc });
        });

        modelBuilder.Entity<RestaurantOrder>(b =>
        {
            b.Property(e => e.RowVersion).IsRowVersion();
            b.HasIndex(e => new { e.TenantId, e.GuestClientRequestId })
                .HasFilter("[GuestClientRequestId] IS NOT NULL")
                .IsUnique();
            b.HasIndex(e => new { e.TenantId, e.PosClientRequestId })
                .HasFilter("[PosClientRequestId] IS NOT NULL")
                .IsUnique();
        });

        modelBuilder.Entity<RestaurantRefund>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ClientRequestId }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.SalesMasterId, e.Status });
            b.HasOne(e => e.OrderFk).WithMany().HasForeignKey(e => e.RestaurantOrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.SalesMasterFk).WithMany().HasForeignKey(e => e.SalesMasterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.SalesReturnMasterFk).WithMany().HasForeignKey(e => e.SalesReturnMasterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.TipSalesReturnMasterFk).WithMany().HasForeignKey(e => e.TipSalesReturnMasterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.CreditNoteSalesReturnMasterFk).WithMany().HasForeignKey(e => e.CreditNoteSalesReturnMasterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantRefundLine>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.RefundId });
            b.HasIndex(e => new { e.TenantId, e.SalesDetailId });
            b.HasOne(e => e.RefundFk).WithMany().HasForeignKey(e => e.RefundId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantRefundTender>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.RefundId });
            b.HasOne(e => e.RefundFk).WithMany().HasForeignKey(e => e.RefundId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantRefundSettlement>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ClientRequestId }).IsUnique();
            b.HasOne(e => e.RefundFk).WithMany().HasForeignKey(e => e.RefundId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.CashShiftFk).WithMany().HasForeignKey(e => e.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantRefundSettlementTender>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.SettlementId });
            b.HasOne(e => e.SettlementFk).WithMany().HasForeignKey(e => e.SettlementId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.RefundTenderFk).WithMany().HasForeignKey(e => e.RefundTenderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantClientOperation>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.UserId, e.ClientRequestId }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.OperationType, e.CreatedAt });
        });

        modelBuilder.Entity<RestaurantSetupAcknowledgement>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.CheckKey }).IsUnique();
        });

        modelBuilder.Entity<RestaurantStockAdjustment>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Date });
            b.HasIndex(e => new { e.TenantId, e.AdjustmentType });
        });

        modelBuilder.Entity<RestaurantStockAdjustmentLine>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ProductId });
            b.HasIndex(e => new { e.TenantId, e.StockAdjustmentId });
        });

        modelBuilder.Entity<RestaurantChannel>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Provider });
            b.HasIndex(e => new { e.TenantId, e.ChannelType });
        });

        modelBuilder.Entity<RestaurantChannelAccount>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ChannelId });
            b.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalStoreId });
        });

        modelBuilder.Entity<RestaurantChannelItem>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ChannelId, e.MenuItemId })
                .HasDatabaseName("IX_tbl_RestaurantChannelItem_ChannelMenuItem_Active")
                .HasFilter("[IsDeleted] = 0")
                .IsUnique();
            b.HasIndex(e => new { e.TenantId, e.ExternalItemId });
        });

        modelBuilder.Entity<RestaurantMenuSyncLog>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.ChannelId, e.CreatedAt });
            b.HasIndex(e => new { e.TenantId, e.Status });
        });

        modelBuilder.Entity<RestaurantAggregatorOrder>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalOrderId })
                .HasDatabaseName("IX_tbl_RestaurantAggregatorOrder_ProviderExternalOrder")
                .IsUnique();
            b.HasIndex(e => new { e.TenantId, e.Status, e.ReceivedAt });
            b.HasIndex(e => new { e.TenantId, e.OrderId });
        });

        modelBuilder.Entity<RestaurantAggregatorPayout>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalPayoutId });
            b.HasIndex(e => new { e.TenantId, e.PeriodFrom, e.PeriodTo });
        });

        modelBuilder.Entity<RestaurantAggregatorPayoutLine>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.PayoutId });
            b.HasIndex(e => new { e.TenantId, e.ExternalOrderId });
            b.HasIndex(e => new { e.TenantId, e.MatchStatus });
        });

        modelBuilder.Entity<RestaurantPushToken>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.DeviceId });
            b.HasIndex(e => new { e.TenantId, e.Token });
        });

        modelBuilder.Entity<RestaurantChangeLog>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Seq })
                .HasDatabaseName("IX_tbl_RestaurantChangeLog_TenantSeq")
                .IsUnique();
            b.HasIndex(e => new { e.TenantId, e.EntityType, e.Seq });
        });

        modelBuilder.Entity<RestaurantSyncUploadBatch>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.BatchGuid })
                .HasDatabaseName("IX_tbl_RestaurantSyncUploadBatch_BatchGuid")
                .IsUnique();
            b.HasIndex(e => new { e.TenantId, e.DeviceId, e.ReceivedAt });
        });

        modelBuilder.Entity<RestaurantPayrollDepartment>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.IsActive, e.SortOrder });
        });

        modelBuilder.Entity<RestaurantPayrollJobRole>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.DepartmentId, e.Name }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.DepartmentId, e.IsActive, e.SortOrder });
            b.HasOne(e => e.DepartmentFk).WithMany(e => e.JobRoles)
                .HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantPayrollEmployee>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.StaffCode }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.UserId })
                .HasFilter("[UserId] IS NOT NULL")
                .IsUnique();
            b.HasIndex(e => new { e.TenantId, e.DepartmentId, e.IsActive });
            b.HasIndex(e => new { e.TenantId, e.JobRoleId, e.IsActive });
            b.HasOne(e => e.DepartmentFk).WithMany()
                .HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(e => e.JobRoleFk).WithMany()
                .HasForeignKey(e => e.JobRoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantPayrollAllowanceHistory>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.EffectiveFrom }).IsUnique();
            b.HasOne(e => e.EmployeeFk).WithMany()
                .HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestaurantPayrollAttendance>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.EmployeeId, e.WorkDate }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.WorkDate, e.Status });
        });

        modelBuilder.Entity<RestaurantPayrollRun>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.RunNumber }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.PeriodStart, e.PeriodEnd }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.Status, e.PeriodEnd });
        });

        modelBuilder.Entity<RestaurantPayrollLine>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.PayrollRunId, e.EmployeeId }).IsUnique();
            b.HasIndex(e => new { e.TenantId, e.EmployeeId });
        });

        modelBuilder.Entity<Tenant>(b =>
        {
            b.HasIndex(e => new { e.SubscriptionEndDateUtc });
            b.HasIndex(e => new { e.CreationTime });
        });

        modelBuilder.Entity<SubscriptionPayment>(b =>
        {
            b.HasIndex(e => new { e.Status, e.CreationTime });
            b.HasIndex(e => new { PaymentId = e.ExternalPaymentId, e.Gateway });
        });

        modelBuilder.Entity<UserDelegation>(b =>
        {
            b.HasIndex(e => new { e.TenantId, e.SourceUserId });
            b.HasIndex(e => new { e.TenantId, e.TargetUserId });
        });

        modelBuilder.ConfigureOpenIddict();

        ConfigureDecimalPrecision(modelBuilder);
    }

    private void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entityType => entityType.GetProperties())
                     .Where(property =>
                     {
                         var propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                         return propertyType == typeof(decimal)
                                && property.GetColumnType() == null
                                && property.GetPrecision() == null;
                     }))
        {
            property.SetPrecision(DecimalPrecision);
            property.SetScale(DecimalScale);
        }
    }

    private static string SerializeExtraProperties(ExtraPropertyDictionary extraProperties)
    {
        return JsonSerializer.Serialize(
            extraProperties ?? new ExtraPropertyDictionary(),
            ExtraPropertiesJsonSerializerOptions);
    }

    private static ExtraPropertyDictionary DeserializeExtraProperties(string extraProperties)
    {
        if (string.IsNullOrWhiteSpace(extraProperties))
        {
            return new ExtraPropertyDictionary();
        }

        return JsonSerializer.Deserialize<ExtraPropertyDictionary>(
                   extraProperties,
                   ExtraPropertiesJsonSerializerOptions)
               ?? new ExtraPropertyDictionary();
    }

    private void ConfigureSelfReferencingRelationships(ModelBuilder modelBuilder)
    {
        // Configure self-referencing entities like AccountGroup, AccountLedger, etc.
        // to use NoAction for delete behavior

        // AccountLedger self-reference through ParentId
        if (modelBuilder.Model.FindEntityType(typeof(AccountLedger)) != null)
        {
            var selfReference = modelBuilder.Model.FindEntityType(typeof(AccountLedger))
                ?.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(AccountLedger));

            if (selfReference != null)
                selfReference.DeleteBehavior = DeleteBehavior.NoAction;
        }

        // AccountGroup self-reference through GroupUnder
        if (modelBuilder.Model.FindEntityType(typeof(AccountGroup)) != null)
        {
            var selfReference = modelBuilder.Model.FindEntityType(typeof(AccountGroup))
                ?.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(AccountGroup));

            if (selfReference != null)
                selfReference.DeleteBehavior = DeleteBehavior.NoAction;
        }

        // AccountGroup self-reference through GroupUnder
        if (modelBuilder.Model.FindEntityType(typeof(ProductGroup)) != null)
        {
            var selfReference = modelBuilder.Model.FindEntityType(typeof(ProductGroup))
                ?.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ProductGroup));

            if (selfReference != null)
                selfReference.DeleteBehavior = DeleteBehavior.NoAction;
        }

        // Branch self-reference through BranchId
        if (modelBuilder.Model.FindEntityType(typeof(Branch)) != null)
        {
            var selfReference = modelBuilder.Model.FindEntityType(typeof(Branch))
                ?.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Branch));

            if (selfReference != null)
                selfReference.DeleteBehavior = DeleteBehavior.NoAction;
        }


        // OrganizationUnit self-reference through ParentId (AbpOrganizationUnits)
        // This fixes the "may cause cycles or multiple cascade paths" error
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (entityType.ClrType.Name == "OrganizationUnit" ||
                entityType.Name.Contains("OrganizationUnit") ||
                entityType.Name.Contains("AbpOrganizationUnits"))
                foreach (var fk in entityType.GetForeignKeys())
                    // For self-referencing relationships (like ParentId)
                    if (fk.PrincipalEntityType == entityType)
                        // Set NoAction delete behavior to prevent cycles
                        fk.DeleteBehavior = DeleteBehavior.NoAction;
    }

    private void ConfigureCircularRelationships(ModelBuilder modelBuilder)
    {
        // Configure User related circular references
        ConfigureUserRelatedEntities(modelBuilder);

        // Configure master-detail relationships that can form cycles
        //    ConfigureMasterDetailRelationships(modelBuilder);
    }

    private void ConfigureRequiredClientCascadeRelationships(ModelBuilder modelBuilder)
    {
        var userTokenForeignKey = modelBuilder.Model
            .FindEntityType(typeof(Abp.Authorization.Users.UserToken))
            ?.GetForeignKeys()
            .FirstOrDefault(fk =>
                fk.PrincipalEntityType.ClrType == typeof(User) &&
                fk.Properties.Any(property => property.Name == "UserId"));

        if (userTokenForeignKey != null)
        {
            userTokenForeignKey.DeleteBehavior = DeleteBehavior.ClientCascade;
        }
    }

    private void ConfigureUserRelatedEntities(ModelBuilder modelBuilder)
    {
        // User/Tenant and UserBranch relationships
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Find foreign keys that reference User entity or any user-related entity
            var userFks = entityType.GetForeignKeys()
                .Where(fk => fk.PrincipalEntityType.ClrType == typeof(User) ||
                             fk.PrincipalEntityType.Name.Contains("User"));

            foreach (var fk in userFks)
            {
                // For entities that can form cycles with User, set DeleteBehavior.NoAction
                // Especially when User has relationships with Role, Tenant, or UserBranch
                if (entityType.ClrType == typeof(UserBranch) ||
                    entityType.ClrType == typeof(Tenant) ||
                    entityType.Name.Contains("Tenant") ||
                    entityType.Name.Contains("Role") ||
                    entityType.Name.Contains("Permission") ||
                    entityType.Name.Contains("OrganizationUnit") ||
                    entityType.Name.Contains("UserOrganizationUnit"))
                    fk.DeleteBehavior = DeleteBehavior.NoAction;

                // For Creator/Modifier/Deleter references to User
                string[] propertyNames = { "CreatorUserId", "LastModifierUserId", "DeleterUserId" };
                if (fk.Properties.Any(p => propertyNames.Contains(p.Name))) fk.DeleteBehavior = DeleteBehavior.NoAction;
            }

            // Check for OrganizationUnit related FKs
            var organizationFKs = entityType.GetForeignKeys()
                .Where(fk => fk.PrincipalEntityType.Name.Contains("OrganizationUnit"));

            foreach (var fk in organizationFKs)
                // Ensure OrganizationUnit relationships use NoAction delete behavior
                fk.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }

    //private void ConfigureMasterDetailRelationships(ModelBuilder modelBuilder)
    //{
    //    // Handle master-detail relationships that can form cycles
    //    var circularMasterDetailPairs = new[]
    //    {
    //        // SalesMaster-SalesDetails cycle
    //        new { DetailType = typeof(SalesDetail), MasterType = typeof(SalesMaster) },
    //        // PurchaseMaster-PurchaseDetail cycle
    //        new { DetailType = typeof(PurchaseDetail), MasterType = typeof(PurchaseMaster) },
    //        // MaterialReceiptMaster-MaterialReceiptDetail cycle
    //        new { DetailType = typeof(MaterialReceiptDetail), MasterType = typeof(MaterialReceiptMaster) },
    //        // StockJournalMaster-StockJournalDetails cycle
    //        new { DetailType = typeof(StockJournalDetails), MasterType = typeof(StockJournalMaster) },
    //        // StockTransfer-StockTransferDetail cycle
    //        new { DetailType = typeof(StockTransferDetail), MasterType = typeof(StockTransfer) },
    //        // PhysicalStockMaster-PhysicalStockDetail cycle
    //        new { DetailType = typeof(PhysicalStockDetail), MasterType = typeof(PhysicalStockMaster) },
    //        // ServiceReceipt-ServiceReceiptDetail cycle
    //        new { DetailType = typeof(ServiceReceiptDetail), MasterType = typeof(ServiceReceipt) },
    //        // ServiceDelivery-ServiceDeliveryDetail cycle
    //        new { DetailType = typeof(ServiceDeliveryDetail), MasterType = typeof(ServiceDelivery) },
    //        // WarrantyTransfer-WarrantyTransferDetail cycle
    //        new { DetailType = typeof(WarrantyTransferDetail), MasterType = typeof(WarrantyTransfer) }
    //    };

    //    foreach (var pair in circularMasterDetailPairs)
    //    {
    //        var detailType = modelBuilder.Model.FindEntityType(pair.DetailType);
    //        if (detailType != null)
    //        {
    //            var masterFks = detailType.GetForeignKeys()
    //                .Where(fk => fk.PrincipalEntityType.ClrType == pair.MasterType);

    //            foreach (var fk in masterFks)
    //                // Set NoAction for master-detail relationships to prevent cycles
    //                fk.DeleteBehavior = DeleteBehavior.NoAction;
    //        }
    //    }
    //}
}

