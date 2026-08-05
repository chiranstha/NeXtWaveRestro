part of '../../main.dart';

class RestaurantSetupScreen extends StatelessWidget {
  const RestaurantSetupScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(
          title: 'Restaurant Setup',
          action: 'Outlet configuration',
          icon: Icons.settings_suggest_rounded,
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 250,
          tileHeight: 132,
          children: const [
            _SetupCard(
              icon: Icons.store_rounded,
              title: 'Outlets',
              value: '3',
              lines: ['Durbar Marg', 'Jhamsikhel', 'Central Kitchen'],
              color: AppColors.primary,
            ),
            _SetupCard(
              icon: Icons.table_bar_rounded,
              title: 'Floors & Tables',
              value: '32',
              lines: [
                'Ground: 14 tables',
                'Rooftop: 10 tables',
                'Private: 8 tables',
              ],
              color: AppColors.green,
            ),
            _SetupCard(
              icon: Icons.soup_kitchen_rounded,
              title: 'Stations',
              value: '5',
              lines: ['Hot Kitchen', 'Tandoor', 'Bar', 'Bakery', 'Packing'],
              color: AppColors.amber,
            ),
            _SetupCard(
              icon: Icons.percent_rounded,
              title: 'Taxes & Charges',
              value: '13%',
              lines: ['VAT 13%', 'Service charge 10%', 'IRD-CBMS enabled'],
              color: AppColors.red,
            ),
            _SetupCard(
              icon: Icons.devices_rounded,
              title: 'Devices',
              value: '9',
              lines: ['3 POS counters', '4 KDS screens', '2 waiter tabs'],
              color: AppColors.teal,
            ),
            _SetupCard(
              icon: Icons.security_rounded,
              title: 'Staff Rights',
              value: 'PIN',
              lines: ['Void bill', 'Discount override', 'Day close'],
              color: AppColors.violet,
            ),
          ],
        ),
      ],
    );
  }
}
