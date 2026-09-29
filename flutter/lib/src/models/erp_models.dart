part of '../../main.dart';

enum ModuleKind {
  dashboard,
  pos,
  kds,
  restaurantInventory,
  menuRecipes,
  channels,
  restaurantSetup,
  restaurantReports,
  restaurantPayroll,
  guestOrders,
  reservations,
  printQueue,
  accountGroups,
  accountLedgers,
  units,
  productGroups,
  products,
  openingStocks,
  purchaseOrder,
  purchaseInvoice,
  purchaseReturns,
  salesInvoice,
  salesReturn,
  paymentMasters,
  restaurantStockUsed,
  receiptMasters,
  journalMasters,
  contraMasters,
  pdcPayable,
  pdcReceivable,
  pdcClearance,
  accountGroupReport,
  accountLedgerReport,
  bookReport,
  inventoryReport,
  purchaseReport,
  salesReport,
}

class ErpModule {
  const ErpModule({
    required this.kind,
    required this.title,
    required this.group,
    required this.path,
    required this.icon,
    required this.color,
    required this.description,
  });

  final ModuleKind kind;
  final String title;
  final String group;
  final String path;
  final IconData icon;
  final Color color;
  final String description;
}

class MenuProduct {
  const MenuProduct({
    required this.id,
    required this.name,
    required this.category,
    required this.station,
    required this.price,
    required this.cost,
    required this.stock,
    required this.color,
    required this.recipe,
    this.productId = '',
    this.categoryId = '',
    this.stationId,
    this.productType = 0,
    this.isAvailable = true,
    this.hasRecipe = false,
    this.variants = const [],
    this.modifierGroups = const [],
    this.shortCode = '',
    this.description = '',
    this.imageUrl = '',
    this.preparationMinutes = 0,
    this.sortOrder = 0,
    this.isVeg,
    this.spiceLevel,
    this.isFeatured = false,
    this.isActive = true,
    this.unavailableUntil,
  });

  final String id;
  final String name;
  final String category;
  final String station;
  final double price;
  final double cost;
  final double stock;
  final Color color;
  final List<String> recipe;
  final String productId;
  final String categoryId;
  final String? stationId;
  final int productType;
  final bool isAvailable;
  final bool hasRecipe;
  final List<MenuVariant> variants;
  final List<ModifierGroup> modifierGroups;
  final String shortCode;
  final String description;
  final String imageUrl;
  final int preparationMinutes;
  final int sortOrder;
  final bool? isVeg;
  final int? spiceLevel;
  final bool isFeatured;
  final bool isActive;
  final DateTime? unavailableUntil;

  bool get available =>
      isActive &&
      isAvailable &&
      stock > 0 &&
      (unavailableUntil == null || unavailableUntil!.isBefore(DateTime.now()));
  double get margin => price == 0 ? 0 : (price - cost) / price;
}

class MenuVariant {
  const MenuVariant({
    required this.id,
    required this.name,
    required this.priceDelta,
    required this.isAbsolutePrice,
    required this.isDefault,
  });

  final String id;
  final String name;
  final double priceDelta;
  final bool isAbsolutePrice;
  final bool isDefault;
}

class MenuModifier {
  const MenuModifier({
    required this.id,
    required this.name,
    required this.priceDelta,
  });

  final String id;
  final String name;
  final double priceDelta;
}

class ModifierGroup {
  const ModifierGroup({
    required this.id,
    required this.name,
    required this.minSelect,
    required this.maxSelect,
    required this.isRequired,
    required this.modifiers,
  });

  final String id;
  final String name;
  final int minSelect;
  final int maxSelect;
  final bool isRequired;
  final List<MenuModifier> modifiers;
}

class ProductConfiguration {
  const ProductConfiguration({this.variant, this.modifiers = const []});

  final MenuVariant? variant;
  final List<MenuModifier> modifiers;
}

class CartLine {
  const CartLine({
    required this.product,
    required this.qty,
    this.orderItemId,
    this.variant,
    this.modifiers = const [],
    this.status = 0,
    this.notes = '',
    this.discountAmount = 0,
    this.billedQty = 0,
    this.unbilledQty,
    this.ticketItemId,
    this.unitName = '',
  });

