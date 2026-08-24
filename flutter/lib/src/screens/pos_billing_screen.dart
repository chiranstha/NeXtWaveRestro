part of '../../main.dart';

class PosBillingScreen extends StatefulWidget {
  const PosBillingScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<PosBillingScreen> createState() => _PosBillingScreenState();
}

class _PosBillingScreenState extends State<PosBillingScreen> {
  final customerController = TextEditingController();
  final phoneController = TextEditingController();
  final discountController = TextEditingController(text: '0');
  final tipController = TextEditingController(text: '0');
  final paidController = TextEditingController();
  String search = '';
  int paymentMethod = 0;
  String? accountLedgerId;
  String? salesLedgerId;
  String? paymentLedgerId;
  bool partialBilling = false;
  final selectedBillQuantities = <String, double>{};
  String? managerPin;

  static const paymentMethods = <int, String>{
    0: 'Cash',
    1: 'Cheque',
    2: 'Credit',
    3: 'Card',
    5: 'QR',
  };

  RestaurantAppController get controller => widget.controller;
  double get subtotal =>
      controller.cart.fold(0, (total, line) => total + line.total);
  double get discount => _number(discountController.text);
  double get tip => _number(tipController.text);
  double get estimate => math.max(0, subtotal - discount + tip).toDouble();

  @override
  void initState() {
    super.initState();
    _setDefaultLedgers();
    _syncCustomerFromCurrentOrder();
  }

  @override
  void didUpdateWidget(covariant PosBillingScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    _setDefaultLedgers();
  }

  void _setDefaultLedgers() {
    accountLedgerId ??= controller.defaultAccountLedger?.id;
    salesLedgerId ??= controller.defaultSalesLedger?.id;
  }

