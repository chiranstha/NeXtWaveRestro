part of '../../main.dart';

class ErpShell extends StatefulWidget {
  const ErpShell({super.key});

  @override
  State<ErpShell> createState() => _ErpShellState();
}

class _ErpShellState extends State<ErpShell> {
  ModuleKind selected = ModuleKind.dashboard;
  String query = '';
  String outlet = 'Durbar Marg Outlet';
  String posTable = 'T-07';
  String posChannel = 'Dine-In';
  String posCategory = 'All';
  String kdsStation = 'All';
  final List<CartLine> cart = <CartLine>[];
  late List<KdsTicket> tickets = seedTickets();

  ErpModule get activeModule => modules.firstWhere((m) => m.kind == selected);

  void selectModule(ModuleKind kind) {
    setState(() => selected = kind);
  }

  void addToCart(MenuProduct product) {
    setState(() {
      final index = cart.indexWhere((line) => line.product.id == product.id);
      if (index == -1) {
        cart.add(CartLine(product: product, qty: 1));
      } else {
        cart[index] = cart[index].copyWith(qty: cart[index].qty + 1);
      }
    });
  }

  void updateQty(MenuProduct product, int delta) {
    setState(() {
      final index = cart.indexWhere((line) => line.product.id == product.id);
      if (index == -1) return;
      final next = cart[index].qty + delta;
      if (next <= 0) {
        cart.removeAt(index);
      } else {
        cart[index] = cart[index].copyWith(qty: next);
      }
    });
  }

