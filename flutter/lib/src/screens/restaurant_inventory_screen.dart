part of '../../main.dart';

class RestaurantInventoryScreen extends StatefulWidget {
  const RestaurantInventoryScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<RestaurantInventoryScreen> createState() =>
      _RestaurantInventoryScreenState();
}

class _RestaurantInventoryScreenState extends State<RestaurantInventoryScreen> {
  String view = 'Replenishment';

  RestaurantAppController get controller => widget.controller;

  @override
  Widget build(BuildContext context) {
    final items = controller.inventory;
    final suggestedValue = items.fold<double>(
      0,
      (sum, item) => sum + item.suggested * item.cost,
    );
    final views = <String>[
      'Replenishment',
      if (controller.hasPermission(
        'Pages.Restaurant.Inventory.SupplierMapping',
      ))
        'Supplier mapping',
      'Adjustments',
      'Consumption',
      'Recipe coverage',
    ];
    if (!views.contains(view)) view = views.first;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Restaurant Inventory',
          action: 'Live stock, recipe usage, wastage and purchasing',
          icon: Icons.inventory_2_rounded,
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 220,
          tileHeight: 148,
          children: [
            _KpiCard(
              title: 'Low-stock items',
              value: '${items.length}',
              helper: 'From recipe/raw stock ledger',
              icon: Icons.warning_rounded,
              color: AppColors.red,
            ),
            _KpiCard(
              title: 'Suggested purchase',
              value: money(suggestedValue),
              helper: 'Estimated at current rate',
              icon: Icons.shopping_cart_checkout_rounded,
              color: AppColors.teal,
            ),
            _KpiCard(
              title: 'Supplier mapping gaps',
              value:
                  '${items.where((item) => item.missingSupplierMapping).length}',
              helper: '${controller.supplierMappings.length} mappings loaded',
              icon: Icons.link_off_rounded,
              color: AppColors.amber,
            ),
          ],
        ),
        const SizedBox(height: 14),
        _Panel(
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              for (final item in views)
                ChoiceChip(
                  selected: view == item,
                  label: Text(item),
                  onSelected: (_) => setState(() => view = item),
                ),
              if (controller.hasPermission(
                'Pages.Restaurant.Inventory.Reorder',
              ))
                FilledButton.icon(
                  onPressed: controller.busy ? null : _generatePurchaseOrders,
                  icon: const Icon(Icons.shopping_cart_checkout_rounded),
                  label: const Text('Generate draft PO'),
                ),
              if (controller.hasPermission(
                    'Pages.Restaurant.Inventory.StockAdjustment',
                  ) ||
                  controller.hasPermission(
                    'Pages.Restaurant.Inventory.Wastage',
                  ))
                OutlinedButton.icon(
                  onPressed: controller.busy ? null : _stockAdjustmentDialog,
                  icon: const Icon(Icons.tune_rounded),
                  label: const Text('Stock adjustment'),
                ),
              if (view == 'Supplier mapping')
                OutlinedButton.icon(
                  onPressed: controller.busy ? null : () => _mappingDialog(),
                  icon: const Icon(Icons.add_link_rounded),
                  label: const Text('Add mapping'),
                ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _buildView(),
      ],
    );
  }

  Widget _buildView() => switch (view) {
    'Supplier mapping' => _mappingTable(),
    'Adjustments' => _adjustmentTable(),
    'Consumption' => _consumptionTable(),
    'Recipe coverage' => _coverageTable(),
    _ => _replenishmentTable(),
  };

  Widget _replenishmentTable() {
    if (controller.inventory.isEmpty) {
      return const _EmptyState(
        icon: Icons.inventory_rounded,
        title: 'No low-stock suggestions',
        message: 'Inventory is above configured reorder levels.',
      );
    }
    return _Panel(
      child: _ErpTable(
        columns: const [
          'Item',
          'Available',
          'Minimum',
          'Pending PO',
          'Suggested',
          'Supplier',
        ],
        rows: [
          for (final item in controller.inventory)
            [
              item.name,
              '${item.available.toStringAsFixed(2)} ${item.unit}',
              item.reorder.toStringAsFixed(2),
              item.pendingPurchase.toStringAsFixed(2),
              item.suggested.toStringAsFixed(2),
              item.supplier,
            ],
        ],
      ),
    );
  }

  Widget _mappingTable() => _Panel(
    child: controller.supplierMappings.isEmpty
        ? const Padding(
            padding: EdgeInsets.all(24),
            child: Text('No supplier-item mappings are configured.'),
          )
        : Column(
            children: [
              for (final mapping in controller.supplierMappings)
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(
                    mapping.productName,
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                  subtitle: Text(
                    '${mapping.supplierName} · ${mapping.unitName} · ${money(mapping.rate)} · lead ${mapping.leadTimeDays}d',
                  ),
                  leading: Icon(
                    mapping.isPreferred
                        ? Icons.star_rounded
                        : Icons.link_rounded,
                    color: mapping.isPreferred
                        ? AppColors.amber
                        : AppColors.primary,
                  ),
                  trailing: Wrap(
                    children: [
                      IconButton(
                        tooltip: 'Edit mapping',
                        onPressed: () => _mappingDialog(mapping),
                        icon: const Icon(Icons.edit_outlined),
                      ),
                      IconButton(
                        tooltip: 'Delete mapping',
                        onPressed: () => _deleteMapping(mapping),
                        icon: const Icon(
                          Icons.delete_outline_rounded,
                          color: AppColors.red,
                        ),
                      ),
                    ],
                  ),
                ),
            ],
          ),
  );

  Widget _adjustmentTable() => _Panel(
    child: _ErpTable(
      columns: const [
        'Voucher',
        'Date',
        'Type',
        'Description',
        'User',
        'Value',
      ],
      rows: [
        for (final item in controller.stockAdjustments)
          [
            item.voucherNo,
            item.dateMiti,
            _adjustmentType(item.adjustmentType),
            item.description,
            item.userName,
            money(item.totalAmount),
          ],
      ],
    ),
  );

  Widget _consumptionTable() => _Panel(
    child: _ErpTable(
      columns: const [
        'Date',
        'Order',
        'Menu item',
        'Raw material',
        'Qty',
        'Amount',
      ],
      rows: [
        for (final item in controller.consumptionLedger)
          [
            item.dateMiti,
            item.orderNo,
            item.menuItemName,
            item.rawMaterialName,
            '${item.qty.toStringAsFixed(2)} ${item.unitName}',
            money(item.amount),
          ],
      ],
    ),
  );

  Widget _coverageTable() => _Panel(
    child: _ErpTable(
      columns: const [
        'Menu item',
        'Category',
        'Recipe',
        'Cost',
        'Price',
        'Food cost',
      ],
      rows: [
        for (final item in controller.recipeCoverage)
          [
            item.productName,
            item.categoryName,
            item.hasRecipe ? '${item.activeRecipeLineCount} lines' : 'Missing',
            money(item.estimatedRecipeCost),
            money(item.menuPrice),
            '${item.foodCostPercent.toStringAsFixed(2)}%',
          ],
      ],
    ),
  );

  Future<void> _generatePurchaseOrders() async {
    try {
      final count = await controller.generatePurchaseOrders();
      _message('$count draft purchase order(s) created.');
    } on ApiException catch (error) {
      _message(error.message);
    }
  }

  Future<void> _stockAdjustmentDialog() async {
    if (controller.rawMaterials.isEmpty || controller.inventoryUnits.isEmpty) {
      _message('Raw materials and units must be configured first.');
      return;
    }
    final types = <int>[
      if (controller.hasPermission(
        'Pages.Restaurant.Inventory.StockAdjustment',
      )) ...[
        0,
        1,
        2,
      ],
      if (controller.hasPermission('Pages.Restaurant.Inventory.Wastage')) 3,
    ];
    var type = types.first;
    var product = controller.rawMaterials.first;
    var unit = controller.inventoryUnits.first;
    final qty = TextEditingController(text: '1');
    final counted = TextEditingController();
    final rate = TextEditingController(text: '0');
    final reason = TextEditingController();
    final description = TextEditingController();
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Create stock adjustment'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  DropdownButtonFormField<int>(
                    initialValue: type,
                    decoration: const InputDecoration(labelText: 'Type'),
                    items: [
                      for (final item in types)
                        DropdownMenuItem(
                          value: item,
                          child: Text(_adjustmentType(item)),
                        ),
                    ],
                    onChanged: (value) =>
                        setDialogState(() => type = value ?? 0),
                  ),
                  const SizedBox(height: 10),
                  DropdownButtonFormField<UniversalOption>(
                    initialValue: product,
                    isExpanded: true,
                    decoration: const InputDecoration(
                      labelText: 'Raw material',
                    ),
                    items: [
                      for (final item in controller.rawMaterials)
                        DropdownMenuItem(value: item, child: Text(item.name)),
                    ],
                    onChanged: (value) =>
                        setDialogState(() => product = value ?? product),
                  ),
                  const SizedBox(height: 10),
                  DropdownButtonFormField<UniversalOption>(
                    initialValue: unit,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Unit'),
                    items: [
                      for (final item in controller.inventoryUnits)
                        DropdownMenuItem(value: item, child: Text(item.name)),
                    ],
                    onChanged: (value) =>
                        setDialogState(() => unit = value ?? unit),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: qty,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Quantity',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: counted,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Counted qty',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: rate,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(labelText: 'Rate'),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: reason,
                    decoration: const InputDecoration(labelText: 'Reason'),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: description,
                    decoration: const InputDecoration(labelText: 'Description'),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Back'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Save'),
            ),
          ],
        ),
      ),
    );
    if (accepted == true) {
      try {
        await controller.saveStockAdjustment(
          adjustmentType: type,
          productId: product.id,
          unitId: unit.id,
          qty: double.tryParse(qty.text) ?? 0,
          countedQty: counted.text.trim().isEmpty
              ? null
              : double.tryParse(counted.text),
          rate: double.tryParse(rate.text) ?? 0,
          reason: reason.text.trim(),
          description: description.text.trim(),
        );
      } on ApiException catch (error) {
        _message(error.message);
      }
    }
    qty.dispose();
    counted.dispose();
    rate.dispose();
    reason.dispose();
    description.dispose();
  }

  Future<void> _mappingDialog([SupplierItemMapping? mapping]) async {
    if (controller.rawMaterials.isEmpty ||
        controller.suppliers.isEmpty ||
        controller.inventoryUnits.isEmpty) {
      _message('Raw materials, suppliers and units must be configured first.');
      return;
    }
    UniversalOption pick(List<UniversalOption> values, String id) =>
        values.where((item) => item.id == id).firstOrNull ?? values.first;
    var product = pick(controller.rawMaterials, mapping?.productId ?? '');
    var supplier = pick(controller.suppliers, mapping?.supplierLedgerId ?? '');
    var unit = pick(controller.inventoryUnits, mapping?.unitId ?? '');
    var preferred = mapping?.isPreferred ?? true;
    var active = mapping?.isActive ?? true;
    final sku = TextEditingController(text: mapping?.supplierSku ?? '');
    final rate = TextEditingController(text: '${mapping?.rate ?? 0}');
    final lead = TextEditingController(text: '${mapping?.leadTimeDays ?? 0}');
    final minimum = TextEditingController(
      text: '${mapping?.minimumOrderQty ?? 1}',
    );
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(
            mapping == null ? 'Add supplier mapping' : 'Edit supplier mapping',
          ),
          content: SizedBox(
            width: 540,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  _optionField(
                    'Raw material',
                    product,
                    controller.rawMaterials,
                    (value) => setDialogState(() => product = value),
                  ),
                  const SizedBox(height: 10),
                  _optionField(
                    'Supplier',
                    supplier,
                    controller.suppliers,
                    (value) => setDialogState(() => supplier = value),
                  ),
                  const SizedBox(height: 10),
                  _optionField(
                    'Unit',
                    unit,
                    controller.inventoryUnits,
                    (value) => setDialogState(() => unit = value),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: sku,
                    decoration: const InputDecoration(
                      labelText: 'Supplier SKU',
                    ),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: rate,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(labelText: 'Rate'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: lead,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Lead days',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: minimum,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Minimum qty',
                          ),
                        ),
                      ),
                    ],
                  ),
                  SwitchListTile(
                    value: preferred,
                    onChanged: (value) =>
                        setDialogState(() => preferred = value),
                    title: const Text('Preferred supplier'),
                  ),
                  SwitchListTile(
                    value: active,
                    onChanged: (value) => setDialogState(() => active = value),
                    title: const Text('Active'),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Back'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Save'),
            ),
          ],
        ),
      ),
    );
    if (accepted == true) {
      try {
        await controller.saveSupplierMapping(
          id: mapping?.id,
          productId: product.id,
          supplierLedgerId: supplier.id,
          supplierSku: sku.text.trim(),
          unitId: unit.id,
          rate: double.tryParse(rate.text) ?? 0,
          leadTimeDays: int.tryParse(lead.text) ?? 0,
          minimumOrderQty: double.tryParse(minimum.text) ?? 0,
          isPreferred: preferred,
          isActive: active,
        );
      } on ApiException catch (error) {
        _message(error.message);
      }
    }
    sku.dispose();
    rate.dispose();
    lead.dispose();
    minimum.dispose();
  }

  Widget _optionField(
    String label,
    UniversalOption value,
    List<UniversalOption> values,
    ValueChanged<UniversalOption> changed,
  ) {
    return DropdownButtonFormField<UniversalOption>(
      initialValue: value,
      isExpanded: true,
      decoration: InputDecoration(labelText: label),
      items: [
        for (final item in values)
          DropdownMenuItem(value: item, child: Text(item.name)),
      ],
      onChanged: (item) {
        if (item != null) changed(item);
      },
    );
  }

  Future<void> _deleteMapping(SupplierItemMapping mapping) async {
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete supplier mapping?'),
        content: Text('${mapping.productName} · ${mapping.supplierName}'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Keep'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (accepted == true) {
      try {
        await controller.deleteSupplierMapping(mapping.id);
      } on ApiException catch (error) {
        _message(error.message);
      }
    }
  }

  void _message(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

String _adjustmentType(int type) => switch (type) {
  0 => 'Increase',
  1 => 'Decrease',
  2 => 'Stock count',
  3 => 'Wastage',
  _ => 'Type $type',
};
