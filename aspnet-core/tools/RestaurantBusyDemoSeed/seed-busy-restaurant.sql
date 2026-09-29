/*
    NeXtWave Restro busy-restaurant UX dataset

    Target: tenant 2 (FutureStar) in RestroErp_db.
    The script is deterministic and additive: stable IDs prevent duplicate rows on rerun,
    while records created outside this script are left untouched.

    Run with:
      sqlcmd -S "localhost\SQLEXPRESS" -d RestroErp_db -E -C -b -i seed-busy-restaurant.sql
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @TenantId int = 2;
DECLARE @Now datetime2 = SYSDATETIME();
DECLARE @AdminUserId bigint;
DECLARE @PcsUnitId uniqueidentifier;
DECLARE @KgUnitId uniqueidentifier;
DECLARE @VatTaxId uniqueidentifier;
DECLARE @NaTaxId uniqueidentifier;
DECLARE @ProductGroupId uniqueidentifier;
DECLARE @FinancialYearId uniqueidentifier;
DECLARE @SalesVoucherTypeId uniqueidentifier;
DECLARE @StockJournalVoucherTypeId uniqueidentifier;
DECLARE @PhysicalStockVoucherTypeId uniqueidentifier;
DECLARE @StockIssueVoucherTypeId uniqueidentifier;
DECLARE @SalesAccountId uniqueidentifier;
DECLARE @CashAccountId uniqueidentifier;
DECLARE @CreditorGroupId uniqueidentifier;

SELECT TOP (1) @AdminUserId = Id FROM AbpUsers WHERE TenantId = @TenantId AND UserName = 'admin';
SELECT TOP (1) @PcsUnitId = Id FROM tbl_Unit WHERE TenantId = @TenantId AND Name = 'Pcs';
SELECT TOP (1) @KgUnitId = Id FROM tbl_Unit WHERE TenantId = @TenantId AND Name = 'Kg';
SELECT TOP (1) @VatTaxId = Id FROM tbl_Tax WHERE TenantId = @TenantId AND Rate = 13 AND IsActive = 1;
SELECT TOP (1) @NaTaxId = Id FROM tbl_Tax WHERE TenantId = @TenantId AND Name = 'NA';
SELECT TOP (1) @ProductGroupId = Id FROM tbl_ProductGroup WHERE TenantId = @TenantId AND Name = 'PRIMARY';
SELECT TOP (1) @FinancialYearId = Id FROM tbl_FinancialYear WHERE TenantId = @TenantId ORDER BY FromDate DESC;
SELECT TOP (1) @SalesVoucherTypeId = Id FROM tbl_VoucherType WHERE TenantId = @TenantId AND Name = 'SalesForTicket';
SELECT TOP (1) @StockJournalVoucherTypeId = Id FROM tbl_VoucherType WHERE TenantId = @TenantId AND Name = 'StockJournal';
SELECT TOP (1) @PhysicalStockVoucherTypeId = Id FROM tbl_VoucherType WHERE TenantId = @TenantId AND Name = 'PhysicalStock';
SELECT TOP (1) @StockIssueVoucherTypeId = Id FROM tbl_VoucherType WHERE TenantId = @TenantId AND Name = 'StockIssue';
SELECT TOP (1) @SalesAccountId = Id FROM tbl_AccountLedger WHERE TenantId = @TenantId AND Name = 'Sales Account';
SELECT TOP (1) @CashAccountId = Id FROM tbl_AccountLedger WHERE TenantId = @TenantId AND Name = 'Cash';
SELECT TOP (1) @CreditorGroupId = Id FROM tbl_AccountGroup WHERE TenantId = @TenantId AND Name = 'Sundry Creditors';

IF NOT EXISTS (SELECT 1 FROM AbpTenants WHERE Id = @TenantId)
    THROW 51000, 'Tenant 2 was not found. The busy restaurant seed was not applied.', 1;

IF @AdminUserId IS NULL OR @PcsUnitId IS NULL OR @KgUnitId IS NULL OR @VatTaxId IS NULL
   OR @NaTaxId IS NULL OR @ProductGroupId IS NULL OR @FinancialYearId IS NULL
   OR @SalesVoucherTypeId IS NULL OR @StockJournalVoucherTypeId IS NULL
   OR @PhysicalStockVoucherTypeId IS NULL OR @StockIssueVoucherTypeId IS NULL OR @SalesAccountId IS NULL
   OR @CashAccountId IS NULL OR @CreditorGroupId IS NULL
    THROW 51001, 'Tenant 2 is missing one or more ERP reference masters required by the restaurant seed.', 1;

BEGIN TRANSACTION;

/* Realistic reference masters stay intentionally compact. */
CREATE TABLE #AreaSeed
(
    AreaNo int NOT NULL PRIMARY KEY,
    Id uniqueidentifier NOT NULL,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(250) NOT NULL
);

INSERT #AreaSeed (AreaNo, Id, Name, Description)
SELECT AreaNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/AREA/', AreaNo))),
       Name,
       Description
FROM (VALUES
    (1, N'Main Dining', N'All-day dining room beside the reception and cashier.'),
    (2, N'Rooftop Terrace', N'Covered rooftop seating with city and mountain views.'),
    (3, N'Garden Courtyard', N'Outdoor courtyard for relaxed lunches and evening dining.'),
    (4, N'Family Lounge', N'Comfortable booth seating for families and larger groups.'),
    (5, N'Private Dining', N'Quiet rooms for meetings, celebrations, and hosted dinners.'),
    (6, N'Banquet Hall', N'Flexible banquet seating for events and group reservations.'),
    (7, N'Cafe & Bakery', N'Casual coffee, bakery, and quick-meal seating.'),
    (8, N'Poolside Deck', N'Sheltered deck seating for snacks, grills, and beverages.')
) x(AreaNo, Name, Description);

INSERT tbl_RestaurantArea (Id, Name, Description, SortOrder, IsActive, IsDeleted, TenantId)
SELECT s.Id, s.Name, s.Description, s.AreaNo, 1, 0, @TenantId
FROM #AreaSeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantArea t WHERE t.Id = s.Id);

UPDATE t SET t.Name=s.Name,t.Description=s.Description,t.SortOrder=s.AreaNo,t.IsActive=1,t.IsDeleted=0
FROM tbl_RestaurantArea t JOIN #AreaSeed s ON s.Id=t.Id
WHERE t.TenantId=@TenantId;

CREATE TABLE #StationSeed
(
    StationNo int NOT NULL PRIMARY KEY,
    Id uniqueidentifier NOT NULL,
    Name nvarchar(100) NOT NULL,
    StationType int NOT NULL
);

INSERT #StationSeed (StationNo, Id, Name, StationType)
SELECT StationNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/STATION/', StationNo))),
       Name,
       StationType
FROM (VALUES
    (1, N'Main Kitchen', 0),
    (2, N'Tandoor & Grill', 0),
    (3, N'Momo & Wok', 0),
    (4, N'Pantry & Bakery', 2),
    (5, N'Bar & Beverages', 1),
    (6, N'Dessert Pass', 3)
) x(StationNo, Name, StationType);

INSERT tbl_RestaurantStation (Id, Name, StationType, IsActive, IsDeleted, TenantId)
SELECT s.Id, s.Name, s.StationType, 1, 0, @TenantId
FROM #StationSeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantStation t WHERE t.Id = s.Id);

CREATE TABLE #TableSeed
(
    TableNo int NOT NULL PRIMARY KEY,
    Id uniqueidentifier NOT NULL,
    AreaId uniqueidentifier NOT NULL,
    Name nvarchar(100) NOT NULL,
    Code nvarchar(50) NOT NULL,
    Capacity int NOT NULL,
    Status int NOT NULL
);

;WITH n AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS TableNo
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT #TableSeed (TableNo, Id, AreaId, Name, Code, Capacity, Status)
SELECT n.TableNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/TABLE/', n.TableNo))),
       a.Id,
       CONCAT(LEFT(a.Name, 3), N' ', FORMAT(((n.TableNo - 1) % 15) + 1, '00')),
       CONCAT(N'T-', FORMAT(n.TableNo, '000')),
       CASE ((n.TableNo - 1) % 6) WHEN 0 THEN 2 WHEN 1 THEN 4 WHEN 2 THEN 4 WHEN 3 THEN 6 WHEN 4 THEN 6 ELSE 8 END,
       CASE WHEN n.TableNo <= 75 THEN 1 WHEN n.TableNo <= 90 THEN 2 WHEN n.TableNo <= 100 THEN 3 ELSE 0 END
FROM n
JOIN #AreaSeed a ON a.AreaNo = ((n.TableNo - 1) / 15) + 1;

INSERT tbl_RestaurantTable (Id, Name, Code, Capacity, SortOrder, Status, IsActive, IsDeleted, AreaId, TenantId)
SELECT s.Id, s.Name, s.Code, s.Capacity, s.TableNo, s.Status, 1, 0, s.AreaId, @TenantId
FROM #TableSeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantTable t WHERE t.Id = s.Id);

CREATE TABLE #DeviceSeed (DeviceNo int PRIMARY KEY, Id uniqueidentifier, DeviceCode nvarchar(50), Name nvarchar(100));
INSERT #DeviceSeed
SELECT DeviceNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/DEVICE/', DeviceNo))),
       DeviceCode,
       Name
FROM (VALUES
    (1, N'POS-FRONT-01', N'Front Desk POS 01'),
    (2, N'POS-FRONT-02', N'Front Desk POS 02'),
    (3, N'POS-ROOF-01', N'Rooftop POS'),
    (4, N'POS-GARDEN-01', N'Garden POS'),
    (5, N'WAITER-HH-01', N'Waiter Handheld 01'),
    (6, N'WAITER-HH-02', N'Waiter Handheld 02'),
    (7, N'WAITER-HH-03', N'Waiter Handheld 03'),
    (8, N'WAITER-HH-04', N'Waiter Handheld 04'),
    (9, N'WAITER-HH-05', N'Waiter Handheld 05'),
    (10, N'WAITER-HH-06', N'Waiter Handheld 06'),
    (11, N'KDS-MAIN-01', N'Main Kitchen Display'),
    (12, N'KDS-BAR-01', N'Bar Kitchen Display')
) x(DeviceNo, DeviceCode, Name);

INSERT tbl_RestaurantDevice
    (Id, DeviceCode, Name, UserId, Status, RegisteredAt, LastSeenAt, TenantId, HasConflict,
     LastAcknowledgedSeq, LastPulledSeq, LastSyncAt, LastSyncError)
SELECT s.Id, s.DeviceCode, s.Name,
       CASE WHEN s.DeviceNo <= 10 THEN @AdminUserId ELSE NULL END,
       0, DATEADD(day, -60, @Now), DATEADD(minute, -s.DeviceNo * 3, @Now), @TenantId, 0,
       1000 + s.DeviceNo * 10, 1000 + s.DeviceNo * 10, DATEADD(minute, -s.DeviceNo * 3, @Now), NULL
FROM #DeviceSeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantDevice t WHERE t.Id = s.Id);

CREATE TABLE #CategorySeed
(
    CategoryNo int PRIMARY KEY,
    Id uniqueidentifier,
    Name nvarchar(100),
    Description nvarchar(250),
    BasePrice decimal(18,2),
    StationNo int
);

INSERT #CategorySeed
SELECT CategoryNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/CATEGORY/', CategoryNo))),
       Name, Description, BasePrice, StationNo
