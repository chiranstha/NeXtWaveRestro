part of '../../main.dart';

class KdsScreen extends StatelessWidget {
  const KdsScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  Widget build(BuildContext context) {
    final stationNames = <String>{
      ...controller.stations.map((station) => station.name),
      ...controller.tickets.map((ticket) => ticket.station),
    }.where((name) => name.trim().isNotEmpty).toList()..sort();
    final filters = ['All', 'Ready', ...stationNames];
    final selected = filters.contains(controller.selectedKdsStation)
        ? controller.selectedKdsStation
        : 'All';
    final visible = controller.tickets.where((ticket) {
      if (ticket.status == TicketStatus.bumped) return false;
      if (selected == 'All') return true;
      if (selected == 'Ready') return ticket.status == TicketStatus.ready;
      return ticket.station == selected;
    }).toList()..sort((a, b) => b.minutes.compareTo(a.minutes));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Kitchen Display System',
          action: '${visible.length} live tickets · refreshes every 15 seconds',
          icon: Icons.restaurant_menu_rounded,
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final filter in filters)
              ChoiceChip(
                selected: selected == filter,
                label: Text(filter),
                onSelected: (_) => controller.selectKdsStation(filter),
              ),
          ],
        ),
        const SizedBox(height: 12),
        if (visible.isEmpty)
          const _EmptyState(
            icon: Icons.task_alt_rounded,
            title: 'No active kitchen tickets',
            message: 'New KOT/BOT tickets will appear here automatically.',
          )
        else
          _ResponsiveGrid(
            minTileWidth: 310,
            tileHeight: 280,
            children: [
              for (final ticket in visible)
                _TicketCard(
                  ticket: ticket,
                  onAdvance: () async {
                    try {
                      await controller.advanceTicket(ticket);
                    } on ApiException catch (error) {
                      if (context.mounted) {
                        ScaffoldMessenger.of(
                          context,
                        ).showSnackBar(SnackBar(content: Text(error.message)));
                      }
                    }
                  },
                  onItemStatus: ticket.purpose == 1
                      ? null
                      : (line, status) =>
                            _updateItem(context, ticket, line, status),
                  onCancel: ticket.purpose == 1
                      ? null
                      : () => _cancelTicket(context, ticket),
                ),
            ],
          ),
      ],
    );
  }

  Future<void> _updateItem(
    BuildContext context,
    KdsTicket ticket,
    CartLine line,
    int status,
  ) async {
    var reason = '';
    if (status == 5) {
      reason = await _reasonDialog(context, 'Cancel kitchen item') ?? '';
      if (reason.isEmpty) return;
    }
    try {
      await controller.updateTicketItem(ticket, line, status, reason: reason);
    } on ApiException catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    }
  }

  Future<void> _cancelTicket(BuildContext context, KdsTicket ticket) async {
    final reason = await _reasonDialog(context, 'Cancel ${ticket.id}');
    if (reason == null || reason.isEmpty) return;
    try {
      await controller.cancelTicket(ticket, reason);
    } on ApiException catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    }
  }

  Future<String?> _reasonDialog(BuildContext context, String title) async {
    final text = TextEditingController();
    final value = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: TextField(
          controller: text,
          autofocus: true,
          decoration: const InputDecoration(labelText: 'Reason'),
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
}
