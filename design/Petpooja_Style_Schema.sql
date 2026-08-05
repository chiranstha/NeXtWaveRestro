/* =============================================================================
   PETPOOJA-STYLE RESTAURANT POS  -  SQL Server schema
   Stack    : .NET 8 / ASP.NET Zero (ABP) + EF Core + SQL Server
   Modeled on Petpooja's footprint, with the signature modules emphasized:
     • Multi-channel + AGGREGATOR integration (Zomato/Swiggy ≈ Foodmandu/Pathao)
       with per-channel menu mapping, online ON/OFF, and PAYOUT RECONCILIATION
     • CENTRAL KITCHEN (hub-and-spoke): production, indents, stock transfers
     • MULTI-STAGE RECIPES + food costing (FIFO)
     • HEAD-OFFICE: brands → zones → outlets, staff rights, SaaS licensing
     • Outbound ERP/Tally INTEGRATION (data-lake push)
   Localized : Nepal VAT 13% / IRD-CBMS / NPR / eSewa-Khalti-IME-Fonepay.
   Convention: FullAuditedEntity<long> + IMustHaveTenant (audit block on each).
              Money decimal(18,4); Qty decimal(18,3); %/rate decimal(9,4).
              Business docs carry AD (datetime2 UTC) + BS (yyyy-MM-dd) dates.
   ============================================================================= */

IF SCHEMA_ID('hq')            IS NULL EXEC('CREATE SCHEMA [hq]');
IF SCHEMA_ID('catalog')       IS NULL EXEC('CREATE SCHEMA [catalog]');
IF SCHEMA_ID('channel')       IS NULL EXEC('CREATE SCHEMA [channel]');
IF SCHEMA_ID('sales')         IS NULL EXEC('CREATE SCHEMA [sales]');
IF SCHEMA_ID('inventory')     IS NULL EXEC('CREATE SCHEMA [inventory]');
IF SCHEMA_ID('centralkitchen')IS NULL EXEC('CREATE SCHEMA [centralkitchen]');
IF SCHEMA_ID('crm')           IS NULL EXEC('CREATE SCHEMA [crm]');
IF SCHEMA_ID('hr')            IS NULL EXEC('CREATE SCHEMA [hr]');
IF SCHEMA_ID('finance')       IS NULL EXEC('CREATE SCHEMA [finance]');
IF SCHEMA_ID('integration')   IS NULL EXEC('CREATE SCHEMA [integration]');
GO

/* =============================================================================
   1. HQ  -  brands, zones, outlets, head office, SaaS licensing, staff rights
   (Petpooja: group outlets by region/state/city/brand/channel; head-office
    dashboard; outlet-wise staff rights.)
   ============================================================================= */

CREATE TABLE hq.Brands (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    LegalName NVARCHAR(200) NULL,
    LogoUrl NVARCHAR(512) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL
);

CREATE TABLE hq.Zones (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    BrandId BIGINT NULL,
    Name NVARCHAR(96) NOT NULL,                   -- 'Kathmandu Valley','Pokhara','Cloud Kitchens'
    ZoneType TINYINT NOT NULL DEFAULT 1,          -- 1=Region,2=City,3=Brand,4=Channel
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Zone_Brand FOREIGN KEY (BrandId) REFERENCES hq.Brands(Id)
);

CREATE TABLE hq.Outlets (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    BrandId BIGINT NULL,
    ZoneId BIGINT NULL,
    Code NVARCHAR(20) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    OutletType TINYINT NOT NULL DEFAULT 1,        -- 1=Restaurant,2=QSR,3=Cafe,4=CloudKitchen,5=Bar,6=CentralKitchen
    Pan NVARCHAR(20) NULL,                         -- IRD PAN/VAT
    IsVatRegistered BIT NOT NULL DEFAULT 1,
    IsCentralKitchen BIT NOT NULL DEFAULT 0,
    AddressLine NVARCHAR(256) NULL,
    District NVARCHAR(64) NULL, Municipality NVARCHAR(96) NULL, WardNo NVARCHAR(10) NULL,
    Phone NVARCHAR(32) NULL,
    Latitude DECIMAL(9,6) NULL, Longitude DECIMAL(9,6) NULL,
    DefaultVatRate DECIMAL(9,4) NOT NULL DEFAULT 13.0000,
    DefaultServiceChargeRate DECIMAL(9,4) NOT NULL DEFAULT 10.0000,
    Currency NVARCHAR(8) NOT NULL DEFAULT 'NPR',
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Outlet_Zone FOREIGN KEY (ZoneId) REFERENCES hq.Zones(Id),
    CONSTRAINT UQ_Outlet_Code UNIQUE (TenantId, Code)
);

-- which central kitchen supplies which outlet (hub-and-spoke wiring)
CREATE TABLE hq.OutletSupplyLinks (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,                      -- spoke
    CentralKitchenOutletId BIGINT NOT NULL,        -- hub
    IsPrimary BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Supply_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_Supply_CK FOREIGN KEY (CentralKitchenOutletId) REFERENCES hq.Outlets(Id)
);

-- SaaS licensing (Petpooja is itself a subscription; model module entitlements)
CREATE TABLE hq.Subscriptions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    PlanName NVARCHAR(64) NOT NULL,
    OutletQuota INT NOT NULL DEFAULT 1,
    StartDateAd date NOT NULL, EndDateAd date NOT NULL,
    Status TINYINT NOT NULL DEFAULT 1,             -- 1=Active,2=Expired,3=Suspended
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL
);

CREATE TABLE hq.ModuleEntitlements (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NULL,                          -- NULL = tenant-wide
    ModuleCode NVARCHAR(40) NOT NULL,              -- 'Inventory','CentralKitchen','Aggregator','CRM','Reports'
    IsEnabled BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Module UNIQUE (TenantId, OutletId, ModuleCode)
);

