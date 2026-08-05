/* =============================================================================
   RESTAURANT MOBILE APP  -  SERVER schema (source of truth)
   Stack    : .NET 8 / ASP.NET Zero (ABP) + EF Core + SQL Server
   App side : Flutter (Riverpod + Freezed) talking to ABP AppServices.
   Convention: FullAuditedEntity<long> + IMustHaveTenant unless noted.
              Money decimal(18,4), Qty decimal(18,3), %/rate decimal(9,4).
              Every txn carries AD (datetime2 UTC) + BS (yyyy-MM-dd) date.
   Mobile    : Orders / OrderItems / Tickets / Reviews carry a device-generated
              ClientGuid for offline-first idempotent sync. ChangeLog is the
              server change-feed the device pulls deltas from.
   ============================================================================= */

IF SCHEMA_ID('catalog') IS NULL EXEC('CREATE SCHEMA [catalog]');
IF SCHEMA_ID('dining')  IS NULL EXEC('CREATE SCHEMA [dining]');
IF SCHEMA_ID('sales')   IS NULL EXEC('CREATE SCHEMA [sales]');
IF SCHEMA_ID('kds')     IS NULL EXEC('CREATE SCHEMA [kds]');
IF SCHEMA_ID('review')  IS NULL EXEC('CREATE SCHEMA [review]');
IF SCHEMA_ID('sync')    IS NULL EXEC('CREATE SCHEMA [sync]');
GO

/* =============================================================================
   CATALOG  -  menu the app renders (read-mostly, cached on device)
   ============================================================================= */

CREATE TABLE catalog.MenuCategories (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ParentId BIGINT NULL,
    Name NVARCHAR(128) NOT NULL,
    NameNp NVARCHAR(128) NULL,                    -- Devanagari display
    DisplayOrder INT NOT NULL DEFAULT 0,
    ColorHex NVARCHAR(9) NULL,                    -- POS button colour
    ImageUrl NVARCHAR(512) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Cat_Parent FOREIGN KEY (ParentId) REFERENCES catalog.MenuCategories(Id)
);

CREATE TABLE catalog.MenuItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    CategoryId BIGINT NOT NULL,
    Sku NVARCHAR(40) NULL,
    Name NVARCHAR(160) NOT NULL,
    NameNp NVARCHAR(160) NULL,
    Description NVARCHAR(512) NULL,
    BasePrice DECIMAL(18,4) NOT NULL DEFAULT 0,
    StationType TINYINT NOT NULL DEFAULT 1,       -- 1=Kitchen(->KOT), 2=Bar(->BOT), 3=Both/Split
    DefaultStationId BIGINT NULL,                 -- explicit routing override
    PreparationMinutes INT NULL,
    IsVatable BIT NOT NULL DEFAULT 1,
    HasVariants BIT NOT NULL DEFAULT 0,
    HasModifiers BIT NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,           -- the "86" toggle (sold out)
    IsActive BIT NOT NULL DEFAULT 1,
    ThumbnailUrl NVARCHAR(512) NULL,
    CalorieKcal INT NULL,
    IsVeg BIT NULL,
    SpiceLevel TINYINT NULL,                      -- 0..3 for menu badges
    DisplayOrder INT NOT NULL DEFAULT 0,
    AvgRating DECIMAL(3,2) NULL,                  -- denormalized from reviews
    RatingCount INT NOT NULL DEFAULT 0,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Item_Cat FOREIGN KEY (CategoryId) REFERENCES catalog.MenuCategories(Id)
);
CREATE INDEX IX_MenuItems_Cat ON catalog.MenuItems(TenantId, CategoryId, IsActive, IsAvailable);

CREATE TABLE catalog.MenuItemImages (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    MenuItemId BIGINT NOT NULL,
    Url NVARCHAR(512) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Img_Item FOREIGN KEY (MenuItemId) REFERENCES catalog.MenuItems(Id)
);

CREATE TABLE catalog.MenuItemVariants (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    MenuItemId BIGINT NOT NULL,
    Name NVARCHAR(64) NOT NULL,                   -- Small/Large, Half/Full
    PriceDelta DECIMAL(18,4) NOT NULL DEFAULT 0,
    IsAbsolutePrice BIT NOT NULL DEFAULT 0,
    IsDefault BIT NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Variant_Item FOREIGN KEY (MenuItemId) REFERENCES catalog.MenuItems(Id)
);

