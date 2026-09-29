part of '../../main.dart';

class RestaurantGuestOrdersScreen extends StatelessWidget {
  const RestaurantGuestOrdersScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  Widget build(BuildContext context) {
    final orders = controller.guestOrders;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Guest QR Orders',
          action: '${orders.length} waiting for staff review',
          icon: Icons.qr_code_2_rounded,
        ),
        const SizedBox(height: 12),
        if (orders.isEmpty)
          const _EmptyState(
            icon: Icons.task_alt_rounded,
            title: 'No guest orders waiting',
            message: 'New table QR orders appear here for approval.',
          )
        else
          for (final order in orders)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _Panel(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            '${order.orderNo} · ${order.tableName}',
                            style: const TextStyle(
                              fontWeight: FontWeight.w900,
                              fontSize: 16,
                            ),
                          ),
                        ),
                        Text(
                          money(order.total),
                          style: const TextStyle(fontWeight: FontWeight.w900),
                        ),
                      ],
                    ),
                    const SizedBox(height: 3),
                    Text(
                      'Submitted ${_guestOpsDate(order.createdAt)}',
                      style: const TextStyle(color: AppColors.muted),
                    ),
                    const Divider(height: 18),
                    for (final line in order.lines)
                      Padding(
                        padding: const EdgeInsets.symmetric(vertical: 2),
                        child: Row(
                          children: [
                            Expanded(
                              child: Text(
                                '${line.qty}× ${line.name}'
                                '${line.variant.isEmpty ? '' : ' · ${line.variant}'}',
                              ),
                            ),
                            Text(money(line.amount)),
                          ],
                        ),
                      ),
                    const SizedBox(height: 10),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        FilledButton.icon(
                          onPressed: controller.busy
                              ? null
                              : () => _review(context, order, approve: true),
                          icon: const Icon(Icons.check_rounded),
                          label: const Text('Approve & send to kitchen'),
                        ),
                        OutlinedButton.icon(
                          onPressed: controller.busy
                              ? null
                              : () => _review(context, order, approve: false),
                          icon: const Icon(Icons.close_rounded),
                          label: const Text('Reject'),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
      ],
    );
  }

  Future<void> _review(
    BuildContext context,
    GuestOrderModel order, {
    required bool approve,
  }) async {
    var reason = '';
    if (!approve) {
      final input = TextEditingController();
      final result = await showDialog<String>(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: const Text('Reject guest order'),
          content: TextField(
            controller: input,
            autofocus: true,
            maxLength: 300,
            decoration: const InputDecoration(labelText: 'Reason'),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext, input.text.trim()),
              child: const Text('Reject order'),
            ),
          ],
        ),
      );
      input.dispose();
      if (result == null || result.isEmpty) return;
      reason = result;
    }
    try {
      await controller.reviewGuestOrder(
        order: order,
        approve: approve,
        rejectionReason: reason,
      );
    } on ApiException catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    }
  }
}

class RestaurantReservationsScreen extends StatefulWidget {
  const RestaurantReservationsScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<RestaurantReservationsScreen> createState() =>
      _RestaurantReservationsScreenState();
}