-- staff rights / anti-pilferage (outlet-wise permissions beyond ABP roles)
CREATE TABLE hr.StaffRights (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    UserId BIGINT NOT NULL,
    RightCode NVARCHAR(60) NOT NULL,               -- 'Bill.Void','Discount.Apply','Item.Cancel','Reports.View'
    IsGranted BIT NOT NULL DEFAULT 1,
    RequiresPin BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Right UNIQUE (TenantId, OutletId, UserId, RightCode)
);
GO

/* =============================================================================
   2. CATALOG  -  menu, variations, add-ons, combos, item tags
   (Petpooja: categories, variations, add-ons, combos; tags like veg/spicy/
    gluten-free associated with items.)
   ============================================================================= */

CREATE TABLE catalog.Categories (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    BrandId BIGINT NULL,                           -- menu can be brand-scoped
    ParentId BIGINT NULL,
    Name NVARCHAR(128) NOT NULL, NameNp NVARCHAR(128) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    DefaultKitchenStationId BIGINT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Cat_Parent FOREIGN KEY (ParentId) REFERENCES catalog.Categories(Id)
);

CREATE TABLE catalog.Items (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    CategoryId BIGINT NOT NULL,
    Sku NVARCHAR(40) NULL, ShortCode NVARCHAR(20) NULL,  -- biller quick-code
    Name NVARCHAR(160) NOT NULL, NameNp NVARCHAR(160) NULL,
    Description NVARCHAR(512) NULL,
    ItemType TINYINT NOT NULL DEFAULT 1,           -- 1=Standard,2=Combo,3=Open/Weighed,4=SemiFinished
    BasePrice DECIMAL(18,4) NOT NULL DEFAULT 0,
    StationType TINYINT NOT NULL DEFAULT 1,        -- 1=Kitchen(KOT),2=Bar(BOT)
    IsVatable BIT NOT NULL DEFAULT 1,
    IsServiceChargeable BIT NOT NULL DEFAULT 1,
    HasVariations BIT NOT NULL DEFAULT 0,
    HasAddons BIT NOT NULL DEFAULT 0,
    TrackInventory BIT NOT NULL DEFAULT 0,
    IsWeighed BIT NOT NULL DEFAULT 0,              -- weighing-scale items
    ThumbnailUrl NVARCHAR(512) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Item_Cat FOREIGN KEY (CategoryId) REFERENCES catalog.Categories(Id),
    CONSTRAINT UQ_Item_Sku UNIQUE (TenantId, Sku)
);
CREATE INDEX IX_Items_Cat ON catalog.Items(TenantId, CategoryId, IsActive);

CREATE TABLE catalog.Variations (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ItemId BIGINT NOT NULL,
    Name NVARCHAR(64) NOT NULL,                    -- Small/Medium/Large, Half/Full
    PriceDelta DECIMAL(18,4) NOT NULL DEFAULT 0,
    IsAbsolutePrice BIT NOT NULL DEFAULT 0,
    IsDefault BIT NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Var_Item FOREIGN KEY (ItemId) REFERENCES catalog.Items(Id)
);

CREATE TABLE catalog.AddonGroups (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    MinSelect INT NOT NULL DEFAULT 0, MaxSelect INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE catalog.Addons (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    AddonGroupId BIGINT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    Price DECIMAL(18,4) NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Addon_Group FOREIGN KEY (AddonGroupId) REFERENCES catalog.AddonGroups(Id)
);

CREATE TABLE catalog.ItemAddonGroups (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ItemId BIGINT NOT NULL,
    AddonGroupId BIGINT NOT NULL,
    CONSTRAINT FK_IAG_Item FOREIGN KEY (ItemId) REFERENCES catalog.Items(Id),
    CONSTRAINT FK_IAG_Group FOREIGN KEY (AddonGroupId) REFERENCES catalog.AddonGroups(Id),
    CONSTRAINT UQ_IAG UNIQUE (ItemId, AddonGroupId)
);

CREATE TABLE catalog.ComboComponents (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ComboItemId BIGINT NOT NULL,
    ComponentItemId BIGINT NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
    IsSwappable BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Combo_Parent FOREIGN KEY (ComboItemId) REFERENCES catalog.Items(Id),
    CONSTRAINT FK_Combo_Comp FOREIGN KEY (ComponentItemId) REFERENCES catalog.Items(Id)
);

-- attribute tags: veg / spicy / gluten-free / jain / bestseller
CREATE TABLE catalog.Tags (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(48) NOT NULL,
    ColorHex NVARCHAR(9) NULL
);
CREATE TABLE catalog.ItemTags (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ItemId BIGINT NOT NULL, TagId BIGINT NOT NULL,
    CONSTRAINT FK_ItemTag_Item FOREIGN KEY (ItemId) REFERENCES catalog.Items(Id),
    CONSTRAINT FK_ItemTag_Tag FOREIGN KEY (TagId) REFERENCES catalog.Tags(Id),
    CONSTRAINT UQ_ItemTag UNIQUE (ItemId, TagId)
);

CREATE TABLE catalog.KitchenStations (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    Name NVARCHAR(64) NOT NULL,                    -- Hot Kitchen / Tandoor / Bar
    StationType TINYINT NOT NULL DEFAULT 1,        -- 1=Kitchen,2=Bar
    PrinterRef NVARCHAR(96) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Station_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id)
);
GO

/* =============================================================================
   3. CHANNEL  -  sales channels + AGGREGATOR integration  (the Petpooja core)
   (Petpooja: integrate Zomato/Swiggy; centrally manage online menus & prices;
    toggle online menu ON/OFF; upload payout reports to see commissions etc.)
   For Nepal: Foodmandu, Pathao, Bhojdeals + own website/app.
   ============================================================================= */