CREATE TABLE catalog.ModifierGroups (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    MinSelect INT NOT NULL DEFAULT 0,
    MaxSelect INT NOT NULL DEFAULT 1,
    IsRequired BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE catalog.Modifiers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ModifierGroupId BIGINT NOT NULL,
    Name NVARCHAR(96) NOT NULL,
    PriceDelta DECIMAL(18,4) NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Mod_Group FOREIGN KEY (ModifierGroupId) REFERENCES catalog.ModifierGroups(Id)
);

CREATE TABLE catalog.MenuItemModifierGroups (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    MenuItemId BIGINT NOT NULL,
    ModifierGroupId BIGINT NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MIMG_Item FOREIGN KEY (MenuItemId) REFERENCES catalog.MenuItems(Id),
    CONSTRAINT FK_MIMG_Group FOREIGN KEY (ModifierGroupId) REFERENCES catalog.ModifierGroups(Id),
    CONSTRAINT UQ_MIMG UNIQUE (MenuItemId, ModifierGroupId)
);

-- per-outlet availability override (item 86'd at one branch only)
CREATE TABLE catalog.OutletItemAvailability (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    MenuItemId BIGINT NOT NULL,
    IsAvailable BIT NOT NULL DEFAULT 1,
    UnavailableUntil datetime2 NULL,             -- auto re-enable
    UpdatedByUserId BIGINT NULL,
    LastModificationTime datetime2 NULL,
    CONSTRAINT UQ_OutletAvail UNIQUE (OutletId, MenuItemId),
    CONSTRAINT FK_Avail_Item FOREIGN KEY (MenuItemId) REFERENCES catalog.MenuItems(Id)
);
GO

/* =============================================================================
   DINING  -  tables + seating session (dine-in orders attach here)
   ============================================================================= */

CREATE TABLE dining.DiningTables (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    FloorName NVARCHAR(64) NULL,
    Name NVARCHAR(32) NOT NULL,                   -- 'T1'
    Seats INT NOT NULL DEFAULT 4,
    QrCode NVARCHAR(128) NULL,                    -- scan-to-order / scan-to-review
    Status TINYINT NOT NULL DEFAULT 1,            -- 1=Free,2=Occupied,3=Reserved,4=Billed,5=Cleaning
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT UQ_Table UNIQUE (TenantId, OutletId, Name)
);

CREATE TABLE dining.TableSessions (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    TableId BIGINT NOT NULL,
    ClientGuid UNIQUEIDENTIFIER NOT NULL,
    OpenedByUserId BIGINT NULL,                   -- waiter
    GuestCount INT NOT NULL DEFAULT 1,
    OpenedAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ClosedAtAd datetime2 NULL,
    Status TINYINT NOT NULL DEFAULT 1,            -- 1=Open,2=Closed
    CONSTRAINT FK_Session_Table FOREIGN KEY (TableId) REFERENCES dining.DiningTables(Id),
    CONSTRAINT UQ_Session_Guid UNIQUE (TenantId, ClientGuid)
);
CREATE INDEX IX_Sessions_Open ON dining.TableSessions(TenantId, OutletId, Status);
GO

/* =============================================================================
   SALES  -  orders the waiter builds on the device
   ============================================================================= */

CREATE TABLE sales.Orders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    ClientGuid UNIQUEIDENTIFIER NOT NULL,         -- device-generated; idempotent upload key
    OrderNumber NVARCHAR(40) NULL,                -- server-assigned on first sync
    OrderType TINYINT NOT NULL DEFAULT 1,         -- 1=DineIn,2=Takeaway,3=Delivery,4=QrSelfOrder
    TableSessionId BIGINT NULL,
    TableId BIGINT NULL,
    WaiterUserId BIGINT NULL,
    DeviceId BIGINT NULL,
    CustomerName NVARCHAR(128) NULL,
    CustomerPhone NVARCHAR(32) NULL,
    GuestCount INT NULL,
    Status TINYINT NOT NULL DEFAULT 1,            -- 1=Draft,2=Placed,3=InProgress,4=Served,5=Billed,6=Closed,7=Cancelled
    SubTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    ServiceChargeAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    VatAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    GrandTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    Notes NVARCHAR(512) NULL,
    OrderDateAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    OrderDateBs NVARCHAR(10) NULL,
    SyncStatus TINYINT NOT NULL DEFAULT 0,        -- 0=Synced,1=PendingUpload,2=Conflict
    RowVersion ROWVERSION,                        -- server concurrency token
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Order_Session FOREIGN KEY (TableSessionId) REFERENCES dining.TableSessions(Id),
    CONSTRAINT FK_Order_Table FOREIGN KEY (TableId) REFERENCES dining.DiningTables(Id),
    CONSTRAINT UQ_Order_Guid UNIQUE (TenantId, ClientGuid)
);
CREATE INDEX IX_Orders_Status ON sales.Orders(TenantId, OutletId, Status, OrderDateAd);

