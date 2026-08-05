part of '../../main.dart';

class _MetricBar extends StatelessWidget {
  const _MetricBar({required this.metrics, required this.color});

  final List<MetricValue> metrics;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        for (final metric in metrics)
          Container(
            width: 210,
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 9),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: AppColors.border),
            ),
            child: Row(
              children: [
                Icon(metric.icon, color: color, size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        metric.value,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      Text(
                        metric.label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: AppColors.muted,
                          fontSize: 11.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

class _ModuleActionPanel extends StatelessWidget {
  const _ModuleActionPanel({
    required this.module,
    required this.data,
    required this.onSave,
  });

  final ErpModule module;
  final ModuleWorkspaceData data;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionHeader(
            title: 'Quick Entry',
            action: data.primaryAction,
            icon: Icons.edit_square,
          ),
          const SizedBox(height: 10),
          for (final field in data.entryFields)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: TextField(
                decoration: InputDecoration(
                  labelText: field,
                  isDense: true,
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 10,
                  ),
                ),
              ),
            ),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: onSave,
              icon: const Icon(Icons.save_rounded),
              label: Text(data.primaryAction),
            ),
          ),
          const Divider(height: 22),
          _SectionHeader(
            title: 'Checklist',
            action: 'Required',
            icon: Icons.fact_check_rounded,
          ),
          const SizedBox(height: 8),
          for (final item in data.checklist)
            Padding(
              padding: const EdgeInsets.only(bottom: 7),
              child: Row(
                children: [
                  Icon(
                    Icons.check_circle_rounded,
                    color: module.color,
                    size: 17,
                  ),
                  const SizedBox(width: 7),
                  Expanded(
                    child: Text(
                      item,
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          const Divider(height: 22),
          _WorkflowStrip(module: module),
        ],
      ),
    );
  }
}

class _ReportSuite extends StatelessWidget {
  const _ReportSuite({required this.title, required this.cards});

  final String title;
  final List<_ReportSpec> cards;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: title,
          action: 'Filters + export-ready summary',
          icon: Icons.insights_rounded,
        ),
        const SizedBox(height: 12),
        _Panel(
          child: Wrap(
            spacing: 10,
            runSpacing: 10,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 230,
                child: TextField(
                  decoration: const InputDecoration(
                    labelText: 'Search',
                    prefixIcon: Icon(Icons.search_rounded),
                  ),
                ),
              ),
              const _SmallFilter(label: 'Today'),
              const _SmallFilter(label: 'This Month'),
              const _SmallFilter(label: 'BS 2082/83'),
              FilledButton.icon(
                onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text('$title export prepared.')),
                ),
                icon: const Icon(Icons.download_rounded),
                label: const Text('Export'),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),
        _ResponsiveGrid(
          minTileWidth: 280,
          tileHeight: 136,
          children: [for (final card in cards) _ReportCard(spec: card)],
        ),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            children: [
              const _Toolbar(
                title: 'Report Preview',
                button: 'Create Excel',
                icon: Icons.table_chart_rounded,
              ),
              _ErpTable(
                columns: const [
                  'Particulars',
                  'Opening',
                  'Debit',
                  'Credit',
                  'Closing',
                ],
                rows: const [
                  [
                    'Cash in Hand',
                    'Rs 24,500',
                    'Rs 86,300',
                    'Rs 72,000',
                    'Rs 38,800 Dr',
                  ],
                  [
                    'Foodmandu Receivable',
                    'Rs 12,000',
                    'Rs 34,200',
                    'Rs 29,100',
                    'Rs 17,100 Dr',
                  ],
                  [
                    'Kitchen Consumption',
                    'Rs 0',
                    'Rs 38,400',
                    'Rs 0',
                    'Rs 38,400 Cr',
                  ],
                  [
                    'VAT Payable',
                    'Rs 8,100',
                    'Rs 0',
                    'Rs 18,720',
                    'Rs 26,820 Cr',
                  ],
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _ResponsiveGrid extends StatelessWidget {
  const _ResponsiveGrid({
    required this.children,
    this.minTileWidth = 240,
    this.tileHeight = 148,
  });

  final List<Widget> children;
  final double minTileWidth;
  final double tileHeight;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = math.max(
          1,
          (constraints.maxWidth / minTileWidth).floor(),
        );
        return GridView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: children.length,
          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: columns,
            crossAxisSpacing: 12,
            mainAxisSpacing: 12,
            mainAxisExtent: tileHeight,
          ),
          itemBuilder: (context, index) => children[index],
        );
      },
    );
  }
}