CREATE TABLE channel.Channels (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(64) NOT NULL,                    -- Dine-In, Takeaway, Delivery, Foodmandu, Pathao, OwnApp
    ChannelType TINYINT NOT NULL,                  -- 1=DineIn,2=Takeaway,3=Delivery,4=Aggregator,5=OwnOnline
    IsOnline BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

-- one connected aggregator account per outlet (Zomato/Swiggy ≈ Foodmandu/Pathao)
CREATE TABLE channel.AggregatorAccounts (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    ChannelId BIGINT NOT NULL,
    Provider NVARCHAR(40) NOT NULL,                -- 'foodmandu','pathao','bhojdeals','zomato'
    ExternalRestaurantId NVARCHAR(96) NULL,        -- the outlet's id on the aggregator
    CommissionPercent DECIMAL(9,4) NOT NULL DEFAULT 0,
    IsOnline BIT NOT NULL DEFAULT 1,               -- master online ON/OFF for this storefront
    ApiCredentialsJson NVARCHAR(MAX) NULL,         -- encrypted tokens/keys
    LastMenuPushAt datetime2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_AggAcct_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_AggAcct_Channel FOREIGN KEY (ChannelId) REFERENCES channel.Channels(Id),
    CONSTRAINT UQ_AggAcct UNIQUE (TenantId, OutletId, ChannelId, Provider)
);

-- per-channel menu mapping + pricing + online toggle (the "manage one menu,
-- publish to all aggregators with their own price & on/off" feature)
CREATE TABLE channel.ChannelItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    ChannelId BIGINT NOT NULL,
    ItemId BIGINT NOT NULL,
    VariationId BIGINT NULL,
    ExternalItemId NVARCHAR(96) NULL,              -- item id on the aggregator
    ChannelPrice DECIMAL(18,4) NOT NULL,           -- often higher than dine-in to offset commission
    IsOnline BIT NOT NULL DEFAULT 1,               -- per-item ON/OFF on this channel
    PackagingCharge DECIMAL(18,4) NOT NULL DEFAULT 0,
    LastSyncedAt datetime2 NULL,
    SyncStatus TINYINT NOT NULL DEFAULT 0,         -- 0=InSync,1=PendingPush,2=Error
    CONSTRAINT FK_ChItem_Item FOREIGN KEY (ItemId) REFERENCES catalog.Items(Id),
    CONSTRAINT FK_ChItem_Channel FOREIGN KEY (ChannelId) REFERENCES channel.Channels(Id),
    CONSTRAINT UQ_ChItem UNIQUE (TenantId, OutletId, ChannelId, ItemId, VariationId)
);
CREATE INDEX IX_ChannelItems_Sync ON channel.ChannelItems(TenantId, OutletId, SyncStatus);

-- raw inbound aggregator order (ingested via webhook/poll, then converted to sales.Orders)
CREATE TABLE channel.AggregatorOrders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    AggregatorAccountId BIGINT NOT NULL,
    Provider NVARCHAR(40) NOT NULL,
    ExternalOrderId NVARCHAR(96) NOT NULL,
    OrderId BIGINT NULL,                           -- linked internal order once accepted
    Status TINYINT NOT NULL DEFAULT 1,             -- 1=Received,2=Accepted,3=Preparing,4=Ready,5=PickedUp,6=Delivered,7=Rejected,8=Cancelled
    GrossAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    AggregatorDiscount DECIMAL(18,4) NOT NULL DEFAULT 0,   -- borne by platform
    RestaurantDiscount DECIMAL(18,4) NOT NULL DEFAULT 0,   -- borne by us
    CommissionAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    PayableToRestaurant DECIMAL(18,4) NULL,
    RawPayloadJson NVARCHAR(MAX) NULL,
    ReceivedAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AggOrder_Acct FOREIGN KEY (AggregatorAccountId) REFERENCES channel.AggregatorAccounts(Id),
    CONSTRAINT UQ_AggOrder UNIQUE (TenantId, Provider, ExternalOrderId)
);
CREATE INDEX IX_AggOrders_Status ON channel.AggregatorOrders(TenantId, OutletId, Status);

-- PAYOUT RECONCILIATION: upload the aggregator settlement report, line per order,
-- to see real commissions / discounts / cancellations / what was actually paid.
CREATE TABLE channel.AggregatorPayouts (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    AggregatorAccountId BIGINT NOT NULL,
    PayoutRef NVARCHAR(96) NULL,
    PeriodStartAd date NOT NULL, PeriodEndAd date NOT NULL,
    TotalGross DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalCommission DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalRestaurantDiscount DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalTaxesCollected DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalCancellations DECIMAL(18,4) NOT NULL DEFAULT 0,
    NetPayout DECIMAL(18,4) NOT NULL DEFAULT 0,
    IsReconciled BIT NOT NULL DEFAULT 0,
    UploadedFileName NVARCHAR(256) NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Payout_Acct FOREIGN KEY (AggregatorAccountId) REFERENCES channel.AggregatorAccounts(Id)
);

CREATE TABLE channel.AggregatorPayoutLines (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    PayoutId BIGINT NOT NULL,
    ExternalOrderId NVARCHAR(96) NOT NULL,
    AggregatorOrderId BIGINT NULL,                 -- matched internal record
    GrossAmount DECIMAL(18,4) NOT NULL,
    CommissionAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    RestaurantDiscount DECIMAL(18,4) NOT NULL DEFAULT 0,
    NetAmount DECIMAL(18,4) NOT NULL,
    IsCancelled BIT NOT NULL DEFAULT 0,
    MatchStatus TINYINT NOT NULL DEFAULT 1,        -- 1=Matched,2=Unmatched,3=Discrepancy
    CONSTRAINT FK_PayoutLine_Payout FOREIGN KEY (PayoutId) REFERENCES channel.AggregatorPayouts(Id)
);

-- log of menu pushes to aggregators (audit of online menu sync)
CREATE TABLE channel.MenuSyncLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    AggregatorAccountId BIGINT NOT NULL,
    Action NVARCHAR(40) NOT NULL,                  -- 'FullPush','PriceUpdate','ItemToggle'
    Status TINYINT NOT NULL DEFAULT 1,             -- 1=Pending,2=Success,3=Failed
    RequestPayload NVARCHAR(MAX) NULL, ResponsePayload NVARCHAR(MAX) NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/* =============================================================================
   4. SALES  -  tables, orders, KOT/BOT, billing, payments, discounts
   ============================================================================= */