FROM (VALUES
    (1, N'Breakfast & Bakery', N'Breakfast plates, breads, and bakery favourites.', 220.00, 4),
    (2, N'Small Plates', N'Shareable starters and bar snacks.', 280.00, 1),
    (3, N'Soups & Salads', N'Comforting soups and fresh composed salads.', 240.00, 1),
    (4, N'Momos & Dumplings', N'Hand-folded steamed, fried, and jhol dumplings.', 230.00, 3),
    (5, N'Nepali Kitchen', N'Classic Nepali meals and regional specialities.', 390.00, 1),
    (6, N'Indian Curries', N'Slow-cooked curries finished to order.', 420.00, 1),
    (7, N'Tandoor & Grill', N'Tandoor breads, kebabs, and live-grill plates.', 480.00, 2),
    (8, N'Noodles & Rice', N'Wok-tossed noodles and fragrant rice dishes.', 330.00, 3),
    (9, N'Pizza & Pasta', N'Stone-baked pizza and fresh pasta classics.', 470.00, 4),
    (10, N'Burgers & Sandwiches', N'Grilled burgers, wraps, and sandwiches.', 390.00, 1),
    (11, N'Desserts', N'House-made desserts and after-dinner treats.', 250.00, 6),
    (12, N'Beverages & Bar', N'Coffee, coolers, mocktails, and bar service.', 140.00, 5)
) x(CategoryNo, Name, Description, BasePrice, StationNo);

INSERT tbl_RestaurantMenuCategory (Id, Name, Description, SortOrder, IsActive, IsDeleted, TenantId)
SELECT s.Id, s.Name, s.Description, s.CategoryNo, 1, 0, @TenantId
FROM #CategorySeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantMenuCategory t WHERE t.Id = s.Id);

/* 120 curated restaurant raw materials. */
CREATE TABLE #RawSeed
(
    RawNo int PRIMARY KEY,
    Id uniqueidentifier,
    Name nvarchar(120),
    UnitId uniqueidentifier,
    PurchaseRate decimal(18,2),
    MinimumStock decimal(18,3),
    MaximumStock decimal(18,3),
    SupplierNo int
);

;WITH RawGroups AS
(
    SELECT * FROM (VALUES
      (1, 1,  N'["Tomato","Red Onion","White Onion","Potato","Carrot","Cabbage","Cauliflower","Green Peas","French Beans","Capsicum","Broccoli","Spinach","Lettuce","Cucumber","Beetroot","Radish","Mushroom","Avocado","Lemon","Lime","Fresh Coriander","Spring Onion","Garlic","Ginger","Green Chilli","Mint Leaves","Basil Leaves","Rosemary","Apple","Banana"]', @KgUnitId, 75.00, 6.00, 1),
      (2, 31, N'["Chicken Breast","Chicken Thigh","Whole Chicken","Chicken Sausage","Buff Tenderloin","Buff Mince","Buff Sukuti","Mutton Curry Cut","Lamb Chops","Pork Belly","Bacon","Eggs","Rohu Fish","Basa Fillet","Prawns","Calamari","Chicken Mince","Buff Bone","Chicken Stock Bones","Smoked Chicken"]', @KgUnitId, 390.00, 10.00, 2),
      (3, 51, N'["Milk","Fresh Cream","Butter","Mozzarella Cheese","Cheddar Cheese","Paneer","Plain Yogurt","Ghee","Cream Cheese","Parmesan Cheese"]', @KgUnitId, 310.00, 8.00, 3),
      (4, 61, N'["Basmati Rice","Jeera Rice","All-Purpose Flour","Whole Wheat Flour","Corn Flour","Rice Noodles","Hakka Noodles","Spaghetti","Penne Pasta","Burger Buns","Sandwich Bread","Pizza Base Flour","Rolled Oats","Quinoa","Chickpeas","Kidney Beans","Black Lentils","Yellow Lentils","Red Lentils","Breadcrumbs"]', @KgUnitId, 145.00, 12.00, 4),
      (5, 81, N'["Cumin Seed","Coriander Powder","Turmeric Powder","Kashmiri Chilli","Garam Masala","Black Pepper","Sichuan Pepper","Timur","Fenugreek Seed","Mustard Seed","Cardamom","Cinnamon","Cloves","Bay Leaf","Oregano","Paprika","Tandoori Masala","Chaat Masala","Sesame Seed","Rock Salt"]', @KgUnitId, 260.00, 4.00, 5),
      (6, 101,N'["Arabica Coffee Beans","Assam Tea","Green Tea","Hot Chocolate Powder","Fresh Orange Juice","Apple Juice","Pineapple Juice","Soda Water","Tonic Water","Mineral Water","Cola","Lemon-Lime Soda","Ginger Ale","House Red Wine","House White Wine","Lager Beer","Paper Takeaway Box","Compostable Cup","Paper Straw","Food-grade Napkin"]', @PcsUnitId, 95.00, 20.00, 6)
    ) g(GroupNo, StartNo, ItemsJson, UnitId, BaseRate, MinimumStock, SupplierNo)
)
INSERT #RawSeed (RawNo, Id, Name, UnitId, PurchaseRate, MinimumStock, MaximumStock, SupplierNo)
SELECT g.StartNo + CONVERT(int, j.[key]),
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/RAW/', g.StartNo + CONVERT(int, j.[key])))),
       CONVERT(nvarchar(120), j.[value]),
       g.UnitId,
       g.BaseRate + CONVERT(int, j.[key]) * CASE WHEN g.GroupNo = 2 THEN 28 WHEN g.GroupNo = 3 THEN 22 ELSE 7 END,
       g.MinimumStock + (CONVERT(int, j.[key]) % 4),
       (g.MinimumStock + (CONVERT(int, j.[key]) % 4)) * 3,
       ((g.SupplierNo - 1) * 2) + (CONVERT(int, j.[key]) % 2) + 1
FROM RawGroups g
CROSS APPLY OPENJSON(g.ItemsJson) j;

