part of '../../main.dart';

String money(num value) {
  final rounded = value.round().toString();
  final buffer = StringBuffer();
  for (var i = 0; i < rounded.length; i++) {
    final reverseIndex = rounded.length - i;
    buffer.write(rounded[i]);
    if (reverseIndex > 1 && reverseIndex % 3 == 1) buffer.write(',');
  }
  return 'Rs ${buffer.toString()}';
}

Color statusColor(TicketStatus status) {
  return switch (status) {
    TicketStatus.queued => AppColors.red,
    TicketStatus.acknowledged => AppColors.amber,
    TicketStatus.preparing => AppColors.primary,
    TicketStatus.ready => AppColors.green,
    TicketStatus.bumped => AppColors.muted,
  };
}

String ticketStatusLabel(TicketStatus status) {
  return switch (status) {
    TicketStatus.queued => 'Queued',
    TicketStatus.acknowledged => 'Acknowledged',
    TicketStatus.preparing => 'Preparing',
    TicketStatus.ready => 'Ready',
    TicketStatus.bumped => 'Bumped',
  };
}

String nextStatusAction(TicketStatus status) {
  return switch (status) {
    TicketStatus.queued => 'Acknowledge',
    TicketStatus.acknowledged => 'Start Prep',
    TicketStatus.preparing => 'Mark Ready',
    TicketStatus.ready => 'Bump Ticket',
    TicketStatus.bumped => 'Done',
  };
}

String actionLabelFor(ErpModule module) {
  if (module.group.contains('Report')) return 'Run Report';
  if (module.group == 'Transactions') return 'New Voucher';
  if (module.group == 'Purchase') return 'New Purchase';
  if (module.group == 'Sales') return 'New Invoice';
  if (module.group == 'Inventory') return 'New Master';
  return 'Create';
}

List<String> workflowFor(ErpModule module) {
  if (module.group == 'Purchase') {
    return ['Draft', 'Approve', 'Receive Stock', 'Post Ledger'];
  }
  if (module.group == 'Sales') {
    return ['Create Bill', 'Apply Tax', 'Collect Payment', 'IRD Sync'];
  }
  if (module.group == 'Transactions') {
    return ['Prepare', 'Verify', 'Approve', 'Post'];
  }
  if (module.group == 'Inventory') {
    return ['Create', 'Map Unit', 'Opening Stock', 'Track Movement'];
  }
  if (module.group == 'Accounting') {
    return ['Create Group', 'Map Ledger', 'Opening Balance', 'Report'];
  }
  return ['Filter', 'Generate', 'Review', 'Export'];
}

List<_ReportSpec> _reportCardsFor(ErpModule module) {
  return [
    _ReportSpec(module.title, module.description, module.icon, module.color),
    const _ReportSpec(
      'Date Wise Summary',
      'Daily and monthly movement by fiscal period',
      Icons.calendar_month_rounded,
      AppColors.primary,
    ),
    const _ReportSpec(
      'Party / Product Drilldown',
      'Tap any row to open ledger or product detail',
      Icons.account_tree_rounded,
      AppColors.teal,
    ),
    const _ReportSpec(
      'Excel / PDF Export',
      'ABP-style file output and print preview',
      Icons.download_rounded,
      AppColors.amber,
    ),
  ];
}