CREATE TABLE sales.DiningTables (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    AreaName NVARCHAR(64) NULL,                     -- area/section
    Name NVARCHAR(32) NOT NULL,
    Seats INT NOT NULL DEFAULT 4,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Free,2=Running,3=Reserved,4=Printed,5=Cleaning
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Table_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT UQ_Table UNIQUE (TenantId, OutletId, Name)
);

CREATE TABLE sales.Orders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    ChannelId BIGINT NOT NULL,                      -- dine-in / aggregator / etc.
    AggregatorOrderId BIGINT NULL,                  -- set when ingested from a platform
    OrderNumber NVARCHAR(40) NOT NULL,
    OrderType TINYINT NOT NULL DEFAULT 1,           -- 1=DineIn,2=Takeaway,3=Delivery
    TableId BIGINT NULL,
    CustomerId BIGINT NULL,
    WaiterUserId BIGINT NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Running,2=KOTPrinted,3=Served,4=BillPrinted,5=Settled,6=Cancelled
    SubTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    ServiceChargeAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    PackagingCharge DECIMAL(18,4) NOT NULL DEFAULT 0,
    DeliveryCharge DECIMAL(18,4) NOT NULL DEFAULT 0,
    VatAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    RoundOff DECIMAL(18,4) NOT NULL DEFAULT 0,
    GrandTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    OrderDateAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    OrderDateBs NVARCHAR(10) NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Order_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_Order_Channel FOREIGN KEY (ChannelId) REFERENCES channel.Channels(Id),
    CONSTRAINT FK_Order_Table FOREIGN KEY (TableId) REFERENCES sales.DiningTables(Id),
    CONSTRAINT FK_Order_Agg FOREIGN KEY (AggregatorOrderId) REFERENCES channel.AggregatorOrders(Id),
    CONSTRAINT UQ_Order_Number UNIQUE (TenantId, OutletId, OrderNumber)
);
CREATE INDEX IX_Orders_Status ON sales.Orders(TenantId, OutletId, Status, OrderDateAd);

CREATE TABLE sales.OrderItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OrderId BIGINT NOT NULL,
    ItemId BIGINT NOT NULL,
    VariationId BIGINT NULL,
    ItemNameSnapshot NVARCHAR(160) NOT NULL,
    StationTypeSnapshot TINYINT NOT NULL,           -- KOT vs BOT routing
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,4) NOT NULL,
    AddonTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    SpecialNote NVARCHAR(256) NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=New,2=Fired,3=Ready,4=Served,5=Void
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_OItem_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id),
    CONSTRAINT FK_OItem_Item FOREIGN KEY (ItemId) REFERENCES catalog.Items(Id)
);
CREATE INDEX IX_OrderItems_Order ON sales.OrderItems(OrderId);

CREATE TABLE sales.OrderItemAddons (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OrderItemId BIGINT NOT NULL,
    AddonId BIGINT NOT NULL,
    AddonNameSnapshot NVARCHAR(96) NOT NULL,
    Price DECIMAL(18,4) NOT NULL DEFAULT 0,
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
    CONSTRAINT FK_OIA_Item FOREIGN KEY (OrderItemId) REFERENCES sales.OrderItems(Id),
    CONSTRAINT FK_OIA_Addon FOREIGN KEY (AddonId) REFERENCES catalog.Addons(Id)
);

-- KOT/BOT (category-wise KOTs; split/merge/move supported via these links)
CREATE TABLE sales.Kots (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    OrderId BIGINT NOT NULL,
    KitchenStationId BIGINT NULL,
    KotNumber NVARCHAR(40) NOT NULL,
    KotKind TINYINT NOT NULL DEFAULT 1,             -- 1=KOT(kitchen),2=BOT(bar)
    KotType TINYINT NOT NULL DEFAULT 1,             -- 1=New,2=Addon,3=Cancel,4=Reprint
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Queued,2=Preparing,3=Ready,4=Served,5=Cancelled
    FiredAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Kot_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id),
    CONSTRAINT UQ_Kot_Number UNIQUE (TenantId, OutletId, KotNumber)
);

CREATE TABLE sales.KotItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    KotId BIGINT NOT NULL,
    OrderItemId BIGINT NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    Status TINYINT NOT NULL DEFAULT 1,
    CONSTRAINT FK_KotItem_Kot FOREIGN KEY (KotId) REFERENCES sales.Kots(Id),
    CONSTRAINT FK_KotItem_OItem FOREIGN KEY (OrderItemId) REFERENCES sales.OrderItems(Id)
);

-- bill / invoice (Nepal materialized, gapless per fiscal year; IRD/CBMS in finance)
CREATE TABLE sales.Invoices (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    FiscalYearId BIGINT NOT NULL,
    OrderId BIGINT NOT NULL,
    InvoiceNumber NVARCHAR(40) NOT NULL,
    InvoiceType TINYINT NOT NULL DEFAULT 1,         -- 1=TaxInvoice,2=Abbreviated
    BuyerName NVARCHAR(160) NULL, BuyerPan NVARCHAR(20) NULL,
    SubTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    ServiceChargeAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    TaxableAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    VatAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    GrandTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    PaymentStatus TINYINT NOT NULL DEFAULT 1,       -- 1=Unpaid,2=Partial,3=Paid
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Active,2=Void
    InvoiceDateAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    InvoiceDateBs NVARCHAR(10) NOT NULL,
    CashierUserId BIGINT NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Invoice_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id),
    CONSTRAINT UQ_Invoice UNIQUE (TenantId, OutletId, FiscalYearId, InvoiceNumber)
);
CREATE INDEX IX_Invoices_Date ON sales.Invoices(TenantId, OutletId, InvoiceDateAd);

