import 'package:flutter_test/flutter_test.dart';
import 'package:restaurant_erp/main.dart';

void main() {
  test('sign-out clears credentials without deleting user-scoped order drafts', () async {
    final store = MemorySessionStore()
      ..value = StoredSession(
        baseUrl: AppConfig.defaultApiBaseUrl,
        accessToken: 'access',
        refreshToken: 'refresh',
        tenantId: 9,
        tenancyName: 'cafe',
        userName: 'waiter',
        userId: 27,
      );
    await store.writeValue('restaurant-cart-draft:9:27', '{"lines":[]}');

    await store.clear();

    expect(await store.read(), isNull);
    expect(await store.readValue('restaurant-cart-draft:9:27'), '{"lines":[]}');
  });

  test(
    'tenant-branded app rejects a saved session from another tenant',
    () async {
      final store = MemorySessionStore()
        ..value = StoredSession(
          baseUrl: AppConfig.defaultApiBaseUrl,
          accessToken: 'old-token',
          refreshToken: 'old-refresh-token',
          tenantId: 41,
          tenancyName: '',
          userName: 'old.user',
        );
      const brand = AppBrand(
        key: 'restaurant-one',
        displayName: 'Restaurant One',
        productName: 'Staff App',
        tenantId: 42,
      );
      final controller = RestaurantAppController.live(
        brand: brand,
        sessionStore: store,
      );

      await controller.initialize();

      expect(controller.initialized, isTrue);
      expect(controller.isAuthenticated, isFalse);
      expect(controller.session, isNull);
      expect(store.value, isNull);
    },
  );

  test('non-NextWave brands require a positive tenant ID', () {
    const brand = AppBrand(
      key: 'restaurant-one',
      displayName: 'Restaurant One',
      productName: 'Staff App',
      tenantId: 0,
    );

    expect(brand.usesTenantName, isFalse);
    expect(brand.fixedTenantId, isNull);
    expect(brand.configurationError, isNotNull);
  });

  test('demo menu availability follows the backend-shaped workflow', () async {
    final controller = RestaurantAppController.demo();
    final product = controller.products.first;

    await controller.setMenuAvailability(product, false);

    final updated = controller.products.firstWhere(
      (item) => item.id == product.id,
    );
    expect(updated.isAvailable, isFalse);
    expect(updated.available, isFalse);
  });

  test('demo KOT creates actionable item identifiers', () async {
    final controller = RestaurantAppController.demo();
    controller.addToCart(
      controller.products.first,
      const ProductConfiguration(),
    );

    await controller.sendCurrentOrder();

    final ticket = controller.tickets.first;
    final line = ticket.items.first;
    expect(line.ticketItemId, isNotEmpty);

    await controller.updateTicketItem(ticket, line, 2);

    expect(controller.tickets.first.items.first.status, 2);
  });

  test(
    'demo setup mutation updates the same controller state as live mode',
    () async {
      final controller = RestaurantAppController.demo();

      await controller.saveArea(name: 'Roof Top', sortOrder: 4);

      expect(controller.areas.any((area) => area.name == 'Roof Top'), isTrue);
    },
  );

  test('demo payroll exposes restaurant wage and shift data', () async {
    final controller = RestaurantAppController.demo();

    expect(
      controller.visibleModules.any(
        (module) => module.kind == ModuleKind.restaurantPayroll,
      ),
      isTrue,
    );
    expect(controller.payrollDashboard?.activeEmployeeCount, greaterThan(0));
    expect(controller.payrollEmployees.first.jobRole, 'Restaurant Manager');
    expect(controller.payrollRuns.first.serviceChargePool, greaterThan(0));

    await controller.payrollClockIn(shiftName: 'Evening');
    expect(controller.noticeMessage, contains('Demo shift'));
  });

  test(
    'demo employee onboarding creates one-time restaurant credentials',
    () async {
      final controller = RestaurantAppController.demo();

      final result = await controller.savePayrollEmployee(
        updateLoginAccess: true,
        loginMode: EmployeeLoginMode.createNew,
        loginUserName: 'new.waiter',
        loginEmailAddress: '',
        loginPhoneNumber: '9800000000',
        restaurantRoleName: 'RestaurantWaiter',
        loginIsActive: true,
        staffCode: 'WTR-002',
        name: 'New Waiter',
        jobRole: 'Waiter',
        department: 'Service',
        employmentType: PayrollEmploymentType.monthly,
        basicSalary: 24000,
        hourlyRate: 0,
        overtimeRate: 150,
        fixedAllowance: 0,
        fixedDeduction: 0,
        serviceChargeWeight: 1,
        bankAccountNumber: '',
        panNumber: '',
        ssfNumber: '',
        joinedOn: DateTime(2026, 8, 15),
        isActive: true,
      );

      expect(controller.payrollRoleOptions, isNotEmpty);
      expect(result.userName, 'new.waiter');
      expect(result.temporaryPassword, isNotEmpty);
    },
  );
}
