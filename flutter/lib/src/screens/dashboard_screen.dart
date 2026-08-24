part of '../../main.dart';

class DashboardScreen extends StatelessWidget {
  const DashboardScreen({
    required this.controller,
    required this.modules,
    required this.onOpen,
    super.key,
  });

  final RestaurantAppController controller;
  final List<ErpModule> modules;
  final ValueChanged<ModuleKind> onOpen;

  @override
  Widget build(BuildContext context) {
    final activeTickets = controller.tickets
        .where((ticket) => ticket.status != TicketStatus.bumped)
        .toList();
    final occupied = controller.tables.where((table) => table.occupied).length;
    final canViewSales = controller.canViewReportCategory(
      'Pages.Restaurant.Reports.Sales',
    );
    final canViewOperations = controller.canViewReportCategory(
      'Pages.Restaurant.Reports.Operations',
    );
    final canViewInventory = controller.canViewReportCategory(
      'Pages.Restaurant.Reports.Inventory',
    );
    final canViewPayroll = controller.canViewReportCategory(
      'Pages.Restaurant.Reports.Payroll',
    );
    final canViewAudit = controller.canViewReportCategory(
      'Pages.Restaurant.Reports.AuditFinance',
    );
    final reportTickets = controller.reportBundle.kotBotStatus;
    final pendingReportTickets = reportTickets.where((row) {
      return _string(row['status']).toLowerCase() == 'pending';
    }).length;
    final lowStock = canViewInventory
        ? controller.reportBundle.lowStock.length
        : controller.inventory.where((item) => item.low).length;
    final wastageAmount = controller.reportBundle.wastage.fold<double>(
      0,
      (total, row) => total + _number(row['amount']),
    );
    final discountAmount = controller.reportBundle.discounts.fold<double>(
      0,
      (total, row) => total + _number(row['totalDiscountAmount']),
    );
    final settledSales = controller.reportBundle.settlements.fold<double>(
      0,
      (total, row) => total + _number(row['grandTotal']),
    );
    final settledOrders = controller.reportBundle.settlements.fold<int>(
      0,
      (total, row) => total + _integer(row['orderCount']),
    );
    final payroll = controller.reportBundle.payrollSummary;
    final managementCards = <Widget>[
      if (canViewSales) ...[
        _KpiCard(
          title: 'Sales today',
          value: money(controller.report.grandTotal),
          helper: '${controller.report.orderCount} billed orders',
          icon: Icons.payments_rounded,
          color: AppColors.green,
        ),
        _KpiCard(
          title: 'Average bill',
          value: money(controller.report.averageBill),
          helper: 'Discount ${money(controller.report.discountAmount)}',
          icon: Icons.receipt_long_rounded,
          color: AppColors.primary,
        ),
      ],
      if (canViewOperations)
        _KpiCard(
          title: 'Pending KOT / BOT',
          value: '$pendingReportTickets',
          helper: '${reportTickets.length} tracked tickets',
          icon: Icons.soup_kitchen_rounded,
          color: AppColors.amber,
        ),
      if (canViewInventory) ...[
        _KpiCard(
          title: 'Low stock',
          value: '$lowStock',
          helper: 'Items needing attention',
          icon: Icons.warning_amber_rounded,
          color: AppColors.red,
        ),
        _KpiCard(
          title: 'Wastage',
          value: money(wastageAmount),
          helper: '${controller.reportBundle.wastage.length} entries',
          icon: Icons.delete_outline_rounded,
          color: AppColors.red,
        ),
      ],
      if (canViewPayroll) ...[
        _KpiCard(
          title: 'Active employees',
          value: '${_integer(payroll['activeEmployeeCount'])}',
          helper:
              '${_integer(payroll['attendanceRecordCount'])} attendance records',
          icon: Icons.groups_rounded,
          color: AppColors.violet,
        ),
        _KpiCard(
          title: 'Net payroll',
          value: money(_number(payroll['totalNet'])),
          helper: '${_integer(payroll['payrollRunCount'])} pay runs',
          icon: Icons.account_balance_wallet_rounded,
          color: AppColors.green,
        ),
      ],
      if (canViewAudit) ...[
        _KpiCard(
          title: 'Settled sales',
          value: money(settledSales),
          helper: '$settledOrders settled orders',
          icon: Icons.account_balance_rounded,
          color: AppColors.green,
        ),
        _KpiCard(
          title: 'Discount exposure',
          value: money(discountAmount),
          helper:
              '${controller.reportBundle.voidAudit.length} void / cancelled records',
          icon: Icons.policy_outlined,
          color: AppColors.amber,
        ),
      ],
    ];
    final operationalCards = <Widget>[
      _KpiCard(
        title: 'Occupied tables',
        value: '$occupied / ${controller.tables.length}',
        helper: '${controller.openOrders.length} open orders',
        icon: Icons.table_bar_rounded,
        color: AppColors.primary,
      ),
      _KpiCard(
        title: 'Kitchen tickets',
        value: '${activeTickets.length}',
        helper:
            '${activeTickets.where((item) => item.minutes >= 15).length} over 15 minutes',
        icon: Icons.soup_kitchen_rounded,
        color: AppColors.amber,
      ),
      _KpiCard(
        title: 'Low stock',
        value: '$lowStock',
        helper: '${controller.inventory.length} suggestions loaded',
        icon: Icons.warning_amber_rounded,
        color: AppColors.red,
      ),
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: AppColors.primaryDark,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Wrap(
            spacing: 24,
            runSpacing: 14,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              const Icon(
                Icons.restaurant_rounded,
                color: Colors.white,
                size: 42,
              ),
              SizedBox(
                width: 430,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Welcome, ${controller.profile?.name ?? controller.profile?.userName ?? 'team'}',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 24,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      controller.hasPermission('Pages.Restaurant.Reports')
                          ? 'Decision-ready restaurant, inventory, payroll, and audit insights for ${controller.brand.displayName}.'
                          : 'Live restaurant operations for ${controller.brand.displayName}.',
                      style: const TextStyle(
                        color: Color(0xFFD9E7FB),
                        height: 1.35,
                      ),
                    ),
                  ],
                ),
              ),
              if (controller.hasPermission('Pages.Restaurant.Pos'))
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    if (controller.tables.isNotEmpty)
                      _orderModeButton(
                        'Dine in',
                        Icons.table_restaurant_rounded,
                        controller.tables.first.id,
                      ),
                    _orderModeButton(
                      'Takeaway',
                      Icons.shopping_bag_rounded,
                      'mode:takeaway',
                    ),
                    _orderModeButton(
                      'Delivery',
                      Icons.delivery_dining_rounded,
                      'mode:delivery',
                    ),
                  ],
                ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        _ResponsiveGrid(
          minTileWidth: 210,
          tileHeight: 148,
          children: managementCards.isNotEmpty
              ? managementCards
              : operationalCards,
        ),
        const SizedBox(height: 16),
        if (controller.hasPermission('Pages.Restaurant.Kds') &&
            activeTickets.isNotEmpty) ...[
          _LiveServiceBoard(tickets: activeTickets),
          const SizedBox(height: 16),
        ],
        _SectionHeader(
          title: 'Available modules',
          action:
              '${math.max(0, modules.length - 1)} based on your permissions',
          icon: Icons.apps_rounded,
        ),
        const SizedBox(height: 10),
        _ResponsiveGrid(
          minTileWidth: 250,
          tileHeight: 132,
          children: [
            for (final module in modules.where(
              (module) => module.kind != ModuleKind.dashboard,
            ))
              _ModuleCard(module: module, onTap: () => onOpen(module.kind)),
          ],
        ),
      ],
    );
  }

  Widget _orderModeButton(String label, IconData icon, String contextValue) {
    return FilledButton.icon(
      onPressed: () {
        controller.selectPosContext(contextValue);
        onOpen(ModuleKind.pos);
      },
      icon: Icon(icon),
      label: Text(label),
    );
  }
}