CREATE TABLE sales.PaymentMethods (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(64) NOT NULL,                     -- Cash, Card, eSewa, Khalti, IME, Fonepay, ConnectIPS, Credit
    MethodType TINYINT NOT NULL,                    -- 1=Cash,2=Card,3=Wallet,4=Bank,5=Credit
    GatewayCode NVARCHAR(40) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE sales.Payments (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    InvoiceId BIGINT NOT NULL,
    PaymentMethodId BIGINT NOT NULL,
    Amount DECIMAL(18,4) NOT NULL,
    ReferenceNo NVARCHAR(96) NULL,
    PaidAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CashierUserId BIGINT NULL,
    CONSTRAINT FK_Pay_Invoice FOREIGN KEY (InvoiceId) REFERENCES sales.Invoices(Id),
    CONSTRAINT FK_Pay_Method FOREIGN KEY (PaymentMethodId) REFERENCES sales.PaymentMethods(Id)
);

-- discount catalog + applied (with approval trail for anti-pilferage)
CREATE TABLE sales.Discounts (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    DiscountType TINYINT NOT NULL,                  -- 1=Percent,2=Flat
    Value DECIMAL(18,4) NOT NULL,
    AppliesTo TINYINT NOT NULL DEFAULT 1,           -- 1=Bill,2=Item
    RequiresApproval BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

-- void / cancellation audit (Petpooja anti-fraud reporting)
CREATE TABLE sales.VoidLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    OrderId BIGINT NULL, OrderItemId BIGINT NULL, InvoiceId BIGINT NULL,
    VoidType TINYINT NOT NULL,                      -- 1=ItemCancel,2=KOTCancel,3=BillVoid,4=DiscountOverride
    Amount DECIMAL(18,4) NULL,
    Reason NVARCHAR(256) NULL,
    ByUserId BIGINT NULL, ApprovedByUserId BIGINT NULL,
    AtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/* =============================================================================
   5. INVENTORY  -  raw materials, MULTI-STAGE recipes, suppliers, PO, FIFO
   (Petpooja: supplier + central-kitchen modules, multi-stage recipes,
    food-costing, FIFO, wastage, low-stock alerts on the billing screen.)
   ============================================================================= */

CREATE TABLE inventory.UnitOfMeasures (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(40) NOT NULL, Symbol NVARCHAR(12) NOT NULL,
    BaseUnitId BIGINT NULL, ConversionToBase DECIMAL(18,6) NOT NULL DEFAULT 1
);

CREATE TABLE inventory.RawMaterials (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Code NVARCHAR(40) NULL,
    Name NVARCHAR(160) NOT NULL, NameNp NVARCHAR(160) NULL,
    StockUomId BIGINT NOT NULL,
    Category NVARCHAR(64) NULL,
    IsSemiFinished BIT NOT NULL DEFAULT 0,          -- produced by central kitchen
    ReorderLevel DECIMAL(18,3) NULL,
    StandardCost DECIMAL(18,4) NOT NULL DEFAULT 0,
    ShelfLifeDays INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_RM_Uom FOREIGN KEY (StockUomId) REFERENCES inventory.UnitOfMeasures(Id)
);

CREATE TABLE inventory.StockLocations (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    LocationType TINYINT NOT NULL DEFAULT 1,        -- 1=Store,2=Kitchen,3=Bar
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Loc_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id)
);

-- MULTI-STAGE recipe: an item (or a semi-finished material) is produced from
-- ingredients across stages; sub-recipes reference other semi-finished materials.
CREATE TABLE inventory.Recipes (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ItemId BIGINT NULL,                             -- output is a menu item, OR
    OutputRawMaterialId BIGINT NULL,                -- output is a semi-finished material
    VariationId BIGINT NULL,
    YieldQty DECIMAL(18,3) NOT NULL DEFAULT 1,
    YieldUomId BIGINT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Recipe_Item FOREIGN KEY (ItemId) REFERENCES catalog.Items(Id),
    CONSTRAINT FK_Recipe_OutRM FOREIGN KEY (OutputRawMaterialId) REFERENCES inventory.RawMaterials(Id)
);

CREATE TABLE inventory.RecipeStages (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    RecipeId BIGINT NOT NULL,
    StageNo INT NOT NULL DEFAULT 1,
    StageName NVARCHAR(96) NULL,                     -- 'Marination','Cooking','Plating'
    CONSTRAINT FK_Stage_Recipe FOREIGN KEY (RecipeId) REFERENCES inventory.Recipes(Id)
);

CREATE TABLE inventory.RecipeLines (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    RecipeStageId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,                  -- ingredient (may itself be semi-finished)
    Quantity DECIMAL(18,3) NOT NULL,
    UomId BIGINT NOT NULL,
    WastagePercent DECIMAL(9,4) NOT NULL DEFAULT 0,
    CONSTRAINT FK_RL_Stage FOREIGN KEY (RecipeStageId) REFERENCES inventory.RecipeStages(Id),
    CONSTRAINT FK_RL_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id)
);

CREATE TABLE inventory.Suppliers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(160) NOT NULL,
    Pan NVARCHAR(20) NULL, Phone NVARCHAR(32) NULL, Address NVARCHAR(256) NULL,
    PaymentTermsDays INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL
);

-- PO can target an external supplier OR the central kitchen (Petpooja: raise/accept
-- PO tickets from suppliers or your central kitchen right from the dashboard).
CREATE TABLE inventory.PurchaseOrders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    SourceType TINYINT NOT NULL DEFAULT 1,          -- 1=Supplier,2=CentralKitchen
    SupplierId BIGINT NULL,
    CentralKitchenOutletId BIGINT NULL,
    PoNumber NVARCHAR(40) NOT NULL,
    OrderDateAd datetime2 NOT NULL, OrderDateBs NVARCHAR(10) NOT NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Draft,2=Sent,3=Accepted,4=PartiallyReceived,5=Received,6=Cancelled
    GrandTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_PO_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_PO_Supplier FOREIGN KEY (SupplierId) REFERENCES inventory.Suppliers(Id),
    CONSTRAINT UQ_PO UNIQUE (TenantId, OutletId, PoNumber)
);