class _Panel extends StatelessWidget {
  const _Panel({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppColors.border),
      ),
      child: child,
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({
    required this.title,
    required this.action,
    required this.icon,
  });

  final String title;
  final String action;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 20, color: AppColors.primary),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: AppColors.text,
              fontSize: 16,
              fontWeight: FontWeight.w900,
            ),
          ),
        ),
        Text(
          action,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(
            color: AppColors.muted,
            fontSize: 12,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    );
  }
}

class _KpiCard extends StatelessWidget {
  const _KpiCard({
    required this.title,
    required this.value,
    required this.helper,
    required this.icon,
    required this.color,
  });

  final String title;
  final String value;
  final String helper;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 32,
                height: 32,
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: color, size: 18),
              ),
              const Spacer(),
              Icon(Icons.more_horiz_rounded, color: Colors.grey.shade400),
            ],
          ),
          const Spacer(),
          Text(
            value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              fontSize: 21,
              fontWeight: FontWeight.w900,
              color: AppColors.text,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            title,
            style: const TextStyle(
              fontWeight: FontWeight.w800,
              color: AppColors.text,
            ),
          ),
          Text(
            helper,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
        ],
      ),
    );
  }
}

class _ModuleCard extends StatelessWidget {
  const _ModuleCard({required this.module, required this.onTap});

  final ErpModule module;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(8),
        child: Padding(
          padding: const EdgeInsets.all(2),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 34,
                    height: 34,
                    decoration: BoxDecoration(
                      color: module.color.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Icon(module.icon, color: module.color),
                  ),
                  const Spacer(),
                  const Icon(
                    Icons.arrow_forward_rounded,
                    color: AppColors.muted,
                    size: 18,
                  ),
                ],
              ),
              const Spacer(),
              Text(
                module.title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontSize: 14.5,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 5),
              Text(
                module.description,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: AppColors.muted,
                  fontSize: 12,
                  height: 1.3,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LiveServiceBoard extends StatelessWidget {
  const _LiveServiceBoard({required this.tickets});

  final List<KdsTicket> tickets;

  @override
  Widget build(BuildContext context) {
    final active = tickets
        .where((ticket) => ticket.status != TicketStatus.bumped)
        .take(5)
        .toList();
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const _SectionHeader(
            title: 'Live Service Board',
            action: 'KOT / BOT queue',
            icon: Icons.monitor_heart_rounded,
          ),
          const SizedBox(height: 12),
          for (final ticket in active)
            _SuggestionTile(
              icon: ticket.station == 'Bar'
                  ? Icons.local_bar_rounded
                  : Icons.soup_kitchen_rounded,
              title: '${ticket.id} - ${ticket.table}',
              subtitle:
                  '${ticket.station} - ${ticket.items.map((e) => '${e.qty}x ${e.product.name}').join(', ')}',
              color: statusColor(ticket.status),
              trailing: '${ticket.minutes}m',
            ),
        ],
      ),
    );
  }
}

class _PriorityWork extends StatelessWidget {
  const _PriorityWork({required this.onOpen});

  final ValueChanged<ModuleKind> onOpen;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const _SectionHeader(
            title: 'Priority Work',
            action: 'Manager queue',
            icon: Icons.priority_high_rounded,
          ),
          const SizedBox(height: 12),
          _ClickableSuggestion(
            icon: Icons.warning_rounded,
            title: 'Low stock purchase draft',
            subtitle: 'Chicken, Paneer, and Coke below reorder level',
            color: AppColors.red,
            onTap: () => onOpen(ModuleKind.restaurantInventory),
          ),
          _ClickableSuggestion(
            icon: Icons.hub_rounded,
            title: 'Foodmandu payout variance',
            subtitle: 'Rs 1,260 mismatch in latest settlement',
            color: AppColors.amber,
            onTap: () => onOpen(ModuleKind.channels),
          ),
          _ClickableSuggestion(
            icon: Icons.policy_rounded,
            title: 'Discount approval audit',
            subtitle: '3 manual discounts over staff limit',
            color: AppColors.violet,
            onTap: () => onOpen(ModuleKind.restaurantReports),
          ),
        ],
      ),
    );
  }
}