  final MenuProduct product;
  final int qty;
  final String? orderItemId;
  final MenuVariant? variant;
  final List<MenuModifier> modifiers;
  final int status;
  final String notes;
  final double discountAmount;
  final double billedQty;
  final double? unbilledQty;
  final String? ticketItemId;
  final String unitName;

  bool get editable => status == 0;
  double get unitPrice {
    final selected = variant;
    if (selected == null) return product.price;
    return selected.isAbsolutePrice
        ? selected.priceDelta
        : product.price + selected.priceDelta;
  }

  double get modifierUnitTotal =>
      modifiers.fold(0, (total, modifier) => total + modifier.priceDelta);
  double get total =>
      math.max(0, (unitPrice + modifierUnitTotal) * qty - discountAmount);
  String get configurationKey {
    final modifierIds = modifiers.map((item) => item.id).toList()..sort();
    return '${product.id}|${variant?.id ?? ''}|${modifierIds.join(',')}';
  }

  String get detailLabel => [
    if (variant != null) variant!.name,
    if (modifiers.isNotEmpty) modifiers.map((item) => item.name).join(', '),
  ].join(' · ');

  CartLine copyWith({
    MenuProduct? product,
    int? qty,
    String? orderItemId,
    MenuVariant? variant,
    List<MenuModifier>? modifiers,
    int? status,
    String? notes,
    double? discountAmount,
    double? billedQty,
    double? unbilledQty,
    String? ticketItemId,
    String? unitName,
  }) {
    return CartLine(
      product: product ?? this.product,
      qty: qty ?? this.qty,
      orderItemId: orderItemId ?? this.orderItemId,
      variant: variant ?? this.variant,
      modifiers: modifiers ?? this.modifiers,
      status: status ?? this.status,
      notes: notes ?? this.notes,
      discountAmount: discountAmount ?? this.discountAmount,
      billedQty: billedQty ?? this.billedQty,
      unbilledQty: unbilledQty ?? this.unbilledQty,
      ticketItemId: ticketItemId ?? this.ticketItemId,
      unitName: unitName ?? this.unitName,
    );
  }
}

enum TicketStatus { queued, acknowledged, preparing, ready, bumped }

class KdsTicket {
  const KdsTicket({
    required this.id,
    required this.table,
    required this.station,
    required this.channel,
    required this.minutes,
    required this.status,
    required this.items,
    this.serverId,
    this.orderId,
    this.orderNo = '',
    this.ticketType = 0,
    this.purpose = 0,
  });

  final String id;
  final String table;
  final String station;
  final String channel;
  final int minutes;
  final TicketStatus status;
  final List<CartLine> items;
  final String? serverId;
  final String? orderId;
  final String orderNo;
  final int ticketType;
  final int purpose;

  KdsTicket copyWith({
    String? id,
    String? table,
    String? station,
    String? channel,
    int? minutes,
    TicketStatus? status,
    List<CartLine>? items,
    String? serverId,
    String? orderId,
    String? orderNo,
    int? ticketType,
    int? purpose,
  }) {
    return KdsTicket(
      id: id ?? this.id,
      table: table ?? this.table,
      station: station ?? this.station,
      channel: channel ?? this.channel,
      minutes: minutes ?? this.minutes,
      status: status ?? this.status,
      items: items ?? this.items,
      serverId: serverId ?? this.serverId,
      orderId: orderId ?? this.orderId,
      orderNo: orderNo ?? this.orderNo,
      ticketType: ticketType ?? this.ticketType,
      purpose: purpose ?? this.purpose,
    );
  }
}

class InventoryItem {
  const InventoryItem({
    required this.name,
    required this.group,
    required this.unit,
    required this.available,
    required this.reorder,
    required this.cost,
    required this.supplier,
    this.productId = '',
    this.unitId = '',
    this.maximum = 0,
    this.pendingPurchase = 0,
    this.suggested = 0,
    this.missingSupplierMapping = false,
  });

  final String name;
  final String group;
  final String unit;
  final double available;
  final double reorder;
  final double cost;
  final String supplier;
  final String productId;
  final String unitId;
  final double maximum;
  final double pendingPurchase;
  final double suggested;
  final bool missingSupplierMapping;

  bool get low => available <= reorder;
}

class ChannelStatus {
  const ChannelStatus({
    required this.name,
    required this.provider,
    required this.orders,
    required this.sales,
    required this.commission,
    required this.online,
    required this.sync,
    this.id = '',
    this.channelType = 0,
    this.providerCode = 0,
    this.priceMarkupPercent = 0,
    this.sortOrder = 0,
    this.isActive = true,
  });

