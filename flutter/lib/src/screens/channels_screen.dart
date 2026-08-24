part of '../../main.dart';

class ChannelsScreen extends StatelessWidget {
  const ChannelsScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  Widget build(BuildContext context) {
    final channels = controller.channels;
    final orders = controller.aggregatorOrders;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Aggregator Channels',
          action:
              '${channels.where((item) => item.online).length} online · ${orders.length} received orders',
          icon: Icons.hub_rounded,
        ),
        const SizedBox(height: 12),
        if (controller.hasPermission('Pages.Restaurant.Channels.Manage')) ...[
          Align(
            alignment: Alignment.centerRight,
            child: FilledButton.icon(
              onPressed: () => _channelDialog(context),
              icon: const Icon(Icons.add_rounded),
              label: const Text('Add channel'),
            ),
          ),
          const SizedBox(height: 10),
        ],
        if (channels.isEmpty)
          const _EmptyState(
            icon: Icons.hub_outlined,
            title: 'No restaurant channels configured',
            message: 'Add a delivery or online ordering channel to begin.',
          )
        else
          _ResponsiveGrid(
            minTileWidth: 260,
            tileHeight: 166,
            children: [
              for (final channel in channels)
                _Panel(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          CircleAvatar(
                            backgroundColor:
                                (channel.online
                                        ? AppColors.green
                                        : AppColors.muted)
                                    .withValues(alpha: 0.12),
                            child: Icon(
                              Icons.delivery_dining_rounded,
                              color: channel.online
                                  ? AppColors.green
                                  : AppColors.muted,
                            ),
                          ),
                          const Spacer(),
                          _TinyTag(
                            label: channel.online ? 'Online' : 'Offline',
                            color: channel.online
                                ? AppColors.green
                                : AppColors.red,
                          ),
                        ],
                      ),
                      const SizedBox(height: 9),
                      Text(
                        channel.name,
                        style: const TextStyle(
                          fontWeight: FontWeight.w900,
                          fontSize: 16,
                        ),
                      ),
                      Text(
                        '${channel.provider} · ${channel.commission.toStringAsFixed(2)}% commission · ${channel.priceMarkupPercent.toStringAsFixed(2)}% markup',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: AppColors.muted,
                          fontSize: 12,
                        ),
                      ),
                      const Spacer(),
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              channel.sync,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: const TextStyle(fontSize: 11.5),
                            ),
                          ),
                          if (controller.hasPermission(
                            'Pages.Restaurant.Channels.Manage',
                          )) ...[
                            IconButton(
                              tooltip: 'Edit channel',
                              onPressed: () => _channelDialog(context, channel),
                              icon: const Icon(Icons.edit_outlined),
                            ),
                            IconButton(
                              tooltip: 'Publish menu',
                              onPressed: () => _publish(context, channel),
                              icon: const Icon(Icons.cloud_upload_outlined),
                            ),
                          ],
                        ],
                      ),
                    ],
                  ),
                ),
            ],
          ),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const _Toolbar(
                title: 'Aggregator orders',
                button: 'Live queue',
                icon: Icons.receipt_long_rounded,
              ),
              if (orders.isEmpty)
                const Padding(
                  padding: EdgeInsets.all(24),
                  child: Text(
                    'No aggregator orders were returned.',
                    style: TextStyle(color: AppColors.muted),
                  ),
                )
              else
                for (final order in orders)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      child: Text(order.channel.characters.firstOrNull ?? '?'),
                    ),
                    title: Text(
                      '${order.channel} · ${order.externalOrderId}',
                      style: const TextStyle(fontWeight: FontWeight.w900),
                    ),
                    subtitle: Text(
                      '${order.customer} · ${order.customerPhone}${order.deliveryAddress.isEmpty ? '' : ' · ${order.deliveryAddress}'}',
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    trailing: Wrap(
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        SizedBox(
                          width: 105,
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text(
                                money(order.amount),
                                style: const TextStyle(
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                              Text(
                                _aggregatorStatus(order.status),
                                style: const TextStyle(
                                  color: AppColors.muted,
                                  fontSize: 11,
                                ),
                              ),
                            ],
                          ),
                        ),
                        if (controller.hasPermission(
                          'Pages.Restaurant.Aggregators',
                        ))
                          PopupMenuButton<String>(
                            tooltip: 'Order action',
                            onSelected: (value) =>
                                _orderAction(context, order, value),
                            itemBuilder: (context) => [
                              if (order.status == 0)
                                const PopupMenuItem(
                                  value: 'accept',
                                  child: Text('Accept and send to kitchen'),
                                ),
                              if (order.status == 1)
                                const PopupMenuItem(
                                  value: 'complete',
                                  child: Text('Mark completed'),
                                ),
                              if (order.status == 0) ...[
                                const PopupMenuItem(
                                  value: 'reject',
                                  child: Text('Reject order'),
                                ),
                                const PopupMenuItem(
                                  value: 'cancel',
                                  child: Text('Cancel order'),
                                ),
                              ],
                              if (order.status == 1)
                                const PopupMenuItem(
                                  value: 'cancel',
                                  child: Text('Cancel order'),
                                ),
                            ],
                          ),
                      ],
                    ),
                  ),
            ],
          ),
        ),
        if (controller.hasPermission('Pages.Restaurant.Payouts')) ...[
          const SizedBox(height: 16),
          _Panel(
            child: Column(
              children: [
                const _Toolbar(
                  title: 'Payout reconciliation',
                  button: 'Backend matched',
                  icon: Icons.account_balance_wallet_outlined,
                ),
                _ErpTable(
                  columns: const [
                    'Channel',
                    'Payout',
                    'Gross',
                    'Commission',
                    'Deductions',
                    'Net paid',
                    'Issues',
                  ],
                  rows: [
                    for (final payout in controller.payouts)
                      [
                        payout.channel,
                        payout.externalPayoutId,
                        money(payout.grossAmount),
                        money(payout.commissionAmount),
                        money(payout.deductionsAmount),
                        money(payout.netPaidAmount),
                        '${payout.issueCount}',
                      ],
                  ],
                ),
              ],
            ),
          ),
        ],
      ],
    );
  }

  Future<void> _publish(BuildContext context, ChannelStatus channel) async {
    try {
      await controller.publishChannelMenu(channel);
      if (context.mounted) _message(context, '${channel.name} publish queued.');
    } on ApiException catch (error) {
      if (context.mounted) _message(context, error.message);
    }
  }

  Future<void> _orderAction(
    BuildContext context,
    RestaurantAggregatorOrderModel order,
    String action,
  ) async {
    try {
      if (action == 'accept') {
        await _acceptDialog(context, order);
        return;
      }
      final status = switch (action) {
        'complete' => 4,
        'reject' => 2,
        _ => 3,
      };
      var message = '';
      if (status == 2 || status == 3) {
        message =
            await _textDialog(
              context,
              status == 2 ? 'Reject order' : 'Cancel order',
              'Reason',
            ) ??
            '';
        if (message.isEmpty) return;
      }
      await controller.updateAggregatorStatus(order, status, message);
    } on ApiException catch (error) {
      if (context.mounted) _message(context, error.message);
    }
  }

  Future<void> _acceptDialog(
    BuildContext context,
    RestaurantAggregatorOrderModel order,
  ) async {
    final available = controller.products
        .where((item) => item.available)
        .toList();
    if (available.isEmpty) {
      _message(context, 'No available menu item can be mapped.');
      return;
    }
    var product = available.first;
    final qty = TextEditingController(text: '1');
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text('Accept ${order.externalOrderId}'),
          content: SizedBox(
            width: 480,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text(
                  'Map this incoming order to the sellable ERP menu item before creating its KOT/BOT.',
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<MenuProduct>(
                  initialValue: product,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Menu item'),
                  items: [
                    for (final item in available)
                      DropdownMenuItem(value: item, child: Text(item.name)),
                  ],
                  onChanged: (value) =>
                      setDialogState(() => product = value ?? product),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: qty,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Quantity'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Back'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Accept'),
            ),
          ],
        ),
      ),
    );
    final amount = double.tryParse(qty.text) ?? 0;
    qty.dispose();
    if (accepted == true) {
      if (amount <= 0) {
        throw const ApiException('Quantity must be greater than zero.');
      }
      await controller.acceptAggregatorOrder(order, product.id, amount);
    }
  }

  Future<void> _channelDialog(
    BuildContext context, [
    ChannelStatus? channel,
  ]) async {
    final name = TextEditingController(text: channel?.name ?? '');
    final commission = TextEditingController(
      text: '${channel?.commission ?? 0}',
    );
    final markup = TextEditingController(
      text: '${channel?.priceMarkupPercent ?? 0}',
    );
    final sort = TextEditingController(text: '${channel?.sortOrder ?? 0}');
    var type = channel?.channelType ?? 0;
    var provider = channel?.providerCode ?? 0;
    var online = channel?.online ?? true;
    var active = channel?.isActive ?? true;
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(channel == null ? 'Add channel' : 'Edit channel'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextField(
                    controller: name,
                    decoration: const InputDecoration(labelText: 'Name'),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: DropdownButtonFormField<int>(
                          initialValue: type,
                          decoration: const InputDecoration(
                            labelText: 'Channel type',
                          ),
                          items: const [
                            DropdownMenuItem(value: 0, child: Text('Dine in')),
                            DropdownMenuItem(value: 1, child: Text('Takeaway')),
                            DropdownMenuItem(
                              value: 2,
                              child: Text('Own online'),
                            ),
                            DropdownMenuItem(
                              value: 3,
                              child: Text('Aggregator'),
                            ),
                          ],
                          onChanged: (value) =>
                              setDialogState(() => type = value ?? 0),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: DropdownButtonFormField<int>(
                          initialValue: provider,
                          decoration: const InputDecoration(
                            labelText: 'Provider',
                          ),
                          items: const [
                            DropdownMenuItem(value: 0, child: Text('Internal')),
                            DropdownMenuItem(
                              value: 1,
                              child: Text('Own online'),
                            ),
                            DropdownMenuItem(
                              value: 2,
                              child: Text('Foodmandu'),
                            ),
                            DropdownMenuItem(value: 3, child: Text('Pathao')),
                            DropdownMenuItem(
                              value: 4,
                              child: Text('Bhojdeals'),
                            ),
                          ],
                          onChanged: (value) =>
                              setDialogState(() => provider = value ?? 0),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: commission,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Commission %',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: markup,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Price markup %',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: sort,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Sort order',
                          ),
                        ),
                      ),
                    ],
                  ),
                  SwitchListTile(
                    value: online,
                    onChanged: (value) => setDialogState(() => online = value),
                    title: const Text('Online'),
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
    if (accepted == true && name.text.trim().isNotEmpty) {
      try {
        await controller.saveChannel(
          id: channel?.id,
          name: name.text.trim(),
          channelType: type,
          provider: provider,
          commissionPercent: double.tryParse(commission.text) ?? 0,
          priceMarkupPercent: double.tryParse(markup.text) ?? 0,
          sortOrder: int.tryParse(sort.text) ?? 0,
          isOnline: online,
          isActive: active,
        );
      } on ApiException catch (error) {
        if (context.mounted) _message(context, error.message);
      }
    }
    name.dispose();
    commission.dispose();
    markup.dispose();
    sort.dispose();
  }

  Future<String?> _textDialog(
    BuildContext context,
    String title,
    String label,
  ) async {
    final text = TextEditingController();
    final value = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: TextField(
          controller: text,
          autofocus: true,
          decoration: InputDecoration(labelText: label),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Back'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, text.text.trim()),
            child: const Text('Confirm'),
          ),
        ],
      ),
    );
    text.dispose();
    return value;
  }

  void _message(BuildContext context, String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

String _aggregatorStatus(int status) => switch (status) {
  0 => 'Received',
  1 => 'Accepted',
  2 => 'Rejected',
  3 => 'Cancelled',
  4 => 'Completed',
  _ => 'Status $status',
};