CREATE TABLE sales.OrderItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OrderId BIGINT NOT NULL,
    ClientGuid UNIQUEIDENTIFIER NOT NULL,
    MenuItemId BIGINT NOT NULL,
    VariantId BIGINT NULL,
    ItemNameSnapshot NVARCHAR(160) NOT NULL,      -- frozen at add time
    StationTypeSnapshot TINYINT NOT NULL,         -- frozen routing (drives KOT vs BOT)
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,4) NOT NULL,
    ModifierTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,4) NOT NULL DEFAULT 0,
    LineTotal DECIMAL(18,4) NOT NULL DEFAULT 0,
    CourseNo INT NULL,                            -- 1=starter,2=main...
    SpecialInstructions NVARCHAR(256) NULL,
    Status TINYINT NOT NULL DEFAULT 1,            -- 1=New,2=Fired,3=Preparing,4=Ready,5=Served,6=Void
    VoidReason NVARCHAR(256) NULL,
    FiredAtAd datetime2 NULL,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_OItem_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id),
    CONSTRAINT FK_OItem_Menu FOREIGN KEY (MenuItemId) REFERENCES catalog.MenuItems(Id),
    CONSTRAINT UQ_OItem_Guid UNIQUE (TenantId, ClientGuid)
);
CREATE INDEX IX_OrderItems_Order ON sales.OrderItems(OrderId, Status);

CREATE TABLE sales.OrderItemModifiers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OrderItemId BIGINT NOT NULL,
    ModifierId BIGINT NOT NULL,
    ModifierNameSnapshot NVARCHAR(96) NOT NULL,
    PriceDelta DECIMAL(18,4) NOT NULL DEFAULT 0,
    Quantity DECIMAL(18,3) NOT NULL DEFAULT 1,
    CONSTRAINT FK_OIM_Item FOREIGN KEY (OrderItemId) REFERENCES sales.OrderItems(Id),
    CONSTRAINT FK_OIM_Mod FOREIGN KEY (ModifierId) REFERENCES catalog.Modifiers(Id)
);

CREATE TABLE sales.OrderStatusHistory (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OrderId BIGINT NOT NULL,
    FromStatus TINYINT NULL,
    ToStatus TINYINT NOT NULL,
    ChangedByUserId BIGINT NULL,
    ChangedAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Note NVARCHAR(256) NULL,
    CONSTRAINT FK_OSH_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id)
);
GO

/* =============================================================================
   KDS  -  stations + tickets. A "ticket" is one station's slice of a fire.
           StationType decides KOT (kitchen) vs BOT (bar).
   ============================================================================= */

CREATE TABLE kds.Stations (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    Name NVARCHAR(64) NOT NULL,                   -- 'Hot Kitchen','Tandoor','Main Bar'
    StationType TINYINT NOT NULL,                 -- 1=Kitchen(KOT), 2=Bar(BOT)
    DisplayOrder INT NOT NULL DEFAULT 0,
    PrinterDeviceId BIGINT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    CONSTRAINT UQ_Station UNIQUE (TenantId, OutletId, Name)
);

CREATE TABLE kds.Tickets (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    OrderId BIGINT NOT NULL,
    StationId BIGINT NOT NULL,
    ClientGuid UNIQUEIDENTIFIER NOT NULL,
    TicketNumber NVARCHAR(40) NULL,
    TicketKind TINYINT NOT NULL,                  -- 1=KOT (kitchen), 2=BOT (bar)  [== station type]
    TicketType TINYINT NOT NULL DEFAULT 1,        -- 1=New,2=Addon,3=Void,4=Reprint
    TableName NVARCHAR(32) NULL,                  -- snapshot for the KDS card
    CourseNo INT NULL,
    Status TINYINT NOT NULL DEFAULT 1,            -- 1=Queued,2=Acknowledged,3=Preparing,4=Ready,5=Bumped/Served,6=Recalled,7=Cancelled
    Priority TINYINT NOT NULL DEFAULT 0,          -- rush flag
    FiredAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AcknowledgedAtAd datetime2 NULL,
    ReadyAtAd datetime2 NULL,
    BumpedAtAd datetime2 NULL,
    PrepSeconds AS DATEDIFF(SECOND, FiredAtAd, BumpedAtAd),  -- computed prep time
    SyncStatus TINYINT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    CONSTRAINT FK_Ticket_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id),
    CONSTRAINT FK_Ticket_Station FOREIGN KEY (StationId) REFERENCES kds.Stations(Id),
    CONSTRAINT UQ_Ticket_Guid UNIQUE (TenantId, ClientGuid)
);
CREATE INDEX IX_Tickets_Board ON kds.Tickets(TenantId, OutletId, StationId, Status, FiredAtAd);

