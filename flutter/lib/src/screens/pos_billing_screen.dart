part of '../../main.dart';

class PosBillingScreen extends StatefulWidget {
  const PosBillingScreen({
    required this.products,
    required this.cart,
    required this.table,
    required this.channel,
    required this.category,
    required this.onTableChanged,
    required this.onChannelChanged,
    required this.onCategoryChanged,
    required this.onAdd,
    required this.onQty,
    required this.onFire,
    required this.onSettle,
    super.key,
  });

  final List<MenuProduct> products;
  final List<CartLine> cart;
  final String table;
  final String channel;
  final String category;
  final ValueChanged<String> onTableChanged;
  final ValueChanged<String> onChannelChanged;
  final ValueChanged<String> onCategoryChanged;
  final ValueChanged<MenuProduct> onAdd;
  final void Function(MenuProduct product, int delta) onQty;
  final VoidCallback onFire;
  final VoidCallback onSettle;

  @override
  State<PosBillingScreen> createState() => _PosBillingScreenState();
}

class _PosBillingScreenState extends State<PosBillingScreen> {
  String search = '';
  String customer = '';
  String paymentMode = 'Cash';
  double discount = 0;

  double get subtotal => cart.fold(0, (sum, line) => sum + line.total);
  double get serviceCharge => subtotal * 0.1;
  double get vat => (subtotal + serviceCharge) * 0.13;
  double get grandTotal => subtotal + serviceCharge + vat;
  double get payable => math.max(0, grandTotal - discount);

  List<CartLine> get cart => widget.cart;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final compact = width < 1050;
    final categories = [
      'All',
      ...widget.products.map((e) => e.category).toSet(),
    ];
    final normalizedSearch = search.trim().toLowerCase();
    final visible = widget.products.where((product) {
      final inCategory =
          widget.category == 'All' || product.category == widget.category;
      final matchesSearch =
          normalizedSearch.isEmpty ||
          product.name.toLowerCase().contains(normalizedSearch) ||
          product.id.toLowerCase().contains(normalizedSearch) ||
          product.category.toLowerCase().contains(normalizedSearch);
      return inCategory && matchesSearch;
    }).toList();
    final favorites = widget.products.take(5).toList();

    final catalog = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'POS Billing',
          action: 'Dine-in / Takeaway / Delivery',
          icon: Icons.point_of_sale_rounded,
        ),
        const SizedBox(height: 10),
        _PosEntryBar(
          search: search,
          table: widget.table,
          channel: widget.channel,
          onSearchChanged: (value) => setState(() => search = value),
          onTableChanged: widget.onTableChanged,
          onChannelChanged: widget.onChannelChanged,
        ),
        const SizedBox(height: 10),
        Wrap(
          spacing: 6,
          runSpacing: 6,
          children: [
            for (final item in categories)
              ChoiceChip(
                label: Text(item),
                selected: item == widget.category,
                visualDensity: VisualDensity.compact,
                onSelected: (_) => widget.onCategoryChanged(item),
              ),
          ],
        ),
        const SizedBox(height: 10),
        _PopularItemStrip(products: favorites, onAdd: widget.onAdd),
        const SizedBox(height: 10),
        _ResponsiveGrid(
          minTileWidth: 170,
          tileHeight: 140,
          children: [
            for (final product in visible)
              _MenuTile(product: product, onTap: () => widget.onAdd(product)),
          ],
        ),
      ],
    );

    final checkout = _CheckoutPanel(
      cart: cart,
      table: widget.table,
      channel: widget.channel,
      subtotal: subtotal,
      serviceCharge: serviceCharge,
      vat: vat,
      grandTotal: grandTotal,
      discount: discount,
      payable: payable,
      customer: customer,
      paymentMode: paymentMode,
      onCustomerChanged: (value) => setState(() => customer = value),
      onDiscountChanged: (value) => setState(() => discount = value),
      onPaymentModeChanged: (value) => setState(() => paymentMode = value),
      onQty: widget.onQty,
      onFire: widget.onFire,
      onSettle: widget.onSettle,
    );

    if (compact) {
      return Column(children: [catalog, const SizedBox(height: 16), checkout]);
    }

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(flex: 7, child: catalog),
        const SizedBox(width: 16),
        Expanded(flex: 4, child: checkout),
      ],
    );
  }
}