IF (SELECT COUNT(*) FROM #RawSeed) <> 120
    THROW 51002, 'The curated raw-material seed must contain exactly 120 rows.', 1;

INSERT tbl_Product
    (Id, DateMiti, ProductCode, ProductType, Name, HsCode, Mrp, SalesRate, PurchaseRate,
     MinimumStock, MaximumStock, Margin, IsOpeningStock, Description, IsActive, IsDeleted,
     TaxId, ProductGroupId, UnitId, TenantId)
SELECT r.Id, N'2083-04-20', CONCAT(N'RAW-', FORMAT(r.RawNo, '000')), 1, r.Name, NULL,
       r.PurchaseRate, r.PurchaseRate, r.PurchaseRate, r.MinimumStock, r.MaximumStock, 0, 0,
       N'Kitchen raw material for the busy restaurant UX dataset.', 1, 0,
       @NaTaxId, @ProductGroupId, r.UnitId, @TenantId
FROM #RawSeed r
WHERE NOT EXISTS (SELECT 1 FROM tbl_Product p WHERE p.Id = r.Id);

/* 150 curated menu products: 13 items in the first six categories, 12 in the remaining six. */
CREATE TABLE #MenuSeed
(
    MenuNo int PRIMARY KEY,
    Id uniqueidentifier,
    ProductId uniqueidentifier,
    CategoryNo int,
    CategoryId uniqueidentifier,
    StationId uniqueidentifier,
    Name nvarchar(150),
    Price decimal(18,2),
    PreparationMinutes int,
    IsVeg bit,
    SpiceLevel int
);

;WITH MenuGroups AS
(
    SELECT * FROM (VALUES
      (1, N'["Masala Omelette","Cheese Omelette","Avocado Toast","Eggs Benedict","Chicken Sausage Breakfast","Aloo Paratha Breakfast","Puri Tarkari","Sel Roti Platter","French Toast","Pancake Stack","Butter Croissant","Cinnamon Roll","Bakery Basket"]', 1),
      (2, N'["Chicken Choila","Buff Sukuti Sadheko","Paneer Chilli","Chicken Chilli","Crispy Corn","Vegetable Spring Rolls","Chicken Wings","Fish Fingers","Potato Wedges","Nachos Supreme","Hummus with Pita","Caesar Salad","Greek Salad"]', 0),
      (3, N'["Tomato Basil Soup","Cream of Mushroom Soup","Sweet Corn Chicken Soup","Hot and Sour Soup","Chicken Thukpa Soup","Quinoa Garden Salad","Grilled Chicken Salad","Beetroot Feta Salad","Roasted Pumpkin Soup","French Onion Soup","Minestrone Soup","Tuna Nicoise Salad","Watermelon Feta Salad"]', 1),
      (4, N'["Steamed Chicken Momo","Steamed Buff Momo","Steamed Vegetable Momo","Kothey Chicken Momo","Kothey Buff Momo","Jhol Chicken Momo","Jhol Buff Momo","Chilli Chicken Momo","Chilli Buff Momo","Fried Vegetable Momo","Open Chicken Momo","Sadheko Momo","Momo Platter"]', 0),
      (5, N'["Chicken Dal Bhat Set","Mutton Dal Bhat Set","Vegetable Dal Bhat Set","Newari Khaja Set","Thakali Chicken Set","Thakali Mutton Set","Dhido Chicken Set","Dhido Gundruk Set","Aloo Tama Curry","Gundruk ko Jhol","Kwati Bowl","Sekuwa Khaja Set","Nepali Fish Curry"]', 0),
      (6, N'["Butter Chicken","Chicken Tikka Masala","Kadai Chicken","Mutton Rogan Josh","Palak Paneer","Paneer Butter Masala","Dal Makhani","Yellow Dal Tadka","Chana Masala","Aloo Gobi","Mixed Vegetable Korma","Fish Masala Curry","Prawn Coconut Curry"]', 0),
      (7, N'["Tandoori Chicken Half","Tandoori Chicken Full","Chicken Tikka","Malai Chicken Tikka","Mutton Seekh Kebab","Buff Seekh Kebab","Paneer Tikka","Grilled Trout","Lamb Chops","Mixed Grill Platter","Garlic Naan","Butter Naan"]', 0),
      (8, N'["Chicken Chow Mein","Buff Chow Mein","Vegetable Chow Mein","Chicken Fried Rice","Buff Fried Rice","Vegetable Fried Rice","Pad Thai Chicken","Thai Basil Fried Rice","Singapore Noodles","Chicken Biryani","Mutton Biryani","Vegetable Biryani"]', 0),
      (9, N'["Margherita Pizza","Farmhouse Vegetable Pizza","Chicken Tikka Pizza","Pepperoni Pizza","Quattro Formaggi Pizza","Spaghetti Aglio Olio","Spaghetti Bolognese","Penne Arrabbiata","Penne Alfredo Chicken","Lasagna Bolognese","Vegetable Lasagna","Pesto Chicken Pasta"]', 1),
      (10,N'["Classic Chicken Burger","Smash Buff Burger","Crispy Chicken Burger","Paneer Tikka Burger","Mushroom Swiss Burger","Club Sandwich","Grilled Chicken Sandwich","Tuna Melt Sandwich","Vegetable Panini","Chicken Shawarma Wrap","Falafel Wrap","BLT Sandwich"]', 0),
      (11,N'["Sizzling Brownie","New York Cheesecake","Tiramisu","Chocolate Lava Cake","Creme Brulee","Seasonal Fruit Platter","Gulab Jamun","Kheer","Carrot Halwa","Apple Pie","Banoffee Pie","Ice Cream Trio"]', 1),
      (12,N'["Espresso","Cappuccino","Cafe Latte","Masala Tea","Fresh Lemon Soda","Mint Lemonade","Mango Lassi","Virgin Mojito","Himalayan Iced Tea","Fresh Orange Juice","House Red Wine Glass","Lager Beer Pint"]', 1)
    ) g(CategoryNo, ItemsJson, DefaultVeg)
), Expanded AS
(
    SELECT g.CategoryNo,
           CONVERT(int, j.[key]) + 1 AS WithinCategory,
           CONVERT(nvarchar(150), j.[value]) AS Name,
           g.DefaultVeg,
           ROW_NUMBER() OVER (ORDER BY g.CategoryNo, CONVERT(int, j.[key])) AS MenuNo
    FROM MenuGroups g
    CROSS APPLY OPENJSON(g.ItemsJson) j
)
INSERT #MenuSeed
    (MenuNo, Id, ProductId, CategoryNo, CategoryId, StationId, Name, Price, PreparationMinutes, IsVeg, SpiceLevel)
SELECT e.MenuNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/MENUITEM/', e.MenuNo))),
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/MENUPRODUCT/', e.MenuNo))),
       e.CategoryNo,
       c.Id,
       s.Id,
       e.Name,
       c.BasePrice + (e.WithinCategory - 1) * CASE WHEN e.CategoryNo = 12 THEN 25 ELSE 35 END,
       CASE e.CategoryNo WHEN 12 THEN 5 WHEN 11 THEN 10 WHEN 7 THEN 28 WHEN 5 THEN 25 WHEN 6 THEN 25 ELSE 18 END
           + (e.WithinCategory % 4),
       CASE
           WHEN e.Name LIKE N'%Chicken%' OR e.Name LIKE N'%Buff%' OR e.Name LIKE N'%Mutton%'
             OR e.Name LIKE N'%Fish%' OR e.Name LIKE N'%Prawn%' OR e.Name LIKE N'%Tuna%'
             OR e.Name LIKE N'%Pepperoni%' OR e.Name LIKE N'%Bacon%' OR e.Name LIKE N'%BLT%'
             OR e.Name LIKE N'%Egg%' OR e.Name LIKE N'%Lamb%' OR e.Name LIKE N'%Trout%' THEN 0
           ELSE e.DefaultVeg
       END,
       CASE WHEN e.CategoryNo IN (2,4,5,6,8) THEN 2 WHEN e.CategoryNo = 7 THEN 1 ELSE 0 END
FROM Expanded e
JOIN #CategorySeed c ON c.CategoryNo = e.CategoryNo
JOIN #StationSeed s ON s.StationNo = c.StationNo;

IF (SELECT COUNT(*) FROM #MenuSeed) <> 150
    THROW 51003, 'The curated menu seed must contain exactly 150 rows.', 1;

INSERT tbl_Product
    (Id, DateMiti, ProductCode, ProductType, Name, HsCode, Mrp, SalesRate, PurchaseRate,
     MinimumStock, MaximumStock, Margin, IsOpeningStock, Description, IsActive, IsDeleted,
     TaxId, ProductGroupId, UnitId, TenantId)
SELECT m.ProductId, N'2083-04-20', CONCAT(N'MENU-', FORMAT(m.MenuNo, '000')), 6, m.Name, NULL,
       m.Price, m.Price, ROUND(m.Price * 0.32, 2), 0, 0, 68, 0,
       CONCAT(N'House-prepared ', LOWER(m.Name), N' made to order.'), 1, 0,
       @VatTaxId, @ProductGroupId, @PcsUnitId, @TenantId
FROM #MenuSeed m
WHERE NOT EXISTS (SELECT 1 FROM tbl_Product p WHERE p.Id = m.ProductId);

UPDATE p
SET p.Name=m.Name,p.ProductCode=CONCAT(N'MENU-',FORMAT(m.MenuNo,'000')),p.Mrp=m.Price,p.SalesRate=m.Price,
    p.PurchaseRate=ROUND(m.Price*0.32,2),p.Description=CONCAT(N'House-prepared ',LOWER(m.Name),N' made to order.'),
    p.IsActive=1,p.IsDeleted=0
FROM tbl_Product p JOIN #MenuSeed m ON m.ProductId=p.Id
WHERE p.TenantId=@TenantId;

INSERT tbl_RestaurantMenuItem
    (Id, CategoryId, ProductId, StationId, DisplayName, Price, PreparationMinutes, SortOrder,
     IsActive, IsDeleted, TenantId, ColorHex, Description, HasModifiers, HasVariants, ImageUrl,
     IsAvailable, IsFeatured, IsVeg, ShortCode, SpiceLevel, UnavailableUntil)
SELECT m.Id, m.CategoryId, m.ProductId, m.StationId, m.Name, m.Price, m.PreparationMinutes, m.MenuNo,
       1, 0, @TenantId,
       CASE m.CategoryNo % 6 WHEN 0 THEN N'#8B5E3C' WHEN 1 THEN N'#D97706' WHEN 2 THEN N'#B45309'
            WHEN 3 THEN N'#047857' WHEN 4 THEN N'#B91C1C' ELSE N'#7C3AED' END,
       CONCAT(N'Kitchen-tested ', LOWER(m.Name), N' prepared with fresh ingredients and consistent portions.'),
       1, 1, NULL, 1, CASE WHEN m.MenuNo % 9 = 0 THEN 1 ELSE 0 END, m.IsVeg,
       CONCAT(N'M', FORMAT(m.MenuNo, '003')), m.SpiceLevel, NULL
FROM #MenuSeed m
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantMenuItem x WHERE x.Id = m.Id);

UPDATE x
SET x.DisplayName=m.Name,x.Price=m.Price,x.PreparationMinutes=m.PreparationMinutes,
    x.Description=CONCAT(N'Kitchen-tested ',LOWER(m.Name),N' prepared with fresh ingredients and consistent portions.'),
    x.IsActive=1,x.IsDeleted=0,x.IsAvailable=1,x.IsVeg=m.IsVeg,x.SpiceLevel=m.SpiceLevel
FROM tbl_RestaurantMenuItem x JOIN #MenuSeed m ON m.Id=x.Id
WHERE x.TenantId=@TenantId;

INSERT tbl_RestaurantMenuVariant
    (Id, MenuItemId, Name, PriceDelta, IsAbsolutePrice, IsDefault, SortOrder, IsActive, IsDeleted, TenantId)
SELECT CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/VARIANT/', m.MenuNo))),
       m.Id,
       CASE m.CategoryNo WHEN 12 THEN N'Regular' WHEN 9 THEN N'10 inch' WHEN 7 THEN N'Single portion' ELSE N'Standard' END,
       0, 0, 1, 1, 1, 0, @TenantId
FROM #MenuSeed m
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantMenuVariant v
    WHERE v.Id = CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/VARIANT/', m.MenuNo)))
);

CREATE TABLE #ModifierGroupSeed (GroupNo int PRIMARY KEY, Id uniqueidentifier, Name nvarchar(100), MinSelect int, MaxSelect int, IsRequired bit);
INSERT #ModifierGroupSeed
SELECT GroupNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/MODGROUP/', GroupNo))),
       Name, MinSelect, MaxSelect, IsRequired
FROM (VALUES
    (1,N'Breakfast Choices',0,2,0),(2,N'Sauces & Sides',0,3,0),(3,N'Soup & Salad Extras',0,3,0),
    (4,N'Momo Sauce & Style',1,2,1),(5,N'Nepali Set Extras',0,3,0),(6,N'Curry Add-ons',0,3,0),
    (7,N'Grill Sides',0,3,0),(8,N'Wok Add-ons',0,3,0),(9,N'Pizza & Pasta Extras',0,4,0),
    (10,N'Burger & Sandwich Extras',0,4,0),(11,N'Dessert Extras',0,3,0),(12,N'Beverage Choices',0,3,0)
) x(GroupNo,Name,MinSelect,MaxSelect,IsRequired);

INSERT tbl_RestaurantModifierGroup (Id, Name, MinSelect, MaxSelect, IsRequired, SortOrder, IsActive, IsDeleted, TenantId)
SELECT g.Id,g.Name,g.MinSelect,g.MaxSelect,g.IsRequired,g.GroupNo,1,0,@TenantId
FROM #ModifierGroupSeed g
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantModifierGroup x WHERE x.Id=g.Id);

CREATE TABLE #ModifierSeed (ModifierNo int PRIMARY KEY, Id uniqueidentifier, GroupId uniqueidentifier, Name nvarchar(100), PriceDelta decimal(18,2));
;WITH ModifierGroups AS
(
    SELECT * FROM (VALUES
      (1,N'["Two Eggs","Chicken Sausage","Hash Brown","Grilled Tomato","Sauteed Mushroom","Cheese Slice","Avocado","Toast Basket","Seasonal Fruit","Extra Butter"]'),
      (2,N'["Garlic Aioli","Hot Chilli Sauce","Tomato Salsa","Cheese Dip","French Fries","Garden Salad","Pita Bread","Pickled Vegetables","Roasted Peanuts","Lemon Wedge"]'),
      (3,N'["Grilled Chicken","Crispy Bacon","Feta Cheese","Boiled Egg","Avocado","Garlic Croutons","Toasted Seeds","Pita Bread","Extra Dressing","Soup Bread Roll"]'),
      (4,N'["Mild Jhol","Spicy Jhol","Tomato Achar","Sesame Achar","Fried Finish","Kothey Finish","Extra Momo 2 pcs","Cheese Topping","Coriander Garnish","Timur Chilli Oil"]'),
      (5,N'["Extra Rice","Extra Dal","Seasonal Tarkari","Chicken Curry Bowl","Mutton Curry Bowl","Gundruk Achar","Papad","Plain Curd","Ghee Spoon","House Pickle"]'),
      (6,N'["Plain Rice","Jeera Rice","Butter Naan","Garlic Naan","Lachha Paratha","Extra Paneer","Extra Chicken","Fresh Cream","Green Salad","Mixed Pickle"]'),
      (7,N'["French Fries","Mashed Potato","Sauteed Vegetables","Garden Salad","Garlic Bread","Pepper Sauce","Mushroom Sauce","Mint Chutney","Grilled Lemon","Extra Kebab"]'),
      (8,N'["Fried Egg","Extra Chicken","Extra Buff","Prawns","Tofu","Mixed Vegetables","Chilli Oil","Roasted Peanuts","Extra Noodles","Extra Rice"]'),
      (9,N'["Extra Mozzarella","Parmesan","Chicken Tikka","Pepperoni","Mushroom","Black Olive","Jalapeno","Garlic Bread","Side Salad","Gluten-free Base"]'),
      (10,N'["Cheddar Cheese","Fried Egg","Crispy Bacon","Avocado","Caramelised Onion","Jalapeno","French Fries","Potato Wedges","Coleslaw","Extra Patty"]'),
      (11,N'["Vanilla Ice Cream","Chocolate Ice Cream","Whipped Cream","Chocolate Sauce","Caramel Sauce","Fresh Berries","Roasted Nuts","Espresso Shot","Seasonal Fruit","Birthday Message"]'),
      (12,N'["Soy Milk","Oat Milk","Extra Espresso Shot","Vanilla Syrup","Caramel Syrup","Lemon Slice","Mint Leaves","Soda Top-up","Less Sugar","Takeaway Cup"]')
    ) g(GroupNo, ItemsJson)
), Expanded AS
(
    SELECT g.GroupNo, CONVERT(int,j.[key])+1 WithinGroup, CONVERT(nvarchar(100),j.[value]) Name,
           ROW_NUMBER() OVER (ORDER BY g.GroupNo,CONVERT(int,j.[key])) ModifierNo
    FROM ModifierGroups g CROSS APPLY OPENJSON(g.ItemsJson) j
)
INSERT #ModifierSeed
SELECT e.ModifierNo,
       CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT(N'NWR-BUSY/MODIFIER/',e.ModifierNo))),
       g.Id,e.Name,
       CASE WHEN e.Name LIKE N'%Less Sugar%' OR e.Name LIKE N'%Garnish%' OR e.Name LIKE N'%Wedge%' THEN 0
            ELSE 35 + (e.WithinGroup-1)*15 END
