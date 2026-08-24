part of '../../main.dart';

class MenuRecipeScreen extends StatefulWidget {
  const MenuRecipeScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<MenuRecipeScreen> createState() => _MenuRecipeScreenState();
}

class _MenuRecipeScreenState extends State<MenuRecipeScreen> {
  String query = '';
  String category = 'All';

  RestaurantAppController get controller => widget.controller;

  @override
  Widget build(BuildContext context) {
    final categories = controller.products.map((item) => item.category).toSet()
      ..remove('');
    final products = controller.products.where((product) {
      final categoryMatch = category == 'All' || product.category == category;
      final text = '${product.name} ${product.shortCode} ${product.category}'
          .toLowerCase();
      return categoryMatch && text.contains(query.trim().toLowerCase());
    }).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Menu & Recipes',
          action:
              '${controller.products.length} sellable items · ${categories.length} categories',
          icon: Icons.menu_book_rounded,
        ),
        const SizedBox(height: 12),
        _Panel(
          child: Wrap(
            spacing: 10,
            runSpacing: 10,
            children: [
              SizedBox(
                width: 300,
                child: TextField(
                  onChanged: (value) => setState(() => query = value),
                  decoration: const InputDecoration(
                    labelText: 'Search item or short code',
                    prefixIcon: Icon(Icons.search_rounded),
                    isDense: true,
                  ),
                ),
              ),
              SizedBox(
                width: 220,
                child: DropdownButtonFormField<String>(
                  initialValue: category,
                  isExpanded: true,
                  decoration: const InputDecoration(
                    labelText: 'Category',
                    isDense: true,
                  ),
                  items: [
                    const DropdownMenuItem(value: 'All', child: Text('All')),
                    for (final value in categories)
                      DropdownMenuItem(value: value, child: Text(value)),
                  ],
                  onChanged: (value) =>
                      setState(() => category = value ?? 'All'),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        if (products.isEmpty)
          const _EmptyState(
            icon: Icons.no_meals_rounded,
            title: 'No menu items found',
            message: 'Try another category or search term.',
          )
        else
          _ResponsiveGrid(
            minTileWidth: 275,
            tileHeight: 205,
            children: [for (final product in products) _productCard(product)],
          ),
      ],
    );
  }

  Widget _productCard(MenuProduct product) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: product.color.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(Icons.fastfood_rounded, color: product.color),
              ),
              const Spacer(),
              _TinyTag(
                label: product.available ? 'Available' : 'Unavailable',
                color: product.available ? AppColors.green : AppColors.red,
              ),
              if (controller.hasPermission('Pages.Restaurant.ItemAvailability'))
                PopupMenuButton<String>(
                  tooltip: 'Availability',
                  onSelected: (value) => _setAvailability(product, value),
                  itemBuilder: (context) => const [
                    PopupMenuItem(value: 'available', child: Text('Available')),
                    PopupMenuItem(
                      value: 'hour',
                      child: Text('Sold out · 1 hour'),
                    ),
                    PopupMenuItem(
                      value: 'today',
                      child: Text('Sold out · today'),
                    ),
                    PopupMenuItem(
                      value: 'indefinite',
                      child: Text('Sold out indefinitely'),
                    ),
                  ],
                ),
            ],
          ),
          const SizedBox(height: 9),
          Text(
            product.name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 15.5, fontWeight: FontWeight.w900),
          ),
          Text(
            '${product.category} · ${product.station}'
            '${product.shortCode.isEmpty ? '' : ' · ${product.shortCode}'}',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
          const Spacer(),
          Row(
            children: [
              Expanded(
                child: Text(
                  money(product.price),
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              if (controller.hasPermission('Pages.Restaurant.Recipe'))
                TextButton.icon(
                  onPressed: product.productId.isEmpty
                      ? null
                      : () => _showRecipe(product),
                  icon: const Icon(Icons.receipt_long_rounded, size: 18),
                  label: const Text('Recipe'),
                ),
            ],
          ),
          Text(
            '${product.variants.length} variants · ${product.modifierGroups.length} modifiers · '
            '${product.preparationMinutes} min prep · ${product.hasRecipe ? 'recipe linked' : 'no recipe'}',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.muted, fontSize: 11.5),
          ),
        ],
      ),
    );
  }

  Future<void> _setAvailability(MenuProduct product, String value) async {
    DateTime? until;
    if (value == 'hour') until = DateTime.now().add(const Duration(hours: 1));
    if (value == 'today') {
      final now = DateTime.now();
      until = DateTime(now.year, now.month, now.day, 23, 59, 59);
    }
    try {
      await controller.setMenuAvailability(
        product,
        value == 'available',
        unavailableUntil: until,
      );
    } on ApiException catch (error) {
      _error(error.message);
    }
  }

  Future<void> _showRecipe(MenuProduct product) async {
    try {
      final details = await controller.getMenuRecipeDetails(product);
      if (!mounted) return;
      final lines = (details['lines'] as List<dynamic>? ?? const [])
          .map((value) => Map<String, dynamic>.from(value as Map))
          .toList();
      final cost = Map<String, dynamic>.from(
        details['cost'] as Map? ?? const <String, dynamic>{},
      );
      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('${product.name} recipe'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (lines.isEmpty)
                    const Text('No active recipe lines are configured.')
                  else
                    for (final line in lines)
                      ListTile(
                        dense: true,
                        contentPadding: EdgeInsets.zero,
                        title: Text(
                          '${line['rawMaterialName'] ?? line['productName'] ?? 'Ingredient'}',
                        ),
                        trailing: Text(
                          '${line['qty'] ?? line['quantity'] ?? 0} ${line['unitName'] ?? ''}',
                        ),
                      ),
                  const Divider(),
                  Text(
                    'Estimated cost: ${money(_reportNumber(cost, ['estimatedCost', 'recipeCost', 'totalCost']))}',
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                  Text(
                    'Food cost: ${_reportNumber(cost, ['foodCostPercent']).toStringAsFixed(2)}%',
                    style: const TextStyle(color: AppColors.muted),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Close'),
            ),
          ],
        ),
      );
    } on ApiException catch (error) {
      _error(error.message);
    }
  }

  void _error(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

double _reportNumber(Map<String, dynamic> row, List<String> keys) {
  for (final key in keys) {
    final value = row[key];
    if (value is num) return value.toDouble();
    final parsed = double.tryParse('$value');
    if (parsed != null) return parsed;
  }
  return 0;
}