class _CheckoutPanel extends StatelessWidget {
  const _CheckoutPanel({
    required this.cart,
    required this.table,
    required this.channel,
    required this.subtotal,
    required this.serviceCharge,
    required this.vat,
    required this.grandTotal,
    required this.discount,
    required this.payable,
    required this.customer,
    required this.paymentMode,
    required this.onCustomerChanged,
    required this.onDiscountChanged,
    required this.onPaymentModeChanged,
    required this.onQty,
    required this.onFire,
    required this.onSettle,
  });

  final List<CartLine> cart;
  final String table;
  final String channel;
  final double subtotal;
  final double serviceCharge;
  final double vat;
  final double grandTotal;
  final double discount;
  final double payable;
  final String customer;
  final String paymentMode;
  final ValueChanged<String> onCustomerChanged;
  final ValueChanged<double> onDiscountChanged;
  final ValueChanged<String> onPaymentModeChanged;
  final void Function(MenuProduct product, int delta) onQty;
  final VoidCallback onFire;
  final VoidCallback onSettle;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const _SectionHeader(
            title: 'Current Bill',
            action: 'Draft',
            icon: Icons.receipt_long_rounded,
          ),
          const SizedBox(height: 10),
          _BillContextStrip(table: table, channel: channel),
          const SizedBox(height: 10),
          TextFormField(
            initialValue: customer,
            onChanged: onCustomerChanged,
            decoration: const InputDecoration(
              labelText: 'Customer / PAN / Phone',
              prefixIcon: Icon(Icons.person_search_rounded),
              isDense: true,
            ),
          ),
          const SizedBox(height: 10),
          if (cart.isEmpty)
            const _EmptyState(
              icon: Icons.shopping_cart_outlined,
              title: 'Cart is empty',
              message: 'Tap menu items to create a bill.',
            )
          else
            Column(
              children: [
                for (final line in cart)
                  _CartLineTile(line: line, onQty: onQty),
              ],
            ),
          const Divider(height: 20),
          _PaymentModeBar(
            selected: paymentMode,
            onSelected: onPaymentModeChanged,
          ),
          const SizedBox(height: 10),
          _DiscountEditor(discount: discount, onChanged: onDiscountChanged),
          const Divider(height: 20),
          _AmountRow(label: 'Subtotal', value: money(subtotal)),
          _AmountRow(label: 'Service Charge 10%', value: money(serviceCharge)),
          _AmountRow(label: 'VAT 13%', value: money(vat)),
          if (discount > 0)
            _AmountRow(label: 'Discount', value: '- ${money(discount)}'),
          const SizedBox(height: 6),
          _AmountRow(label: 'Payable', value: money(payable), strong: true),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: cart.isEmpty ? null : onFire,
                  icon: const Icon(Icons.local_fire_department_rounded),
                  label: const Text('Send KOT/BOT'),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: FilledButton.icon(
                  onPressed: cart.isEmpty ? null : onSettle,
                  icon: const Icon(Icons.done_all_rounded),
                  label: Text('Settle ${money(payable)}'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _PosEntryBar extends StatelessWidget {
  const _PosEntryBar({
    required this.search,
    required this.table,
    required this.channel,
    required this.onSearchChanged,
    required this.onTableChanged,
    required this.onChannelChanged,
  });

  final String search;
  final String table;
  final String channel;
  final ValueChanged<String> onSearchChanged;
  final ValueChanged<String> onTableChanged;
  final ValueChanged<String> onChannelChanged;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          LayoutBuilder(
            builder: (context, constraints) {
              final wide = constraints.maxWidth >= 760;
              final searchBox = TextFormField(
                initialValue: search,
                onChanged: onSearchChanged,
                decoration: const InputDecoration(
                  hintText:
                      'Search item, short code, category, or scan barcode',
                  prefixIcon: Icon(Icons.qr_code_scanner_rounded),
                  suffixIcon: Icon(Icons.keyboard_command_key_rounded),
                  isDense: true,
                ),
              );
              final contextFields = Row(
                children: [
                  Expanded(
                    child: _DropdownField(
                      label: 'Table',
                      value: table,
                      values: const [
                        'T-01',
                        'T-04',
                        'T-07',
                        'T-11',
                        'Takeaway',
                      ],
                      onChanged: onTableChanged,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _DropdownField(
                      label: 'Channel',
                      value: channel,
                      values: const [
                        'Dine-In',
                        'Takeaway',
                        'QR',
                        'Foodmandu',
                        'Pathao',
                      ],
                      onChanged: onChannelChanged,
                    ),
                  ),
                ],
              );
              if (!wide) {
                return Column(
                  children: [
                    searchBox,
                    const SizedBox(height: 8),
                    contextFields,
                  ],
                );
              }
              return Row(
                children: [
                  Expanded(flex: 5, child: searchBox),
                  const SizedBox(width: 10),
                  Expanded(flex: 4, child: contextFields),
                ],
              );
            },
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              for (final item in const [
                'T-01',
                'T-04',
                'T-07',
                'T-11',
                'Takeaway',
              ])
                _QuickSelectChip(
                  label: item,
                  selected: table == item,
                  onTap: () => onTableChanged(item),
                  icon: Icons.table_bar_rounded,
                ),
              for (final item in const ['Dine-In', 'Takeaway', 'QR'])
                _QuickSelectChip(
                  label: item,
                  selected: channel == item,
                  onTap: () => onChannelChanged(item),
                  icon: Icons.delivery_dining_rounded,
                ),
            ],
          ),
        ],
      ),
    );
  }
}