FROM Expanded e JOIN #ModifierGroupSeed g ON g.GroupNo=e.GroupNo;

IF (SELECT COUNT(*) FROM #ModifierSeed) <> 120
    THROW 51004, 'The curated modifier seed must contain exactly 120 rows.', 1;

INSERT tbl_RestaurantModifier (Id, ModifierGroupId, Name, PriceDelta, SortOrder, IsActive, IsDeleted, TenantId)
SELECT m.Id,m.GroupId,m.Name,m.PriceDelta,((m.ModifierNo-1)%10)+1,1,0,@TenantId
FROM #ModifierSeed m
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantModifier x WHERE x.Id=m.Id);

UPDATE x SET x.Name=m.Name,x.PriceDelta=m.PriceDelta,x.IsActive=1,x.IsDeleted=0
FROM tbl_RestaurantModifier x JOIN #ModifierSeed m ON m.Id=x.Id
WHERE x.TenantId=@TenantId;

INSERT tbl_RestaurantMenuItemModifierGroup (Id, MenuItemId, ModifierGroupId, SortOrder, TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ITEMMODGROUP/',m.MenuNo))),
       m.Id,g.Id,1,@TenantId
FROM #MenuSeed m JOIN #ModifierGroupSeed g ON g.GroupNo=m.CategoryNo
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantMenuItemModifierGroup x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ITEMMODGROUP/',m.MenuNo)))
);

INSERT tbl_RestaurantMenuItemTag (Id, MenuItemId, Name, ColorHex, SortOrder, TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/TAG/',m.MenuNo))),m.Id,
       CASE m.MenuNo%5 WHEN 0 THEN N'Bestseller' WHEN 1 THEN N'Chef''s Pick' WHEN 2 THEN N'Popular'
            WHEN 3 THEN CASE WHEN m.IsVeg=1 THEN N'Vegetarian' ELSE N'House Special' END ELSE N'Guest Favourite' END,
       CASE m.MenuNo%5 WHEN 0 THEN N'#DC2626' WHEN 1 THEN N'#D97706' WHEN 2 THEN N'#2563EB'
            WHEN 3 THEN N'#059669' ELSE N'#7C3AED' END,1,@TenantId
FROM #MenuSeed m
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantMenuItemTag x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/TAG/',m.MenuNo)))
);

/* Two coherent recipe lines per menu item provide 100% recipe coverage. */
;WITH RecipeLines AS
(
    SELECT m.MenuNo,m.ProductId,1 RecipeLineNo,
           CASE m.CategoryNo WHEN 1 THEN 42 WHEN 2 THEN 31 WHEN 3 THEN 1 WHEN 4 THEN 47 WHEN 5 THEN 61 WHEN 6 THEN 31
                WHEN 7 THEN 31 WHEN 8 THEN 66 WHEN 9 THEN 54 WHEN 10 THEN 36 WHEN 11 THEN 51 ELSE 101 END RawNo,
           CAST(CASE WHEN m.CategoryNo=12 THEN 1.000 ELSE 0.180 END AS decimal(18,3)) Qty
    FROM #MenuSeed m
    UNION ALL
    SELECT m.MenuNo,m.ProductId,2,
           CASE m.CategoryNo WHEN 1 THEN 63 WHEN 2 THEN 23 WHEN 3 THEN 49 WHEN 4 THEN 63 WHEN 5 THEN 17 WHEN 6 THEN 57
                WHEN 7 THEN 57 WHEN 8 THEN 10 WHEN 9 THEN 63 WHEN 10 THEN 55 WHEN 11 THEN 52 ELSE 104 END,
           CAST(CASE WHEN m.CategoryNo=12 THEN 0.050 ELSE 0.080 END AS decimal(18,3))
    FROM #MenuSeed m
)
INSERT tbl_Bom (Id,IsDeleted,ProductId,RawMaterialId,Quantity,UnitId,Date,TenantId,CostRate,IsActive,WastagePercentage)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BOM/',r.MenuNo,N'/',r.RecipeLineNo))),
       0,r.ProductId,raw.Id,r.Qty,raw.UnitId,DATEADD(day,-90,@Now),@TenantId,raw.PurchaseRate,1,
       CASE WHEN r.RecipeLineNo=1 THEN 3.00 ELSE 2.00 END
FROM RecipeLines r JOIN #RawSeed raw ON raw.RawNo=r.RawNo
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_Bom b
    WHERE b.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BOM/',r.MenuNo,N'/',r.RecipeLineNo)))
);

/* Twelve real suppliers, with one preferred mapping per raw material (120 rows). */
CREATE TABLE #SupplierSeed (SupplierNo int PRIMARY KEY, Id uniqueidentifier, Name nvarchar(150), Phone nvarchar(50), Email nvarchar(150));
INSERT #SupplierSeed
SELECT SupplierNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SUPPLIER/',SupplierNo))),
       Name,Phone,Email
FROM (VALUES
    (1,N'Kalimati Fresh Produce Co.',N'01-5900101',N'orders@kalimatifresh.test'),
    (2,N'Valley Organic Farms',N'01-5900102',N'sales@valleyorganic.test'),
    (3,N'Himalayan Poultry Supply',N'01-5900103',N'orders@himalayanpoultry.test'),
    (4,N'Kathmandu Meat House',N'01-5900104',N'trade@ktmmeathouse.test'),
    (5,N'Everest Dairy Distributors',N'01-5900105',N'orders@everestdairy.test'),
    (6,N'Annapurna Cheese & Cream',N'01-5900106',N'supply@annapurnacheese.test'),
    (7,N'New Road Food Grains',N'01-5900107',N'orders@newroadgrains.test'),
    (8,N'Patan Bakery Ingredients',N'01-5900108',N'sales@patanbakery.test'),
    (9,N'Asan Spice Traders',N'01-5900109',N'orders@asanspice.test'),
    (10,N'Himalayan Herbs & Seasoning',N'01-5900110',N'supply@himalayanherbs.test'),
    (11,N'Nepal Beverage Solutions',N'01-5900111',N'orders@nepalbeverage.test'),
    (12,N'GreenPack Hospitality Supplies',N'01-5900112',N'sales@greenpack.test')
) x(SupplierNo,Name,Phone,Email);

INSERT tbl_AccountLedger
    (Id,Name,OpeningBalance,IsDefault,CrOrDr,Narration,Address,Phone,Email,CreditPeriod,CreditLimit,
     IsBillByBill,Pan,Status,IsDelete,IsCompany,OpeningDate,UserId,CreateUserId,UpdateUserId,ParentId,
     AccountGroupId,TenantId)
SELECT s.Id,s.Name,0,0,1,N'Restaurant supply partner',N'Kathmandu, Nepal',s.Phone,s.Email,30,500000,
       1,NULL,1,0,1,DATEADD(day,-365,@Now),NULL,@AdminUserId,NULL,NULL,@CreditorGroupId,@TenantId
FROM #SupplierSeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_AccountLedger l WHERE l.Id=s.Id);

INSERT tbl_RestaurantSupplierItemMapping
    (Id,ProductId,SupplierLedgerId,SupplierSku,UnitId,Rate,LeadTimeDays,MinimumOrderQty,
     IsPreferred,IsActive,IsDeleted,CreatedAt,TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SUPPLIERMAP/',r.RawNo))),
       r.Id,s.Id,CONCAT(N'SKU-',FORMAT(r.RawNo,'000')),r.UnitId,r.PurchaseRate,
       1+(r.RawNo%4),CASE WHEN r.UnitId=@PcsUnitId THEN 12 ELSE 2 END,1,1,0,DATEADD(day,-90,@Now),@TenantId
FROM #RawSeed r JOIN #SupplierSeed s ON s.SupplierNo=r.SupplierNo
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantSupplierItemMapping x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SUPPLIERMAP/',r.RawNo)))
);

/* Natural customer names are reused in a deterministic service pattern; phone numbers are fictional test data. */
CREATE TABLE #CustomerSeed (CustomerNo int PRIMARY KEY, Name nvarchar(150), Phone nvarchar(50));
INSERT #CustomerSeed
SELECT CustomerNo,Name,CONCAT(N'980000',FORMAT(CustomerNo,'0000'))
FROM (VALUES
    (1,N'Aarav Shrestha'),(2,N'Anisha Karki'),(3,N'Bikash Gurung'),(4,N'Pragya Adhikari'),
    (5,N'Rohan Maharjan'),(6,N'Sneha Joshi'),(7,N'Nabin Rai'),(8,N'Sarita Thapa'),
    (9,N'Sujan Tamang'),(10,N'Asmita Poudel'),(11,N'Kiran Lama'),(12,N'Monika Bista'),
    (13,N'Dipesh Khadka'),(14,N'Ritika KC'),(15,N'Saurav Gautam'),(16,N'Puja Basnet'),
    (17,N'Roshan Shahi'),(18,N'Manju Chaudhary'),(19,N'Aashish Pandey'),(20,N'Kabita Oli'),
    (21,N'Prabin Malla'),(22,N'Smriti Shakya'),(23,N'Bimal Raut'),(24,N'Neha Bhattarai'),
    (25,N'Rajendra Singh'),(26,N'Meena Yadav'),(27,N'Samir Tuladhar'),(28,N'Bina Dongol'),
    (29,N'Ayush Ghimire'),(30,N'Rachana Regmi'),(31,N'Suraj Koirala'),(32,N'Karuna Dahal'),
    (33,N'Bibek Sapkota'),(34,N'Nisha Acharya'),(35,N'Sanjay Mandal'),(36,N'Anju Sherpa'),
    (37,N'Ramesh Khatri'),(38,N'Sabina Magar'),(39,N'Lokesh Jha'),(40,N'Sushma Rana')
) x(CustomerNo,Name);

/* 150 table sessions: 75 active service sessions and 75 recent completed sessions. */
CREATE TABLE #SessionSeed
(
    SessionNo int PRIMARY KEY,
    Id uniqueidentifier,
    TableId uniqueidentifier,
    Status int,
    OpenedAt datetime2,
    ClosedAt datetime2 NULL,
    GuestCount int,
    CustomerName nvarchar(150),
    CustomerPhone nvarchar(50)
);