CREATE TABLE inventory.PurchaseOrderLines (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    PurchaseOrderId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL, UomId BIGINT NOT NULL,
    UnitPrice DECIMAL(18,4) NOT NULL,
    ReceivedQty DECIMAL(18,3) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,4) NOT NULL,
    CONSTRAINT FK_POL_PO FOREIGN KEY (PurchaseOrderId) REFERENCES inventory.PurchaseOrders(Id),
    CONSTRAINT FK_POL_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id)
);

-- FIFO stock batches (Petpooja: FIFO inventory method)
CREATE TABLE inventory.StockBatches (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    StockLocationId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    BatchNo NVARCHAR(40) NULL,
    ReceivedQty DECIMAL(18,3) NOT NULL,
    RemainingQty DECIMAL(18,3) NOT NULL,
    UnitCost DECIMAL(18,4) NOT NULL,
    ReceivedAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiryDateAd date NULL,
    CONSTRAINT FK_Batch_Loc FOREIGN KEY (StockLocationId) REFERENCES inventory.StockLocations(Id),
    CONSTRAINT FK_Batch_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id)
);
CREATE INDEX IX_StockBatches_Fifo ON inventory.StockBatches(TenantId, StockLocationId, RawMaterialId, ReceivedAtAd);

-- immutable movement ledger (sale-depletion, receipt, transfer, wastage, production)
CREATE TABLE inventory.StockMovements (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    StockLocationId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    MovementType TINYINT NOT NULL,                  -- 1=Receipt,2=SaleConsume,3=Adjust,4=Wastage,5=TransferOut,6=TransferIn,7=Production
    QtyIn DECIMAL(18,3) NOT NULL DEFAULT 0,
    QtyOut DECIMAL(18,3) NOT NULL DEFAULT 0,
    UnitCost DECIMAL(18,4) NOT NULL DEFAULT 0,
    RefTable NVARCHAR(64) NULL, RefId BIGINT NULL,
    MovementAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Mov_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id),
    CONSTRAINT FK_Mov_Loc FOREIGN KEY (StockLocationId) REFERENCES inventory.StockLocations(Id)
);
CREATE INDEX IX_Movements_RM ON inventory.StockMovements(TenantId, RawMaterialId, StockLocationId, MovementAtAd);

CREATE TABLE inventory.StockBalances (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    StockLocationId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 0,
    AvgCost DECIMAL(18,4) NOT NULL DEFAULT 0,
    RowVersion ROWVERSION,
    CONSTRAINT UQ_Balance UNIQUE (StockLocationId, RawMaterialId)
);

CREATE TABLE inventory.Wastage (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    StockLocationId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    Reason NVARCHAR(96) NULL,                        -- Spoilage, Spillage, Expiry
    CostValue DECIMAL(18,4) NOT NULL DEFAULT 0,
    WastageAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ByUserId BIGINT NULL,
    CONSTRAINT FK_Waste_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id)
);
GO

/* =============================================================================
   6. CENTRAL KITCHEN  -  production, indents, transfers (hub-and-spoke)
   (Petpooja: operate a central kitchen in any city, supply raw/semi-cooked
    items to outlets; item conversion; delivery route planning.)
   ============================================================================= */

-- production / item conversion: consume ingredients -> produce semi-finished output
CREATE TABLE centralkitchen.ProductionOrders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    CentralKitchenOutletId BIGINT NOT NULL,
    RecipeId BIGINT NOT NULL,
    OutputRawMaterialId BIGINT NOT NULL,
    PlannedQty DECIMAL(18,3) NOT NULL,
    ProducedQty DECIMAL(18,3) NOT NULL DEFAULT 0,
    ConversionCost DECIMAL(18,4) NOT NULL DEFAULT 0,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Planned,2=InProgress,3=Completed
    ProductionDateAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ProductionDateBs NVARCHAR(10) NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Prod_CK FOREIGN KEY (CentralKitchenOutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_Prod_Recipe FOREIGN KEY (RecipeId) REFERENCES inventory.Recipes(Id),
    CONSTRAINT FK_Prod_OutRM FOREIGN KEY (OutputRawMaterialId) REFERENCES inventory.RawMaterials(Id)
);