  void fireCart() {
    if (cart.isEmpty) return;
    final byStation = <String, List<CartLine>>{};
    for (final line in cart) {
      byStation.putIfAbsent(line.product.station, () => <CartLine>[]).add(line);
    }
    final ticketCount = byStation.length;
    setState(() {
      var offset = tickets.length + 1;
      for (final entry in byStation.entries) {
        tickets.insert(
          0,
          KdsTicket(
            id: 'KOT-${(210 + offset).toString().padLeft(3, '0')}',
            table: posTable,
            station: entry.key,
            channel: posChannel,
            minutes: 0,
            status: TicketStatus.queued,
            items: List<CartLine>.from(entry.value),
          ),
        );
        offset++;
      }
      cart.clear();
      selected = ModuleKind.kds;
    });
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          'Sent $ticketCount KOT/BOT ticket${ticketCount == 1 ? '' : 's'} to KDS.',
        ),
      ),
    );
  }

  void settleCart() {
    if (cart.isEmpty) return;
    setState(() => cart.clear());
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Bill settled and invoice queued for sync.'),
      ),
    );
  }

  void advanceTicket(KdsTicket ticket) {
    final next = switch (ticket.status) {
      TicketStatus.queued => TicketStatus.acknowledged,
      TicketStatus.acknowledged => TicketStatus.preparing,
      TicketStatus.preparing => TicketStatus.ready,
      TicketStatus.ready => TicketStatus.bumped,
      TicketStatus.bumped => TicketStatus.bumped,
    };
    setState(() {
      final index = tickets.indexWhere((item) => item.id == ticket.id);
      if (index != -1) tickets[index] = ticket.copyWith(status: next);
    });
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final compact = width < 900;
    final filteredModules = modules
        .where(
          (module) => module.title.toLowerCase().contains(query.toLowerCase()),
        )
        .toList();

    return Scaffold(
      drawer: compact
          ? Drawer(
              child: SafeArea(
                child: _ModuleNavigation(
                  selected: selected,
                  modules: filteredModules,
                  onSelected: (kind) {
                    Navigator.pop(context);
                    selectModule(kind);
                  },
                ),
              ),
            )
          : null,
      body: Row(
        children: [
          if (!compact)
            SizedBox(
              width: 264,
              child: _ModuleNavigation(
                selected: selected,
                modules: filteredModules,
                onSelected: selectModule,
              ),
            ),
          Expanded(
            child: Column(
              children: [
                _TopBar(
                  module: activeModule,
                  compact: compact,
                  outlet: outlet,
                  query: query,
                  onQueryChanged: (value) => setState(() => query = value),
                  onOutletChanged: (value) => setState(() => outlet = value),
                ),
                Expanded(
                  child: _ScreenFrame(child: _buildSelectedScreen(compact)),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSelectedScreen(bool compact) {
    return switch (selected) {
      ModuleKind.dashboard => DashboardScreen(
        modules: modules,
        tickets: tickets,
        cart: cart,
        onOpen: selectModule,
      ),
      ModuleKind.pos => PosBillingScreen(
        products: menuProducts,
        cart: cart,
        table: posTable,
        channel: posChannel,
        category: posCategory,
        onTableChanged: (value) => setState(() => posTable = value),
        onChannelChanged: (value) => setState(() => posChannel = value),
        onCategoryChanged: (value) => setState(() => posCategory = value),
        onAdd: addToCart,
        onQty: updateQty,
        onFire: fireCart,
        onSettle: settleCart,
      ),
      ModuleKind.kds => KdsScreen(
        tickets: tickets,
        station: kdsStation,
        onStationChanged: (value) => setState(() => kdsStation = value),
        onAdvance: advanceTicket,
      ),
      ModuleKind.restaurantInventory => const RestaurantInventoryScreen(),
      ModuleKind.menuRecipes => const MenuRecipeScreen(),
      ModuleKind.channels => const ChannelsScreen(),
      ModuleKind.restaurantSetup => const RestaurantSetupScreen(),
      ModuleKind.restaurantReports => const RestaurantReportsScreen(),
      ModuleKind.accountGroups ||
      ModuleKind.accountLedgers ||
      ModuleKind.units ||
      ModuleKind.productGroups ||
      ModuleKind.products ||
      ModuleKind.openingStocks ||
      ModuleKind.purchaseOrder ||
      ModuleKind.purchaseInvoice ||
      ModuleKind.purchaseReturns ||
      ModuleKind.salesInvoice ||
      ModuleKind.salesReturn ||
      ModuleKind.paymentMasters ||
      ModuleKind.restaurantStockUsed ||
      ModuleKind.receiptMasters ||
      ModuleKind.journalMasters ||
      ModuleKind.contraMasters ||
      ModuleKind.pdcPayable ||
      ModuleKind.pdcReceivable ||
      ModuleKind.pdcClearance ||
      ModuleKind.accountGroupReport ||
      ModuleKind.accountLedgerReport ||
      ModuleKind.bookReport ||
      ModuleKind.inventoryReport ||
      ModuleKind.purchaseReport ||
      ModuleKind.salesReport => GenericModuleScreen(module: activeModule),
    };
  }
}

class _TopBar extends StatelessWidget {
  const _TopBar({
    required this.module,
    required this.compact,
    required this.outlet,
    required this.query,
    required this.onQueryChanged,
    required this.onOutletChanged,
  });

  final ErpModule module;
  final bool compact;
  final String outlet;
  final String query;
  final ValueChanged<String> onQueryChanged;
  final ValueChanged<String> onOutletChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: compact ? 78 : 64,
      padding: EdgeInsets.fromLTRB(compact ? 8 : 16, 8, compact ? 8 : 16, 8),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: AppColors.border)),
      ),
      child: Row(
        children: [
          if (compact)
            IconButton(
              tooltip: 'Menu',
              onPressed: () => Scaffold.of(context).openDrawer(),
              icon: const Icon(Icons.menu_rounded),
            ),
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: module.color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(module.icon, color: module.color),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  module.title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: AppColors.text,
                    fontSize: 19,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  module.path,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: AppColors.muted,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
          if (!compact) ...[
            SizedBox(
              width: 300,
              child: TextField(
                onChanged: onQueryChanged,
                decoration: const InputDecoration(
                  hintText: 'Search module',
                  prefixIcon: Icon(Icons.search_rounded),
                  contentPadding: EdgeInsets.symmetric(horizontal: 12),
                ),
              ),
            ),
            const SizedBox(width: 12),
            _OutletChip(outlet: outlet, onChanged: onOutletChanged),
          ],
        ],
      ),
    );
  }
}

class _OutletChip extends StatelessWidget {
  const _OutletChip({required this.outlet, required this.onChanged});

  final String outlet;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<String>(
      tooltip: 'Outlet',
      onSelected: onChanged,
      itemBuilder: (context) => const [
        PopupMenuItem(
          value: 'Durbar Marg Outlet',
          child: Text('Durbar Marg Outlet'),
        ),
        PopupMenuItem(
          value: 'Jhamsikhel Outlet',
          child: Text('Jhamsikhel Outlet'),
        ),
        PopupMenuItem(value: 'Central Kitchen', child: Text('Central Kitchen')),
      ],
      child: Container(
        height: 46,
        padding: const EdgeInsets.symmetric(horizontal: 12),
        decoration: BoxDecoration(
          color: AppColors.background,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: AppColors.border),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.storefront_rounded,
              color: AppColors.primary,
              size: 19,
            ),
            const SizedBox(width: 8),
            Text(outlet, style: const TextStyle(fontWeight: FontWeight.w800)),
            const SizedBox(width: 6),
            const Icon(Icons.keyboard_arrow_down_rounded, size: 19),
          ],
        ),
      ),
    );
  }
}