class _MenuTile extends StatelessWidget {
  const _MenuTile({required this.product, required this.onTap});

  final MenuProduct product;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: InkWell(
        onTap: product.available ? onTap : null,
        borderRadius: BorderRadius.circular(8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 42,
                  height: 42,
                  decoration: BoxDecoration(
                    color: product.color.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Icon(Icons.fastfood_rounded, color: product.color),
                ),
                const Spacer(),
                _TinyTag(
                  label: product.station,
                  color: product.station == 'Bar'
                      ? AppColors.amber
                      : AppColors.teal,
                ),
              ],
            ),
            const Spacer(),
            Text(
              product.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 15),
            ),
            Text(
              product.category,
              style: const TextStyle(color: AppColors.muted, fontSize: 12),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                Text(
                  money(product.price),
                  style: const TextStyle(
                    fontWeight: FontWeight.w900,
                    fontSize: 16,
                  ),
                ),
                const Spacer(),
                Icon(
                  product.available
                      ? Icons.add_circle_rounded
                      : Icons.block_rounded,
                  color: product.available ? product.color : AppColors.muted,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CartLineTile extends StatelessWidget {
  const _CartLineTile({required this.line, required this.onQty});

  final CartLine line;
  final void Function(MenuProduct product, int delta) onQty;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  line.product.name,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  '${line.product.station} - ${money(line.product.price)}',
                  style: const TextStyle(color: AppColors.muted, fontSize: 12),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Decrease',
            onPressed: () => onQty(line.product, -1),
            icon: const Icon(Icons.remove_circle_outline_rounded),
          ),
          Text(
            '${line.qty}',
            style: const TextStyle(fontWeight: FontWeight.w900),
          ),
          IconButton(
            tooltip: 'Increase',
            onPressed: () => onQty(line.product, 1),
            icon: const Icon(Icons.add_circle_outline_rounded),
          ),
          SizedBox(
            width: 82,
            child: Text(
              money(line.total),
              textAlign: TextAlign.right,
              style: const TextStyle(fontWeight: FontWeight.w900),
            ),
          ),
        ],
      ),
    );
  }
}

class _TicketCard extends StatelessWidget {
  const _TicketCard({required this.ticket, required this.onAdvance});

  final KdsTicket ticket;
  final VoidCallback onAdvance;

  @override
  Widget build(BuildContext context) {
    final color = statusColor(ticket.status);
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              _TinyTag(
                label: ticket.station == 'Bar' ? 'BOT' : 'KOT',
                color: color,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  ticket.id,
                  style: const TextStyle(
                    fontWeight: FontWeight.w900,
                    fontSize: 16,
                  ),
                ),
              ),
              Text(
                '${ticket.minutes}m',
                style: TextStyle(color: color, fontWeight: FontWeight.w900),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            '${ticket.table} - ${ticket.channel}',
            style: const TextStyle(fontWeight: FontWeight.w800),
          ),
          Text(
            ticketStatusLabel(ticket.status),
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
          const Spacer(),
          for (final line in ticket.items.take(3))
            Text(
              '${line.qty}x ${line.product.name}',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          const SizedBox(height: 10),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: ticket.status == TicketStatus.bumped
                  ? null
                  : onAdvance,
              icon: const Icon(Icons.arrow_forward_rounded),
              label: Text(nextStatusAction(ticket.status)),
            ),
          ),
        ],
      ),
    );
  }
}

class _RecipeCard extends StatelessWidget {
  const _RecipeCard({required this.product});

  final MenuProduct product;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: product.color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(Icons.restaurant_rounded, color: product.color),
              ),
              const Spacer(),
              _TinyTag(
                label: '${(product.margin * 100).round()}% margin',
                color: product.margin > 0.6 ? AppColors.green : AppColors.amber,
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            product.name,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 15.5),
          ),
          Text(
            '${product.category} - ${product.station}',
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
          const Spacer(),
          Text(
            'Recipe: ${product.recipe.join(', ')}',
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 12.5),
          ),
          const SizedBox(height: 6),
          Text(
            'Price ${money(product.price)} | Cost ${money(product.cost)}',
            style: const TextStyle(fontWeight: FontWeight.w800),
          ),
        ],
      ),
    );
  }
}