  final String name;
  final String provider;
  final int orders;
  final double sales;
  final double commission;
  final bool online;
  final String sync;
  final String id;
  final int channelType;
  final int providerCode;
  final double priceMarkupPercent;
  final int sortOrder;
  final bool isActive;
}

class RestaurantAreaModel {
  const RestaurantAreaModel({
    required this.id,
    required this.name,
    this.description = '',
    this.sortOrder = 0,
    this.isActive = true,
  });

  final String id;
  final String name;
  final String description;
  final int sortOrder;
  final bool isActive;
}

class RestaurantTableModel {
  const RestaurantTableModel({
    required this.id,
    required this.name,
    required this.code,
    required this.areaId,
    required this.areaName,
    required this.capacity,
    required this.status,
    this.sortOrder = 0,
    this.isActive = true,
  });

  final String id;
  final String name;
  final String code;
  final String areaId;
  final String areaName;
  final int capacity;
  final int status;
  final int sortOrder;
  final bool isActive;

  bool get occupied => status == 1;
  String get displayName => code.isEmpty ? name : '$code · $name';
}

class RestaurantStationModel {
  const RestaurantStationModel({
    required this.id,
    required this.name,
    required this.type,
    this.isActive = true,
  });

  final String id;
  final String name;
  final int type;
  final bool isActive;
}

class GuestOrderLineModel {
  const GuestOrderLineModel({
    required this.name,
    required this.variant,
    required this.qty,
    required this.amount,
  });

  final String name;
  final String variant;
  final double qty;
  final double amount;
}

class GuestOrderModel {
  const GuestOrderModel({
    required this.id,
    required this.orderNo,
    required this.tableId,
    required this.tableName,
    required this.createdAt,
    required this.total,
    required this.lines,
  });

  final String id;
  final String orderNo;
  final String tableId;
  final String tableName;
  final DateTime createdAt;
  final double total;
  final List<GuestOrderLineModel> lines;
}

class RestaurantReservationRecord {
  const RestaurantReservationRecord({
    required this.id,
    required this.status,
    required this.isWalkIn,
    required this.guestName,
    required this.phoneNumber,
    required this.notes,
    required this.partySize,
    required this.startsAt,
    required this.endsAt,
    required this.tableId,
    required this.tableName,
    required this.smsStatus,
  });

  final String id;
  final int status;
  final bool isWalkIn;
  final String guestName;
  final String phoneNumber;
  final String notes;
  final int partySize;
  final DateTime startsAt;
  final DateTime endsAt;
  final String? tableId;
  final String? tableName;
  final String smsStatus;
}

class RestaurantPrintJobRecord {
  const RestaurantPrintJobRecord({
    required this.id,
    required this.externalJobId,
    required this.type,
    required this.routeName,
    required this.status,
    required this.lastError,
    required this.attempts,
    required this.reprintReason,
  });

  final String id;
  final String externalJobId;
  final int type;
  final String routeName;
  final int status;
  final String? lastError;
  final int attempts;
  final String? reprintReason;
}

class RestaurantOrderModel {
  const RestaurantOrderModel({
    required this.id,
    required this.orderNo,
    required this.orderType,
    required this.status,
    required this.tableId,
    required this.tableName,
    required this.customerName,
    required this.customerPhone,
    required this.grandTotal,
    required this.remainingGrandTotal,
    required this.items,
  });

  final String id;
  final String orderNo;
  final int orderType;
  final int status;
  final String? tableId;
  final String tableName;
  final String customerName;
  final String customerPhone;
  final double grandTotal;
  final double remainingGrandTotal;
  final List<CartLine> items;
}

class LedgerOption {
  const LedgerOption({required this.id, required this.name});

  final String id;
  final String name;
}

class LoginProfile {
  const LoginProfile({
    required this.userId,
    required this.userName,
    required this.name,
    required this.tenantName,
  });

  final int userId;
  final String userName;
  final String name;
  final String tenantName;
}

class AuthResult {
  const AuthResult({
    required this.accessToken,
    required this.refreshToken,
    required this.userId,
    required this.shouldResetPassword,
    required this.resetCode,
    required this.requiresTwoFactor,
    required this.twoFactorProviders,
    required this.twoFactorRememberClientToken,
  });

