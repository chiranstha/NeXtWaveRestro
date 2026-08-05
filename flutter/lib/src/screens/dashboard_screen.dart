part of '../../main.dart';

class DashboardScreen extends StatelessWidget {
  const DashboardScreen({
    required this.modules,
    required this.tickets,
    required this.cart,
    required this.onOpen,
    super.key,
  });

  final List<ErpModule> modules;
  final List<KdsTicket> tickets;
  final List<CartLine> cart;
  final ValueChanged<ModuleKind> onOpen;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    final compact = width < 900;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _CommandPanel(onOpen: onOpen),
        const SizedBox(height: 16),
        _ResponsiveGrid(
          minTileWidth: 210,
          tileHeight: 148,
          children: [
            _KpiCard(
              title: 'Net Sales',
              value: 'Rs 1,42,850',
              helper: '+18.6% vs yesterday',
              icon: Icons.trending_up_rounded,
              color: AppColors.green,
            ),
            _KpiCard(
              title: 'Open Tables',
              value: '12 / 32',
              helper: '4 tables need service',
              icon: Icons.table_bar_rounded,
              color: AppColors.primary,
            ),
            _KpiCard(
              title: 'Kitchen Tickets',
              value:
                  '${tickets.where((t) => t.status != TicketStatus.bumped).length}',
              helper: '3 over target prep time',
              icon: Icons.soup_kitchen_rounded,
              color: AppColors.amber,
            ),
            _KpiCard(
              title: 'Low Stock',
              value: '${inventoryItems.where((i) => i.low).length}',
              helper: 'Auto PO suggestions ready',
              icon: Icons.warning_amber_rounded,
              color: AppColors.red,
            ),
          ],
        ),
        const SizedBox(height: 16),
        if (compact)
          Column(
            children: [
              _LiveServiceBoard(tickets: tickets),
              const SizedBox(height: 16),
              _PriorityWork(onOpen: onOpen),
            ],
          )
        else
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(flex: 7, child: _LiveServiceBoard(tickets: tickets)),
              const SizedBox(width: 16),
              Expanded(flex: 4, child: _PriorityWork(onOpen: onOpen)),
            ],
          ),
        const SizedBox(height: 16),
        _SectionHeader(
          title: 'ERP Modules',
          action: 'All ${modules.length} modules',
          icon: Icons.apps_rounded,
        ),
        const SizedBox(height: 10),
        _ResponsiveGrid(
          minTileWidth: 260,
          tileHeight: 132,
          children: [
            for (final module in modules.where(
              (m) => m.kind != ModuleKind.dashboard,
            ))
              _ModuleCard(module: module, onTap: () => onOpen(module.kind)),
          ],
        ),
      ],
    );
  }
}

class _CommandPanel extends StatelessWidget {
  const _CommandPanel({required this.onOpen});

  final ValueChanged<ModuleKind> onOpen;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.primaryDark,
        borderRadius: BorderRadius.circular(8),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final wide = constraints.maxWidth > 760;
          return Flex(
            direction: wide ? Axis.horizontal : Axis.vertical,
            crossAxisAlignment: wide
                ? CrossAxisAlignment.center
                : CrossAxisAlignment.start,
            children: [
              Flexible(
                flex: wide ? 3 : 0,
                fit: wide ? FlexFit.tight : FlexFit.loose,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: const [
                        _StatusPill(
                          label: 'OPEN SHIFT',
                          color: AppColors.green,
                        ),
                        _StatusPill(
                          label: 'IRD QUEUE: 2',
                          color: AppColors.amber,
                        ),
                        _StatusPill(
                          label: 'SYNC ONLINE',
                          color: AppColors.teal,
                        ),
                      ],
                    ),
                    const SizedBox(height: 18),
                    const Text(
                      'Restaurant command center',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 28,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Operate counter billing, KOT/BOT, purchases, stock, channels, and accounting from one mobile-first ERP workspace.',
                      style: TextStyle(
                        color: Color(0xFFC8D7EF),
                        fontSize: 14.5,
                        height: 1.45,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 18),
                    Wrap(
                      spacing: 10,
                      runSpacing: 10,
                      children: [
                        _ActionButton(
                          label: 'New Bill',
                          icon: Icons.point_of_sale_rounded,
                          color: AppColors.green,
                          onTap: () => onOpen(ModuleKind.pos),
                        ),
                        _ActionButton(
                          label: 'KDS Board',
                          icon: Icons.restaurant_menu_rounded,
                          color: AppColors.amber,
                          onTap: () => onOpen(ModuleKind.kds),
                        ),
                        _ActionButton(
                          label: 'Stock Used',
                          icon: Icons.soup_kitchen_rounded,
                          color: AppColors.teal,
                          onTap: () => onOpen(ModuleKind.restaurantStockUsed),
                        ),
                        _ActionButton(
                          label: 'Reports',
                          icon: Icons.insights_rounded,
                          color: AppColors.violet,
                          onTap: () => onOpen(ModuleKind.restaurantReports),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              if (wide) const SizedBox(width: 20),
              SizedBox(
                width: wide ? 360 : double.infinity,
                height: 220,
                child: CustomPaint(
                  painter: _DashboardChartPainter(),
                  child: const Align(
                    alignment: Alignment.bottomLeft,
                    child: Padding(
                      padding: EdgeInsets.all(16),
                      child: Text(
                        'Peak: 7:45 PM  |  Avg ticket Rs 1,185',
                        style: TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