class _ChannelCard extends StatelessWidget {
  const _ChannelCard({required this.channel});

  final ChannelStatus channel;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                channel.online
                    ? Icons.toggle_on_rounded
                    : Icons.toggle_off_rounded,
                color: channel.online ? AppColors.green : AppColors.muted,
                size: 34,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  channel.name,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              _TinyTag(label: channel.provider, color: AppColors.primary),
            ],
          ),
          const Spacer(),
          Text(
            '${channel.orders} orders',
            style: const TextStyle(
              color: AppColors.muted,
              fontWeight: FontWeight.w700,
            ),
          ),
          Text(
            money(channel.sales),
            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w900),
          ),
          Text(
            'Commission ${money(channel.commission)} - ${channel.sync}',
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
        ],
      ),
    );
  }
}

class _SetupCard extends StatelessWidget {
  const _SetupCard({
    required this.icon,
    required this.title,
    required this.value,
    required this.lines,
    required this.color,
  });

  final IconData icon;
  final String title;
  final String value;
  final List<String> lines;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: color),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              Text(
                value,
                style: TextStyle(
                  color: color,
                  fontWeight: FontWeight.w900,
                  fontSize: 18,
                ),
              ),
            ],
          ),
          const Spacer(),
          for (final line in lines.take(3))
            Text(
              line,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.muted, fontSize: 12.5),
            ),
        ],
      ),
    );
  }
}

class _ReportSpec {
  const _ReportSpec(this.title, this.subtitle, this.icon, this.color);

  final String title;
  final String subtitle;
  final IconData icon;
  final Color color;
}

class _ReportCard extends StatelessWidget {
  const _ReportCard({required this.spec});

  final _ReportSpec spec;

  @override
  Widget build(BuildContext context) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(spec.icon, color: spec.color),
              const Spacer(),
              const Icon(
                Icons.open_in_new_rounded,
                color: AppColors.muted,
                size: 18,
              ),
            ],
          ),
          const Spacer(),
          Text(
            spec.title,
            style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 16),
          ),
          const SizedBox(height: 6),
          Text(
            spec.subtitle,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: AppColors.muted,
              fontSize: 12.5,
              height: 1.3,
            ),
          ),
        ],
      ),
    );
  }
}

class _Toolbar extends StatelessWidget {
  const _Toolbar({
    required this.title,
    required this.button,
    required this.icon,
    this.onPressed,
  });

  final String title;
  final String button;
  final IconData icon;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Expanded(
            child: Text(
              title,
              style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 16),
            ),
          ),
          OutlinedButton.icon(
            onPressed:
                onPressed ??
                () => ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text('$button is ready for $title.')),
                ),
            icon: Icon(icon, size: 18),
            label: Text(button),
          ),
        ],
      ),
    );
  }
}

class _ErpTable extends StatelessWidget {
  const _ErpTable({required this.columns, required this.rows});

  final List<String> columns;
  final List<List<String>> rows;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        headingRowHeight: 38,
        dataRowMinHeight: 38,
        dataRowMaxHeight: 42,
        columnSpacing: 22,
        horizontalMargin: 12,
        headingRowColor: WidgetStateProperty.all(AppColors.background),
        border: TableBorder.all(
          color: AppColors.border,
          borderRadius: BorderRadius.circular(8),
        ),
        columns: [
          for (final column in columns)
            DataColumn(
              label: Text(
                column,
                style: const TextStyle(fontWeight: FontWeight.w900),
              ),
            ),
        ],
        rows: [
          for (final row in rows)
            DataRow(
              cells: [
                for (final cell in row)
                  DataCell(Text(cell, overflow: TextOverflow.ellipsis)),
              ],
            ),
        ],
      ),
    );
  }
}

class _WorkflowStrip extends StatelessWidget {
  const _WorkflowStrip({required this.module});

  final ErpModule module;

  @override
  Widget build(BuildContext context) {
    final steps = workflowFor(module);
    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: [
        for (var index = 0; index < steps.length; index++)
          Container(
            width: 190,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: module.color.withValues(alpha: 0.07),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: module.color.withValues(alpha: 0.18)),
            ),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 14,
                  backgroundColor: module.color,
                  child: Text(
                    '${index + 1}',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    steps[index],
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

class _SuggestionTile extends StatelessWidget {
  const _SuggestionTile({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.color,
    this.trailing,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color color;
  final String? trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Icon(icon, color: color),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
                Text(
                  subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: AppColors.muted, fontSize: 12),
                ),
              ],
            ),
          ),
          if (trailing != null)
            Text(
              trailing!,
              style: TextStyle(color: color, fontWeight: FontWeight.w900),
            ),
        ],
      ),
    );
  }
}