  final String accessToken;
  final String refreshToken;
  final int userId;
  final bool shouldResetPassword;
  final String? resetCode;
  final bool requiresTwoFactor;
  final List<String> twoFactorProviders;
  final String? twoFactorRememberClientToken;
}

class PosContextOption {
  const PosContextOption({
    required this.value,
    required this.label,
    required this.icon,
  });

  final String value;
  final String label;
  final IconData icon;
}

class PosCheckoutDraft {
  const PosCheckoutDraft({
    required this.customerName,
    required this.customerPhone,
    required this.discount,
    required this.paymentMethod,
    required this.tipAmount,
    required this.customerPaidAmount,
    required this.ledgerId,
    required this.salesAccountId,
    required this.paymentLedgerId,
    this.billLines = const [],
    this.approvalPin,
  });

  final String customerName;
  final String customerPhone;
  final double discount;
  final int paymentMethod;
  final double tipAmount;
  final double? customerPaidAmount;
  final String ledgerId;
  final String salesAccountId;
  final String? paymentLedgerId;
  final List<BillLineSelection> billLines;
  final String? approvalPin;
}

class StockRequirement {
  const StockRequirement({
    required this.productName,
    required this.unitName,
    required this.requiredQty,
    required this.availableQty,
    required this.available,
  });

  final String productName;
  final String unitName;
  final double requiredQty;
  final double availableQty;
  final bool available;
}

class StockValidationResult {
  const StockValidationResult({required this.valid, required this.items});

  final bool valid;
  final List<StockRequirement> items;
}

class StockShortageException implements Exception {
  const StockShortageException(this.items);

  final List<StockRequirement> items;
}

class BillingResult {
  const BillingResult({
    required this.orderNo,
    required this.salesMasterId,
    required this.payable,
    required this.returnAmount,
    this.isFullyBilled = true,
    this.remainingGrandTotal = 0,
  });

  final String orderNo;
  final String salesMasterId;
  final double payable;
  final double returnAmount;
  final bool isFullyBilled;
  final double remainingGrandTotal;
}

class ReportSummary {
  const ReportSummary({
    this.orderCount = 0,
    this.grossAmount = 0,
    this.discountAmount = 0,
    this.taxAmount = 0,
    this.grandTotal = 0,
    this.averageBill = 0,
    this.topItems = const [],
    this.settlements = const [],
  });

  final int orderCount;
  final double grossAmount;
  final double discountAmount;
  final double taxAmount;
  final double grandTotal;
  final double averageBill;
  final List<ReportLine> topItems;
  final List<ReportLine> settlements;
}

class ReportLine {
  const ReportLine({
    required this.name,
    required this.quantity,
    required this.amount,
  });

  final String name;
  final double quantity;
  final double amount;
}

class BillLineSelection {
  const BillLineSelection({required this.orderItemId, required this.qty});

  final String orderItemId;
  final double qty;
}

class UniversalOption {
  const UniversalOption({required this.id, required this.name});

  final String id;
  final String name;
}

class SupplierItemMapping {
  const SupplierItemMapping({
    required this.id,
    required this.productId,
    required this.productName,
    required this.supplierLedgerId,
    required this.supplierName,
    required this.supplierSku,
    required this.unitId,
    required this.unitName,
    required this.rate,
    required this.leadTimeDays,
    required this.minimumOrderQty,
    required this.isPreferred,
    required this.isActive,
  });

  final String id;
  final String productId;
  final String productName;
  final String supplierLedgerId;
  final String supplierName;
  final String supplierSku;
  final String unitId;
  final String unitName;
  final double rate;
  final int leadTimeDays;
  final double minimumOrderQty;
  final bool isPreferred;
  final bool isActive;
}

class StockAdjustmentRecord {
  const StockAdjustmentRecord({
    required this.id,
    required this.voucherNo,
    required this.dateMiti,
    required this.adjustmentType,
    required this.description,
    required this.userName,
    required this.totalAmount,
  });

  final String id;
  final String voucherNo;
  final String dateMiti;
  final int adjustmentType;
  final String description;
  final String userName;
  final double totalAmount;
}

