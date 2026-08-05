part of '../../main.dart';

class KdsScreen extends StatelessWidget {
  const KdsScreen({
    required this.tickets,
    required this.station,
    required this.onStationChanged,
    required this.onAdvance,
    super.key,
  });

  final List<KdsTicket> tickets;
  final String station;
  final ValueChanged<String> onStationChanged;
  final ValueChanged<KdsTicket> onAdvance;

  @override
  Widget build(BuildContext context) {
    final stations = ['All', 'Kitchen', 'Bar', 'Ready'];
    final visible = tickets.where((ticket) {
      if (station == 'All') return ticket.status != TicketStatus.bumped;
      if (station == 'Ready') return ticket.status == TicketStatus.ready;
      return ticket.station == station && ticket.status != TicketStatus.bumped;
    }).toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Kitchen Display System',
          action: '${visible.length} live tickets',
          icon: Icons.restaurant_menu_rounded,
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            for (final item in stations)
              ChoiceChip(
                selected: station == item,
                label: Text(item),
                onSelected: (_) => onStationChanged(item),
              ),
          ],
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 310,
          tileHeight: 190,
          children: [
            for (final ticket in visible)
              _TicketCard(ticket: ticket, onAdvance: () => onAdvance(ticket)),
          ],
        ),
      ],
    );
  }
}