-- indent: an outlet requests supply from its central kitchen
CREATE TABLE centralkitchen.Indents (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    RequestingOutletId BIGINT NOT NULL,
    CentralKitchenOutletId BIGINT NOT NULL,
    IndentNumber NVARCHAR(40) NOT NULL,
    RequiredByAd date NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Requested,2=Approved,3=Dispatched,4=Received,5=Rejected
    IndentDateAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IndentDateBs NVARCHAR(10) NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Indent_Req FOREIGN KEY (RequestingOutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_Indent_CK FOREIGN KEY (CentralKitchenOutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT UQ_Indent UNIQUE (TenantId, IndentNumber)
);

CREATE TABLE centralkitchen.IndentLines (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    IndentId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    RequestedQty DECIMAL(18,3) NOT NULL,
    ApprovedQty DECIMAL(18,3) NULL,
    UomId BIGINT NOT NULL,
    CONSTRAINT FK_IndentLine_Indent FOREIGN KEY (IndentId) REFERENCES centralkitchen.Indents(Id),
    CONSTRAINT FK_IndentLine_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id)
);

-- stock transfer order: central kitchen dispatches to an outlet (fulfils an indent)
CREATE TABLE centralkitchen.StockTransfers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    FromOutletId BIGINT NOT NULL,                   -- central kitchen
    ToOutletId BIGINT NOT NULL,                     -- spoke
    IndentId BIGINT NULL,
    TransferNumber NVARCHAR(40) NOT NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Draft,2=Dispatched,3=InTransit,4=Received,5=PartialReceived
    DeliveryRouteRef NVARCHAR(96) NULL,             -- route/vehicle plan
    DispatchedAtAd datetime2 NULL, ReceivedAtAd datetime2 NULL,
    TotalCostValue DECIMAL(18,4) NOT NULL DEFAULT 0,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Transfer_From FOREIGN KEY (FromOutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_Transfer_To FOREIGN KEY (ToOutletId) REFERENCES hq.Outlets(Id),
    CONSTRAINT FK_Transfer_Indent FOREIGN KEY (IndentId) REFERENCES centralkitchen.Indents(Id),
    CONSTRAINT UQ_Transfer UNIQUE (TenantId, TransferNumber)
);

CREATE TABLE centralkitchen.StockTransferLines (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    StockTransferId BIGINT NOT NULL,
    RawMaterialId BIGINT NOT NULL,
    DispatchedQty DECIMAL(18,3) NOT NULL,
    ReceivedQty DECIMAL(18,3) NULL,
    UnitCost DECIMAL(18,4) NOT NULL DEFAULT 0,
    UomId BIGINT NOT NULL,
    CONSTRAINT FK_TLine_Transfer FOREIGN KEY (StockTransferId) REFERENCES centralkitchen.StockTransfers(Id),
    CONSTRAINT FK_TLine_RM FOREIGN KEY (RawMaterialId) REFERENCES inventory.RawMaterials(Id)
);
GO

/* =============================================================================
   7. CRM  -  customers, loyalty, feedback, SMS / marketing campaigns
   (Petpooja: customer DB, SMS connect, loyalty, feedback.)
   ============================================================================= */

CREATE TABLE crm.Customers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(160) NULL, Phone NVARCHAR(32) NULL, Email NVARCHAR(128) NULL,
    Pan NVARCHAR(20) NULL,
    LoyaltyPoints DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalSpend DECIMAL(18,4) NOT NULL DEFAULT 0,
    VisitCount INT NOT NULL DEFAULT 0,
    LastVisitAd datetime2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL
);
CREATE INDEX IX_Customers_Phone ON crm.Customers(TenantId, Phone);

CREATE TABLE crm.LoyaltyTransactions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    CustomerId BIGINT NOT NULL,
    InvoiceId BIGINT NULL,
    TxnType TINYINT NOT NULL,                        -- 1=Earn,2=Redeem,3=Adjust
    Points DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Loyalty_Cust FOREIGN KEY (CustomerId) REFERENCES crm.Customers(Id)
);

