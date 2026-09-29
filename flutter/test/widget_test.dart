import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:restaurant_erp/main.dart';

void main() {
  testWidgets('NextWave sign-in shows tenant name but never a server URL', (
    tester,
  ) async {
    final controller = RestaurantAppController.live(
      sessionStore: MemorySessionStore(),
    );

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    expect(find.text('NextWave'), findsOneWidget);
    expect(find.text('Tenant name'), findsOneWidget);
    expect(find.text('Server URL'), findsNothing);
    expect(find.textContaining('https://'), findsNothing);
  });

  testWidgets('tenant-branded sign-in hides tenant selection', (tester) async {
    const brand = AppBrand(
      key: 'restaurant-one',
      displayName: 'Restaurant One',
      productName: 'Staff App',
      tenantId: 42,
    );
    final controller = RestaurantAppController.live(
      brand: brand,
      sessionStore: MemorySessionStore(),
    );

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    expect(find.text('Restaurant One'), findsOneWidget);
    expect(find.text('Tenant name'), findsNothing);
    expect(find.text('Server URL'), findsNothing);
    expect(find.textContaining('Restaurant One staff account'), findsOneWidget);
  });

  testWidgets('restaurant shell renders permission-aware live modules', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    expect(find.text('NextWave'), findsOneWidget);
    expect(find.text('Command Center'), findsWidgets);
    expect(find.text('POS Billing'), findsWidgets);
    expect(find.text('KDS'), findsWidgets);
    expect(find.text('Restaurant Inventory'), findsWidgets);
    expect(find.textContaining('Welcome, Demo Manager'), findsOneWidget);
  });

  testWidgets('manager navigation adapts when report permissions change', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo(
      grantedPermissions: const {
        'Pages.Restaurant',
        'Pages.Restaurant.Reports',
        'Pages.Restaurant.Reports.Sales',
      },
    );

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    expect(find.text('Restaurant Reports'), findsWidgets);
    expect(find.text('POS Billing'), findsNothing);
    expect(find.text('Sales today'), findsOneWidget);

    await tester.tap(find.text('Restaurant Reports').first);
    await tester.pumpAndSettle();
    expect(find.text('Sales'), findsOneWidget);
    expect(find.text('Inventory & Cost'), findsNothing);

    controller.setGrantedPermissionsForTesting(const {'Pages.Restaurant'});
    await tester.pumpAndSettle();

    expect(find.text('Restaurant Reports'), findsNothing);
    expect(find.text('Command Center'), findsWidgets);
    expect(tester.takeException(), isNull);
  });

  testWidgets('setup read permission opens setup without edit actions', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo(
      grantedPermissions: const {'Pages.Restaurant', 'Pages.Restaurant.Setup'},
    );

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    expect(find.text('Restaurant Setup'), findsWidgets);
    expect(
      controller.visibleModules.any(
        (module) => module.kind == ModuleKind.restaurantSetup,
      ),
      isTrue,
    );

    await tester.tap(find.text('Restaurant Setup').first);
    await tester.pumpAndSettle();

    expect(find.text('Areas'), findsOneWidget);
    expect(find.byTooltip('Add Areas'), findsNothing);
    expect(find.byTooltip('Add Tables'), findsNothing);
    expect(find.byTooltip('Add Stations'), findsNothing);
    expect(find.byTooltip('Edit settings'), findsNothing);
  });

  testWidgets('finance manager dashboard shows only finance report cards', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo(
      grantedPermissions: const {
        'Pages.Restaurant',
        'Pages.Restaurant.Reports',
        'Pages.Restaurant.Reports.AuditFinance',
      },
    );

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    expect(find.text('Settled sales'), findsOneWidget);
    expect(find.text('Discount exposure'), findsOneWidget);
    expect(find.text('Sales today'), findsNothing);
    expect(find.text('Pending KOT / BOT'), findsNothing);
    expect(find.text('Active employees'), findsNothing);
  });

  testWidgets('POS order sends KOT to the kitchen board', (tester) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();

    await tester.tap(find.text('POS Billing').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Chicken Momo').first);
    await tester.pumpAndSettle();

    expect(find.text('Current order'), findsOneWidget);
    expect(controller.cart, hasLength(1));

    final sendButton = find.text('Send KOT');
    await tester.ensureVisible(sendButton);
    await tester.tap(sendButton);
    await tester.pumpAndSettle();

    expect(controller.cart, isEmpty);
    expect(
      controller.tickets.any((ticket) => ticket.id.startsWith('KOT-')),
      isTrue,
    );

    await tester.tap(find.text('KDS').first);
    await tester.pumpAndSettle();
    expect(find.text('Kitchen Display System'), findsOneWidget);
    expect(find.textContaining('KOT-'), findsWidgets);
  });

  testWidgets('Android POS keeps final billing in the browser cashier', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();
    await tester.tap(find.text('POS Billing').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Veg Chowmein').first);
    await tester.pumpAndSettle();

    expect(
      find.text(
        'Final bills and payment settlement are completed in the browser cashier.',
      ),
      findsOneWidget,
    );
    expect(find.text('Settle'), findsNothing);
    expect(controller.cart, hasLength(1));
  });

  testWidgets('phone layout opens the POS without render overflows', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);

    await tester.tap(find.byTooltip('Menu'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('POS Billing').last);
    await tester.pumpAndSettle();

    expect(find.text('Current order'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('restaurant payroll renders role-aware shift and pay-run views', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Staff & Payroll').first);
    await tester.pumpAndSettle();

    expect(find.text('My shift today'), findsOneWidget);
    expect(find.text('Active staff'), findsOneWidget);
    expect(find.text('Pay runs'), findsWidgets);
    expect(tester.takeException(), isNull);
  });

  testWidgets('phone layout opens payroll overview without render overflows', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();
    await tester.tap(find.byTooltip('Menu'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Staff & Payroll').last);
    await tester.pumpAndSettle();

    expect(find.text('My shift today'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('staff onboarding combines employee, login and role assignment', (
    tester,
  ) async {
    await _setDesktopViewport(tester);
    final controller = RestaurantAppController.demo();

    await tester.pumpWidget(RestaurantErpApp(controller: controller));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Staff & Payroll').first);
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ChoiceChip, 'Staff'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Add staff'));
    await tester.pumpAndSettle();

    expect(find.text('App login & access'), findsOneWidget);
    expect(find.text('Login setup'), findsOneWidget);
    expect(
      find.textContaining('app role controls permissions'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });
}

Future<void> _setDesktopViewport(WidgetTester tester) async {
  tester.view.physicalSize = const Size(1440, 1200);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}
