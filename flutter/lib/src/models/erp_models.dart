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

  bool get available => stock > 0;
  double get margin => price == 0 ? 0 : (price - cost) / price;
}

class CartLine {
  const CartLine({required this.product, required this.qty});

  final MenuProduct product;
  final int qty;

  double get total => product.price * qty;

  CartLine copyWith({MenuProduct? product, int? qty}) {
    return CartLine(product: product ?? this.product, qty: qty ?? this.qty);
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
  });

  final String id;
  final String table;
  final String station;
  final String channel;
  final int minutes;
  final TicketStatus status;
  final List<CartLine> items;

  KdsTicket copyWith({
    String? id,
    String? table,
    String? station,
    String? channel,
    int? minutes,
    TicketStatus? status,
    List<CartLine>? items,
  }) {
    return KdsTicket(
      id: id ?? this.id,
      table: table ?? this.table,
      station: station ?? this.station,
      channel: channel ?? this.channel,
      minutes: minutes ?? this.minutes,
      status: status ?? this.status,
      items: items ?? this.items,
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
  });

  final String name;
  final String group;
  final String unit;
  final double available;
  final double reorder;
  final double cost;
  final String supplier;

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
  });

  final String name;
  final String provider;
  final int orders;
  final double sales;
  final double commission;
  final bool online;
  final String sync;
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