ModuleWorkspaceData moduleWorkspaceFor(ErpModule module) {
  final commonMetrics = <MetricValue>[
    const MetricValue('Active rows', '6', Icons.list_alt_rounded),
    const MetricValue('Pending review', '2', Icons.pending_actions_rounded),
    MetricValue(
      'Total value',
      money(68420 + module.index * 1820),
      Icons.payments_rounded,
    ),
  ];

  return switch (module.kind) {
    ModuleKind.accountGroups => ModuleWorkspaceData(
      listTitle: 'Account Group Tree',
      primaryAction: 'Save Group',
      columns: const ['Group', 'Parent', 'Nature', 'Position', 'Status'],
      rows: const [
        ['Cash & Bank', 'Primary', 'Asset', '01', 'Active'],
        ['Sundry Debtors', 'Current Assets', 'Asset', '02', 'Active'],
        [
          'Sundry Creditors',
          'Current Liabilities',
          'Liability',
          '03',
          'Active',
        ],
        ['Direct Expenses', 'Expenses', 'Expense', '04', 'Active'],
        ['Sales Accounts', 'Income', 'Income', '05', 'Active'],
      ],
      metrics: commonMetrics,
      entryFields: const ['Group Name', 'Parent Group', 'Account Nature'],
      checklist: const [
        'Map parent group',
        'Select debit/credit nature',
        'Keep report order unique',
      ],
    ),
    ModuleKind.accountLedgers => ModuleWorkspaceData(
      listTitle: 'Ledger Master',
      primaryAction: 'Save Ledger',
      columns: const ['Ledger', 'Group', 'PAN / Phone', 'Opening', 'Balance'],
      rows: const [
        ['Cash Counter', 'Cash & Bank', '-', 'Rs 24,500 Dr', 'Rs 38,800 Dr'],
        [
          'Foodmandu Receivable',
          'Sundry Debtors',
          '609000111',
          'Rs 12,000 Dr',
          'Rs 17,100 Dr',
        ],
        [
          'Himalayan Fresh Mart',
          'Sundry Creditors',
          '9800000001',
          'Rs 42,000 Cr',
          'Rs 61,500 Cr',
        ],
        ['VAT Payable', 'Duties & Taxes', '-', 'Rs 8,100 Cr', 'Rs 26,820 Cr'],
      ],
      metrics: commonMetrics,
      entryFields: const [
        'Ledger Name',
        'Account Group',
        'Opening Balance',
        'PAN / Contact',
      ],
      checklist: const [
        'Assign account group',
        'Set party type',
        'Confirm opening Dr/Cr',
      ],
    ),
    ModuleKind.units => ModuleWorkspaceData(
      listTitle: 'Unit Conversion Master',
      primaryAction: 'Save Unit',
      columns: const ['Unit', 'Symbol', 'Decimal', 'Base Unit', 'Conversion'],
      rows: const [
        ['Piece', 'PCS', '0', 'PCS', '1 PCS'],
        ['Kilogram', 'KG', '3', 'GM', '1000 GM'],
        ['Litre', 'LTR', '3', 'ML', '1000 ML'],
        ['Plate', 'PLT', '0', 'PCS', '1 PCS'],
        ['Bottle', 'BTL', '0', 'PCS', '1 PCS'],
      ],
      metrics: commonMetrics,
      entryFields: const [
        'Unit Name',
        'Symbol',
        'Decimal Places',
        'Conversion Factor',
      ],
      checklist: const [
        'Define base unit',
        'Check decimal precision',
        'Use in product setup',
      ],
    ),
    ModuleKind.productGroups => ModuleWorkspaceData(
      listTitle: 'Product Groups',
      primaryAction: 'Save Group',
      columns: const ['Group', 'Type', 'Parent', 'Stock', 'Status'],
      rows: const [
        ['Raw Material', 'Inventory', 'Primary', 'Maintain', 'Active'],
        ['Semi Finished', 'Kitchen', 'Raw Material', 'Maintain', 'Active'],
        ['Menu Items', 'Sales', 'Primary', 'No Stock', 'Active'],
        ['Beverages', 'Sales', 'Menu Items', 'Maintain', 'Active'],
      ],
      metrics: commonMetrics,
      entryFields: const ['Group Name', 'Parent Group', 'Product Type'],
      checklist: const [
        'Choose stock behavior',
        'Map recipe usage',
        'Set display order',
      ],
    ),
    ModuleKind.products => ModuleWorkspaceData(
      listTitle: 'Product Master',
      primaryAction: 'Save Product',
      columns: const ['Product', 'Group', 'Unit', 'Sale Rate', 'Stock'],
      rows: const [
        ['Chicken Momo', 'Menu Items', 'Plate', 'Rs 280', '64'],
        ['Chicken Breast', 'Raw Material', 'KG', '-', '8.5 KG'],
        ['Paneer', 'Raw Material', 'KG', '-', '4.0 KG'],
        ['Coke 500ml', 'Beverages', 'Bottle', 'Rs 120', '18'],
        ['Cafe Latte', 'Beverages', 'Cup', 'Rs 210', '80'],
      ],
      metrics: commonMetrics,
      entryFields: const [
        'Product Name',
        'Product Group',
        'Unit',
        'Sale Rate',
        'Tax',
      ],
      checklist: const [
        'Map stock unit',
        'Assign tax',
        'Enable menu item if sellable',
      ],
    ),
    ModuleKind.openingStocks => ModuleWorkspaceData(
      listTitle: 'Opening Stock Batches',
      primaryAction: 'Post Opening',
      columns: const ['Product', 'Outlet', 'Batch', 'Qty', 'Cost', 'Expiry'],
      rows: const [
        [
          'Chicken Breast',
          'Durbar Marg',
          'OP-001',
          '12 KG',
          'Rs 510',
          '2082/05/20',
        ],
        ['Paneer', 'Durbar Marg', 'OP-002', '8 KG', 'Rs 600', '2082/05/12'],
        ['Basmati Rice', 'Central Kitchen', 'OP-003', '60 KG', 'Rs 158', '-'],
        ['Coke 500ml', 'Jhamsikhel', 'OP-004', '48 PCS', 'Rs 62', '2083/01/01'],
      ],
      metrics: commonMetrics,
      entryFields: const ['Product', 'Outlet', 'Batch No', 'Qty', 'Rate'],
      checklist: const [
        'Confirm outlet',
        'Enter FIFO cost',
        'Lock after posting',
      ],
    ),
    ModuleKind.purchaseOrder => _documentWorkspace(
      module,
      'Purchase Orders',
      'New PO',
      const ['PO No', 'Supplier', 'Due Date', 'Source', 'Amount', 'Status'],
      const [
        [
          'PO-1001',
          'Himalayan Fresh Mart',
          '2082/04/18',
          'Supplier',
          'Rs 42,500',
          'Approved',
        ],
        [
          'PO-1002',
          'Kathmandu Dairy',
          '2082/04/18',
          'Supplier',
          'Rs 18,200',
          'Draft',
        ],
        [
          'PO-1003',
          'Central Kitchen',
          '2082/04/19',
          'Hub Transfer',
          'Rs 31,750',
          'Sent',
        ],
      ],
      const ['Supplier / Hub', 'Expected Date', 'Product', 'Qty', 'Rate'],
      const [
        'Approve before receiving',
        'Can target supplier or central kitchen',
        'Creates pending purchase or transfer',
      ],
    ),
    ModuleKind.purchaseInvoice => _documentWorkspace(
      module,
      'Purchase Invoices',
      'New Purchase',
      const ['Bill No', 'Supplier', 'Date', 'Taxable', 'VAT', 'Status'],
      const [
        [
          'PI-501',
          'Himalayan Fresh Mart',
          '2082/04/12',
          'Rs 38,000',
          'Rs 4,940',
          'Posted',
        ],
        [
          'PI-502',
          'Everest Beverages',
          '2082/04/13',
          'Rs 24,500',
          'Rs 3,185',
          'GRN Pending',
        ],
        [
          'PI-503',
          'Kathmandu Dairy',
          '2082/04/14',
          'Rs 16,800',
          'Rs 2,184',
          'Draft',
        ],
      ],
      const ['Supplier', 'Bill No', 'Product', 'Qty', 'Tax'],
      const [
        'Posts stock batches',
        'Creates supplier payable',
        'Tracks VAT input',
      ],
    ),
    ModuleKind.purchaseReturns => _documentWorkspace(
      module,
      'Purchase Returns',
      'New Return',
      const ['Return No', 'Supplier', 'Date', 'Reason', 'Amount', 'Status'],
      const [
        [
          'PR-091',
          'Kathmandu Dairy',
          '2082/04/15',
          'Damaged',
          'Rs 3,200',
          'Posted',
        ],
        [
          'PR-092',
          'Everest Beverages',
          '2082/04/15',
          'Expired',
          'Rs 2,480',
          'Draft',
        ],
      ],
      const ['Supplier', 'Reference Bill', 'Product', 'Qty', 'Reason'],
      const ['Reduce stock', 'Create debit note', 'Attach reason'],
    ),
    ModuleKind.salesInvoice => _documentWorkspace(
      module,
      'Sales Invoices',
      'New Invoice',
      const ['Invoice', 'Channel', 'Date', 'Taxable', 'VAT', 'IRD'],
      const [
        [
          'SI-2082-1001',
          'Dine-In',
          '2082/04/15',
          'Rs 78,400',
          'Rs 10,192',
          'Synced',
        ],
        [
          'SI-2082-1002',
          'Foodmandu',
          '2082/04/15',
          'Rs 24,200',
          'Rs 3,146',
          'Queued',
        ],
        [
          'SI-2082-1003',
          'Takeaway',
          '2082/04/15',
          'Rs 16,850',
          'Rs 2,191',
          'Synced',
        ],
      ],
      const ['Customer / Table', 'Channel', 'PAN', 'Payment Mode'],
      const [
        'Gapless fiscal number',
        'VAT/service charge split',
        'IRD sync queued offline',
      ],
    ),
    ModuleKind.salesReturn => _documentWorkspace(
      module,
      'Sales Returns',
      'New Return',
      const ['Credit Note', 'Customer', 'Date', 'Invoice', 'Amount', 'Status'],
      const [
        [
          'SR-031',
          'Walk-in Customer',
          '2082/04/15',
          'SI-2082-0991',
          'Rs 1,240',
          'Posted',
        ],
        [
          'SR-032',
          'Foodmandu',
          '2082/04/16',
          'SI-2082-1002',
          'Rs 980',
          'Pending',
        ],
      ],
      const ['Invoice No', 'Item', 'Qty', 'Reason'],
      const [
        'Reverse sales ledger',
        'Reverse stock if needed',
        'Create credit note',
      ],
    ),
    ModuleKind.paymentMasters => _voucherWorkspace(
      module,
      'Payment Vouchers',
      'New Payment',
      const [
        'PV-701',
        'Himalayan Fresh Mart',
        'Nabil Bank',
        'Rs 28,000',
        'Approved',
      ],
      const ['PV-702', 'Rent Expense', 'Cash Counter', 'Rs 85,000', 'Posted'],
    ),
    ModuleKind.receiptMasters => _voucherWorkspace(
      module,
      'Receipt Vouchers',
      'New Receipt',
      const [
        'RV-801',
        'Foodmandu Receivable',
        'Nabil Bank',
        'Rs 31,420',
        'Matched',
      ],
      const [
        'RV-802',
        'Walk-in Customer',
        'Cash Counter',
        'Rs 12,800',
        'Posted',
      ],
    ),
    ModuleKind.journalMasters => _voucherWorkspace(
      module,
      'Journal Vouchers',
      'New Journal',
      const [
        'JV-301',
        'Kitchen Wastage',
        'Stock Adjustment',
        'Rs 2,450',
        'Posted',
      ],
      const ['JV-302', 'Round Off', 'Sales Adjustment', 'Rs 120', 'Draft'],
    ),
    ModuleKind.contraMasters => _voucherWorkspace(
      module,
      'Contra Vouchers',
      'New Contra',
      const ['CV-201', 'Cash Counter', 'Nabil Bank', 'Rs 50,000', 'Deposited'],
      const ['CV-202', 'Nabil Bank', 'Esewa Wallet', 'Rs 15,000', 'Posted'],
    ),
    ModuleKind.restaurantStockUsed => ModuleWorkspaceData(
      listTitle: 'Kitchen Stock Consumption',
      primaryAction: 'Post Stock Used',
      columns: const ['Doc No', 'Station', 'Product', 'Qty', 'Cost', 'Source'],
      rows: const [
        [
          'RSU-101',
          'Hot Kitchen',
          'Chicken Breast',
          '5.2 KG',
          'Rs 2,704',
          'Recipe',
        ],
        ['RSU-102', 'Bar', 'Mint Leaves', '0.8 KG', 'Rs 320', 'Manual'],
        ['RSU-103', 'Bakery', 'Flour', '6 KG', 'Rs 720', 'Recipe'],
      ],
      metrics: commonMetrics,
      entryFields: const ['Station', 'Raw Material', 'Qty', 'Reason'],
      checklist: const [
        'Post by station',
        'Use recipe source when possible',
        'Capture wastage reason',
      ],
    ),
    ModuleKind.pdcPayable ||
    ModuleKind.pdcReceivable ||
    ModuleKind.pdcClearance => ModuleWorkspaceData(
      listTitle: 'PDC Register',
      primaryAction: module.kind == ModuleKind.pdcClearance
          ? 'Clear Cheque'
          : 'Save PDC',
      columns: const [
        'Cheque No',
        'Party',
        'Bank',
        'Maturity',
        'Amount',
        'Status',
      ],
      rows: const [
        [
          '458921',
          'Himalayan Fresh Mart',
          'Nabil Bank',
          '2082/05/01',
          'Rs 42,000',
          'Pending',
        ],
        [
          '458922',
          'Foodmandu Receivable',
          'NIC Asia',
          '2082/05/05',
          'Rs 31,420',
          'Deposited',
        ],
        [
          '458923',
          'Kathmandu Dairy',
          'Nabil Bank',
          '2082/05/08',
          'Rs 18,000',
          'Cleared',
        ],
      ],
      metrics: commonMetrics,
      entryFields: const [
        'Cheque No',
        'Party',
        'Bank',
        'Maturity Date',
        'Amount',
      ],
      checklist: const [
        'Track maturity date',
        'Post only on clearance',
        'Mark bounced with reason',
      ],
    ),
    _ => _documentWorkspace(
      module,
      module.title,
      actionLabelFor(module),
      const ['Number', 'Party / Name', 'Date', 'Status', 'Amount'],
      [
        for (final row in genericRowsFor(module))
          [row.number, row.party, row.date, row.status, money(row.amount)],
      ],
      const ['Name / Party', 'Date', 'Amount', 'Remarks'],
      workflowFor(module),
    ),
  };
}