class _ModuleNavigation extends StatelessWidget {
  const _ModuleNavigation({
    required this.selected,
    required this.modules,
    required this.onSelected,
  });

  final ModuleKind selected;
  final List<ErpModule> modules;
  final ValueChanged<ModuleKind> onSelected;

  @override
  Widget build(BuildContext context) {
    final grouped = <String, List<ErpModule>>{};
    for (final module in modules) {
      grouped.putIfAbsent(module.group, () => <ErpModule>[]).add(module);
    }

    return Container(
      color: AppColors.rail,
      child: Column(
        children: [
          _BrandHeader(),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(12, 6, 12, 18),
              children: [
                for (final entry in grouped.entries) ...[
                  Padding(
                    padding: const EdgeInsets.fromLTRB(10, 18, 10, 8),
                    child: Text(
                      entry.key.toUpperCase(),
                      style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.54),
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 0.8,
                      ),
                    ),
                  ),
                  for (final module in entry.value)
                    _NavItem(
                      module: module,
                      selected: module.kind == selected,
                      onTap: () => onSelected(module.kind),
                    ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _BrandHeader extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      height: 84,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.04),
        border: Border(
          bottom: BorderSide(color: Colors.white.withValues(alpha: 0.08)),
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(
              Icons.restaurant_rounded,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(width: 12),
          const Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'NextWave',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  'Restaurant ERP',
                  style: TextStyle(
                    color: Color(0xFFB8C5DA),
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.module,
    required this.selected,
    required this.onTap,
  });

  final ErpModule module;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Tooltip(
        message: module.description,
        waitDuration: const Duration(milliseconds: 500),
        child: InkWell(
          borderRadius: BorderRadius.circular(8),
          onTap: onTap,
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 160),
            padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 8),
            decoration: BoxDecoration(
              color: selected ? Colors.white : Colors.transparent,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                Icon(
                  module.icon,
                  size: 20,
                  color: selected
                      ? module.color
                      : Colors.white.withValues(alpha: 0.72),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    module.title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: selected
                          ? AppColors.text
                          : Colors.white.withValues(alpha: 0.84),
                      fontSize: 13.4,
                      fontWeight: selected ? FontWeight.w900 : FontWeight.w700,
                    ),
                  ),
                ),
                if (selected)
                  Icon(
                    Icons.chevron_right_rounded,
                    color: module.color,
                    size: 18,
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ScreenFrame extends StatelessWidget {
  const _ScreenFrame({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(12),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 1360),
        child: child,
      ),
    );
  }
}