class ConsumptionRecord {
  const ConsumptionRecord({
    required this.dateMiti,
    required this.orderNo,
    required this.menuItemName,
    required this.rawMaterialName,
    required this.unitName,
    required this.qty,
    required this.rate,
    required this.amount,
  });

  final String dateMiti;
  final String orderNo;
  final String menuItemName;
  final String rawMaterialName;
  final String unitName;
  final double qty;
  final double rate;
  final double amount;
}

class RecipeCoverageRecord {
  const RecipeCoverageRecord({
    required this.menuItemId,
    required this.productId,
    required this.productName,
    required this.categoryName,
    required this.hasRecipe,
    required this.activeRecipeLineCount,
    required this.estimatedRecipeCost,
    required this.menuPrice,
    required this.foodCostPercent,
    required this.missingRawMaterialSetup,
  });

  final String menuItemId;
  final String productId;
  final String productName;
  final String categoryName;
  final bool hasRecipe;
  final int activeRecipeLineCount;
  final double estimatedRecipeCost;
  final double menuPrice;
  final double foodCostPercent;
  final bool missingRawMaterialSetup;
}

class RestaurantDeviceModel {
  const RestaurantDeviceModel({
    required this.id,
    required this.deviceCode,
    required this.name,
    required this.status,
    required this.lastPulledSeq,
    required this.lastAcknowledgedSeq,
    required this.hasConflict,
    this.lastSeenAt,
    this.lastSyncAt,
    this.lastSyncError = '',
  });

  final String id;
  final String deviceCode;
  final String name;
  final int status;
  final int lastPulledSeq;
  final int lastAcknowledgedSeq;
  final bool hasConflict;
  final DateTime? lastSeenAt;
  final DateTime? lastSyncAt;
  final String lastSyncError;
}

class PayoutRecord {
  const PayoutRecord({
    required this.id,
    required this.channel,
    required this.externalPayoutId,
    required this.grossAmount,
    required this.commissionAmount,
    required this.deductionsAmount,
    required this.netPaidAmount,
    required this.issueCount,
  });

  final String id;
  final String channel;
  final String externalPayoutId;
  final double grossAmount;
  final double commissionAmount;
  final double deductionsAmount;
  final double netPaidAmount;
  final int issueCount;
}

class RestaurantReportBundle {
  const RestaurantReportBundle({
    this.summary = const ReportSummary(),
    this.materialConsumption = const [],
    this.itemSales = const [],
    this.tableSales = const [],
    this.waiterSales = const [],
    this.dailySales = const [],
    this.kotBotStatus = const [],
    this.itemMargins = const [],
    this.waiterPerformance = const [],
    this.tableTurnover = const [],
    this.voidAudit = const [],
    this.discounts = const [],
    this.settlements = const [],
    this.recipeCosting = const [],
    this.foodCosting = const [],
    this.wastage = const [],
    this.lowStock = const [],
    this.payrollSummary = const {},
    this.payrollRuns = const [],
    this.payrollEmployeeCosts = const [],
    this.payrollAttendance = const [],
  });

  final ReportSummary summary;
  final List<Map<String, dynamic>> materialConsumption;
  final List<Map<String, dynamic>> itemSales;
  final List<Map<String, dynamic>> tableSales;
  final List<Map<String, dynamic>> waiterSales;
  final List<Map<String, dynamic>> dailySales;
  final List<Map<String, dynamic>> kotBotStatus;
  final List<Map<String, dynamic>> itemMargins;
  final List<Map<String, dynamic>> waiterPerformance;
  final List<Map<String, dynamic>> tableTurnover;
  final List<Map<String, dynamic>> voidAudit;
  final List<Map<String, dynamic>> discounts;
  final List<Map<String, dynamic>> settlements;
  final List<Map<String, dynamic>> recipeCosting;
  final List<Map<String, dynamic>> foodCosting;
  final List<Map<String, dynamic>> wastage;
  final List<Map<String, dynamic>> lowStock;
  final Map<String, dynamic> payrollSummary;
  final List<Map<String, dynamic>> payrollRuns;
  final List<Map<String, dynamic>> payrollEmployeeCosts;
  final List<Map<String, dynamic>> payrollAttendance;
}

class DocumentRow {
  const DocumentRow({
    required this.number,
    required this.party,
    required this.date,
    required this.status,
    required this.amount,
  });

  final String number;
  final String party;
  final String date;
  final String status;
  final double amount;
}