  @override
  void dispose() {
    customerController.dispose();
    phoneController.dispose();
    discountController.dispose();
    tipController.dispose();
    paidController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    _setDefaultLedgers();
    final compact = MediaQuery.sizeOf(context).width < 1040;
    final categories = <String>{
      'All',
      ...controller.products.map((product) => product.category),
    }.toList();
    final selectedCategory = categories.contains(controller.selectedCategory)
        ? controller.selectedCategory
        : 'All';
    final normalizedSearch = search.trim().toLowerCase();
    final products = controller.products.where((product) {
      final categoryMatches =
          selectedCategory == 'All' || product.category == selectedCategory;
      final searchMatches =
          normalizedSearch.isEmpty ||
          product.name.toLowerCase().contains(normalizedSearch) ||
          product.id.toLowerCase().contains(normalizedSearch) ||
          product.category.toLowerCase().contains(normalizedSearch);
      return categoryMatches && searchMatches;
    }).toList();

    final catalog = _buildCatalog(categories, selectedCategory, products);
    final checkout = _buildCheckout();
    if (compact) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [catalog, const SizedBox(height: 16), checkout],
      );
    }
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(flex: 7, child: catalog),
        const SizedBox(width: 14),
        SizedBox(width: 390, child: checkout),
      ],
    );
  }

  Widget _buildCatalog(
    List<String> categories,
    String selectedCategory,
    List<MenuProduct> products,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'POS Billing',
          action: controller.currentOrderId == null
              ? 'New ${controller.selectedPosLabel} order'
              : 'Editing open order',
          icon: Icons.point_of_sale_rounded,
        ),
        const SizedBox(height: 10),
        _Panel(
          child: Wrap(
            spacing: 10,
            runSpacing: 10,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 290,
                child: TextField(
                  onChanged: (value) => setState(() => search = value),
                  decoration: const InputDecoration(
                    labelText: 'Search menu',
                    prefixIcon: Icon(Icons.search_rounded),
                    isDense: true,
                  ),
                ),
              ),
              SizedBox(
                width: 250,
                child: DropdownButtonFormField<String>(
                  key: ValueKey(controller.selectedPosContext),
                  initialValue: controller.selectedPosContext,
                  isExpanded: true,
                  decoration: const InputDecoration(
                    labelText: 'Table / order type',
                    isDense: true,
                  ),
                  items: [
                    for (final option in controller.posContexts)
                      DropdownMenuItem(
                        value: option.value,
                        child: Row(
                          children: [
                            Icon(option.icon, size: 18),
                            const SizedBox(width: 7),
                            Expanded(
                              child: Text(
                                option.label,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ],
                        ),
                      ),
                  ],
                  onChanged: controller.busy
                      ? null
                      : (value) => _changeContext(value),
                ),
              ),
              OutlinedButton.icon(
                onPressed: controller.openOrders.isEmpty
                    ? null
                    : _showOpenOrders,
                icon: const Icon(Icons.receipt_long_rounded),
                label: Text('Open orders (${controller.openOrders.length})'),
              ),
              if (controller.currentOrderId != null)
                PopupMenuButton<String>(
                  tooltip: 'Order actions',
                  onSelected: _handleOrderAction,
                  itemBuilder: (context) => [
                    if (controller.hasPermission(
                      'Pages.Restaurant.Pos.TableTransfer',
                    ))
                      const PopupMenuItem(
                        value: 'transfer',
                        child: Text('Transfer table'),
                      ),
                    if (controller.hasPermission(
                      'Pages.Restaurant.Pos.SplitMerge',
                    )) ...[
                      const PopupMenuItem(
                        value: 'split',
                        child: Text('Split order'),
                      ),
                      const PopupMenuItem(
                        value: 'merge',
                        child: Text('Merge orders'),
                      ),
                    ],
                    const PopupMenuItem(
                      value: 'tickets',
                      child: Text('Ticket history / reprint'),
                    ),
                  ],
                  child: const Chip(
                    avatar: Icon(Icons.more_horiz_rounded, size: 18),
                    label: Text('Order actions'),
                  ),
                ),
              if (controller.currentOrderId != null)
                const _TinyTag(label: 'OPEN ORDER', color: AppColors.amber),
            ],
          ),
        ),
        const SizedBox(height: 10),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              for (final category in categories)
                Padding(
                  padding: const EdgeInsets.only(right: 6),
                  child: ChoiceChip(
                    label: Text(category),
                    selected: selectedCategory == category,
                    onSelected: (_) => controller.selectCategory(category),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 10),
        if (products.isEmpty)
          const _EmptyState(
            icon: Icons.search_off_rounded,
            title: 'No menu items found',
            message: 'Try another search or publish the item in the web ERP.',
          )
        else
          _ResponsiveGrid(
            minTileWidth: 175,
            tileHeight: 158,
            children: [
              for (final product in products)
                _MenuTile(product: product, onTap: () => _addProduct(product)),
            ],
          ),
      ],
    );
  }

  Widget _buildCheckout() {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Text(
                  'Current order',
                  style: TextStyle(fontSize: 17, fontWeight: FontWeight.w900),
                ),
              ),
              _TinyTag(
                label: '${controller.cart.length} lines',
                color: AppColors.primary,
              ),
            ],
          ),
          const SizedBox(height: 10),
          if (controller.cart.isEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: Center(
                child: Text(
                  'Tap a menu item to start the order.',
                  style: TextStyle(color: AppColors.muted),
                ),
              ),
            )
          else
            for (final line in controller.cart)
              _CartLineTile(
                line: line,
                onQty: controller.updateCartQuantity,
                onVoid:
                    controller.hasPermission('Pages.Restaurant.Pos.Void') ||
                        line.orderItemId == null
                    ? () => _voidLine(line)
                    : null,
              ),
          const Divider(height: 24),
          TextField(
            controller: customerController,
            decoration: const InputDecoration(
              labelText: 'Customer name (optional)',
              prefixIcon: Icon(Icons.person_outline_rounded),
              isDense: true,
            ),
          ),
          const SizedBox(height: 8),
          TextField(
            controller: phoneController,
            keyboardType: TextInputType.phone,
            decoration: const InputDecoration(
              labelText: 'Phone (optional)',
              prefixIcon: Icon(Icons.phone_outlined),
              isDense: true,
            ),
          ),
          const SizedBox(height: 10),
          DropdownButtonFormField<int>(
            initialValue: paymentMethod,
            decoration: const InputDecoration(
              labelText: 'Payment method',
              isDense: true,
            ),
            items: [
              for (final entry in paymentMethods.entries)
                DropdownMenuItem(value: entry.key, child: Text(entry.value)),
            ],
            onChanged: (value) => setState(() {
              paymentMethod = value ?? 0;
              if (paymentMethod == 0) paymentLedgerId = null;
            }),
          ),
          const SizedBox(height: 8),
          if (controller.hasPermission('Pages.Restaurant.Pos.Discount'))
            _PosNumberField(
              controller: discountController,
              label: 'Order discount (Rs)',
              onChanged: (_) => setState(() {}),
            ),
          const SizedBox(height: 8),
          _PosNumberField(
            controller: tipController,
            label: 'Tip amount (Rs)',
            onChanged: (_) => setState(() {}),
          ),
          const SizedBox(height: 8),
          _PosNumberField(
            controller: paidController,
            label: 'Customer paid (blank = payable)',
            onChanged: (_) {},
          ),
          const SizedBox(height: 8),
          if (controller.currentOrderId != null &&
              controller.cart.any(
                (line) =>
                    line.orderItemId != null &&
                    (line.unbilledQty ?? line.qty) > 0,
              ))
            ExpansionTile(
              tilePadding: EdgeInsets.zero,
              title: const Text(
                'Split / partial bill',
                style: TextStyle(fontWeight: FontWeight.w800),
              ),
              subtitle: Text(
                partialBilling
                    ? '${selectedBillQuantities.length} line(s) selected'
                    : 'Full remaining order',
              ),
              leading: Switch(
                value: partialBilling,
                onChanged: (value) => setState(() {
                  partialBilling = value;
                  if (!value) selectedBillQuantities.clear();
                }),
              ),
              children: partialBilling
                  ? [
                      for (final line in controller.cart.where(
                        (line) =>
                            line.orderItemId != null &&
                            (line.unbilledQty ?? line.qty) > 0,
                      ))
                        CheckboxListTile(
                          dense: true,
                          contentPadding: EdgeInsets.zero,
                          value: selectedBillQuantities.containsKey(
                            line.orderItemId,
                          ),
                          title: Text(line.product.name),
                          subtitle: Text(
                            '${selectedBillQuantities[line.orderItemId]?.toStringAsFixed(2) ?? '0'} of '
                            '${(line.unbilledQty ?? line.qty).toStringAsFixed(2)} selected · ${money(line.total)}',
                          ),
                          secondary:
                              selectedBillQuantities.containsKey(
                                line.orderItemId,
                              )
                              ? SizedBox(
                                  width: 104,
                                  child: Row(
                                    children: [
                                      IconButton(
                                        tooltip: 'Reduce bill quantity',
                                        onPressed: () =>
                                            _changeBillQty(line, -1),
                                        icon: const Icon(
                                          Icons.remove_circle_outline_rounded,
                                        ),
                                      ),
                                      IconButton(
                                        tooltip: 'Increase bill quantity',
                                        onPressed: () =>
                                            _changeBillQty(line, 1),
                                        icon: const Icon(
                                          Icons.add_circle_outline_rounded,
                                        ),
                                      ),
                                    ],
                                  ),
                                )
                              : null,
                          onChanged: (checked) => setState(() {
                            if (checked == true) {
                              selectedBillQuantities[line.orderItemId!] =
                                  line.unbilledQty ?? line.qty.toDouble();
                            } else {
                              selectedBillQuantities.remove(line.orderItemId);
                            }
                          }),
                        ),
                    ]
                  : const [],
            ),
          const SizedBox(height: 8),
          ExpansionTile(
            tilePadding: EdgeInsets.zero,
            childrenPadding: EdgeInsets.zero,
            title: const Text(
              'Accounting ledgers',
              style: TextStyle(fontWeight: FontWeight.w800),
            ),
            subtitle: const Text('Required by the current billing workflow'),
            children: [
              _ledgerDropdown(
                label: 'Customer / cash ledger',
                value: accountLedgerId,
                options: controller.accountLedgers,
                onChanged: (value) => setState(() => accountLedgerId = value),
              ),
              const SizedBox(height: 8),
              _ledgerDropdown(
                label: 'Sales account',
                value: salesLedgerId,
                options: controller.salesLedgers,
                onChanged: (value) => setState(() => salesLedgerId = value),
              ),
              if (paymentMethod != 0 && paymentMethod != 2) ...[
                const SizedBox(height: 8),
                _ledgerDropdown(
                  label: 'Payment ledger (optional)',
                  value: paymentLedgerId,
                  options: controller.accountLedgers,
                  onChanged: (value) => setState(() => paymentLedgerId = value),
                  optional: true,
                ),
              ],
            ],
          ),
          const Divider(height: 22),
          _AmountRow(label: 'Menu subtotal', value: money(subtotal)),
          if (discount > 0)
            _AmountRow(
              label: 'Requested discount',
              value: '- ${money(discount)}',
            ),
          if (tip > 0) _AmountRow(label: 'Tip', value: money(tip)),
          _AmountRow(
            label: 'Estimated payable',
            value: money(estimate),
            strong: true,
          ),
          const Padding(
            padding: EdgeInsets.only(top: 4, bottom: 10),
            child: Text(
              'Tax, service charge, discounts, and final payable are recalculated by the server.',
              style: TextStyle(color: AppColors.muted, fontSize: 11.5),
            ),
          ),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: controller.busy || controller.cart.isEmpty
                      ? null
                      : _sendKot,
                  icon: const Icon(Icons.soup_kitchen_rounded),
                  label: const Text('Send KOT'),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: FilledButton.icon(
                  onPressed: controller.busy || controller.cart.isEmpty
                      ? null
                      : () => _settle(false),
                  icon: controller.busy
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.check_circle_rounded),
                  label: const Text('Settle'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _ledgerDropdown({
    required String label,
    required String? value,
    required List<LedgerOption> options,
    required ValueChanged<String?> onChanged,
    bool optional = false,
  }) {
    final validValue = options.any((item) => item.id == value) ? value : null;
    return DropdownButtonFormField<String>(
      initialValue: validValue,
      isExpanded: true,
      decoration: InputDecoration(labelText: label, isDense: true),
      items: [
        if (optional)
          const DropdownMenuItem(value: '', child: Text('Server default')),
        for (final option in options)
          DropdownMenuItem(
            value: option.id,
            child: Text(option.name, overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: options.isEmpty
          ? null
          : (next) => onChanged(next?.isEmpty == true ? null : next),
    );
  }

  Future<void> _addProduct(MenuProduct product) async {
    if (product.variants.isEmpty && product.modifierGroups.isEmpty) {
      controller.addToCart(product, const ProductConfiguration());
      return;
    }
    final configuration = await showModalBottomSheet<ProductConfiguration>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (context) => _ProductConfigurationSheet(product: product),
    );
    if (configuration != null) controller.addToCart(product, configuration);
  }

  bool get _requiresManagerPin =>
      controller.operationalSettings['requireManagerPinForSensitiveActions'] ==
      true;

  void _changeBillQty(CartLine line, int delta) {
    final id = line.orderItemId;
    if (id == null) return;
    final maximum = line.unbilledQty ?? line.qty.toDouble();
    final step = maximum < 1 ? maximum : 1.0;
    final current = selectedBillQuantities[id] ?? maximum;
    final next = math.min(maximum, current + delta * step);
    setState(() {
      if (next <= 0) {
        selectedBillQuantities.remove(id);
      } else {
        selectedBillQuantities[id] = next;
      }
    });
  }

  Future<void> _changeContext(String? value) async {
    if (value == null || value == controller.selectedPosContext) return;
    if (controller.orderDirty && !await _confirmDiscard()) return;
    controller.selectPosContext(value);
    _syncCustomerFromCurrentOrder();
    partialBilling = false;
    selectedBillQuantities.clear();
  }

  Future<bool> _confirmDiscard() async {
    return await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Discard unsaved order changes?'),
            content: const Text(
              'Switching the table or order will reload server data and remove unsaved draft changes.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Keep editing'),
              ),
              FilledButton(
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Discard changes'),
              ),
            ],
          ),
        ) ??
        false;
  }

  Future<void> _showOpenOrders() async {
    if (controller.orderDirty && !await _confirmDiscard()) return;
    if (!mounted) return;
    final selected = await showDialog<RestaurantOrderModel>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Open orders'),
        content: SizedBox(
          width: 520,
          child: ListView.separated(
            shrinkWrap: true,
            itemCount: controller.openOrders.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, index) {
              final order = controller.openOrders[index];
              return ListTile(
                leading: const Icon(Icons.receipt_long_rounded),
                title: Text(
                  '${order.orderNo} · ${order.tableName.isEmpty ? _orderTypeName(order.orderType) : order.tableName}',
                ),
                subtitle: Text(
                  '${order.items.length} lines · ${order.customerName.isEmpty ? 'Guest' : order.customerName}',
                ),
                trailing: Text(
                  money(order.remainingGrandTotal),
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                onTap: () => Navigator.pop(context, order),
              );
            },
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
    if (selected != null) {
      controller.loadOpenOrder(selected);
      _syncCustomerFromCurrentOrder();
      partialBilling = false;
      selectedBillQuantities.clear();
    }
  }

  void _syncCustomerFromCurrentOrder() {
    final orderId = controller.currentOrderId;
    final matches = controller.openOrders.where((item) => item.id == orderId);
    if (matches.isEmpty) {
      customerController.clear();
      phoneController.clear();
      return;
    }
    customerController.text = matches.first.customerName;
    phoneController.text = matches.first.customerPhone;
  }

  Future<void> _handleOrderAction(String action) async {
    try {
      switch (action) {
        case 'transfer':
          await _transferOrder();
        case 'split':
          await _splitOrder();
        case 'merge':
          await _mergeOrders();
        case 'tickets':
          await _showTickets();
      }
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _transferOrder() async {
    final options = controller.tables
        .where(
          (table) => table.id != controller.currentTableId && table.isActive,
        )
        .toList();
    if (options.isEmpty) {
      _showError('No other active table is available.');
      return;
    }
    final selected = await showDialog<String>(
      context: context,
      builder: (context) => SimpleDialog(
        title: const Text('Transfer order to table'),
        children: [
          for (final table in options)
            SimpleDialogOption(
              onPressed: () => Navigator.pop(context, table.id),
              child: ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(
                  table.occupied
                      ? Icons.people_alt_rounded
                      : Icons.table_bar_rounded,
                ),
                title: Text(table.displayName),
                subtitle: Text(
                  '${table.areaName} · capacity ${table.capacity}',
                ),
              ),
            ),
        ],
      ),
    );
    if (selected != null) await controller.transferCurrentOrder(selected);
  }

  Future<void> _splitOrder() async {
    final lines = controller.cart
        .where((line) => line.orderItemId != null && line.status != 5)
        .toList();
    if (lines.length < 2) {
      _showError('A split requires at least two saved order lines.');
      return;
    }
    final selected = <String>{};
    String? tableId;
    final result = await showDialog<(List<String>, String?)>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Split order'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  for (final line in lines)
                    CheckboxListTile(
                      contentPadding: EdgeInsets.zero,
                      value: selected.contains(line.orderItemId),
                      title: Text('${line.qty} × ${line.product.name}'),
                      subtitle: Text(money(line.total)),
                      onChanged: (checked) => setDialogState(() {
                        if (checked == true) {
                          selected.add(line.orderItemId!);
                        } else {
                          selected.remove(line.orderItemId);
                        }
                      }),
                    ),
                  DropdownButtonFormField<String>(
                    initialValue: tableId,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      labelText: 'New table (optional)',
                    ),
                    items: [
                      const DropdownMenuItem(
                        value: '',
                        child: Text('No table / takeaway'),
                      ),
                      for (final table in controller.tables.where(
                        (table) => table.id != controller.currentTableId,
                      ))
                        DropdownMenuItem(
                          value: table.id,
                          child: Text(table.displayName),
                        ),
                    ],
                    onChanged: (value) => setDialogState(
                      () => tableId = value?.isEmpty == true ? null : value,
                    ),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: selected.isEmpty || selected.length == lines.length
                  ? null
                  : () => Navigator.pop(context, (selected.toList(), tableId)),
              child: const Text('Create split'),
            ),
          ],
        ),
      ),
    );
    if (result != null) {
      await controller.splitCurrentOrder(
        orderItemIds: result.$1,
        newTableId: result.$2,
      );
    }
  }

  Future<void> _mergeOrders() async {
    final sources = controller.openOrders
        .where((order) => order.id != controller.currentOrderId)
        .toList();
    if (sources.isEmpty) {
      _showError('There are no other open orders to merge.');
      return;
    }
    final selected = <String>{};
    final result = await showDialog<List<String>>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Merge into current order'),
          content: SizedBox(
            width: 500,
            child: ListView(
              shrinkWrap: true,
              children: [
                for (final order in sources)
                  CheckboxListTile(
                    value: selected.contains(order.id),
                    title: Text('${order.orderNo} · ${order.tableName}'),
                    subtitle: Text(money(order.remainingGrandTotal)),
                    onChanged: (checked) => setDialogState(() {
                      if (checked == true) {
                        selected.add(order.id);
                      } else {
                        selected.remove(order.id);
                      }
                    }),
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: selected.isEmpty
                  ? null
                  : () => Navigator.pop(context, selected.toList()),
              child: const Text('Merge'),
            ),
          ],
        ),
      ),
    );
    if (result != null) await controller.mergeIntoCurrentOrder(result);
  }

  Future<void> _showTickets() async {
    final tickets = await controller.loadCurrentOrderTickets();
    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Ticket history'),
        content: SizedBox(
          width: 560,
          child: tickets.isEmpty
              ? const Text('No KOT/BOT tickets for this order.')
              : ListView.separated(
                  shrinkWrap: true,
                  itemCount: tickets.length,
                  separatorBuilder: (_, _) => const Divider(),
                  itemBuilder: (context, index) {
                    final ticket = tickets[index];
                    return ListTile(
                      leading: const Icon(Icons.receipt_rounded),
                      title: Text('${ticket.id} · ${ticket.station}'),
                      subtitle: Text(
                        '${ticket.items.map((line) => '${line.qty}× ${line.product.name}').join(', ')}\n'
                        '${ticket.purpose == 2
                            ? 'Add-on'
                            : ticket.purpose == 1
                            ? 'Cancellation'
                            : 'New order'}',
                      ),
                      isThreeLine: true,
                      trailing:
                          controller.hasPermission(
                            'Pages.Restaurant.KotBot.Reprint',
                          )
                          ? IconButton(
                              tooltip: 'Reprint ticket',
                              onPressed: () async {
                                final pin = _requiresManagerPin
                                    ? await _requestPin('Ticket reprint')
                                    : null;
                                if (_requiresManagerPin && pin == null) return;
                                try {
                                  await controller.reprintTicket(
                                    ticket,
                                    approvalPin: pin,
                                  );
                                  if (mounted) {
                                    ScaffoldMessenger.of(
                                      this.context,
                                    ).showSnackBar(
                                      SnackBar(
                                        content: Text(
                                          '${ticket.id} reprint recorded.',
                                        ),
                                      ),
                                    );
                                  }
                                } on ApiException catch (error) {
                                  _showError(error.message);
                                }
                              },
                              icon: const Icon(Icons.print_rounded),
                            )
                          : null,
                    );
                  },
                ),
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Done'),
          ),
        ],
      ),
    );
  }

  Future<void> _voidLine(CartLine line) async {
    final reason = await _requestText(
      title: line.orderItemId == null ? 'Remove item' : 'Void order item',
      label: 'Reason',
      initialValue: line.orderItemId == null ? 'Draft item removed' : '',
    );
    if (reason == null || reason.trim().isEmpty) return;
    final pin = line.orderItemId != null && _requiresManagerPin
        ? await _requestPin('Void approval')
        : null;
    if (line.orderItemId != null && _requiresManagerPin && pin == null) return;
    try {
      await controller.voidCartLine(
        line,
        reason: reason.trim(),
        approvalPin: pin,
      );
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<String?> _requestPin(String title) {
    return _requestText(title: title, label: 'Manager PIN', obscure: true);
  }

  Future<String?> _requestText({
    required String title,
    required String label,
    String initialValue = '',
    bool obscure = false,
  }) async {
    final textController = TextEditingController(text: initialValue);
    final value = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: TextField(
          controller: textController,
          autofocus: true,
          obscureText: obscure,
          decoration: InputDecoration(labelText: label),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, textController.text),
            child: const Text('Continue'),
          ),
        ],
      ),
    );
    textController.dispose();
    return value;
  }

  String _orderTypeName(int type) => switch (type) {
    0 => 'Dine-in',
    2 => 'Delivery',
    _ => 'Takeaway',
  };

  Future<void> _sendKot() async {
    try {
      await controller.sendCurrentOrder(
        customerName: customerController.text.trim(),
        customerPhone: phoneController.text.trim(),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('KOT/BOT sent successfully.')),
        );
      }
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _settle(bool confirmNegativeStock) async {
    if (partialBilling && selectedBillQuantities.isEmpty) {
      _showError('Select at least one saved order line for a partial bill.');
      return;
    }
    if (discount > 0 &&
        _requiresManagerPin &&
        (managerPin == null || managerPin!.isEmpty)) {
      managerPin = await _requestPin('Discount approval');
      if (managerPin == null || managerPin!.isEmpty) return;
    }
    final ledgerId =
        accountLedgerId ?? controller.defaultAccountLedger?.id ?? '';
    final salesId = salesLedgerId ?? controller.defaultSalesLedger?.id ?? '';
    final draft = PosCheckoutDraft(
      customerName: customerController.text.trim(),
      customerPhone: phoneController.text.trim(),
      discount: discount,
      paymentMethod: paymentMethod,
      tipAmount: tip,
      customerPaidAmount: paidController.text.trim().isEmpty
          ? null
          : _number(paidController.text),
      ledgerId: ledgerId,
      salesAccountId: salesId,
      paymentLedgerId: paymentLedgerId,
      approvalPin: managerPin,
      billLines: partialBilling
          ? [
              for (final line in controller.cart.where(
                (line) => selectedBillQuantities.containsKey(line.orderItemId),
              ))
                BillLineSelection(
                  orderItemId: line.orderItemId!,
                  qty: selectedBillQuantities[line.orderItemId]!,
                ),
            ]
          : const [],
    );
    try {
      final result = await controller.settle(
        draft,
        confirmNegativeStock: confirmNegativeStock,
      );
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          icon: const Icon(
            Icons.check_circle_rounded,
            color: AppColors.green,
            size: 42,
          ),
          title: Text('Bill ${result.orderNo} posted'),
          content: Text(
            'Payable: ${money(result.payable)}\nReturn: ${money(result.returnAmount)}'
            '${result.isFullyBilled ? '' : '\nRemaining order balance: ${money(result.remainingGrandTotal)}'}',
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Done'),
            ),
          ],
        ),
      );
      if (result.isFullyBilled) {
        customerController.clear();
        phoneController.clear();
      }
      discountController.text = '0';
      tipController.text = '0';
      paidController.clear();
      partialBilling = false;
      selectedBillQuantities.clear();
      managerPin = null;
      setState(() {});
    } on StockShortageException catch (error) {
      if (!mounted) return;
      final proceed = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Insufficient recipe stock'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('The server reported these shortages:'),
                const SizedBox(height: 10),
                for (final item in error.items)
                  Text(
                    '• ${item.productName}: need ${item.requiredQty.toStringAsFixed(2)} ${item.unitName}, available ${item.availableQty.toStringAsFixed(2)}',
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Post anyway'),
            ),
          ],
        ),
      );
      if (proceed == true) await _settle(true);
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: AppColors.red),
    );
  }

  static double _number(String value) {
    return double.tryParse(value.trim().replaceAll(',', '')) ?? 0;
  }
}

