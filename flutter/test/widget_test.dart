import 'dart:ui';

import 'package:flutter_test/flutter_test.dart';
import 'package:restaurant_erp/main.dart';

void main() {
  testWidgets('restaurant ERP shell renders primary modules', (tester) async {
    tester.view.physicalSize = const Size(1440, 1000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const RestaurantErpApp());

    expect(find.text('NextWave'), findsOneWidget);
    expect(find.text('Command Center'), findsWidgets);
    expect(find.text('POS Billing'), findsWidgets);
    expect(find.text('KDS'), findsWidgets);
    expect(find.text('Restaurant Inventory'), findsWidgets);
  });

  testWidgets('POS item entry sends tickets to KDS', (tester) async {
    tester.view.physicalSize = const Size(1440, 1000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const RestaurantErpApp());

    await tester.tap(find.text('POS Billing').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Chicken Momo').first);
    await tester.pumpAndSettle();

    expect(find.text('Current Bill'), findsOneWidget);
    expect(find.text('1'), findsWidgets);

    await tester.tap(find.text('Send KOT/BOT'));
    await tester.pumpAndSettle();

    expect(find.text('Kitchen Display System'), findsOneWidget);
    expect(find.textContaining('KOT-'), findsWidgets);
    expect(find.textContaining('Sent 1 KOT/BOT ticket'), findsOneWidget);
  });

  testWidgets('generic ERP module quick entry creates a draft row', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1440, 1000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const RestaurantErpApp());

    await tester.tap(find.text('Account Ledgers').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Save Ledger').first);
    await tester.pumpAndSettle();

    expect(find.text('ACC-2005'), findsOneWidget);
    expect(
      find.text('Save Ledger created in Account Ledgers.'),
      findsOneWidget,
    );
  });
}
