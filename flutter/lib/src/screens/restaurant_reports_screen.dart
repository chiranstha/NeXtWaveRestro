part of '../../main.dart';

class RestaurantReportsScreen extends StatefulWidget {
  const RestaurantReportsScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<RestaurantReportsScreen> createState() =>
      _RestaurantReportsScreenState();
}

class _RestaurantReportsScreenState extends State<RestaurantReportsScreen> {
  late DateTime from = DateTime(
    DateTime.now().year,
    DateTime.now().month,
    DateTime.now().day,
  );
  late DateTime to = DateTime.now();
  String category = 'Sales';
  String selected = 'Item sales';

  RestaurantAppController get controller => widget.controller;

  Map<String, Map<String, List<Map<String, dynamic>>>> get reportGroups {
    final data = controller.reportBundle;
    return {
      if (controller.canViewReportCategory('Pages.Restaurant.Reports.Sales'))
        'Sales': {
          'Item sales': data.itemSales,
          'Daily sales': data.dailySales,
          'Item margins': data.itemMargins,
        },
      if (controller.canViewReportCategory(
        'Pages.Restaurant.Reports.Operations',
      ))
        'Operations': {
          'Table sales': data.tableSales,
          'Waiter sales': data.waiterSales,
          'KOT / BOT status': data.kotBotStatus,
          'Waiter performance': data.waiterPerformance,
          'Table turnover': data.tableTurnover,
        },
      if (controller.canViewReportCategory(
        'Pages.Restaurant.Reports.Inventory',
      ))
        'Inventory & Cost': {
          'Material consumption': data.materialConsumption,
          'Recipe costing': data.recipeCosting,
          'Food costing': data.foodCosting,
          'Wastage': data.wastage,
          'Low stock': data.lowStock,
        },
      if (controller.canViewReportCategory('Pages.Restaurant.Reports.Payroll'))
        'Payroll & Attendance': {
          'Payroll runs': data.payrollRuns,
          'Employee payroll cost': data.payrollEmployeeCosts,
          'Attendance summary': data.payrollAttendance,
        },
      if (controller.canViewReportCategory(
        'Pages.Restaurant.Reports.AuditFinance',
      ))
        'Audit & Finance': {
          'Void / cancelled audit': data.voidAudit,
          'Discount audit': data.discounts,
          'Settlements': data.settlements,
        },
    };
  }