;WITH n AS
(
    SELECT TOP (150) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) SessionNo
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT #SessionSeed
SELECT n.SessionNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SESSION/',n.SessionNo))),
       t.Id,
       CASE WHEN n.SessionNo<=75 THEN 2 ELSE 6 END,
       CASE WHEN n.SessionNo<=75
            THEN DATEADD(minute,-(25+n.SessionNo*2),@Now)
            ELSE DATEADD(minute,720+((n.SessionNo%8)*35),DATEADD(day,-(1+((n.SessionNo-76)%30)),CONVERT(datetime2,CONVERT(date,@Now)))) END,
       CASE WHEN n.SessionNo<=75 THEN NULL
            ELSE DATEADD(minute,790+((n.SessionNo%8)*35),DATEADD(day,-(1+((n.SessionNo-76)%30)),CONVERT(datetime2,CONVERT(date,@Now)))) END,
       1+(n.SessionNo%6),c.Name,c.Phone
FROM n
JOIN #TableSeed t ON t.TableNo=CASE WHEN n.SessionNo<=75 THEN n.SessionNo ELSE n.SessionNo-75 END
JOIN #CustomerSeed c ON c.CustomerNo=((n.SessionNo-1)%40)+1;

INSERT tbl_RestaurantTableSession
    (Id,TableId,SessionNo,Status,OpenedAt,ClosedAt,GuestCount,WaiterUserId,CustomerName,CustomerPhoneNo,TenantId)
SELECT s.Id,s.TableId,CONCAT(N'SES-',FORMAT(s.SessionNo,'0000')),s.Status,s.OpenedAt,s.ClosedAt,
       s.GuestCount,@AdminUserId,s.CustomerName,s.CustomerPhone,@TenantId
FROM #SessionSeed s
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantTableSession x WHERE x.Id=s.Id);

/* Build all 200 orders and 600 coherent order lines in memory before inserting accounting links. */
CREATE TABLE #OrderSeed
(
    OrderNo int PRIMARY KEY,
    Id uniqueidentifier,
    SalesMasterId uniqueidentifier NULL,
    OrderCode nvarchar(50),
    OrderType int,
    Status int,
    TableId uniqueidentifier NULL,
    TableSessionId uniqueidentifier NULL,
    DeviceId uniqueidentifier,
    Source nvarchar(100),
    CustomerName nvarchar(150),
    CustomerPhone nvarchar(50),
    Notes nvarchar(250),
    CreatedAt datetime2,
    SentAt datetime2,
    BilledAt datetime2 NULL,
    GrossAmount decimal(18,2),
    DiscountAmount decimal(18,2),
    TaxAmount decimal(18,2),
    NetAmount decimal(18,2),
    GrandTotal decimal(18,2)
);

;WITH n AS
(
    SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) OrderNo
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
), Prepared AS
(
    SELECT n.OrderNo,
           ((n.OrderNo-1)%100)+1 ServiceNo,
           CASE WHEN n.OrderNo<=100 THEN 1 ELSE 0 END IsOpen,
           CASE WHEN ((n.OrderNo-1)%100)+1<=75 THEN 0
                WHEN ((n.OrderNo-1)%100)+1<=88 THEN 1 ELSE 2 END OrderType
    FROM n
)
INSERT #OrderSeed
SELECT p.OrderNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ORDER/',p.OrderNo))),
       CASE WHEN p.IsOpen=0 THEN CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SALE/',p.ServiceNo))) END,
       CONCAT(CASE WHEN p.IsOpen=1 THEN N'LIVE-' ELSE N'INV-' END,FORMAT(p.ServiceNo,'0000')),
       p.OrderType,
       CASE WHEN p.IsOpen=1 THEN CASE p.ServiceNo%4 WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 3 ELSE 2 END
            ELSE CASE WHEN p.ServiceNo%5=0 THEN 6 ELSE 5 END END,
       CASE WHEN p.OrderType=0 THEN t.Id END,
       CASE WHEN p.OrderType=0 THEN ss.Id END,
       d.Id,
       CASE p.OrderType WHEN 0 THEN N'POS' WHEN 1 THEN N'Takeaway Counter'
            ELSE CASE p.ServiceNo%3 WHEN 0 THEN N'Foodmandu' WHEN 1 THEN N'Pathao Food' ELSE N'Bhojdeals' END END,
       c.Name,c.Phone,
       CASE p.ServiceNo%8 WHEN 0 THEN N'Guest requested quick service.' WHEN 1 THEN N'Please serve water first.'
            WHEN 2 THEN N'Medium spice preferred.' WHEN 3 THEN N'Birthday table; dessert after mains.'
            WHEN 4 THEN N'No plastic cutlery for this order.' ELSE N'' END,
       service.CreatedAt,
       DATEADD(minute,4,service.CreatedAt),
       CASE WHEN p.IsOpen=0 THEN DATEADD(minute,68,service.CreatedAt) END,
       0,0,0,0,0
FROM Prepared p
CROSS APPLY
(
    SELECT CASE WHEN p.IsOpen=1
                THEN DATEADD(minute,-(8+p.ServiceNo*2),@Now)
                ELSE DATEADD(minute,720+((p.ServiceNo%9)*45),DATEADD(day,-(1+((p.ServiceNo-1)%30)),CONVERT(datetime2,CONVERT(date,@Now)))) END CreatedAt
) service
JOIN #CustomerSeed c ON c.CustomerNo=((p.ServiceNo-1)%40)+1
JOIN #DeviceSeed d ON d.DeviceNo=((p.ServiceNo-1)%10)+1
LEFT JOIN #TableSeed t ON t.TableNo=p.ServiceNo AND p.OrderType=0
LEFT JOIN #SessionSeed ss ON ss.SessionNo=CASE WHEN p.IsOpen=1 THEN p.ServiceNo ELSE 75+p.ServiceNo END AND p.OrderType=0;

CREATE TABLE #OrderItemSeed
(
    ItemNo int PRIMARY KEY,
    Id uniqueidentifier,
    OrderNo int,
    OrderId uniqueidentifier,
    ItemLineNo int,
    MenuNo int,
    MenuItemId uniqueidentifier,
    ProductId uniqueidentifier,
    StationId uniqueidentifier,
    TaxId uniqueidentifier,
    Qty decimal(18,3),
    Rate decimal(18,2),
    ModifierTotal decimal(18,2),
    DiscountAmount decimal(18,2),
    TaxAmount decimal(18,2),
    NetAmount decimal(18,2),
    Amount decimal(18,2),
    Status int,
    CreatedAt datetime2,
    VariantId uniqueidentifier,
    VariantName nvarchar(100),
    ItemName nvarchar(150),
    CategoryNo int
);

;WITH Lines AS
(
    SELECT o.OrderNo,o.Id OrderId,o.Status,o.CreatedAt,v.ItemLineNo,
           ((o.OrderNo*7+v.ItemLineNo*11-1)%150)+1 MenuNo,
           (1+((o.OrderNo+v.ItemLineNo)%3)/2) Qty
    FROM #OrderSeed o CROSS JOIN (VALUES(1),(2),(3)) v(ItemLineNo)
), Priced AS
(
    SELECT l.*,m.Id MenuItemId,m.ProductId,m.StationId,m.Price,m.Name,m.CategoryNo,
           CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/VARIANT/',m.MenuNo))) VariantId,
           CASE WHEN l.ItemLineNo=1 THEN ms.PriceDelta*l.Qty ELSE 0 END ModifierTotal,
           l.Qty*m.Price + CASE WHEN l.ItemLineNo=1 THEN ms.PriceDelta*l.Qty ELSE 0 END Gross
    FROM Lines l
    JOIN #MenuSeed m ON m.MenuNo=l.MenuNo
    JOIN #ModifierSeed ms ON ms.ModifierNo=((m.CategoryNo-1)*10)+((l.OrderNo-1)%10)+1
), Netted AS
(
    SELECT p.*,ROUND(CASE WHEN p.OrderNo%10=0 THEN p.Gross*0.05 ELSE 0 END,2) Discount
    FROM Priced p
)
INSERT #OrderItemSeed
SELECT (n.OrderNo-1)*3+n.ItemLineNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ORDERITEM/',n.OrderNo,N'/',n.ItemLineNo))),
       n.OrderNo,n.OrderId,n.ItemLineNo,n.MenuNo,n.MenuItemId,n.ProductId,n.StationId,@VatTaxId,n.Qty,n.Price,n.ModifierTotal,n.Discount,
       ROUND((n.Gross-n.Discount)*0.13,2),n.Gross-n.Discount,
       ROUND((n.Gross-n.Discount)*1.13,2),
       CASE WHEN n.Status IN (5,6) THEN 4 WHEN n.Status=1 THEN 1 WHEN n.Status=2 THEN 2 WHEN n.Status=3 THEN 3 ELSE 1 END,
       DATEADD(minute,n.ItemLineNo,n.CreatedAt),n.VariantId,
       CASE n.CategoryNo WHEN 12 THEN N'Regular' WHEN 9 THEN N'10 inch' WHEN 7 THEN N'Single portion' ELSE N'Standard' END,
       n.Name,n.CategoryNo
FROM Netted n;

UPDATE o
SET GrossAmount=x.GrossAmount,
    DiscountAmount=x.DiscountAmount,
    TaxAmount=x.TaxAmount,
    NetAmount=x.NetAmount,
    GrandTotal=x.GrandTotal
FROM #OrderSeed o
JOIN
(
    SELECT OrderNo,SUM(Qty*Rate+ModifierTotal) GrossAmount,SUM(DiscountAmount) DiscountAmount,
           SUM(TaxAmount) TaxAmount,SUM(NetAmount) NetAmount,SUM(Amount) GrandTotal
    FROM #OrderItemSeed GROUP BY OrderNo
) x ON x.OrderNo=o.OrderNo;

/* 100 restaurant-tagged sales documents drive all sales and margin reports. */
INSERT tbl_SalesMaster
    (Id,VoucherNumbering,TenantId,VoucherNo,SalesAccountId,Date,DateMiti,CreditPeriod,CreditDate,
     CreatedDate,Description,TaxAmount,BillDiscount,GrandTotal,GrossAmount,TaxableAmount,IsPrint,
     NoOfPrint,NetAmount,SyncwithIrd,IrdSyncDateTime,PrintedTime,IsRealTime,PaymentMethod,
     PaymentMethodLedgerId,IsDelete,VatRefundAmount,LrNo,VehicleNo,AgainstId,SalesModeType,
     AgainstVoucherNo,PINumber,InvoiceType,SalesType,PrintUserId,VoucherTypeId,LedgerName,
     CustomerAddress,CustomerPhoneNo,VatNo,LedgerId,ServiceDeliveryId,IsErrorFixed,FinancialYearId,
     CreateUserId,UpdateUserId,PostingNumbering,SourceDocumentId,SourceModule)
SELECT o.SalesMasterId,900000+s.ServiceNo,@TenantId,CONCAT(N'RS-',FORMAT(s.ServiceNo,'0000')),
       @SalesAccountId,o.BilledAt,N'2083-04-20',0,o.BilledAt,o.BilledAt,
       CONCAT(N'Restaurant bill for ',o.OrderCode),o.TaxAmount,o.DiscountAmount,o.GrandTotal,o.GrossAmount,
       o.NetAmount,1,1,o.GrandTotal,0,NULL,CONVERT(nvarchar(30),o.BilledAt,120),1,
       CASE s.ServiceNo%10 WHEN 0 THEN 2 WHEN 1 THEN 5 WHEN 2 THEN 5 WHEN 3 THEN 3 ELSE 0 END,
       CASE WHEN s.ServiceNo%10 IN (4,5,6,7,8,9) THEN @CashAccountId END,
       0,0,NULL,NULL,NULL,0,NULL,NULL,0,0,@AdminUserId,@SalesVoucherTypeId,o.CustomerName,
       N'Kathmandu',o.CustomerPhone,NULL,NULL,NULL,0,@FinancialYearId,@AdminUserId,NULL,
       900000+s.ServiceNo,o.Id,N'Restaurant'