CREATE TABLE crm.Feedback (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    CustomerId BIGINT NULL, InvoiceId BIGINT NULL,
    OverallRating TINYINT NULL, FoodRating TINYINT NULL, ServiceRating TINYINT NULL,
    Comment NVARCHAR(1000) NULL,
    Source TINYINT NOT NULL DEFAULT 1,              -- 1=SMS,2=App,3=QR,4=Aggregator
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE crm.Segments (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    RuleJson NVARCHAR(MAX) NULL,                     -- e.g. spend > X, lapsed > 30d
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE crm.Campaigns (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Channel TINYINT NOT NULL,                        -- 1=SMS,2=WhatsApp,3=Email,4=Push
    SegmentId BIGINT NULL,
    MessageTemplate NVARCHAR(MAX) NULL,
    ScheduledAtAd datetime2 NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Draft,2=Scheduled,3=Sent,4=Cancelled
    SentCount INT NOT NULL DEFAULT 0,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Campaign_Segment FOREIGN KEY (SegmentId) REFERENCES crm.Segments(Id)
);

CREATE TABLE crm.CampaignRecipients (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    CampaignId BIGINT NOT NULL,
    CustomerId BIGINT NOT NULL,
    DeliveryStatus TINYINT NOT NULL DEFAULT 1,      -- 1=Queued,2=Sent,3=Delivered,4=Failed
    SentAt datetime2 NULL,
    CONSTRAINT FK_CampRec_Campaign FOREIGN KEY (CampaignId) REFERENCES crm.Campaigns(Id),
    CONSTRAINT FK_CampRec_Cust FOREIGN KEY (CustomerId) REFERENCES crm.Customers(Id)
);
GO

/* =============================================================================
   8. FINANCE  -  fiscal year, taxes, day close, IRD/CBMS, expenses, food cost
   ============================================================================= */

CREATE TABLE finance.FiscalYears (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(16) NOT NULL,                      -- '2081/82'
    StartDateAd date NOT NULL, EndDateAd date NOT NULL,
    StartDateBs NVARCHAR(10) NOT NULL, EndDateBs NVARCHAR(10) NOT NULL,
    IsCurrent BIT NOT NULL DEFAULT 0, IsClosed BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_FY UNIQUE (TenantId, Name)
);

CREATE TABLE finance.Taxes (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(40) NOT NULL,                      -- VAT
    Rate DECIMAL(9,4) NOT NULL DEFAULT 13.0000,
    IsInclusive BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE finance.DayCloses (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    BusinessDateAd date NOT NULL, BusinessDateBs NVARCHAR(10) NOT NULL,
    TotalSales DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalVat DECIMAL(18,4) NOT NULL DEFAULT 0,
    TotalDiscount DECIMAL(18,4) NOT NULL DEFAULT 0,
    CashCollected DECIMAL(18,4) NOT NULL DEFAULT 0,
    DigitalCollected DECIMAL(18,4) NOT NULL DEFAULT 0,
    AggregatorSales DECIMAL(18,4) NOT NULL DEFAULT 0,
    FoodCost DECIMAL(18,4) NOT NULL DEFAULT 0,       -- consumed RM cost (food-costing)
    InvoiceCount INT NOT NULL DEFAULT 0, VoidCount INT NOT NULL DEFAULT 0,
    Status TINYINT NOT NULL DEFAULT 1,
    ClosedByUserId BIGINT NULL, ClosedAtAd datetime2 NULL,
    CONSTRAINT UQ_DayClose UNIQUE (TenantId, OutletId, BusinessDateAd)
);

CREATE TABLE finance.Expenses (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    Category NVARCHAR(96) NOT NULL,
    Amount DECIMAL(18,4) NOT NULL, VatAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    VendorName NVARCHAR(160) NULL, BillNo NVARCHAR(60) NULL,
    ExpenseDateAd datetime2 NOT NULL, ExpenseDateBs NVARCHAR(10) NOT NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL
);

-- Nepal IRD CBMS real-time billing sync (each invoice/credit-note pushed to IRD)
CREATE TABLE finance.CbmsSyncLog (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    InvoiceId BIGINT NULL,
    SellerPan NVARCHAR(20) NOT NULL, BuyerPan NVARCHAR(20) NULL,
    FiscalYear NVARCHAR(16) NOT NULL,
    RefInvoiceNumber NVARCHAR(40) NOT NULL,
    TotalSales DECIMAL(18,4) NOT NULL, TaxableAmount DECIMAL(18,4) NOT NULL, VatAmount DECIMAL(18,4) NOT NULL,
    RequestPayload NVARCHAR(MAX) NULL, ResponsePayload NVARCHAR(MAX) NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Pending,2=Synced,3=Failed,4=Retrying
    AttemptCount INT NOT NULL DEFAULT 0, SyncedAt datetime2 NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Cbms_Invoice FOREIGN KEY (InvoiceId) REFERENCES sales.Invoices(Id)
);
CREATE INDEX IX_Cbms_Status ON finance.CbmsSyncLog(TenantId, OutletId, Status);
GO

/* =============================================================================
   9. INTEGRATION  -  outbound ERP/Tally push (data lake), devices/peripherals
   (Petpooja: push sales/inventory/customer data to any ERP — Tally/SAP/NAV.)
   ============================================================================= */

-- outbound change feed for external ERP/accounting consumers
CREATE TABLE integration.ErpOutbox (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NULL,
    Target NVARCHAR(40) NOT NULL,                   -- 'Tally','SAP','NAV','DataLake'
    EntityType NVARCHAR(40) NOT NULL,               -- 'Invoice','Purchase','StockMovement','Customer'
    EntityId BIGINT NOT NULL,
    Operation TINYINT NOT NULL,                     -- 1=Insert,2=Update,3=Delete
    PayloadJson NVARCHAR(MAX) NULL,
    Status TINYINT NOT NULL DEFAULT 1,              -- 1=Pending,2=Sent,3=Failed
    AttemptCount INT NOT NULL DEFAULT 0,
    LastError NVARCHAR(MAX) NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    SentAt datetime2 NULL
);
CREATE INDEX IX_ErpOutbox_Pending ON integration.ErpOutbox(TenantId, Target, Status);

-- peripheral devices (Petpooja: 200+ printers/scales/scanners compatibility)
CREATE TABLE integration.Devices (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    DeviceType TINYINT NOT NULL,                    -- 1=BillPrinter,2=KotPrinter,3=WeighingScale,4=Scanner,5=KDS,6=POS
    Name NVARCHAR(96) NOT NULL,
    ConnectionType TINYINT NULL,                    -- 1=USB,2=Network,3=Bluetooth
    Address NVARCHAR(128) NULL,                     -- IP / MAC / COM
    KitchenStationId BIGINT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Device_Outlet FOREIGN KEY (OutletId) REFERENCES hq.Outlets(Id)
);
GO

/* cross-cutting FKs now that referenced tables exist */
ALTER TABLE catalog.Categories
    ADD CONSTRAINT FK_Cat_Brand FOREIGN KEY (BrandId) REFERENCES hq.Brands(Id);
ALTER TABLE catalog.Categories
    ADD CONSTRAINT FK_Cat_Station FOREIGN KEY (DefaultKitchenStationId) REFERENCES catalog.KitchenStations(Id);
ALTER TABLE sales.Orders
    ADD CONSTRAINT FK_Order_Customer FOREIGN KEY (CustomerId) REFERENCES crm.Customers(Id);
ALTER TABLE sales.Invoices
    ADD CONSTRAINT FK_Invoice_FY FOREIGN KEY (FiscalYearId) REFERENCES finance.FiscalYears(Id);
GO

/* =============================================================================
   REPORTING VIEWS
   ============================================================================= */
GO
-- channel-wise sales (dine-in vs each aggregator) with commission visibility
CREATE VIEW sales.vw_ChannelSales AS
SELECT o.TenantId, o.OutletId, o.ChannelId, c.Name AS ChannelName,
       CAST(o.OrderDateAd AS date) AS BusinessDate,
       COUNT(*) AS OrderCount,
       SUM(o.GrandTotal) AS GrossSales,
       SUM(ISNULL(ag.CommissionAmount,0)) AS AggregatorCommission
FROM sales.Orders o
JOIN channel.Channels c ON c.Id = o.ChannelId
LEFT JOIN channel.AggregatorOrders ag ON ag.Id = o.AggregatorOrderId
WHERE o.IsDeleted = 0 AND o.Status = 5
GROUP BY o.TenantId, o.OutletId, o.ChannelId, c.Name, CAST(o.OrderDateAd AS date);
GO

-- food cost vs sales per item (food-costing report seed)
CREATE VIEW catalog.vw_ItemFoodCost AS
SELECT r.TenantId, r.ItemId,
       SUM(rl.Quantity * (1 + rl.WastagePercent/100.0) * rm.StandardCost) AS RecipeCost
FROM inventory.Recipes r
JOIN inventory.RecipeStages rs ON rs.RecipeId = r.Id
JOIN inventory.RecipeLines rl ON rl.RecipeStageId = rs.Id
JOIN inventory.RawMaterials rm ON rm.Id = rl.RawMaterialId
WHERE r.ItemId IS NOT NULL
GROUP BY r.TenantId, r.ItemId;
GO