List<String> draftRowFor(
  ErpModule module,
  ModuleWorkspaceData data,
  int sequence,
) {
  final prefix = module.title
      .replaceAll(RegExp(r'[^A-Za-z]'), '')
      .toUpperCase()
      .substring(
        0,
        math.min(3, module.title.replaceAll(RegExp(r'[^A-Za-z]'), '').length),
      );
  final party = switch (module.group) {
    'Accounting' => 'Walk-in Customer',
    'Inventory' => 'Chicken Breast',
    'Purchase' => 'Himalayan Fresh Mart',
    'Sales' => 'Table T-07',
    'Transactions' => 'Cash Counter',
    _ => 'Durbar Marg Outlet',
  };
  final number = '$prefix-${(2000 + sequence).toString()}';

  return [
    for (var index = 0; index < data.columns.length; index++)
      _draftCellFor(
        data.columns[index],
        index,
        number,
        party,
        sequence,
        module,
      ),
  ];
}

String _draftCellFor(
  String column,
  int index,
  String number,
  String party,
  int sequence,
  ErpModule module,
) {
  final key = column.toLowerCase();
  if (index == 0) return number;
  if (key.contains('party') ||
      key.contains('supplier') ||
      key.contains('customer') ||
      key.contains('ledger') ||
      key.contains('name') ||
      key.contains('product') ||
      key.contains('item')) {
    return party;
  }
  if (key.contains('group')) return module.group;
  if (key.contains('date') || key.contains('due') || key.contains('maturity')) {
    return '2082/04/${(20 + sequence % 8).toString().padLeft(2, '0')}';
  }
  if (key.contains('amount') ||
      key.contains('balance') ||
      key.contains('opening') ||
      key.contains('cost') ||
      key.contains('rate') ||
      key.contains('taxable') ||
      key.contains('vat')) {
    return money(1500 + sequence * 475 + module.index * 40);
  }
  if (key.contains('qty') || key.contains('stock')) {
    return '${sequence + 1} PCS';
  }
  if (key.contains('status') || key.contains('ird')) return 'Draft';
  if (key.contains('bank')) return 'Nabil Bank';
  if (key.contains('channel') || key.contains('source')) return 'Dine-In';
  if (key.contains('unit') || key.contains('symbol')) return 'PCS';
  if (key.contains('pan') || key.contains('phone')) return '980000000$sequence';
  if (key.contains('position')) return '${sequence + 10}';
  if (key.contains('reason')) return 'Manual Entry';
  if (key.contains('expiry')) return '-';
  return 'Draft';
}