FROM #OrderSeed o
CROSS APPLY (SELECT o.OrderNo-100 ServiceNo) s
WHERE o.OrderNo>100
  AND NOT EXISTS (SELECT 1 FROM tbl_SalesMaster x WHERE x.Id=o.SalesMasterId);

INSERT tbl_RestaurantOrder
    (Id,OrderNo,OrderType,Status,TableId,TableSessionId,WaiterUserId,DeviceId,Source,ClientRequestId,
     CustomerName,CustomerPhoneNo,Notes,GrossAmount,DiscountAmount,TaxAmount,NetAmount,GrandTotal,
     SalesMasterId,CreatedAt,SentAt,BilledAt,TenantId)
SELECT o.Id,o.OrderCode,o.OrderType,o.Status,o.TableId,o.TableSessionId,@AdminUserId,o.DeviceId,o.Source,
       CONCAT(N'busy-seed-',FORMAT(o.OrderNo,'0000')),o.CustomerName,o.CustomerPhone,o.Notes,
       o.GrossAmount,o.DiscountAmount,o.TaxAmount,o.NetAmount,o.GrandTotal,o.SalesMasterId,
       o.CreatedAt,o.SentAt,o.BilledAt,@TenantId
FROM #OrderSeed o
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantOrder x WHERE x.Id=o.Id);

INSERT tbl_RestaurantOrderItem
    (Id,OrderId,MenuItemId,ProductId,UnitId,StationId,TaxId,Qty,Rate,DiscountAmount,TaxAmount,
     NetAmount,Amount,Status,Notes,CreatedAt,TenantId,CancelReason,FireSequence,ItemNameSnapshot,
     ModifierTotal,StationTypeSnapshot,UnitPriceSnapshot,VariantId,VariantNameSnapshot)
SELECT i.Id,i.OrderId,i.MenuItemId,i.ProductId,@PcsUnitId,i.StationId,i.TaxId,i.Qty,i.Rate,
       i.DiscountAmount,i.TaxAmount,i.NetAmount,i.Amount,i.Status,
       CASE i.ItemLineNo WHEN 1 THEN N'Fire with starters.' WHEN 3 THEN N'Course after mains.' ELSE N'' END,
       i.CreatedAt,@TenantId,NULL,i.ItemLineNo,i.ItemName,i.ModifierTotal,
       s.StationType,i.Rate,i.VariantId,i.VariantName
FROM #OrderItemSeed i JOIN #StationSeed s ON s.Id=i.StationId
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantOrderItem x WHERE x.Id=i.Id);

UPDATE x
SET x.ItemNameSnapshot=i.ItemName,x.VariantNameSnapshot=i.VariantName,x.Rate=i.Rate,
    x.UnitPriceSnapshot=i.Rate,x.ModifierTotal=i.ModifierTotal
FROM tbl_RestaurantOrderItem x JOIN #OrderItemSeed i ON i.Id=x.Id
WHERE x.TenantId=@TenantId;

/* One relevant modifier on the first line of each order: 200 rows. */
INSERT tbl_RestaurantOrderItemModifier
    (Id,OrderItemId,ModifierId,ModifierNameSnapshot,PriceDelta,Qty,Amount,TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ORDERITEMMOD/',i.OrderNo))),
       i.Id,m.Id,m.Name,m.PriceDelta,i.Qty,m.PriceDelta*i.Qty,@TenantId
FROM #OrderItemSeed i
JOIN #ModifierSeed m ON m.ModifierNo=((i.CategoryNo-1)*10)+((i.OrderNo-1)%10)+1
WHERE i.ItemLineNo=1
  AND NOT EXISTS
  (
      SELECT 1 FROM tbl_RestaurantOrderItemModifier x
      WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ORDERITEMMOD/',i.OrderNo)))
  );

UPDATE x
SET x.ModifierNameSnapshot=m.Name,x.PriceDelta=m.PriceDelta,x.Amount=m.PriceDelta*i.Qty
FROM tbl_RestaurantOrderItemModifier x
JOIN #OrderItemSeed i ON i.Id=x.OrderItemId AND i.ItemLineNo=1
JOIN #ModifierSeed m ON m.ModifierNo=((i.CategoryNo-1)*10)+((i.OrderNo-1)%10)+1
WHERE x.TenantId=@TenantId
  AND x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/ORDERITEMMOD/',i.OrderNo)));

/* 300 sales details and bill lines support item, category, food-cost, and margin reports. */
INSERT tbl_SalesDetails
    (Id,TenantId,Qty,Rate,TaxAmount,Discount,DiscountPer,GrossAmount,NetAmount,AgainstDetailId,
     Amount,SalesMasterId,ProductName,ProductId,UnitId,TaxId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SALEDETAIL/',i.ItemNo))),
       @TenantId,i.Qty,i.Rate,i.TaxAmount,i.DiscountAmount,
       CASE WHEN i.DiscountAmount>0 THEN 5 ELSE 0 END,i.Qty*i.Rate+i.ModifierTotal,i.Amount,NULL,
       i.Amount,o.SalesMasterId,i.ItemName,i.ProductId,@PcsUnitId,@VatTaxId
FROM #OrderItemSeed i JOIN #OrderSeed o ON o.OrderNo=i.OrderNo
WHERE i.OrderNo>100
  AND NOT EXISTS
  (
      SELECT 1 FROM tbl_SalesDetails x
      WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SALEDETAIL/',i.ItemNo)))
  );

UPDATE x SET x.ProductName=i.ItemName
FROM tbl_SalesDetails x
JOIN #OrderItemSeed i
  ON x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SALEDETAIL/',i.ItemNo)))
WHERE x.TenantId=@TenantId AND i.OrderNo>100;

INSERT tbl_RestaurantBillLine
    (Id,OrderId,OrderItemId,SalesMasterId,Qty,GrossAmount,ModifierTotal,DiscountAmount,TaxAmount,
     NetAmount,Amount,BilledAt,TenantId,SalesDetailId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BILLLINE/',i.ItemNo))),
       i.OrderId,i.Id,o.SalesMasterId,i.Qty,i.Qty*i.Rate+i.ModifierTotal,i.ModifierTotal,
       i.DiscountAmount,i.TaxAmount,i.NetAmount,i.Amount,o.BilledAt,@TenantId,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/SALEDETAIL/',i.ItemNo)))
FROM #OrderItemSeed i JOIN #OrderSeed o ON o.OrderNo=i.OrderNo
WHERE i.OrderNo>100
  AND NOT EXISTS
  (
      SELECT 1 FROM tbl_RestaurantBillLine x
      WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BILLLINE/',i.ItemNo)))
  );

INSERT tbl_RestaurantBillPayment
    (Id,OrderId,SalesMasterId,BillAmount,TipAmount,PayableAmount,CustomerPaidAmount,ReturnAmount,PaidAt,TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BILLPAYMENT/',o.OrderNo-100))),
       o.Id,o.SalesMasterId,o.GrandTotal,
       CASE WHEN o.OrderNo%4=0 THEN ROUND(o.GrandTotal*0.05,2) ELSE 0 END,
       o.GrandTotal+CASE WHEN o.OrderNo%4=0 THEN ROUND(o.GrandTotal*0.05,2) ELSE 0 END,
       CEILING((o.GrandTotal+CASE WHEN o.OrderNo%4=0 THEN ROUND(o.GrandTotal*0.05,2) ELSE 0 END)/100)*100,
       CEILING((o.GrandTotal+CASE WHEN o.OrderNo%4=0 THEN ROUND(o.GrandTotal*0.05,2) ELSE 0 END)/100)*100
         -(o.GrandTotal+CASE WHEN o.OrderNo%4=0 THEN ROUND(o.GrandTotal*0.05,2) ELSE 0 END),
       o.BilledAt,@TenantId
FROM #OrderSeed o
WHERE o.OrderNo>100
  AND NOT EXISTS
  (
      SELECT 1 FROM tbl_RestaurantBillPayment x
      WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BILLPAYMENT/',o.OrderNo-100)))
  );

/* 150 live KOT/BOT cards spread across the 100 open orders. */
CREATE TABLE #TicketSeed
(
    TicketNo int PRIMARY KEY,
    Id uniqueidentifier,
    OrderItemId uniqueidentifier,
    OrderId uniqueidentifier,
    StationId uniqueidentifier,
    TicketType int,
    Status int,
    SentAt datetime2,
    StartedAt datetime2 NULL,
    ReadyAt datetime2 NULL
);

;WITH n AS
(
    SELECT TOP (150) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) TicketNo
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
), Mapped AS
(
    SELECT n.TicketNo,CASE WHEN n.TicketNo<=100 THEN n.TicketNo ELSE n.TicketNo-100 END OrderNo,
           CASE WHEN n.TicketNo<=100 THEN 1 ELSE 2 END ItemLineNo
    FROM n
)
INSERT #TicketSeed
SELECT m.TicketNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/TICKET/',m.TicketNo))),
       i.Id,i.OrderId,i.StationId,CASE WHEN s.StationType=1 THEN 1 ELSE 0 END,
       CASE m.TicketNo%3 WHEN 0 THEN 0 WHEN 1 THEN 1 ELSE 2 END,
       DATEADD(minute,5,i.CreatedAt),
       CASE WHEN m.TicketNo%3 IN (1,2) THEN DATEADD(minute,9,i.CreatedAt) END,
       CASE WHEN m.TicketNo%3=2 THEN DATEADD(minute,17,i.CreatedAt) END
FROM Mapped m
JOIN #OrderItemSeed i ON i.OrderNo=m.OrderNo AND i.ItemLineNo=m.ItemLineNo
JOIN #StationSeed s ON s.Id=i.StationId;

INSERT tbl_RestaurantTicket
    (Id,TicketNo,OrderId,StationId,TicketType,Status,SentAt,ReadyAt,TenantId,CancelReason,
     CancelledAt,LastPrintedAt,PrintCount,PrintedAt,Purpose,ServedAt,StartedAt)
SELECT t.Id,CONCAT(CASE WHEN t.TicketType=1 THEN N'BOT-' ELSE N'KOT-' END,FORMAT(t.TicketNo,'0000')),
       t.OrderId,t.StationId,t.TicketType,t.Status,t.SentAt,t.ReadyAt,@TenantId,NULL,NULL,
       DATEADD(minute,1,t.SentAt),1,DATEADD(minute,1,t.SentAt),0,NULL,t.StartedAt
FROM #TicketSeed t
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantTicket x WHERE x.Id=t.Id);

INSERT tbl_RestaurantTicketItem (Id,TicketId,OrderItemId,Qty,Status,TenantId,CancelReason)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/TICKETITEM/',t.TicketNo))),
       t.Id,t.OrderItemId,i.Qty,CASE t.Status WHEN 0 THEN 1 WHEN 1 THEN 2 ELSE 3 END,@TenantId,NULL
FROM #TicketSeed t JOIN #OrderItemSeed i ON i.Id=t.OrderItemId
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantTicketItem x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/TICKETITEM/',t.TicketNo)))
);

/* 120 auditable stock adjustments, each linked to its ERP stock posting. */
CREATE TABLE #StockAdjustmentSeed
(
    AdjustmentNo int PRIMARY KEY,
    Id uniqueidentifier,
    LineId uniqueidentifier,
    StockPostingId uniqueidentifier,
    RawId uniqueidentifier,
    UnitId uniqueidentifier,
    Rate decimal(18,2),
    Qty decimal(18,3),
    AdjustmentType int,
    Date datetime2,
    VoucherNo nvarchar(50),
    VoucherTypeId uniqueidentifier,
    Reason nvarchar(250)
);

INSERT #StockAdjustmentSeed
SELECT r.RawNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/STOCKADJ/',r.RawNo))),
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/STOCKADJLINE/',r.RawNo))),
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/STOCKPOST/',r.RawNo))),
       r.Id,r.UnitId,r.PurchaseRate,CAST(2+(r.RawNo%5) AS decimal(18,3)),
       CASE WHEN r.RawNo<=40 THEN 0 WHEN r.RawNo<=80 THEN 2 WHEN r.RawNo<=100 THEN 1 ELSE 3 END,
       DATEADD(hour,9+(r.RawNo%8),DATEADD(day,-(r.RawNo%30),CONVERT(datetime2,CONVERT(date,@Now)))),
       CONCAT(N'RSA-',FORMAT(r.RawNo,'0000')),
       CASE WHEN r.RawNo BETWEEN 41 AND 80 THEN @PhysicalStockVoucherTypeId ELSE @StockJournalVoucherTypeId END,
       CASE WHEN r.RawNo<=40 THEN N'Opening shift top-up after receiving.'
            WHEN r.RawNo<=80 THEN N'Weekly physical count correction.'
            WHEN r.RawNo<=100 THEN N'Production transfer variance.'
            ELSE N'Kitchen wastage recorded during closing.' END
FROM #RawSeed r;

INSERT tbl_RestaurantStockAdjustment
    (Id,VoucherNo,Date,DateMiti,AdjustmentType,Description,VoucherTypeId,VoucherNumbering,
     FinancialYearId,CreateUserId,CreatedAt,IsDeleted,TenantId)
SELECT a.Id,a.VoucherNo,a.Date,N'2083-04-20',a.AdjustmentType,a.Reason,a.VoucherTypeId,
       920000+a.AdjustmentNo,@FinancialYearId,@AdminUserId,a.Date,0,@TenantId
FROM #StockAdjustmentSeed a
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantStockAdjustment x WHERE x.Id=a.Id);

