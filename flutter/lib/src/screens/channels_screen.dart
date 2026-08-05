part of '../../main.dart';

class ChannelsScreen extends StatelessWidget {
  const ChannelsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(
          title: 'Channels',
          action: 'Aggregator integration',
          icon: Icons.hub_rounded,
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 250,
          tileHeight: 138,
          children: [
            for (final channel in channelStatuses)
              _ChannelCard(channel: channel),
          ],
        ),
        const SizedBox(height: 16),
        _Panel(
          child: Column(
            children: [
              const _Toolbar(
                title: 'Payout Reconciliation',
                button: 'Import Settlement',
                icon: Icons.upload_file_rounded,
              ),
              _ErpTable(
                columns: const [
                  'Provider',
                  'Orders',
                  'Gross Sales',
                  'Commission',
                  'Expected Payout',
                  'Status',
                ],
                rows: [
                  for (final channel in channelStatuses)
                    [
                      channel.name,
                      '${channel.orders}',
                      money(channel.sales),
                      money(channel.commission),
                      money(channel.sales - channel.commission),
                      channel.sync,
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