ModuleWorkspaceData _documentWorkspace(
  ErpModule module,
  String title,
  String action,
  List<String> columns,
  List<List<String>> rows,
  List<String> fields,
  List<String> checklist,
) {
  final total = rows.fold<double>(0, (sum, row) {
    final values = row
        .map((cell) => double.tryParse(cell.replaceAll(RegExp(r'[^0-9.]'), '')))
        .whereType<double>()
        .toList();
    return sum + (values.isEmpty ? 0 : values.reduce(math.max));
  });
  return ModuleWorkspaceData(
    listTitle: title,
    primaryAction: action,
    columns: columns,
    rows: rows,
    metrics: [
      MetricValue('Documents', '${rows.length}', Icons.receipt_long_rounded),
      const MetricValue('Pending', '2', Icons.pending_actions_rounded),
      MetricValue('Value', money(total), Icons.payments_rounded),
    ],
    entryFields: fields,
    checklist: checklist,
  );
}

ModuleWorkspaceData _voucherWorkspace(
  ErpModule module,
  String title,
  String action,
  List<String> rowA,
  List<String> rowB,
) {
  return ModuleWorkspaceData(
    listTitle: title,
    primaryAction: action,
    columns: const [
      'Voucher',
      'Debit / From',
      'Credit / To',
      'Amount',
      'Status',
    ],
    rows: [rowA, rowB],
    metrics: [
      const MetricValue('Vouchers', '2', Icons.edit_note_rounded),
      const MetricValue('Approval Queue', '1', Icons.verified_user_rounded),
      MetricValue(
        'Voucher Total',
        money(128420 + module.index * 510),
        Icons.payments_rounded,
      ),
    ],
    entryFields: const ['Debit Ledger', 'Credit Ledger', 'Amount', 'Narration'],
    checklist: const [
      'Debit and credit must balance',
      'Require approval above limit',
      'Post to ledger instantly',
    ],
  );
}