  @override
  Widget build(BuildContext context) {
    final report = controller.reportBundle.summary;
    final groups = reportGroups;
    if (!groups.containsKey(category) && groups.isNotEmpty) {
      category = groups.keys.first;
    }
    final sections = groups[category] ?? const {};
    if (!sections.containsKey(selected) && sections.isNotEmpty) {
      selected = sections.keys.first;
    }
    final rows = sections[selected] ?? const [];
    final kpis = _categoryKpis(category, report);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Restaurant Reports',
          action: '${_date(from)} to ${_date(to)} · server-calculated',
          icon: Icons.insights_rounded,
        ),
        const SizedBox(height: 12),
        _Panel(
          child: Wrap(
            spacing: 10,
            runSpacing: 10,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              for (final group in groups.keys)
                ChoiceChip(
                  label: Text(group),
                  selected: category == group,
                  onSelected: (_) => setState(() {
                    category = group;
                    selected = groups[group]!.keys.first;
                  }),
                ),
              const SizedBox(width: 4),
              SizedBox(
                width: 270,
                child: DropdownButtonFormField<String>(
                  initialValue: selected,
                  isExpanded: true,
                  decoration: const InputDecoration(
                    labelText: 'Detailed report',
                    isDense: true,
                  ),
                  items: [
                    for (final title in sections.keys)
                      DropdownMenuItem(value: title, child: Text(title)),
                  ],
                  onChanged: (value) =>
                      setState(() => selected = value ?? selected),
                ),
              ),
              OutlinedButton.icon(
                onPressed: _pickDates,
                icon: const Icon(Icons.date_range_rounded),
                label: Text('${_date(from)} – ${_date(to)}'),
              ),
              FilledButton.icon(
                onPressed: controller.busy ? null : _refresh,
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('Run report'),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(minTileWidth: 210, tileHeight: 145, children: kpis),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            children: [
              _Toolbar(
                title: selected,
                button: '${rows.length} rows',
                icon: _reportIcon(selected),
              ),
              if (rows.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(28),
                  child: Text(
                    'No rows were returned for this report and date range.',
                    style: TextStyle(color: AppColors.muted),
                  ),
                )
              else
                _genericReportTable(rows),
            ],
          ),
        ),
      ],
    );
  }

  List<Widget> _categoryKpis(String group, ReportSummary report) {
    final data = controller.reportBundle;
    switch (group) {
      case 'Operations':
        return [
          _KpiCard(
            title: 'Tracked tickets',
            value: '${data.kotBotStatus.length}',
            helper: '${data.tableTurnover.length} tables measured',
            icon: Icons.soup_kitchen_rounded,
            color: AppColors.amber,
          ),
          _KpiCard(
            title: 'Waiters',
            value: '${data.waiterPerformance.length}',
            helper: 'Performance rows',
            icon: Icons.badge_outlined,
            color: AppColors.primary,
          ),
        ];
      case 'Inventory & Cost':
        final wastage = data.wastage.fold<double>(
          0,
          (total, row) => total + _number(row['amount']),
        );
        return [
          _KpiCard(
            title: 'Low stock',
            value: '${data.lowStock.length}',
            helper: 'Items needing attention',
            icon: Icons.warning_amber_rounded,
            color: AppColors.red,
          ),
          _KpiCard(
            title: 'Wastage',
            value: money(wastage),
            helper: '${data.wastage.length} entries',
            icon: Icons.delete_outline_rounded,
            color: AppColors.red,
          ),
          _KpiCard(
            title: 'Food cost items',
            value: '${data.foodCosting.length}',
            helper: 'Actual and theoretical cost',
            icon: Icons.calculate_outlined,
            color: AppColors.violet,
          ),
        ];
      case 'Payroll & Attendance':
        final payroll = data.payrollSummary;
        return [
          _KpiCard(
            title: 'Active employees',
            value: '${_integer(payroll['activeEmployeeCount'])}',
            helper:
                '${_integer(payroll['attendanceRecordCount'])} attendance records',
            icon: Icons.groups_rounded,
            color: AppColors.primary,
          ),
          _KpiCard(
            title: 'Gross payroll',
            value: money(_number(payroll['totalGross'])),
            helper: '${_integer(payroll['payrollRunCount'])} runs',
            icon: Icons.payments_outlined,
            color: AppColors.amber,
          ),
          _KpiCard(
            title: 'Net payroll',
            value: money(_number(payroll['totalNet'])),
            helper: 'After deductions',
            icon: Icons.account_balance_wallet_outlined,
            color: AppColors.green,
          ),
        ];
      case 'Audit & Finance':
        final discounts = data.discounts.fold<double>(
          0,
          (total, row) => total + _number(row['totalDiscountAmount']),
        );
        return [
          _KpiCard(
            title: 'Discounts',
            value: money(discounts),
            helper: '${data.discounts.length} audited orders',
            icon: Icons.discount_outlined,
            color: AppColors.amber,
          ),
          _KpiCard(
            title: 'Void / cancelled',
            value: '${data.voidAudit.length}',
            helper: 'Audit records',
            icon: Icons.policy_outlined,
            color: AppColors.red,
          ),
          _KpiCard(
            title: 'Payment methods',
            value: '${data.settlements.length}',
            helper: 'Settlement groups',
            icon: Icons.account_balance_outlined,
            color: AppColors.violet,
          ),
        ];
      default:
        return [
          _KpiCard(
            title: 'Net sales',
            value: money(report.grandTotal),
            helper: 'Gross ${money(report.grossAmount)}',
            icon: Icons.payments_rounded,
            color: AppColors.green,
          ),
          _KpiCard(
            title: 'Orders',
            value: '${report.orderCount}',
            helper: 'Average ${money(report.averageBill)}',
            icon: Icons.receipt_long_rounded,
            color: AppColors.primary,
          ),
          _KpiCard(
            title: 'Discounts',
            value: money(report.discountAmount),
            helper: 'Auditable in discount report',
            icon: Icons.discount_rounded,
            color: AppColors.amber,
          ),
        ];
    }
  }

  Widget _genericReportTable(List<Map<String, dynamic>> rows) {
    const preferred = [
      'dateMiti',
      'orderNo',
      'productName',
      'menuItemName',
      'tableName',
      'waiterName',
      'paymentMethodName',
      'qty',
      'orderCount',
      'grossAmount',
      'discountAmount',
      'taxAmount',
      'grandTotal',
      'amount',
      'statusName',
      'reason',
    ];
    final available = <String>{for (final row in rows) ...row.keys};
    final keys = [
      ...preferred.where(available.contains),
      ...available.where((key) => !preferred.contains(key)),
    ].take(8).toList();
    return _ErpTable(
      columns: [for (final key in keys) _readableKey(key)],
      rows: [
        for (final row in rows.take(150))
          [for (final key in keys) _reportCell(key, row[key])],
      ],
    );
  }

  Future<void> _pickDates() async {
    final range = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 1)),
      initialDateRange: DateTimeRange(start: from, end: to),
    );
    if (range != null) {
      setState(() {
        from = range.start;
        to = range.end.add(const Duration(hours: 23, minutes: 59, seconds: 59));
      });
    }
  }

  Future<void> _refresh() async {
    try {
      await controller.refreshReports(from, to);
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    }
  }
}

String _reportCell(String key, dynamic value) {
  if (value == null) return '';
  if (value is bool) return value ? 'Yes' : 'No';
  if (value is num) {
    final lower = key.toLowerCase();
    if (lower.contains('amount') ||
        lower.contains('total') ||
        lower.contains('cost') ||
        lower.contains('sales') ||
        lower.contains('margin')) {
      return value.toStringAsFixed(2);
    }
    return value % 1 == 0 ? value.toInt().toString() : value.toStringAsFixed(2);
  }
  if (value is List || value is Map) return jsonEncode(value);
  return '$value';
}

String _date(DateTime value) =>
    '${value.year}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';

IconData _reportIcon(String title) {
  if (title.contains('Waiter')) return Icons.badge_outlined;
  if (title.contains('Table')) return Icons.table_restaurant_outlined;
  if (title.contains('Stock') || title.contains('Material')) {
    return Icons.inventory_2_outlined;
  }
  if (title.contains('Void') || title.contains('Discount')) {
    return Icons.policy_outlined;
  }
  if (title.contains('Cost') || title.contains('margin')) {
    return Icons.calculate_outlined;
  }
  return Icons.bar_chart_rounded;
}