class _RestaurantReservationsScreenState
    extends State<RestaurantReservationsScreen> {
  final Map<String, String> _tableChoices = {};

  RestaurantAppController get controller => widget.controller;

  @override
  Widget build(BuildContext context) {
    final reservations = [...controller.reservations]
      ..sort((a, b) => a.startsAt.compareTo(b.startsAt));
    final waitlist = reservations.where((item) => item.isWalkIn).length;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Reservations & Waitlist',
          action: '${reservations.length} bookings · $waitlist walk-ins',
          icon: Icons.event_seat_rounded,
        ),
        const SizedBox(height: 12),
        Align(
          alignment: Alignment.centerRight,
          child: FilledButton.icon(
            onPressed: controller.busy ? null : () => _addWalkIn(context),
            icon: const Icon(Icons.person_add_alt_1_rounded),
            label: const Text('Add walk-in'),
          ),
        ),
        const SizedBox(height: 10),
        if (reservations.isEmpty)
          const _EmptyState(
            icon: Icons.event_available_rounded,
            title: 'No bookings or waitlist guests',
            message: 'Public requests and walk-ins will appear here.',
          )
        else
          for (final reservation in reservations)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _reservationCard(context, reservation),
            ),
      ],
    );
  }

  Widget _reservationCard(
    BuildContext context,
    RestaurantReservationRecord reservation,
  ) {
    final availableTables = controller.tables
        .where((table) => table.isActive && table.capacity >= reservation.partySize)
        .toList();
    final chosenId = _tableChoices[reservation.id] ?? reservation.tableId;
    final selectedId = availableTables.any((table) => table.id == chosenId)
        ? chosenId
        : null;
    final actions = <Widget>[];
    if (reservation.status == 0 || reservation.status == 3) {
      actions.add(_action(
        context,
        reservation,
        1,
        'Confirm',
        Icons.check_rounded,
        tableId: selectedId,
      ));
      actions.add(_action(
        context,
        reservation,
        2,
        'Decline',
        Icons.close_rounded,
      ));
    }
    if (reservation.status == 1 || reservation.status == 3) {
      actions.add(_action(
        context,
        reservation,
        4,
        'Seat & open table',
        Icons.table_bar_rounded,
        tableId: selectedId,
      ));
    }
    if (reservation.status == 0 || reservation.status == 1 || reservation.status == 3) {
      actions.add(_action(
        context,
        reservation,
        7,
        'No-show',
        Icons.person_off_rounded,
      ));
      actions.add(_action(
        context,
        reservation,
        5,
        'Cancel',
        Icons.event_busy_rounded,
      ));
    }
    if (reservation.status != 2 &&
        reservation.status != 5 &&
        reservation.status != 6 &&
        reservation.status != 7) {
      actions.add(OutlinedButton.icon(
        onPressed: controller.busy ? null : () => _adjustDuration(context, reservation),
        icon: const Icon(Icons.schedule_rounded, size: 18),
        label: const Text('Adjust duration'),
      ));
    }

    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  '${reservation.guestName} · ${reservation.partySize} guests',
                  style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 16),
                ),
              ),
              _statusChip(reservation.status),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            '${reservation.isWalkIn ? 'Walk-in' : 'Booking'} · ${_guestOpsDate(reservation.startsAt)} to ${_guestOpsClock(reservation.endsAt)}',
          ),
          if (reservation.phoneNumber.isNotEmpty)
            Text('Phone: ${reservation.phoneNumber}'),
          if (reservation.notes.isNotEmpty) Text('Note: ${reservation.notes}'),
          if (reservation.tableName != null)
            Text('Table: ${reservation.tableName}'),
          if (!reservation.isWalkIn && reservation.smsStatus != 'No message')
            Text(
              'SMS: ${reservation.smsStatus}',
              style: TextStyle(
                color: reservation.smsStatus.startsWith('Failed')
                    ? AppColors.red
                    : AppColors.teal,
              ),
            ),
          if (actions.isNotEmpty) ...[
            const SizedBox(height: 8),
            if (reservation.status == 0 || reservation.status == 3 || reservation.status == 1)
              DropdownButtonFormField<String>(
                initialValue: selectedId,
                isExpanded: true,
                decoration: const InputDecoration(
                  labelText: 'Suitable table',
                  isDense: true,
                ),
                items: [
                  for (final table in availableTables)
                    DropdownMenuItem(
                      value: table.id,
                      child: Text('${table.displayName} · ${table.capacity} seats'),
                    ),
                ],
                onChanged: controller.busy
                    ? null
                    : (value) => setState(() {
                        if (value == null) {
                          _tableChoices.remove(reservation.id);
                        } else {
                          _tableChoices[reservation.id] = value;
                        }
                      }),
              ),
            const SizedBox(height: 8),
            Wrap(spacing: 8, runSpacing: 6, children: actions),
          ],
        ],
      ),
    );
  }

  Widget _action(
    BuildContext context,
    RestaurantReservationRecord reservation,
    int status,
    String label,
    IconData icon, {
    String? tableId,
  }) {
    return OutlinedButton.icon(
      onPressed: controller.busy
          ? null
          : () async {
              if ((status == 1 || status == 4) && tableId == null) {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Select a suitable table first.')),
                );
                return;
              }
              try {
                await controller.updateReservation(
                  reservation: reservation,
                  status: status,
                  tableId: tableId,
                );
              } on ApiException catch (error) {
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(error.message)),
                  );
                }
              }
            },
      icon: Icon(icon, size: 18),
      label: Text(label),
    );
  }

  Widget _statusChip(int status) {
    final label = switch (status) {
      0 => 'Requested',
      1 => 'Confirmed',
      2 => 'Declined',
      3 => 'Waitlisted',
      4 => 'Seated',
      5 => 'Cancelled',
      6 => 'Completed',
      7 => 'No-show',
      _ => 'Unknown',
    };
    final color = switch (status) {
      1 || 4 || 6 => AppColors.green,
      2 || 5 || 7 => AppColors.red,
      _ => AppColors.amber,
    };
    return Chip(
      visualDensity: VisualDensity.compact,
      label: Text(label),
      side: BorderSide.none,
      backgroundColor: color.withValues(alpha: 0.12),
      labelStyle: TextStyle(color: color, fontWeight: FontWeight.w800),
    );
  }

  Future<void> _adjustDuration(
    BuildContext context,
    RestaurantReservationRecord reservation,
  ) async {
    final selectedDate = await showDatePicker(
      context: context,
      initialDate: reservation.endsAt,
      firstDate: reservation.startsAt,
      lastDate: reservation.startsAt.add(const Duration(days: 2)),
    );
    if (selectedDate == null || !context.mounted) return;
    final selectedTime = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(reservation.endsAt),
    );
    if (selectedTime == null || !context.mounted) return;
    final end = DateTime(
      selectedDate.year,
      selectedDate.month,
      selectedDate.day,
      selectedTime.hour,
      selectedTime.minute,
    );
    if (!end.isAfter(reservation.startsAt)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('The end time must be after the start time.')),
      );
      return;
    }
    try {
      await controller.updateReservation(
        reservation: reservation,
        status: reservation.status,
        endsAt: end,
      );
    } on ApiException catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error.message)),
        );
      }
    }
  }

  Future<void> _addWalkIn(BuildContext context) async {
    final name = TextEditingController();
    final phone = TextEditingController();
    final party = TextEditingController(text: '2');
    final notes = TextEditingController();
    final formKey = GlobalKey<FormState>();
    final saved = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Add walk-in to waitlist'),
        content: Form(
          key: formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: name,
                  decoration: const InputDecoration(labelText: 'Guest name'),
                  validator: (value) => (value?.trim().isEmpty ?? true)
                      ? 'Enter a guest name'
                      : null,
                ),
                TextFormField(
                  controller: phone,
                  decoration: const InputDecoration(labelText: 'Phone (optional)'),
                  keyboardType: TextInputType.phone,
                ),
                TextFormField(
                  controller: party,
                  decoration: const InputDecoration(labelText: 'Party size'),
                  keyboardType: TextInputType.number,
                  validator: (value) {
                    final count = int.tryParse(value ?? '');
                    return count == null || count < 1 || count > 30
                        ? 'Enter 1 to 30 guests'
                        : null;
                  },
                ),
                TextFormField(
                  controller: notes,
                  decoration: const InputDecoration(labelText: 'Notes (optional)'),
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () {
              if (formKey.currentState?.validate() ?? false) {
                Navigator.pop(dialogContext, true);
              }
            },
            child: const Text('Add to waitlist'),
          ),
        ],
      ),
    );
    if (saved == true) {
      try {
        await controller.addWalkIn(
          guestName: name.text.trim(),
          phoneNumber: phone.text.trim(),
          partySize: int.parse(party.text),
          notes: notes.text.trim(),
        );
      } on ApiException catch (error) {
        if (context.mounted) {
          ScaffoldMessenger.of(
            context,
          ).showSnackBar(SnackBar(content: Text(error.message)));
        }
      }
    }
    name.dispose();
    phone.dispose();
    party.dispose();
    notes.dispose();
  }
}

