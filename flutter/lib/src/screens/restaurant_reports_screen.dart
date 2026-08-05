part of '../../main.dart';

class RestaurantReportsScreen extends StatelessWidget {
  const RestaurantReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return _ReportSuite(
      title: 'Restaurant Reports',
      cards: const [
        _ReportSpec(
          'Z Report',
          'Sales, tax, cash, card, aggregator settlement',
          Icons.receipt_rounded,
          AppColors.primary,
        ),
        _ReportSpec(
          'KOT Speed',
          'Prep time by station, item, waiter, delay reason',
          Icons.timer_rounded,
          AppColors.amber,
        ),
        _ReportSpec(
          'Food Cost',
          'Recipe cost vs menu price and margin leakage',
          Icons.pie_chart_rounded,
          AppColors.green,
        ),
        _ReportSpec(
          'Void & Discount',
          'PIN approvals and anti-pilferage audit',
          Icons.policy_rounded,
          AppColors.red,
        ),
        _ReportSpec(
          'Channel Profit',
          'Foodmandu/Pathao commission and payout variance',
          Icons.hub_rounded,
          AppColors.teal,
        ),
        _ReportSpec(
          'Stock Variance',
          'Theoretical vs actual kitchen consumption',
          Icons.inventory_rounded,
          AppColors.violet,
        ),
      ],
    );
  }
}