class _PosNumberField extends StatelessWidget {
  const _PosNumberField({
    required this.controller,
    required this.label,
    required this.onChanged,
  });

  final TextEditingController controller;
  final String label;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      onChanged: onChanged,
      decoration: InputDecoration(
        labelText: label,
        prefixText: 'Rs ',
        isDense: true,
      ),
    );
  }
}

class _ProductConfigurationSheet extends StatefulWidget {
  const _ProductConfigurationSheet({required this.product});

  final MenuProduct product;

  @override
  State<_ProductConfigurationSheet> createState() =>
      _ProductConfigurationSheetState();
}

class _ProductConfigurationSheetState
    extends State<_ProductConfigurationSheet> {
  MenuVariant? variant;
  final selectedModifierIds = <String>{};
  String? validationMessage;

  @override
  void initState() {
    super.initState();
    final defaults = widget.product.variants.where((item) => item.isDefault);
    variant = defaults.isEmpty ? null : defaults.first;
  }

  @override
  Widget build(BuildContext context) {
    final product = widget.product;
    return Padding(
      padding: EdgeInsets.only(
        left: 18,
        right: 18,
        top: 18,
        bottom: 18 + MediaQuery.viewInsetsOf(context).bottom,
      ),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              product.name,
              style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900),
            ),
            Text(
              '${product.category} · base ${money(product.price)}',
              style: const TextStyle(color: AppColors.muted),
            ),
            if (product.variants.isNotEmpty) ...[
              const SizedBox(height: 18),
              const Text(
                'Variant',
                style: TextStyle(fontWeight: FontWeight.w900),
              ),
              RadioGroup<MenuVariant>(
                groupValue: variant,
                onChanged: (value) => setState(() => variant = value),
                child: Column(
                  children: [
                    for (final item in product.variants)
                      RadioListTile<MenuVariant>(
                        value: item,
                        dense: true,
                        contentPadding: EdgeInsets.zero,
                        title: Text(item.name),
                        subtitle: Text(
                          item.isAbsolutePrice
                              ? money(item.priceDelta)
                              : '+ ${money(item.priceDelta)}',
                        ),
                      ),
                  ],
                ),
              ),
            ],
            for (final group in product.modifierGroups) ...[
              const SizedBox(height: 12),
              Text(
                '${group.name}${group.isRequired ? ' · required' : ''} '
                '(choose ${group.minSelect}-${group.maxSelect})',
                style: const TextStyle(fontWeight: FontWeight.w900),
              ),
              for (final modifier in group.modifiers)
                CheckboxListTile(
                  value: selectedModifierIds.contains(modifier.id),
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  title: Text(modifier.name),
                  subtitle: modifier.priceDelta == 0
                      ? null
                      : Text('+ ${money(modifier.priceDelta)}'),
                  onChanged: (checked) {
                    final groupSelected = group.modifiers
                        .where((item) => selectedModifierIds.contains(item.id))
                        .length;
                    if (checked == true && groupSelected >= group.maxSelect) {
                      setState(() {
                        validationMessage =
                            'Choose at most ${group.maxSelect} from ${group.name}.';
                      });
                      return;
                    }
                    setState(() {
                      validationMessage = null;
                      if (checked == true) {
                        selectedModifierIds.add(modifier.id);
                      } else {
                        selectedModifierIds.remove(modifier.id);
                      }
                    });
                  },
                ),
            ],
            if (validationMessage != null)
              Text(
                validationMessage!,
                style: const TextStyle(color: AppColors.red),
              ),
            const SizedBox(height: 18),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _complete,
                icon: const Icon(Icons.add_shopping_cart_rounded),
                label: const Text('Add to order'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _complete() {
    if (widget.product.variants.isNotEmpty && variant == null) {
      setState(() => validationMessage = 'Choose a variant.');
      return;
    }
    for (final group in widget.product.modifierGroups) {
      final count = group.modifiers
          .where((item) => selectedModifierIds.contains(item.id))
          .length;
      if (count < group.minSelect || (group.isRequired && count == 0)) {
        setState(() {
          validationMessage =
              'Choose at least ${group.minSelect} from ${group.name}.';
        });
        return;
      }
    }
    final modifiers = widget.product.modifierGroups
        .expand((group) => group.modifiers)
        .where((item) => selectedModifierIds.contains(item.id))
        .toList();
    Navigator.pop(
      context,
      ProductConfiguration(variant: variant, modifiers: modifiers),
    );
  }
}