class RestaurantPrintQueueScreen extends StatelessWidget {
  const RestaurantPrintQueueScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  Widget build(BuildContext context) {
    final jobs = controller.printJobs;
    final failed = jobs.where((job) => job.status == 3).length;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'ERP Print Queue',
          action: '${jobs.length} recent jobs · $failed failed',
          icon: Icons.print_rounded,
        ),
        const SizedBox(height: 12),
        if (jobs.isEmpty)
          const _EmptyState(
            icon: Icons.print_disabled_rounded,
            title: 'No print jobs yet',
            message: 'Kitchen tickets and finalized bills will be queued here.',
          )
        else
          _Panel(
            child: Column(
              children: [
                for (final job in jobs)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(
                      job.type == 0 ? Icons.soup_kitchen_rounded : Icons.receipt_long_rounded,
                      color: job.status == 3 ? AppColors.red : AppColors.primary,
                    ),
                    title: Text(
                      '${job.type == 0 ? 'Kitchen ticket' : 'Bill receipt'} · ${job.externalJobId}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    subtitle: Text(
                      '${job.routeName} · ${_printJobStatus(job.status)} · ${job.attempts} attempts'
                      '${job.reprintReason == null ? '' : '\nReprint reason: ${job.reprintReason}'}'
                      '${job.lastError == null ? '' : '\n${job.lastError}'}',
                    ),
                    isThreeLine: job.lastError != null || job.reprintReason != null,
                    trailing: job.status == 3
                        ? TextButton.icon(
                            onPressed: controller.busy
                                ? null
                                : () async {
                                    try {
                                      await controller.retryPrintJob(job);
                                    } on ApiException catch (error) {
                                      if (context.mounted) {
                                        ScaffoldMessenger.of(context).showSnackBar(
                                          SnackBar(content: Text(error.message)),
                                        );
                                      }
                                    }
                                  },
                            icon: const Icon(Icons.replay_rounded),
                            label: const Text('Retry'),
                          )
                        : null,
                  ),
              ],
            ),
          ),
      ],
    );
  }
}

String _printJobStatus(int status) => switch (status) {
  0 => 'Waiting for print station',
  1 => 'Printing',
  2 => 'Printed',
  3 => 'Failed',
  4 => 'Cancelled',
  _ => 'Unknown',
};

String _guestOpsDate(DateTime value) {
  final local = value.toLocal();
  final month = local.month.toString().padLeft(2, '0');
  final day = local.day.toString().padLeft(2, '0');
  return '${local.year}-$month-$day ${_guestOpsClock(local)}';
}

String _guestOpsClock(DateTime value) {
  final local = value.toLocal();
  final hour = local.hour.toString().padLeft(2, '0');
  final minute = local.minute.toString().padLeft(2, '0');
  return '$hour:$minute';
}