class _ClickableSuggestion extends StatelessWidget {
  const _ClickableSuggestion({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.color,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: _SuggestionTile(
        icon: icon,
        title: title,
        subtitle: subtitle,
        color: color,
      ),
    );
  }
}

class _TinyTag extends StatelessWidget {
  const _TinyTag({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w900,
          fontSize: 11,
        ),
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.18),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Colors.white,
          fontSize: 11,
          fontWeight: FontWeight.w900,
        ),
      ),
    );
  }
}

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.icon,
    required this.color,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return FilledButton.icon(
      style: FilledButton.styleFrom(backgroundColor: color),
      onPressed: onTap,
      icon: Icon(icon),
      label: Text(label),
    );
  }
}

class _AmountRow extends StatelessWidget {
  const _AmountRow({
    required this.label,
    required this.value,
    this.strong = false,
  });

  final String label;
  final String value;
  final bool strong;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: TextStyle(
                color: strong ? AppColors.text : AppColors.muted,
                fontWeight: strong ? FontWeight.w900 : FontWeight.w700,
              ),
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontSize: strong ? 18 : 14,
              fontWeight: FontWeight.w900,
            ),
          ),
        ],
      ),
    );
  }
}

class _DropdownField extends StatelessWidget {
  const _DropdownField({
    required this.label,
    required this.value,
    required this.values,
    required this.onChanged,
  });

  final String label;
  final String value;
  final List<String> values;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: value,
      decoration: InputDecoration(labelText: label),
      items: [
        for (final item in values)
          DropdownMenuItem(value: item, child: Text(item)),
      ],
      onChanged: (value) {
        if (value != null) onChanged(value);
      },
    );
  }
}

class _SmallFilter extends StatelessWidget {
  const _SmallFilter({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return FilterChip(
      label: Text(label),
      selected: false,
      onSelected: (_) => ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('$label filter applied.'))),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({
    required this.icon,
    required this.title,
    required this.message,
  });

  final IconData icon;
  final String title;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        children: [
          Icon(icon, size: 38, color: AppColors.muted),
          const SizedBox(height: 8),
          Text(title, style: const TextStyle(fontWeight: FontWeight.w900)),
          Text(message, style: const TextStyle(color: AppColors.muted)),
        ],
      ),
    );
  }
}

class _DashboardChartPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final grid = Paint()
      ..color = Colors.white.withValues(alpha: 0.1)
      ..strokeWidth = 1;
    for (var i = 1; i < 5; i++) {
      final y = size.height * i / 5;
      canvas.drawLine(Offset(0, y), Offset(size.width, y), grid);
    }

    final bars = [0.35, 0.58, 0.42, 0.74, 0.66, 0.9, 0.7];
    final barPaint = Paint()..color = AppColors.teal;
    final width = size.width / 18;
    for (var i = 0; i < bars.length; i++) {
      final x = 24 + i * width * 2.25;
      final height = size.height * bars[i] * 0.72;
      canvas.drawRRect(
        RRect.fromRectAndRadius(
          Rect.fromLTWH(x, size.height - height - 34, width, height),
          const Radius.circular(5),
        ),
        barPaint..color = i.isEven ? AppColors.teal : AppColors.amber,
      );
    }

    final path = Path()
      ..moveTo(12, size.height * 0.62)
      ..cubicTo(
        size.width * 0.24,
        size.height * 0.26,
        size.width * 0.36,
        size.height * 0.74,
        size.width * 0.54,
        size.height * 0.42,
      )
      ..cubicTo(
        size.width * 0.72,
        size.height * 0.1,
        size.width * 0.82,
        size.height * 0.54,
        size.width - 12,
        size.height * 0.24,
      );
    canvas.drawPath(
      path,
      Paint()
        ..color = Colors.white
        ..strokeWidth = 3
        ..style = PaintingStyle.stroke
        ..strokeCap = StrokeCap.round,
    );
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