INSERT tbl_StockPosting
    (Id,VoucherNumbering,IsDeleted,Date,DateMiti,VoucherTypeId,VoucherNo,GrossAmount,DiscountAmount,
     NetAmount,Amount,TaxAmount,ProductId,UnitId,LedgerId,AgainstVoucherTypeId,AgainstVoucherNo,
     InWardQty,OutWardQty,Rate,IsValueIncrease,FinancialYearId,VendorVoucherNo,MasterId,TenantId,SourceDetailId)
SELECT a.StockPostingId,920000+a.AdjustmentNo,0,a.Date,N'2083-04-20',a.VoucherTypeId,a.VoucherNo,
       a.Qty*a.Rate,0,a.Qty*a.Rate,a.Qty*a.Rate,0,a.RawId,a.UnitId,NULL,a.VoucherTypeId,a.VoucherNo,
       CASE WHEN a.AdjustmentType IN (0,2) THEN a.Qty ELSE 0 END,
       CASE WHEN a.AdjustmentType IN (1,3) THEN a.Qty ELSE 0 END,
       a.Rate,CASE WHEN a.AdjustmentType IN (0,2) THEN 1 ELSE 0 END,@FinancialYearId,NULL,a.Id,@TenantId,NULL
FROM #StockAdjustmentSeed a
WHERE NOT EXISTS (SELECT 1 FROM tbl_StockPosting x WHERE x.Id=a.StockPostingId);

INSERT tbl_RestaurantStockAdjustmentLine
    (Id,StockAdjustmentId,ProductId,UnitId,Qty,Rate,Amount,SystemQty,CountedQty,Reason,
     StockPostingId,IsDeleted,TenantId)
SELECT a.LineId,a.Id,a.RawId,a.UnitId,a.Qty,a.Rate,a.Qty*a.Rate,
       CASE WHEN a.AdjustmentType=2 THEN 2 ELSE 0 END,
       CASE WHEN a.AdjustmentType=2 THEN 2+a.Qty ELSE a.Qty END,
       a.Reason,a.StockPostingId,0,@TenantId
FROM #StockAdjustmentSeed a
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantStockAdjustmentLine x WHERE x.Id=a.LineId);

/* 150 consumption rows connect billed menu sales back to their recipe ingredients. */
;WITH Consumption AS
(
    SELECT TOP (150) i.ItemNo,i.Id OrderItemId,i.ProductId MenuProductId,i.Qty,i.OrderId,o.SalesMasterId,
           o.BilledAt,bl.SalesDetailId,bom.RawMaterialId,bom.UnitId,bom.CostRate,
           ROW_NUMBER() OVER (ORDER BY i.ItemNo) ConsumptionNo
    FROM #OrderItemSeed i
    JOIN #OrderSeed o ON o.OrderNo=i.OrderNo
    JOIN tbl_RestaurantBillLine bl ON bl.OrderItemId=i.Id AND bl.TenantId=@TenantId
    JOIN tbl_Bom bom ON bom.ProductId=i.ProductId AND bom.TenantId=@TenantId AND bom.IsActive=1 AND bom.IsDeleted=0
    WHERE i.OrderNo>100
      AND bom.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/BOM/',i.MenuNo,N'/1')))
    ORDER BY i.ItemNo
)
INSERT tbl_StockPosting
    (Id,VoucherNumbering,IsDeleted,Date,DateMiti,VoucherTypeId,VoucherNo,GrossAmount,DiscountAmount,
     NetAmount,Amount,TaxAmount,ProductId,UnitId,LedgerId,AgainstVoucherTypeId,AgainstVoucherNo,
     InWardQty,OutWardQty,Rate,IsValueIncrease,FinancialYearId,VendorVoucherNo,MasterId,TenantId,SourceDetailId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/CONSUMPTION/',c.ConsumptionNo))),
       930000+c.ConsumptionNo,0,c.BilledAt,N'2083-04-20',@StockIssueVoucherTypeId,
       CONCAT(N'RCON-',FORMAT(c.ConsumptionNo,'0000')),
       ROUND(c.Qty*0.18*c.CostRate,2),0,ROUND(c.Qty*0.18*c.CostRate,2),ROUND(c.Qty*0.18*c.CostRate,2),0,
       c.RawMaterialId,c.UnitId,NULL,@SalesVoucherTypeId,CONCAT(N'RS-',FORMAT(((c.ItemNo-1)/3)-99,'0000')),
       0,c.Qty*0.18,c.CostRate,0,@FinancialYearId,NULL,c.SalesMasterId,@TenantId,c.SalesDetailId
FROM Consumption c
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_StockPosting x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/CONSUMPTION/',c.ConsumptionNo)))
);

/* Compact channel masters plus 100-150 row operational grids. */
CREATE TABLE #ChannelSeed
(
    ChannelNo int PRIMARY KEY,
    Id uniqueidentifier,
    Name nvarchar(100),
    ChannelType int,
    Provider int,
    Commission decimal(18,2),
    Markup decimal(18,2)
);
INSERT #ChannelSeed
SELECT ChannelNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/CHANNEL/',ChannelNo))),
       Name,ChannelType,Provider,Commission,Markup
FROM (VALUES
    (1,N'Dine-in POS',0,0,0.00,0.00),
    (2,N'Takeaway Counter',1,0,0.00,0.00),
    (3,N'Direct Web & QR',2,1,3.00,0.00),
    (4,N'Foodmandu',3,2,25.00,12.00),
    (5,N'Pathao Food',3,3,22.00,10.00),
    (6,N'Bhojdeals',3,4,20.00,8.00)
) x(ChannelNo,Name,ChannelType,Provider,Commission,Markup);

INSERT tbl_RestaurantChannel
    (Id,Name,ChannelType,Provider,CommissionPercent,DefaultPriceMarkupPercent,SortOrder,
     IsOnline,IsActive,IsDeleted,CreatedAt,TenantId)
SELECT c.Id,c.Name,c.ChannelType,c.Provider,c.Commission,c.Markup,c.ChannelNo,
       CASE WHEN c.ChannelNo>=3 THEN 1 ELSE 0 END,1,0,DATEADD(day,-120,@Now),@TenantId
FROM #ChannelSeed c
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantChannel x WHERE x.Id=c.Id);

INSERT tbl_RestaurantChannelAccount
    (Id,ChannelId,Provider,ExternalStoreId,DisplayName,ApiBaseUrl,ApiCredentialsJson,WebhookSecret,
     IsOnline,IsActive,IsDeleted,LastMenuSyncAt,LastOrderSyncAt,CreatedAt,TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/CHANNELACCOUNT/',c.ChannelNo))),
       c.Id,c.Provider,CONCAT(N'KTM-RESTRO-',c.ChannelNo),CONCAT(c.Name,N' - Kathmandu'),
       CASE c.Provider WHEN 1 THEN N'https://orders.nextwaverestro.test'
            WHEN 2 THEN N'https://partner.foodmandu.test' WHEN 3 THEN N'https://food.pathao.test'
            ELSE N'https://merchant.bhojdeals.test' END,
       N'{"mode":"demo","credentials":"not-stored"}',N'demo-webhook-secret',1,1,0,
       DATEADD(minute,-18,@Now),DATEADD(minute,-5*c.ChannelNo,@Now),DATEADD(day,-120,@Now),@TenantId
FROM #ChannelSeed c
WHERE c.ChannelNo>=3
  AND NOT EXISTS
  (
      SELECT 1 FROM tbl_RestaurantChannelAccount x
      WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/CHANNELACCOUNT/',c.ChannelNo)))
  );

CREATE TABLE #ChannelItemSeed
(
    ChannelItemNo int PRIMARY KEY,
    Id uniqueidentifier,
    ChannelId uniqueidentifier,
    Provider int,
    MenuItemId uniqueidentifier,
    ChannelPrice decimal(18,2)
);
INSERT #ChannelItemSeed
SELECT m.MenuNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/CHANNELITEM/',m.MenuNo))),
       c.Id,c.Provider,m.Id,ROUND(m.Price*(1+c.Markup/100.0),0)
FROM #MenuSeed m
JOIN #ChannelSeed c ON c.ChannelNo=3+((m.MenuNo-1)%4);

INSERT tbl_RestaurantChannelItem
    (Id,ChannelId,MenuItemId,ExternalItemId,ExternalSku,ChannelPrice,IsOnline,SyncStatus,
     LastSyncedAt,LastSyncMessage,IsDeleted,TenantId)
SELECT ci.Id,ci.ChannelId,ci.MenuItemId,CONCAT(N'EXT-',FORMAT(ci.ChannelItemNo,'0000')),
       CONCAT(N'CH-',FORMAT(ci.ChannelItemNo,'0000')),ci.ChannelPrice,1,
       CASE WHEN ci.ChannelItemNo%20=0 THEN 2 ELSE 1 END,DATEADD(minute,-(ci.ChannelItemNo%90),@Now),
       CASE WHEN ci.ChannelItemNo%20=0 THEN N'Awaiting provider retry after timeout.' ELSE N'Menu item synchronized.' END,
       0,@TenantId
FROM #ChannelItemSeed ci
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantChannelItem x WHERE x.Id=ci.Id);