CREATE TABLE kds.TicketItems (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    TicketId BIGINT NOT NULL,
    OrderItemId BIGINT NOT NULL,
    NameSnapshot NVARCHAR(160) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    ModifiersText NVARCHAR(512) NULL,            -- flattened "No onion, Extra cheese" for the card
    SpecialInstructions NVARCHAR(256) NULL,
    Status TINYINT NOT NULL DEFAULT 1,            -- 1=Queued,2=Preparing,3=Ready,4=Bumped,5=Void
    CONSTRAINT FK_TItem_Ticket FOREIGN KEY (TicketId) REFERENCES kds.Tickets(Id),
    CONSTRAINT FK_TItem_OItem FOREIGN KEY (OrderItemId) REFERENCES sales.OrderItems(Id)
);

CREATE TABLE kds.TicketEvents (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    TicketId BIGINT NOT NULL,
    EventType TINYINT NOT NULL,                   -- 1=Fired,2=Ack,3=StartPrep,4=Ready,5=Bump,6=Recall,7=Cancel
    ByUserId BIGINT NULL,
    AtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Note NVARCHAR(256) NULL,
    CONSTRAINT FK_TEvent_Ticket FOREIGN KEY (TicketId) REFERENCES kds.Tickets(Id)
);
GO

/* =============================================================================
   REVIEW  -  customer reviews + media + staff responses
   ============================================================================= */

CREATE TABLE review.Reviews (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    ClientGuid UNIQUEIDENTIFIER NOT NULL,
    OrderId BIGINT NULL,
    TableId BIGINT NULL,
    MenuItemId BIGINT NULL,                       -- NULL = whole-visit review, set = dish review
    CustomerName NVARCHAR(128) NULL,
    CustomerPhone NVARCHAR(32) NULL,
    OverallRating TINYINT NOT NULL,               -- 1..5
    FoodRating TINYINT NULL,
    ServiceRating TINYINT NULL,
    AmbienceRating TINYINT NULL,
    ValueRating TINYINT NULL,
    Comment NVARCHAR(2000) NULL,
    Source TINYINT NOT NULL DEFAULT 1,            -- 1=App,2=QrScan,3=Staff,4=Aggregator
    Sentiment TINYINT NULL,                       -- 1=Negative,2=Neutral,3=Positive (NLP fill)
    IsPublished BIT NOT NULL DEFAULT 1,
    IsFlagged BIT NOT NULL DEFAULT 0,
    ReviewDateAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ReviewDateBs NVARCHAR(10) NULL,
    SyncStatus TINYINT NOT NULL DEFAULT 0,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CreatorUserId BIGINT NULL,
    LastModificationTime datetime2 NULL, LastModifierUserId BIGINT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, DeleterUserId BIGINT NULL, DeletionTime datetime2 NULL,
    CONSTRAINT FK_Review_Order FOREIGN KEY (OrderId) REFERENCES sales.Orders(Id),
    CONSTRAINT FK_Review_Item FOREIGN KEY (MenuItemId) REFERENCES catalog.MenuItems(Id),
    CONSTRAINT UQ_Review_Guid UNIQUE (TenantId, ClientGuid)
);
CREATE INDEX IX_Reviews_Outlet ON review.Reviews(TenantId, OutletId, ReviewDateAd);
CREATE INDEX IX_Reviews_Item ON review.Reviews(TenantId, MenuItemId);

CREATE TABLE review.ReviewMedia (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ReviewId BIGINT NOT NULL,
    MediaUrl NVARCHAR(512) NOT NULL,
    MediaType TINYINT NOT NULL DEFAULT 1,         -- 1=Image,2=Video
    DisplayOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Media_Review FOREIGN KEY (ReviewId) REFERENCES review.Reviews(Id)
);

CREATE TABLE review.ReviewResponses (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    ReviewId BIGINT NOT NULL,
    ResponderUserId BIGINT NULL,
    ResponseText NVARCHAR(2000) NOT NULL,
    RespondedAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Resp_Review FOREIGN KEY (ReviewId) REFERENCES review.Reviews(Id)
);
GO

/* =============================================================================
   SYNC  -  device registry, FCM tokens, and the server CHANGE-FEED
            that the device pulls deltas from.
   ============================================================================= */