class _PopularItemStrip extends StatelessWidget {
  const _PopularItemStrip({required this.products, required this.onAdd});

  final List<MenuProduct> products;
  final ValueChanged<MenuProduct> onAdd;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 6,
      runSpacing: 6,
      children: [
        for (final product in products)
          ActionChip(
            visualDensity: VisualDensity.compact,
            avatar: Icon(Icons.add_rounded, size: 17, color: product.color),
            label: Text('${product.name}  ${money(product.price)}'),
            onPressed: () => onAdd(product),
          ),
      ],
    );
  }
}

class _QuickSelectChip extends StatelessWidget {
  const _QuickSelectChip({
    required this.label,
    required this.selected,
    required this.onTap,
    required this.icon,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return ChoiceChip(
      avatar: Icon(icon, size: 15),
      selected: selected,
      label: Text(label),
      visualDensity: VisualDensity.compact,
      onSelected: (_) => onTap(),
    );
  }
}

class _BillContextStrip extends StatelessWidget {
  const _BillContextStrip({required this.table, required this.channel});

  final String table;
  final String channel;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _ContextPill(
            icon: Icons.table_bar_rounded,
            label: table,
            color: AppColors.primary,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: _ContextPill(
            icon: Icons.hub_rounded,
            label: channel,
            color: AppColors.teal,
          ),
        ),
      ],
    );
  }
}

class _ContextPill extends StatelessWidget {
  const _ContextPill({
    required this.icon,
    required this.label,
    required this.color,
  });

  final IconData icon;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withValues(alpha: 0.16)),
      ),
      child: Row(
        children: [
          Icon(icon, color: color, size: 18),
          const SizedBox(width: 7),
          Expanded(
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w900),
            ),
          ),
        ],
      ),
    );
  }
}

class _PaymentModeBar extends StatelessWidget {
  const _PaymentModeBar({required this.selected, required this.onSelected});

  final String selected;
  final ValueChanged<String> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 6,
      runSpacing: 6,
      children: [
        for (final item in const ['Cash', 'Card', 'eSewa', 'Khalti', 'Credit'])
          ChoiceChip(
            selected: selected == item,
            label: Text(item),
            visualDensity: VisualDensity.compact,
            onSelected: (_) => onSelected(item),
          ),
      ],
    );
  }
}

class _DiscountEditor extends StatelessWidget {
  const _DiscountEditor({required this.discount, required this.onChanged});

  final double discount;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: TextFormField(
            key: ValueKey('discount-$discount'),
            initialValue: discount == 0 ? '' : discount.round().toString(),
            keyboardType: TextInputType.number,
            onChanged: (value) => onChanged(double.tryParse(value) ?? 0),
            decoration: const InputDecoration(
              labelText: 'Discount Rs',
              prefixIcon: Icon(Icons.percent_rounded),
              isDense: true,
            ),
          ),
        ),
        const SizedBox(width: 8),
        for (final value in const [50, 100, 250])
          Padding(
            padding: const EdgeInsets.only(left: 4),
            child: ActionChip(
              visualDensity: VisualDensity.compact,
              label: Text('-$value'),
              onPressed: () => onChanged(value.toDouble()),
            ),
          ),
      ],
    );
  }
}