INSERT tbl_RestaurantMenuSyncLog
    (Id,ChannelId,ChannelItemId,Provider,Operation,Status,RequestJson,ResponseJson,Message,
     CreatedAt,CompletedAt,TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/MENUSYNC/',ci.ChannelItemNo))),
       ci.ChannelId,ci.Id,ci.Provider,N'Upsert',CASE WHEN ci.ChannelItemNo%20=0 THEN 2 ELSE 1 END,
       CONCAT(N'{"sku":"CH-',FORMAT(ci.ChannelItemNo,'0000'),N'","operation":"upsert"}'),
       CASE WHEN ci.ChannelItemNo%20=0 THEN N'{"status":"timeout"}' ELSE N'{"status":"accepted"}' END,
       CASE WHEN ci.ChannelItemNo%20=0 THEN N'Provider timeout; retry is queued.' ELSE N'Published successfully.' END,
       DATEADD(minute,-(150-ci.ChannelItemNo),@Now),
       CASE WHEN ci.ChannelItemNo%20=0 THEN NULL ELSE DATEADD(second,4,DATEADD(minute,-(150-ci.ChannelItemNo),@Now)) END,
       @TenantId
FROM #ChannelItemSeed ci
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantMenuSyncLog x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/MENUSYNC/',ci.ChannelItemNo)))
);

CREATE TABLE #AggregatorOrderSeed
(
    AggregatorNo int PRIMARY KEY,
    Id uniqueidentifier,
    ChannelId uniqueidentifier,
    Provider int,
    ExternalOrderId nvarchar(100),
    Status int,
    ExpectedAmount decimal(18,2),
    CommissionAmount decimal(18,2),
    RestaurantDiscountAmount decimal(18,2),
    DeliveryFeeAmount decimal(18,2),
    PaidAmount decimal(18,2),
    ReceivedAt datetime2,
    CustomerName nvarchar(150),
    CustomerPhone nvarchar(50),
    OrderId uniqueidentifier NULL
);

;WITH n AS
(
    SELECT TOP (120) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AggregatorNo
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT #AggregatorOrderSeed
SELECT n.AggregatorNo,
       CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/AGGORDER/',n.AggregatorNo))),
       c.Id,c.Provider,CONCAT(CASE c.Provider WHEN 2 THEN N'FM-' WHEN 3 THEN N'PF-' ELSE N'BD-' END,FORMAT(n.AggregatorNo,'00000')),
       CASE WHEN n.AggregatorNo<=80 THEN 4 WHEN n.AggregatorNo<=90 THEN 1 WHEN n.AggregatorNo<=100 THEN 0
            WHEN n.AggregatorNo<=110 THEN 2 ELSE 3 END,
       amount.Expected,
       ROUND(amount.Expected*c.Commission/100.0,2),
       CASE WHEN n.AggregatorNo%6=0 THEN 75 ELSE 0 END,
       60+(n.AggregatorNo%3)*20,
       CASE WHEN n.AggregatorNo<=80 THEN amount.Expected-ROUND(amount.Expected*c.Commission/100.0,2)
            -CASE WHEN n.AggregatorNo%6=0 THEN 75 ELSE 0 END ELSE 0 END,
       DATEADD(minute,660+(n.AggregatorNo%12)*35,DATEADD(day,-(n.AggregatorNo%30),CONVERT(datetime2,CONVERT(date,@Now)))),
       cust.Name,cust.Phone,
       CASE WHEN n.AggregatorNo<=80 THEN o.Id END
FROM n
JOIN #ChannelSeed c ON c.ChannelNo=4+((n.AggregatorNo-1)%3)
JOIN #CustomerSeed cust ON cust.CustomerNo=((n.AggregatorNo-1)%40)+1
LEFT JOIN #OrderSeed o ON o.OrderNo=100+((n.AggregatorNo-1)%100)+1
CROSS APPLY (SELECT CAST(650+(n.AggregatorNo%9)*125 AS decimal(18,2)) Expected) amount;

INSERT tbl_RestaurantAggregatorOrder
    (Id,ChannelId,Provider,ExternalOrderId,Status,CustomerName,CustomerPhoneNo,DeliveryAddress,
     ExpectedAmount,CommissionAmount,RestaurantDiscountAmount,DeliveryFeeAmount,PaidAmount,RawPayloadJson,
     OrderId,ReceivedAt,AcceptedAt,RejectedAt,CancelledAt,StatusMessage,TenantId)
SELECT a.Id,a.ChannelId,a.Provider,a.ExternalOrderId,a.Status,a.CustomerName,a.CustomerPhone,
       CONCAT(N'Ward ',1+(a.AggregatorNo%15),N', Kathmandu'),a.ExpectedAmount,a.CommissionAmount,
       a.RestaurantDiscountAmount,a.DeliveryFeeAmount,a.PaidAmount,
       CONCAT(N'{"source":"busy-demo","externalOrderId":"',a.ExternalOrderId,N'"}'),a.OrderId,a.ReceivedAt,
       CASE WHEN a.Status IN (1,4) THEN DATEADD(minute,3,a.ReceivedAt) END,
       CASE WHEN a.Status=2 THEN DATEADD(minute,5,a.ReceivedAt) END,
       CASE WHEN a.Status=3 THEN DATEADD(minute,7,a.ReceivedAt) END,
       CASE a.Status WHEN 0 THEN N'Awaiting restaurant acceptance.' WHEN 1 THEN N'Accepted; kitchen preparation started.'
            WHEN 2 THEN N'Outside delivery radius.' WHEN 3 THEN N'Cancelled by customer.' ELSE N'Completed and reconciled.' END,
       @TenantId
FROM #AggregatorOrderSeed a
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantAggregatorOrder x WHERE x.Id=a.Id);

/* 100 payout batches and 100 reconciliation lines. */
CREATE TABLE #PayoutSeed
(
    PayoutNo int PRIMARY KEY,
    Id uniqueidentifier,
    ChannelId uniqueidentifier,
    Provider int,
    AggregatorOrderId uniqueidentifier,
    ExternalOrderId nvarchar(100),
    GrossAmount decimal(18,2),
    CommissionAmount decimal(18,2),
    DiscountAmount decimal(18,2),
    DeliveryFee decimal(18,2),
    NetAmount decimal(18,2),
    PaidAt datetime2
);
INSERT #PayoutSeed
SELECT a.AggregatorNo,a.Id,
       a.ChannelId,a.Provider,a.Id,a.ExternalOrderId,a.ExpectedAmount,a.CommissionAmount,
       a.RestaurantDiscountAmount,a.DeliveryFeeAmount,
       a.ExpectedAmount-a.CommissionAmount-a.RestaurantDiscountAmount,
       DATEADD(day,3,a.ReceivedAt)
FROM #AggregatorOrderSeed a WHERE a.AggregatorNo<=100;

UPDATE #PayoutSeed
SET Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/PAYOUT/',PayoutNo)));

INSERT tbl_RestaurantAggregatorPayout
    (Id,ChannelId,Provider,ExternalPayoutId,PeriodFrom,PeriodTo,PaidAt,GrossAmount,CommissionAmount,
     DeductionsAmount,NetPaidAmount,Notes,CreatedAt,TenantId)
SELECT p.Id,p.ChannelId,p.Provider,CONCAT(N'PAY-',FORMAT(p.PayoutNo,'00000')),
       DATEADD(day,-1,p.PaidAt),p.PaidAt,p.PaidAt,p.GrossAmount,p.CommissionAmount,
       p.DiscountAmount,p.NetAmount,N'Daily provider settlement batch.',p.PaidAt,@TenantId
FROM #PayoutSeed p
WHERE NOT EXISTS (SELECT 1 FROM tbl_RestaurantAggregatorPayout x WHERE x.Id=p.Id);

INSERT tbl_RestaurantAggregatorPayoutLine
    (Id,PayoutId,AggregatorOrderId,ExternalOrderId,ExpectedAmount,CommissionAmount,
     RestaurantDiscountAmount,DeliveryFeeAmount,PaidAmount,MatchStatus,MatchMessage,TenantId)
SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/PAYOUTLINE/',p.PayoutNo))),
       p.Id,p.AggregatorOrderId,p.ExternalOrderId,p.GrossAmount,p.CommissionAmount,
       p.DiscountAmount,p.DeliveryFee,p.NetAmount,
       CASE p.PayoutNo%20 WHEN 0 THEN 3 WHEN 1 THEN 2 ELSE 1 END,
       CASE p.PayoutNo%20 WHEN 0 THEN N'Provider commission differs by NPR 25.'
            WHEN 1 THEN N'Order reference requires manual review.' ELSE N'Amounts matched automatically.' END,
       @TenantId
FROM #PayoutSeed p
WHERE NOT EXISTS
(
    SELECT 1 FROM tbl_RestaurantAggregatorPayoutLine x
    WHERE x.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT(N'NWR-BUSY/PAYOUTLINE/',p.PayoutNo)))
);

COMMIT TRANSACTION;

/* A compact post-run manifest is returned to sqlcmd and CI logs. */
SELECT Dataset, [Rows]
FROM (VALUES
    (N'Dining tables',             (SELECT COUNT(*) FROM tbl_RestaurantTable WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #TableSeed))),
    (N'Menu items',                (SELECT COUNT(*) FROM tbl_RestaurantMenuItem WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #MenuSeed))),
    (N'Raw materials',             (SELECT COUNT(*) FROM tbl_Product WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #RawSeed))),
    (N'Supplier mappings',         (SELECT COUNT(*) FROM tbl_RestaurantSupplierItemMapping WHERE TenantId=@TenantId AND ProductId IN (SELECT Id FROM #RawSeed))),
    (N'Table sessions',            (SELECT COUNT(*) FROM tbl_RestaurantTableSession WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #SessionSeed))),
    (N'Open POS orders',           (SELECT COUNT(*) FROM tbl_RestaurantOrder WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #OrderSeed WHERE OrderNo<=100))),
    (N'Billed/closed orders',      (SELECT COUNT(*) FROM tbl_RestaurantOrder WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #OrderSeed WHERE OrderNo>100))),
    (N'Open KOT/BOT tickets',      (SELECT COUNT(*) FROM tbl_RestaurantTicket WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #TicketSeed))),
    (N'Stock adjustments',         (SELECT COUNT(*) FROM tbl_RestaurantStockAdjustment WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #StockAdjustmentSeed))),
    (N'Channel menu items',        (SELECT COUNT(*) FROM tbl_RestaurantChannelItem WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #ChannelItemSeed))),
    (N'Aggregator orders',         (SELECT COUNT(*) FROM tbl_RestaurantAggregatorOrder WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #AggregatorOrderSeed))),
    (N'Aggregator payouts',        (SELECT COUNT(*) FROM tbl_RestaurantAggregatorPayout WHERE TenantId=@TenantId AND Id IN (SELECT Id FROM #PayoutSeed))),
    (N'Menu sync logs',            (SELECT COUNT(*) FROM tbl_RestaurantMenuSyncLog WHERE TenantId=@TenantId))
) summary(Dataset,[Rows])
ORDER BY Dataset;