List<DocumentRow> genericRowsFor(ErpModule module) {
  final prefix = module.title
      .replaceAll(RegExp(r'[^A-Za-z]'), '')
      .toUpperCase();
  final parties = switch (module.group) {
    'Accounting' => [
      'Cash in Hand',
      'Foodmandu Receivable',
      'Nabil Bank',
      'VAT Payable',
    ],
    'Inventory' => ['Chicken Breast', 'Paneer', 'Basmati Rice', 'Coke 500ml'],
    'Purchase' => [
      'Himalayan Fresh Mart',
      'Kathmandu Dairy',
      'Everest Beverages',
      'Central Kitchen',
    ],
    'Sales' => ['Walk-in Customer', 'Table T-07', 'Foodmandu', 'Pathao'],
    'Transactions' => [
      'Nabil Bank',
      'Cash Counter',
      'Himalayan Fresh Mart',
      'Staff Meal Expense',
    ],
    _ => ['Restaurant Ops', 'Counter', 'Manager', 'System'],
  };
  return [
    for (var i = 0; i < 6; i++)
      DocumentRow(
        number:
            '${prefix.substring(0, math.min(3, prefix.length))}-${(1001 + i)}',
        party: parties[i % parties.length],
        date: '2082/04/${(12 + i).toString().padLeft(2, '0')}',
        status: ['Draft', 'Posted', 'Synced', 'Approved'][i % 4],
        amount: 4200 + i * 3180 + module.index * 125,
      ),
  ];
}

extension on ErpModule {
  int get index => modules.indexWhere((module) => module.kind == kind);
}