CREATE TABLE sync.Devices (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    OutletId BIGINT NOT NULL,
    DeviceType TINYINT NOT NULL,                  -- 1=WaiterPhone,2=Tablet,3=KdsScreen,4=Kiosk
    Name NVARCHAR(96) NOT NULL,
    InstallId NVARCHAR(128) NOT NULL,             -- stable per-install id
    Platform NVARCHAR(16) NULL,                   -- android/ios
    AppVersion NVARCHAR(32) NULL,
    AssignedUserId BIGINT NULL,
    LastSyncCursor BIGINT NOT NULL DEFAULT 0,     -- last ChangeLog.Seq pulled
    LastSeenAt datetime2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Device_Install UNIQUE (TenantId, InstallId)
);

CREATE TABLE sync.PushTokens (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    DeviceId BIGINT NOT NULL,
    UserId BIGINT NULL,
    FcmToken NVARCHAR(512) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreationTime datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Push_Device FOREIGN KEY (DeviceId) REFERENCES sync.Devices(Id),
    CONSTRAINT UQ_Push_Token UNIQUE (TenantId, FcmToken)
);

-- Append-only change feed. Every insert/update/delete on a syncable entity
-- writes one row here (via app service or SQL trigger). Devices pull rows
-- WHERE Seq > LastSyncCursor to get deltas. Seq is the monotonic cursor.
CREATE TABLE sync.ChangeLog (
    Seq BIGINT IDENTITY(1,1) PRIMARY KEY,        -- global monotonic cursor
    TenantId INT NOT NULL,
    OutletId BIGINT NULL,
    EntityType NVARCHAR(40) NOT NULL,            -- 'MenuItem','Order','Ticket','Review'...
    EntityId BIGINT NOT NULL,
    EntityClientGuid UNIQUEIDENTIFIER NULL,
    Operation TINYINT NOT NULL,                  -- 1=Insert,2=Update,3=Delete
    PayloadJson NVARCHAR(MAX) NULL,              -- optional full snapshot for fast apply
    ChangedAtAd datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ChangedByDeviceId BIGINT NULL               -- so a device can skip echoes of its own writes
);
CREATE INDEX IX_ChangeLog_Pull ON sync.ChangeLog(TenantId, OutletId, Seq);
CREATE INDEX IX_ChangeLog_Entity ON sync.ChangeLog(EntityType, EntityId);

-- Server-side record of accepted offline batches (idempotency ledger).
CREATE TABLE sync.UploadBatches (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TenantId INT NOT NULL,
    DeviceId BIGINT NOT NULL,
    BatchGuid UNIQUEIDENTIFIER NOT NULL,
    EntityCount INT NOT NULL DEFAULT 0,
    AcceptedCount INT NOT NULL DEFAULT 0,
    ConflictCount INT NOT NULL DEFAULT 0,
    Status TINYINT NOT NULL DEFAULT 1,           -- 1=Received,2=Processed,3=PartialConflict,4=Failed
    ResultJson NVARCHAR(MAX) NULL,               -- per-item {clientGuid, serverId, status}
    ReceivedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ProcessedAt datetime2 NULL,
    CONSTRAINT FK_Batch_Device FOREIGN KEY (DeviceId) REFERENCES sync.Devices(Id),
    CONSTRAINT UQ_Batch_Guid UNIQUE (TenantId, BatchGuid)
);
GO

/* cross-cutting FK now that kds.Stations exists */
ALTER TABLE catalog.MenuItems
    ADD CONSTRAINT FK_Item_Station FOREIGN KEY (DefaultStationId) REFERENCES kds.Stations(Id);
ALTER TABLE sales.Orders
    ADD CONSTRAINT FK_Order_Device FOREIGN KEY (DeviceId) REFERENCES sync.Devices(Id);
GO

/* =============================================================================
   Convenience views for the app's home/board screens
   ============================================================================= */
GO
CREATE VIEW kds.vw_LiveBoard AS
SELECT t.TenantId, t.OutletId, t.StationId, s.Name AS StationName, s.StationType,
       t.Id AS TicketId, t.TicketKind, t.TableName, t.Status, t.Priority,
       t.FiredAtAd, DATEDIFF(SECOND, t.FiredAtAd, SYSUTCDATETIME()) AS AgeSeconds
FROM kds.Tickets t
JOIN kds.Stations s ON s.Id = t.StationId
WHERE t.Status IN (1,2,3,4);   -- still on the board
GO

CREATE VIEW review.vw_ItemRatingRollup AS
SELECT TenantId, MenuItemId,
       COUNT(*) AS ReviewCount,
       CAST(AVG(CAST(OverallRating AS decimal(5,2))) AS decimal(3,2)) AS AvgRating
FROM review.Reviews
WHERE MenuItemId IS NOT NULL AND IsDeleted = 0 AND IsPublished = 1
GROUP BY TenantId, MenuItemId;
GO
