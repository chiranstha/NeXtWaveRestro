part of '../../main.dart';

class RestaurantInventoryScreen extends StatelessWidget {
  const RestaurantInventoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final lowStock = inventoryItems.where((item) => item.low).toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(
          title: 'Restaurant Inventory',
          action: 'Raw material + recipe stock',
          icon: Icons.inventory_2_rounded,
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 220,
          tileHeight: 148,
          children: [
            _KpiCard(
              title: 'Stock Value',
              value: 'Rs 8,42,500',
              helper: 'FIFO valuation',
              icon: Icons.warehouse_rounded,
              color: AppColors.teal,
            ),
            _KpiCard(
              title: 'Low Stock',
              value: '${lowStock.length}',
              helper: 'PO draft ready',
              icon: Icons.warning_rounded,
              color: AppColors.red,
            ),
            const _KpiCard(
              title: 'Consumption Today',
              value: 'Rs 36,850',
              helper: 'Recipe auto-posting',
              icon: Icons.soup_kitchen_rounded,
              color: AppColors.amber,
            ),
          ],
        ),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            children: [
              const _Toolbar(
                title: 'Stock Ledger',
                button: 'Stock Adjustment',
                icon: Icons.add_box_rounded,
              ),
              _ErpTable(
                columns: const [
                  'Item',
                  'Group',
                  'Qty',
                  'Reorder',
                  'Cost',
                  'Supplier',
                  'Status',
                ],
                rows: [
                  for (final item in inventoryItems)
                    [
                      item.name,
                      item.group,
                      '${item.available.toStringAsFixed(1)} ${item.unit}',
                      '${item.reorder.toStringAsFixed(1)} ${item.unit}',
                      money(item.cost),
                      item.supplier,
                      item.low ? 'Low Stock' : 'OK',
                    ],
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const _SectionHeader(
                title: 'Auto PO Suggestions',
                action: 'Based on reorder level',
                icon: Icons.auto_awesome_motion_rounded,
              ),
              const SizedBox(height: 10),
              for (final item in lowStock)
                _SuggestionTile(
                  icon: Icons.shopping_bag_rounded,
                  title: item.name,
                  subtitle:
                      'Order ${(item.reorder * 2 - item.available).toStringAsFixed(1)} ${item.unit} from ${item.supplier}',
                  color: AppColors.amber,
                ),
            ],
          ),
        ),
      ],
    );
  }
}
