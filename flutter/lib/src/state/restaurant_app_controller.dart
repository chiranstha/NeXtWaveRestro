part of '../../main.dart';

class RestaurantAppController extends ChangeNotifier {
  RestaurantAppController._({required this._sessionStore, required this.brand})
    : baseUrl = AppConfig.normalizeBaseUrl(AppConfig.defaultApiBaseUrl);

  factory RestaurantAppController.live({
    AppBrand brand = AppConfig.brand,
    SessionStore? sessionStore,
  }) {
    return RestaurantAppController._(
      sessionStore: sessionStore ?? const SecureSessionStore(),
      brand: brand,
    );
  }

  factory RestaurantAppController.demo({
    AppBrand brand = AppConfig.brand,
    Set<String>? grantedPermissions,
  }) {
    final controller = RestaurantAppController._(
      sessionStore: MemorySessionStore(),
      brand: brand,
    );
    controller
      .._demoMode = true
      ..initialized = true
      ..isAuthenticated = true
      ..profile = const LoginProfile(
        userId: 1,
        userName: 'demo',
        name: 'Demo Manager',
        tenantName: 'NextWave Demo',
      )
      ..permissions = grantedPermissions ?? _allRestaurantPermissions
      ..permissionsLoaded = true
      ..products = List<MenuProduct>.from(menuProducts)
      ..tickets = seedTickets()
      ..inventory = List<InventoryItem>.from(inventoryItems)
      ..rawMaterials = const [
        UniversalOption(id: 'raw-1', name: 'Chicken Breast'),
        UniversalOption(id: 'raw-2', name: 'Paneer'),
      ]
      ..inventoryUnits = const [
        UniversalOption(id: 'unit-kg', name: 'KG'),
        UniversalOption(id: 'unit-pcs', name: 'PCS'),
      ]
      ..suppliers = const [
        UniversalOption(id: 'supplier-1', name: 'Himalayan Fresh Mart'),
      ]
      ..areas = const [RestaurantAreaModel(id: 'area-1', name: 'Main Dining')]
      ..tables = const [
        RestaurantTableModel(
          id: 'table-1',
          name: 'Table 07',
          code: 'T-07',
          areaId: 'area-1',
          areaName: 'Main Dining',
          capacity: 4,
          status: 0,
        ),
      ]
      ..stations = const [
        RestaurantStationModel(id: 'kitchen', name: 'Kitchen', type: 0),
        RestaurantStationModel(id: 'bar', name: 'Bar', type: 1),
      ]
      ..selectedPosContext = 'table-1'
      ..accountLedgers = const [LedgerOption(id: 'cash', name: 'Cash')]
      ..salesLedgers = const [LedgerOption(id: 'sales', name: 'Sales Account')]
      ..guestOrders = const []
      ..reservations = const []
      ..printJobs = const []
      ..report = const ReportSummary(
        orderCount: 24,
        grossAmount: 42850,
        discountAmount: 450,
        taxAmount: 5512,
        grandTotal: 47912,
        averageBill: 1996.33,
      )
      ..reportBundle = const RestaurantReportBundle(
        summary: ReportSummary(
          orderCount: 24,
          grossAmount: 42850,
          discountAmount: 450,
          taxAmount: 5512,
          grandTotal: 47912,
          averageBill: 1996.33,
        ),
      );
    controller._seedDemoPayroll();
    return controller;
  }

  final SessionStore _sessionStore;
  final AppBrand brand;
  RestaurantApi? _api;
  Timer? _kdsTimer;
  Timer? _permissionTimer;
  Future<String?>? _refreshInFlight;
  Future<bool>? _permissionRefreshInFlight;
  bool _demoMode = false;

  bool initialized = false;
  bool isAuthenticated = false;
  bool serverReachable = false;
  bool draftSynced = false;
  bool androidDraftRecoveryEnabled = false;
  RestaurantReleaseCapabilities releaseCapabilities =
      const RestaurantReleaseCapabilities();
  bool draftNeedsReview = false;
  String draftReviewMessage = '';
  DateTime? draftSavedAt;
  bool busy = false;
  bool refreshing = false;
  bool permissionsLoaded = false;
  String baseUrl;
  String? errorMessage;
  String? noticeMessage;
  String? passwordResetCode;
  bool requiresTwoFactor = false;
  List<String> twoFactorProviders = const [];
  String? _twoFactorRememberClientToken;
  int? _pendingTwoFactorUserId;
  int? _pendingTenantId;
  String _pendingUserName = '';
  String _pendingPassword = '';
  StoredSession? session;
  LoginProfile? profile;
  Set<String> permissions = <String>{};

  List<RestaurantAreaModel> areas = [];
  List<RestaurantTableModel> tables = [];
  List<RestaurantStationModel> stations = [];
  List<MenuProduct> products = [];
  List<RestaurantOrderModel> openOrders = [];
  List<KdsTicket> tickets = [];
  List<InventoryItem> inventory = [];
  List<UniversalOption> rawMaterials = [];
  List<UniversalOption> inventoryUnits = [];
  List<UniversalOption> suppliers = [];
  List<SupplierItemMapping> supplierMappings = [];
  List<StockAdjustmentRecord> stockAdjustments = [];
  List<ConsumptionRecord> consumptionLedger = [];
  List<RecipeCoverageRecord> recipeCoverage = [];
  List<ChannelStatus> channels = [];
  List<RestaurantAggregatorOrderModel> aggregatorOrders = [];
  List<PayoutRecord> payouts = [];
  List<RestaurantDeviceModel> devices = [];
  List<KdsTicket> currentOrderTickets = [];
  List<LedgerOption> accountLedgers = [];
  List<LedgerOption> salesLedgers = [];
  ReportSummary report = const ReportSummary();
  RestaurantReportBundle reportBundle = const RestaurantReportBundle();
  Map<String, dynamic> operationalSettings = {};
  PayrollDashboard? payrollDashboard;
  List<PayrollEmployee> payrollEmployees = [];
  List<PayrollUserOption> payrollUserOptions = [];
  List<PayrollRoleOption> payrollRoleOptions = [];
  List<PayrollAttendance> payrollAttendance = [];
  List<PayrollRun> payrollRuns = [];
  List<PayrollLine> myPayslips = [];
  List<GuestOrderModel> guestOrders = [];
  List<RestaurantReservationRecord> reservations = [];
  List<RestaurantPrintJobRecord> printJobs = [];
  bool mobilePrinterRunning = false;

  String selectedPosContext = 'mode:takeaway';
  String selectedCategory = 'All';
  String selectedKdsStation = 'All';
  String? currentOrderId;
  String? currentOrderVersion;
  List<CartLine> cart = [];
  bool orderDirty = false;
  String? draftOrderRequestId;

  static const _allRestaurantPermissions = <String>{
    'Pages.Restaurant',
    'Pages.Restaurant.Setup',
    'Pages.Restaurant.Setup.Edit',
    'Pages.Restaurant.Setup.Create',
    'Pages.Restaurant.Setup.Delete',
    'Pages.Restaurant.Menu',
    'Pages.Restaurant.Menu.Edit',
    'Pages.Restaurant.ItemAvailability',
    'Pages.Restaurant.Recipe',
    'Pages.Restaurant.Pos',
    'Pages.Restaurant.Pos.Discount',
    'Pages.Restaurant.Pos.Void',
    'Pages.Restaurant.Pos.TableTransfer',
    'Pages.Restaurant.Pos.SplitMerge',
    'Pages.Restaurant.Reservations',
    'Pages.Restaurant.PrinterSetup',
    'Pages.Restaurant.Billing',
    'Pages.Restaurant.KotBot',
    'Pages.Restaurant.KotBot.Reprint',
    'Pages.Restaurant.Kds',
    'Pages.Restaurant.Inventory',
    'Pages.Restaurant.Inventory.SupplierMapping',
    'Pages.Restaurant.Inventory.StockAdjustment',
    'Pages.Restaurant.Inventory.Wastage',
    'Pages.Restaurant.Inventory.Reorder',
    'Pages.Restaurant.Channels',
    'Pages.Restaurant.Channels.Manage',
    'Pages.Restaurant.Aggregators',
    'Pages.Restaurant.Payouts',
    'Pages.Restaurant.Reports',
    'Pages.Restaurant.Reports.Sales',
    'Pages.Restaurant.Reports.Operations',
    'Pages.Restaurant.Reports.Inventory',
    'Pages.Restaurant.Reports.Payroll',
    'Pages.Restaurant.Reports.AuditFinance',
    'Pages.Restaurant.Payroll',
    'Pages.Restaurant.Payroll.Staff',
    'Pages.Restaurant.Payroll.Staff.Access',
    'Pages.Restaurant.Payroll.Attendance',
    'Pages.Restaurant.Payroll.Attendance.Manage',
    'Pages.Restaurant.Payroll.Process',
    'Pages.Restaurant.Payroll.Approve',
    'Pages.Restaurant.Payroll.Reports',
    'Pages.Restaurant.Payroll.OwnPayslip',
  };

  Future<void> initialize() async {
    if (initialized) return;
    try {
      final stored = await _sessionStore.read();
      if (stored != null) {
        if (!_sessionMatchesAppConfiguration(stored)) {
          await _sessionStore.clear();
          _detachSession();
          return;
        }
        final configuredSession = StoredSession(
          baseUrl: baseUrl,
          accessToken: stored.accessToken,
          refreshToken: stored.refreshToken,
          tenantId: stored.tenantId,
          tenancyName: stored.tenancyName,
          userName: stored.userName,
          userId: stored.userId,
        );
        session = configuredSession;
        _attachAuthenticatedApi(configuredSession);
        try {
          await _loadAuthenticatedData();
        } on ApiException catch (error) {
          if (error.isUnauthorized) rethrow;
          await _restoreLocalCartDraft();
          errorMessage = error.message;
        } on TimeoutException {
          serverReachable = false;
          await _restoreLocalCartDraft();
          errorMessage =
              'The restaurant server is offline. Your saved on-device draft is available.';
        }
      }
    } on ApiException catch (error) {
      if (error.isUnauthorized) {
        await _sessionStore.clear();
        _detachSession();
      } else {
        serverReachable = false;
        await _restoreLocalCartDraft();
        errorMessage = error.message;
      }
    } catch (error) {
      errorMessage = 'Could not restore the saved session: $error';
    } finally {
      initialized = true;
      notifyListeners();
    }
  }

  Future<void> login({
    required String tenancyName,
    required String userName,
    required String password,
    String? twoFactorCode,
  }) async {
    if (busy) return;
    busy = true;
    errorMessage = null;
    noticeMessage = null;
    notifyListeners();
    try {
      final configurationError = brand.configurationError;
      if (configurationError != null) {
        throw ApiException(configurationError);
      }

      final resolvedBaseUrl = AppConfig.normalizeBaseUrl(
        AppConfig.defaultApiBaseUrl,
      );
      int? tenantId = _pendingTenantId;
      if (twoFactorCode == null) {
        if (brand.usesTenantName) {
          final resolved = await RestaurantApi.resolveTenant(
            resolvedBaseUrl,
            tenancyName,
          );
          tenantId = resolved.$1;
        } else {
          tenantId = brand.fixedTenantId;
        }
        _pendingTenantId = tenantId;
        _pendingUserName = userName.trim();
        _pendingPassword = password;
      }

      final loginClient = AbpApiClient(
        baseUrl: resolvedBaseUrl,
        tenantId: tenantId,
      );
      final loginApi = RestaurantApi(loginClient);
      final auth = await loginApi.authenticate(
        userName: _pendingUserName.isEmpty ? userName : _pendingUserName,
        password: _pendingPassword.isEmpty ? password : _pendingPassword,
        twoFactorCode: twoFactorCode,
        twoFactorRememberClientToken: _twoFactorRememberClientToken,
      );

      if (auth.shouldResetPassword) {
        passwordResetCode = auth.resetCode;
        baseUrl = resolvedBaseUrl;
        _api?.close();
        _api = loginApi;
        noticeMessage =
            'A new password is required before this account can sign in.';
        return;
      }
      if (auth.requiresTwoFactor) {
        requiresTwoFactor = true;
        twoFactorProviders = auth.twoFactorProviders;
        _pendingTwoFactorUserId = auth.userId;
        _twoFactorRememberClientToken = auth.twoFactorRememberClientToken;
        baseUrl = resolvedBaseUrl;
        loginApi.close();
        noticeMessage =
            'Choose a configured provider, then enter its verification code.';
        return;
      }
      if (auth.accessToken.isEmpty) {
        throw const ApiException('The server did not return an access token.');
      }

      final stored = StoredSession(
        baseUrl: resolvedBaseUrl,
        accessToken: auth.accessToken,
        refreshToken: auth.refreshToken,
        tenantId: tenantId,
        tenancyName: brand.usesTenantName ? tenancyName.trim() : '',
        userName: userName.trim(),
        userId: auth.userId,
      );
      await _sessionStore.write(stored);
      loginApi.close();
      session = stored;
      baseUrl = resolvedBaseUrl;
      requiresTwoFactor = false;
      _pendingTwoFactorUserId = null;
      _attachAuthenticatedApi(stored);
      await _loadAuthenticatedData();
    } on ApiException catch (error) {
      errorMessage = error.details?.isNotEmpty == true
          ? '${error.message}\n${error.details}'
          : error.message;
      serverReachable = false;
      await _restoreLocalCartDraft();
    } on TimeoutException {
      errorMessage = 'The restaurant server did not respond in time.';
    } catch (error) {
      errorMessage = 'Unable to sign in: $error';
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  bool _sessionMatchesAppConfiguration(StoredSession stored) {
    final configuredBaseUrl = AppConfig.normalizeBaseUrl(
      AppConfig.defaultApiBaseUrl,
    );
    if (AppConfig.normalizeBaseUrl(stored.baseUrl) != configuredBaseUrl) {
      return false;
    }
    if (!brand.usesTenantName && stored.tenantId != brand.fixedTenantId) {
      return false;
    }
    return brand.configurationError == null;
  }

  Future<void> sendTwoFactorCode(String provider) async {
    final userId = _pendingTwoFactorUserId;
    if (!requiresTwoFactor || userId == null || busy) return;
    busy = true;
    errorMessage = null;
    noticeMessage = null;
    notifyListeners();
    final client = AbpApiClient(baseUrl: baseUrl, tenantId: _pendingTenantId);
    try {
      await RestaurantApi(
        client,
      ).sendTwoFactorCode(userId: userId, provider: provider);
      noticeMessage = 'Verification code sent through $provider.';
    } on ApiException catch (error) {
      errorMessage = error.message;
    } finally {
      client.close();
      busy = false;
      notifyListeners();
    }
  }

  void cancelTwoFactor() {
    requiresTwoFactor = false;
    twoFactorProviders = const [];
    _pendingTwoFactorUserId = null;
    _twoFactorRememberClientToken = null;
    _pendingTenantId = null;
    _pendingUserName = '';
    _pendingPassword = '';
    noticeMessage = null;
    errorMessage = null;
    notifyListeners();
  }

  Future<void> resetRequiredPassword(String newPassword) async {
    final code = passwordResetCode;
    final api = _api;
    if (code == null || api == null || busy) return;
    busy = true;
    errorMessage = null;
    notifyListeners();
    try {
      await api.resetPassword(resetCode: code, password: newPassword);
      passwordResetCode = null;
      noticeMessage = 'Password changed. Sign in with your new password.';
      _pendingPassword = '';
      api.close();
      _api = null;
    } on ApiException catch (error) {
      errorMessage = error.message;
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  void cancelPasswordReset() {
    passwordResetCode = null;
    _api?.close();
    _api = null;
    notifyListeners();
  }

  Future<void> logout() async {
    if (Platform.isAndroid) {
      try {
        await stopMobilePrintStation();
      } catch (_) {
        await AndroidPrintStationService.stop();
      }
    }
    await _sessionStore.clear();
    _detachSession();
    initialized = true;
    notifyListeners();
  }

  Future<void> refreshAll() async {
    if (_api == null || refreshing) return;
    refreshing = true;
    errorMessage = null;
    notifyListeners();
    try {
      final accessChanged = await _refreshGrantedPermissions();
      await _loadRestaurantData();
      _startKdsRefresh();
      noticeMessage = accessChanged
          ? 'Your access and restaurant data were refreshed.'
          : 'Restaurant data refreshed.';
    } on ApiException catch (error) {
      if (error.isUnauthorized) {
        await logout();
        return;
      }
      errorMessage = error.message;
    } finally {
      refreshing = false;
      notifyListeners();
    }
  }

  bool hasPermission(String permission) {
    return permissions.contains(permission);
  }

  static const reportCategoryPermissions = <String>{
    'Pages.Restaurant.Reports.Sales',
    'Pages.Restaurant.Reports.Operations',
    'Pages.Restaurant.Reports.Inventory',
    'Pages.Restaurant.Reports.Payroll',
    'Pages.Restaurant.Reports.AuditFinance',
  };

  bool canViewReportCategory(String permission) {
    final hasSpecificCategory = reportCategoryPermissions.any(hasPermission);
    return hasPermission('Pages.Restaurant.Reports') &&
        (!hasSpecificCategory || hasPermission(permission));
  }

  Future<void> syncAccess({bool background = false}) async {
    if (_api == null || !isAuthenticated || busy || refreshing) return;
    try {
      final changed = await _refreshGrantedPermissions();
      if (!changed) return;
      await _loadRestaurantData();
      _startKdsRefresh();
      noticeMessage = 'Your access was updated by an administrator.';
      notifyListeners();
    } on ApiException catch (error) {
      if (error.statusCode == 401) {
        await logout();
      } else if (!background) {
        errorMessage = error.message;
        notifyListeners();
      }
    } catch (error) {
      if (!background) {
        errorMessage = 'Could not refresh app access: $error';
        notifyListeners();
      }
    }
  }

  @visibleForTesting
  void setGrantedPermissionsForTesting(Set<String> grantedPermissions) {
    _applyGrantedPermissions(grantedPermissions);
    notifyListeners();
  }

  List<ErpModule> get visibleModules {
    return modules.where((module) {
      return switch (module.kind) {
        ModuleKind.dashboard => true,
        ModuleKind.pos => hasPermission('Pages.Restaurant.Pos'),
        ModuleKind.kds => hasPermission('Pages.Restaurant.Kds'),
        ModuleKind.restaurantInventory => hasPermission(
          'Pages.Restaurant.Inventory',
        ),
        ModuleKind.menuRecipes => hasPermission('Pages.Restaurant.Menu'),
        ModuleKind.channels => hasPermission('Pages.Restaurant.Channels'),
        // Setup is the read/access permission for this module. Mutating setup
        // data is gated separately by Setup.Create, Setup.Edit and Setup.Delete
        // in the screen and by ASP.NET Zero on the corresponding API methods.
        ModuleKind.restaurantSetup => hasPermission('Pages.Restaurant.Setup'),
        ModuleKind.restaurantReports => hasPermission(
          'Pages.Restaurant.Reports',
        ),
        ModuleKind.restaurantPayroll => hasPermission(
          'Pages.Restaurant.Payroll',
        ),
        ModuleKind.guestOrders => hasPermission('Pages.Restaurant.Pos'),
        ModuleKind.reservations => hasPermission(
          'Pages.Restaurant.Reservations',
        ),
        ModuleKind.printQueue => hasPermission('Pages.Restaurant.PrinterSetup'),
        _ => false,
      };
    }).toList();
  }

  List<PosContextOption> get posContexts => [
    for (final table in tables)
      PosContextOption(
        value: table.id,
        label: table.displayName,
        icon: table.occupied
            ? Icons.people_alt_rounded
            : Icons.table_bar_rounded,
      ),
    const PosContextOption(
      value: 'mode:takeaway',
      label: 'Takeaway',
      icon: Icons.shopping_bag_rounded,
    ),
    const PosContextOption(
      value: 'mode:delivery',
      label: 'Delivery',
      icon: Icons.delivery_dining_rounded,
    ),
  ];

  String get selectedPosLabel {
    final matching = posContexts.where(
      (option) => option.value == selectedPosContext,
    );
    return matching.isEmpty ? 'Takeaway' : matching.first.label;
  }

  int get currentOrderType => switch (selectedPosContext) {
    'mode:takeaway' => 1,
    'mode:delivery' => 2,
    _ => 0,
  };

  String? get currentTableId =>
      selectedPosContext.startsWith('mode:') ? null : selectedPosContext;

  LedgerOption? get defaultAccountLedger {
    if (accountLedgers.isEmpty) return null;
    final exact = accountLedgers.where(
      (item) => item.name.trim().toLowerCase() == 'cash',
    );
    if (exact.isNotEmpty) return exact.first;
    final contains = accountLedgers.where(
      (item) => item.name.toLowerCase().contains('cash'),
    );
    return contains.isNotEmpty ? contains.first : accountLedgers.first;
  }

  LedgerOption? get defaultSalesLedger {
    if (salesLedgers.isEmpty) return null;
    final matches = salesLedgers.where(
      (item) => item.name.toLowerCase().contains('sales'),
    );
    return matches.isNotEmpty ? matches.first : salesLedgers.first;
  }

  void selectPosContext(String value) {
    if (selectedPosContext == value) return;
    selectedPosContext = value;
    final matchingOrder = openOrders.where((order) {
      if (value.startsWith('mode:')) {
        return order.tableId == null && order.orderType == currentOrderType;
      }
      return order.tableId == value;
    });
    if (matchingOrder.isEmpty) {
      currentOrderId = null;
      currentOrderVersion = null;
      cart = [];
      currentOrderTickets = [];
    } else {
      final order = matchingOrder.first;
      currentOrderId = order.id;
      currentOrderVersion = order.rowVersion;
      cart = List<CartLine>.from(order.items);
      currentOrderTickets = tickets
          .where((ticket) => ticket.orderId == order.id)
          .toList();
    }
    orderDirty = false;
    unawaited(_persistLocalCartDraft(synced: true));
    notifyListeners();
  }

  void loadOpenOrder(RestaurantOrderModel order) {
    currentOrderId = order.id;
    currentOrderVersion = order.rowVersion;
    selectedPosContext =
        order.tableId ??
        switch (order.orderType) {
          2 => 'mode:delivery',
          _ => 'mode:takeaway',
        };
    cart = List<CartLine>.from(order.items);
    currentOrderTickets = tickets
        .where((ticket) => ticket.orderId == order.id)
        .toList();
    orderDirty = false;
    unawaited(_persistLocalCartDraft(synced: true));
    notifyListeners();
  }

  void selectCategory(String value) {
    selectedCategory = value;
    notifyListeners();
  }

  void selectKdsStation(String value) {
    selectedKdsStation = value;
    notifyListeners();
  }

  void addToCart(MenuProduct product, ProductConfiguration configuration) {
    if (!product.available) return;
    final candidate = CartLine(
      product: product,
      qty: 1,
      variant: configuration.variant,
      modifiers: configuration.modifiers,
    );
    final index = cart.indexWhere(
      (line) =>
          line.editable && line.configurationKey == candidate.configurationKey,
    );
    if (index < 0) {
      cart = [...cart, candidate];
    } else {
      final next = List<CartLine>.from(cart);
      next[index] = next[index].copyWith(qty: next[index].qty + 1);
      cart = next;
    }
    orderDirty = true;
    draftNeedsReview = false;
    unawaited(_persistLocalCartDraft());
    notifyListeners();
  }

  void updateCartQuantity(CartLine line, int delta) {
    if (!line.editable) return;
    final index = cart.indexOf(line);
    if (index < 0) return;
    final next = List<CartLine>.from(cart);
    final qty = line.qty + delta;
    if (qty <= 0) {
      next.removeAt(index);
    } else {
      next[index] = line.copyWith(qty: qty);
    }
    cart = next;
    orderDirty = true;
    draftNeedsReview = false;
    unawaited(_persistLocalCartDraft());
    notifyListeners();
  }

  Future<void> sendCurrentOrder({
    String customerName = '',
    String customerPhone = '',
  }) async {
    if (cart.isEmpty) throw const ApiException('Add at least one menu item.');
    if (draftNeedsReview) {
      throw const ApiException(
        'Review the restored order against current prices and availability before sending it.',
      );
    }
    if (_demoMode) {
      _demoSendToKitchen();
      return;
    }
    await _runBusy(() async {
      final id = await _saveCurrentOrder(
        customerName: customerName,
        customerPhone: customerPhone,
      );
      final kitchenRequestId = await _stableOperationRequestId(
        'kitchen-send',
        jsonEncode({'orderId': id, 'expectedOrderVersion': currentOrderVersion}),
      );
      await _api!.sendToKitchen(
        id,
        clientRequestId: kitchenRequestId,
        expectedOrderVersion: currentOrderVersion,
      );
      await _sessionStore.deleteValue(_orderOperationKey('kitchen-send'));
      noticeMessage = 'KOT/BOT sent to the kitchen.';
      await _refreshOrdersAndTickets();
      final updated = openOrders.where((order) => order.id == id);
      if (updated.isNotEmpty) {
        currentOrderId = id;
        currentOrderVersion = updated.first.rowVersion;
        cart = List<CartLine>.from(updated.first.items);
      }
      orderDirty = false;
      draftNeedsReview = false;
      await _persistLocalCartDraft(synced: true);
    });
  }

  Future<BillingResult> settle(
    PosCheckoutDraft draft, {
    bool confirmNegativeStock = false,
  }) async {
    if (!_demoMode) {
      throw const ApiException(
        'Final billing is completed in the browser cashier. Android is for waiter and kitchen work.',
      );
    }
    if (cart.isEmpty) throw const ApiException('Add at least one menu item.');
    if (draft.ledgerId.isEmpty || draft.salesAccountId.isEmpty) {
      throw const ApiException(
        'Customer ledger and sales account are required for billing.',
      );
    }
    if (_demoMode) {
      final result = BillingResult(
        orderNo: 'DEMO-${DateTime.now().millisecondsSinceEpoch}',
        salesMasterId: 'demo-sale',
        payable: cart.fold(0, (sum, line) => sum + line.total),
        returnAmount: 0,
      );
      cart = [];
      currentOrderId = null;
      noticeMessage = 'Demo bill settled.';
      notifyListeners();
      return result;
    }

    late BillingResult result;
    await _runBusy(() async {
      final id = await _saveCurrentOrder(
        customerName: draft.customerName,
        customerPhone: draft.customerPhone,
      );
      if (draft.discount > 0) {
        await _api!.applyDiscount(
          id,
          draft.discount,
          approvalPin: draft.approvalPin,
        );
      }
      final order = await _api!.getOrder(id, products);
      final validation = await _api!.validateStock(
        id,
        billLines: draft.billLines,
      );
      final shortages = validation.items
          .where((item) => !item.available)
          .toList();
      if (shortages.isNotEmpty && !confirmNegativeStock) {
        currentOrderId = id;
        cart = List<CartLine>.from(order.items);
        throw StockShortageException(shortages);
      }
      result = await _api!.finalizeBill(
        orderId: id,
        dateMiti: NepaliDateFormat('yyyy-MM-dd').format(NepaliDateTime.now()),
        salesAccountId: draft.salesAccountId,
        ledgerId: draft.ledgerId,
        customerName: draft.customerName,
        customerPhone: draft.customerPhone,
        paymentMethod: draft.paymentMethod,
        paymentLedgerId: draft.paymentLedgerId,
        tipAmount: draft.tipAmount,
        customerPaidAmount:
            draft.customerPaidAmount ?? (draft.paymentMethod == 2 ? 0 : null),
        confirmNegativeStock: confirmNegativeStock,
        billLines: draft.billLines,
      );
      noticeMessage = 'Bill ${result.orderNo} posted successfully.';
      await _refreshOrdersAndTickets();
      if (result.isFullyBilled) {
        cart = [];
        currentOrderId = null;
        currentOrderTickets = [];
      } else {
        final remaining = openOrders.where((item) => item.id == id);
        if (remaining.isNotEmpty) {
          currentOrderId = id;
          cart = List<CartLine>.from(remaining.first.items);
        }
      }
      orderDirty = false;
      if (hasPermission('Pages.Restaurant.Reports')) {
        report = await _api!.getReportSummary();
      }
    });
    return result;
  }

  Future<void> advanceTicket(KdsTicket ticket) async {
    if (_demoMode) {
      final next = switch (ticket.status) {
        TicketStatus.queued => TicketStatus.preparing,
        TicketStatus.acknowledged => TicketStatus.preparing,
        TicketStatus.preparing => TicketStatus.ready,
        TicketStatus.ready => TicketStatus.bumped,
        TicketStatus.bumped => TicketStatus.bumped,
      };
      final index = tickets.indexOf(ticket);
      if (index >= 0) {
        final values = List<KdsTicket>.from(tickets);
        values[index] = ticket.copyWith(status: next);
        tickets = values;
        notifyListeners();
      }
      return;
    }
    final serverId = ticket.serverId;
    if (serverId == null) return;
    final nextStatus = switch (ticket.status) {
      TicketStatus.queued || TicketStatus.acknowledged => 1,
      TicketStatus.preparing => 2,
      TicketStatus.ready => 3,
      TicketStatus.bumped => 3,
    };
    final orderId = ticket.orderId;
    final orderVersion = ticket.orderRowVersion;
    if (orderId == null || orderVersion == null || orderVersion.isEmpty) {
      throw const ApiException('The kitchen ticket has no current order version. Refresh the KDS.');
    }
    await _runBusy(() async {
      await _runVersionedKitchenMutation(
        operationType: 'KdsTicketStatus',
        actionKey: serverId,
        expectedOrderVersions: {orderId: orderVersion},
        signature: {'ticketId': serverId, 'status': nextStatus, 'cancelReason': ''},
        send: (requestId, versions) => _api!.updateTicketStatus(
          serverId,
          nextStatus,
          clientRequestId: requestId,
          expectedOrderVersion: versions[orderId],
        ),
      );
      tickets = await _api!.getOpenTickets(products);
      noticeMessage = 'Ticket ${ticket.id} updated.';
    });
  }

  Future<void> updateTicketItem(
    KdsTicket ticket,
    CartLine line,
    int status, {
    String reason = '',
  }) async {
    final itemId = line.ticketItemId;
    if (itemId == null || itemId.isEmpty) {
      throw const ApiException('Kitchen ticket item is not available.');
    }
    if (_demoMode) {
      final ticketIndex = tickets.indexOf(ticket);
      if (ticketIndex >= 0) {
        final nextTickets = List<KdsTicket>.from(tickets);
        final nextItems = List<CartLine>.from(ticket.items);
        final lineIndex = nextItems.indexOf(line);
        if (lineIndex >= 0) {
          nextItems[lineIndex] = line.copyWith(status: status);
          nextTickets[ticketIndex] = ticket.copyWith(items: nextItems);
          tickets = nextTickets;
          notifyListeners();
        }
      }
      return;
    }
    final orderId = ticket.orderId;
    final orderVersion = ticket.orderRowVersion;
    if (orderId == null || orderVersion == null || orderVersion.isEmpty) {
      throw const ApiException('The kitchen ticket has no current order version. Refresh the KDS.');
    }
    await _runBusy(() async {
      await _runVersionedKitchenMutation(
        operationType: 'KdsTicketItemStatus',
        actionKey: itemId,
        expectedOrderVersions: {orderId: orderVersion},
        signature: {
          'ticketItemId': itemId,
          'status': status,
          'cancelReason': reason,
        },
        send: (requestId, versions) => _api!.updateTicketItemStatus(
          ticketItemId: itemId,
          status: status,
          reason: reason,
          clientRequestId: requestId,
          expectedOrderVersion: versions[orderId],
        ),
      );
      tickets = await _api!.getOpenTickets(products);
      noticeMessage = 'Kitchen item updated.';
    });
  }

  Future<void> cancelTicket(KdsTicket ticket, String reason) async {
    final ticketId = ticket.serverId;
    if (ticketId == null || ticketId.isEmpty) return;
    if (_demoMode) {
      tickets = tickets.where((item) => item != ticket).toList();
      notifyListeners();
      return;
    }
    final orderId = ticket.orderId;
    final orderVersion = ticket.orderRowVersion;
    if (orderId == null || orderVersion == null || orderVersion.isEmpty) {
      throw const ApiException('The kitchen ticket has no current order version. Refresh the KDS.');
    }
    await _runBusy(() async {
      await _runVersionedKitchenMutation(
        operationType: 'KdsTicketStatus',
        actionKey: ticketId,
        expectedOrderVersions: {orderId: orderVersion},
        signature: {'ticketId': ticketId, 'status': 4, 'cancelReason': reason},
        send: (requestId, versions) => _api!.updateTicketStatus(
          ticketId,
          4,
          reason: reason,
          clientRequestId: requestId,
          expectedOrderVersion: versions[orderId],
        ),
      );
      tickets = await _api!.getOpenTickets(products);
      noticeMessage = 'Ticket ${ticket.id} cancelled: $reason';
    });
  }

  Future<void> voidCartLine(
    CartLine line, {
    required String reason,
    String? approvalPin,
  }) async {
    final itemId = line.orderItemId;
    if (itemId == null || itemId.isEmpty || _demoMode) {
      cart = cart.where((item) => item != line).toList();
      orderDirty = true;
      draftNeedsReview = false;
      unawaited(_persistLocalCartDraft());
      notifyListeners();
      return;
    }
    final orderId = currentOrderId;
    final orderVersion = currentOrderVersion;
    if (orderId == null || orderVersion == null || orderVersion.isEmpty) {
      throw const ApiException('The order version is unavailable. Reload the order before voiding an item.');
    }
    await _runBusy(() async {
      await _runVersionedKitchenMutation(
        operationType: 'OrderVoidItem',
        actionKey: '$itemId:void',
        expectedOrderVersions: {orderId: orderVersion},
        signature: {'orderItemId': itemId, 'reason': reason},
        send: (requestId, versions) => _api!.voidOrderItem(
          orderItemId: itemId,
          reason: reason,
          approvalPin: approvalPin,
          clientRequestId: requestId,
          expectedOrderVersion: versions[orderId],
        ),
      );
      await _reloadCurrentOrder();
      noticeMessage = '${line.product.name} voided.';
    });
  }

  Future<void> transferCurrentOrder(String newTableId) async {
    final orderId = currentOrderId;
    if (orderId == null) {
      throw const ApiException('Save the order before transferring its table.');
    }
    if (_demoMode) {
      selectedPosContext = newTableId;
      noticeMessage = 'Demo order transferred.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.transferTable(orderId, newTableId);
      await _refreshOrdersAndTickets();
      final updated = openOrders.where((item) => item.id == orderId);
      if (updated.isNotEmpty) loadOpenOrder(updated.first);
      noticeMessage = 'Order transferred to the selected table.';
    });
  }

  Future<String> splitCurrentOrder({
    required List<String> orderItemIds,
    String? newTableId,
  }) async {
    final orderId = currentOrderId;
    if (orderId == null || orderItemIds.isEmpty) {
      throw const ApiException('Select saved order items to split.');
    }
    if (_demoMode) {
      noticeMessage = 'Demo split order created.';
      notifyListeners();
      return 'demo-split';
    }
    var newOrderId = '';
    await _runBusy(() async {
      newOrderId = await _api!.splitOrder(
        sourceOrderId: orderId,
        newTableId: newTableId,
        orderItemIds: orderItemIds,
      );
      await _refreshOrdersAndTickets();
      await _reloadCurrentOrder();
      noticeMessage = 'Split order created.';
    });
    return newOrderId;
  }

  Future<void> mergeIntoCurrentOrder(List<String> sourceOrderIds) async {
    final targetId = currentOrderId;
    if (targetId == null || sourceOrderIds.isEmpty) {
      throw const ApiException('Choose at least one order to merge.');
    }
    if (_demoMode) {
      noticeMessage = 'Demo orders merged.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.mergeOrders(
        targetOrderId: targetId,
        sourceOrderIds: sourceOrderIds,
      );
      await _refreshOrdersAndTickets();
      await _reloadCurrentOrder();
      noticeMessage = 'Orders merged successfully.';
    });
  }

  Future<List<KdsTicket>> loadCurrentOrderTickets() async {
    final orderId = currentOrderId;
    if (orderId == null) return const [];
    if (_demoMode) {
      currentOrderTickets = tickets
          .where((ticket) => ticket.orderId == orderId)
          .toList();
      return currentOrderTickets;
    }
    currentOrderTickets = await _api!.getTicketsForOrder(orderId, products);
    notifyListeners();
    return currentOrderTickets;
  }

  Future<KdsTicket> reprintTicket(
    KdsTicket ticket, {
    required String reason,
    String? approvalPin,
  }) async {
    if (_demoMode) return ticket;
    final ticketId = ticket.serverId;
    if (ticketId == null) {
      throw const ApiException('Ticket is unavailable for reprint.');
    }
    late KdsTicket printable;
    await _runBusy(() async {
      printable = await _api!.reprintTicket(
        ticketId,
        products,
        approvalPin: approvalPin,
        reason: reason,
      );
      currentOrderTickets = await _api!.getTicketsForOrder(
        currentOrderId!,
        products,
      );
      noticeMessage = 'Ticket ${ticket.id} queued for reprint.';
    });
    return printable;
  }

  Future<void> setMenuAvailability(
    MenuProduct product,
    bool available, {
    DateTime? unavailableUntil,
  }) async {
    if (_demoMode) {
      final index = products.indexOf(product);
      if (index >= 0) {
        final updated = List<MenuProduct>.from(products);
        updated[index] = _copyMenuAvailability(
          product,
          available,
          unavailableUntil,
        );
        products = updated;
        notifyListeners();
      }
      return;
    }
    await _runBusy(() async {
      await _api!.setItemAvailability(
        menuItemId: product.id,
        isAvailable: available,
        unavailableUntil: unavailableUntil,
      );
      products = await _api!.getPosMenu();
      noticeMessage = '${product.name} availability updated.';
    });
  }

  Future<Map<String, dynamic>> getMenuRecipeDetails(MenuProduct product) async {
    if (_demoMode) {
      return {
        'lines': [
          for (final ingredient in product.recipe)
            {'rawMaterialName': ingredient, 'qty': 1, 'unitName': 'unit'},
        ],
        'cost': {
          'estimatedCost': product.cost,
          'menuPrice': product.price,
          'foodCostPercent': product.price == 0
              ? 0
              : product.cost / product.price * 100,
        },
      };
    }
    final values = await Future.wait([
      _api!.getRecipe(product.productId),
      _api!.getRecipeCost(product.productId),
    ]);
    return {'lines': values[0], 'cost': values[1]};
  }

  Future<int> generatePurchaseOrders() async {
    final productIds = inventory
        .where((item) => !item.missingSupplierMapping && item.suggested > 0)
        .map((item) => item.productId)
        .where((id) => id.isNotEmpty)
        .toList();
    if (productIds.isEmpty) {
      throw const ApiException('No mapped low-stock items are ready to order.');
    }
    if (_demoMode) return 1;
    var count = 0;
    await _runBusy(() async {
      count = await _api!.generateDraftPurchaseOrders(productIds);
      inventory = await _api!.getLowStock();
      noticeMessage = '$count draft purchase order(s) created.';
    });
    return count;
  }

  Future<void> saveStockAdjustment({
    required int adjustmentType,
    required String productId,
    required String unitId,
    required double qty,
    required double? countedQty,
    required double rate,
    required String reason,
    required String description,
  }) async {
    if (_demoMode) {
      noticeMessage = 'Demo stock adjustment saved.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.createStockAdjustment(
        adjustmentType: adjustmentType,
        dateMiti: NepaliDateFormat('yyyy-MM-dd').format(NepaliDateTime.now()),
        description: description,
        productId: productId,
        unitId: unitId,
        qty: qty,
        countedQty: countedQty,
        rate: rate,
        reason: reason,
      );
      final adjustmentFuture = _api!.getStockAdjustments();
      final lowStockFuture = _api!.getLowStock();
      stockAdjustments = await adjustmentFuture;
      inventory = await lowStockFuture;
      noticeMessage = 'Stock adjustment saved.';
    });
  }

  Future<void> saveSupplierMapping({
    String? id,
    required String productId,
    required String supplierLedgerId,
    required String supplierSku,
    required String unitId,
    required double rate,
    required int leadTimeDays,
    required double minimumOrderQty,
    required bool isPreferred,
    required bool isActive,
  }) async {
    if (_demoMode) {
      noticeMessage = 'Demo supplier mapping saved.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.saveSupplierMapping(
        id: id,
        productId: productId,
        supplierLedgerId: supplierLedgerId,
        supplierSku: supplierSku,
        unitId: unitId,
        rate: rate,
        leadTimeDays: leadTimeDays,
        minimumOrderQty: minimumOrderQty,
        isPreferred: isPreferred,
        isActive: isActive,
      );
      supplierMappings = await _api!.getSupplierMappings();
      noticeMessage = 'Supplier mapping saved.';
    });
  }

  Future<void> deleteSupplierMapping(String id) async {
    if (_demoMode) {
      supplierMappings = supplierMappings
          .where((item) => item.id != id)
          .toList();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.deleteSupplierMapping(id);
      supplierMappings = await _api!.getSupplierMappings();
      noticeMessage = 'Supplier mapping deleted.';
    });
  }

  Future<void> acceptAggregatorOrder(
    RestaurantAggregatorOrderModel order,
    String menuItemId,
    double qty,
  ) async {
    if (_demoMode) {
      noticeMessage = 'Demo aggregator order accepted.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.acceptAggregatorOrder(
        aggregatorOrderId: order.id,
        menuItemId: menuItemId,
        qty: qty,
      );
      aggregatorOrders = await _api!.getAggregatorOrders();
      await _refreshOrdersAndTickets();
      noticeMessage = 'Aggregator order accepted and sent to kitchen.';
    });
  }

  Future<void> updateAggregatorStatus(
    RestaurantAggregatorOrderModel order,
    int status,
    String message,
  ) async {
    if (_demoMode) {
      noticeMessage = 'Demo aggregator status updated.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.updateAggregatorOrderStatus(
        aggregatorOrderId: order.id,
        status: status,
        message: message,
      );
      aggregatorOrders = await _api!.getAggregatorOrders();
      noticeMessage = 'Aggregator order status updated.';
    });
  }

  Future<void> publishChannelMenu(ChannelStatus channel) async {
    if (_demoMode) {
      noticeMessage = 'Demo menu publish queued.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.queueMenuPublish(channel.id);
      noticeMessage = '${channel.name} menu publish queued.';
    });
  }

  Future<void> refreshReports(DateTime from, DateTime to) async {
    if (_demoMode) return;
    await _runBusy(() async {
      reportBundle = await _api!.getReportBundle(
        fromDate: from,
        toDate: to,
        grantedPermissions: permissions,
      );
      report = reportBundle.summary;
      noticeMessage = 'Restaurant reports refreshed.';
    });
  }

  Future<void> saveArea({
    String? id,
    required String name,
    String description = '',
    int sortOrder = 0,
    bool isActive = true,
  }) async {
    if (_demoMode) {
      final updated = RestaurantAreaModel(
        id: id ?? 'area-${areas.length + 1}',
        name: name,
        description: description,
        sortOrder: sortOrder,
        isActive: isActive,
      );
      areas = id == null
          ? [...areas, updated]
          : areas.map((item) => item.id == id ? updated : item).toList();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.saveArea(
        id: id,
        name: name,
        description: description,
        sortOrder: sortOrder,
        isActive: isActive,
      );
      areas = await _api!.getAreas();
      noticeMessage = 'Restaurant area saved.';
    });
  }

  Future<void> deleteArea(String id) async {
    if (_demoMode) {
      areas = areas.where((item) => item.id != id).toList();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.deleteArea(id);
      areas = await _api!.getAreas();
      noticeMessage = 'Restaurant area deleted.';
    });
  }

  Future<void> saveTable({
    String? id,
    required String name,
    required String code,
    required int capacity,
    required int sortOrder,
    required int status,
    required bool isActive,
    required String areaId,
  }) async {
    if (_demoMode) {
      final area = areas.firstWhere((item) => item.id == areaId);
      final updated = RestaurantTableModel(
        id: id ?? 'table-${tables.length + 1}',
        name: name,
        code: code,
        areaId: areaId,
        areaName: area.name,
        capacity: capacity,
        status: status,
        sortOrder: sortOrder,
        isActive: isActive,
      );
      tables = id == null
          ? [...tables, updated]
          : tables.map((item) => item.id == id ? updated : item).toList();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.saveTable(
        id: id,
        name: name,
        code: code,
        capacity: capacity,
        sortOrder: sortOrder,
        status: status,
        isActive: isActive,
        areaId: areaId,
      );
      tables = await _api!.getTables();
      noticeMessage = 'Restaurant table saved.';
    });
  }

  Future<void> saveStation({
    String? id,
    required String name,
    required int stationType,
    required bool isActive,
  }) async {
    if (_demoMode) {
      final updated = RestaurantStationModel(
        id: id ?? 'station-${stations.length + 1}',
        name: name,
        type: stationType,
        isActive: isActive,
      );
      stations = id == null
          ? [...stations, updated]
          : stations.map((item) => item.id == id ? updated : item).toList();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.saveStation(
        id: id,
        name: name,
        stationType: stationType,
        isActive: isActive,
      );
      stations = await _api!.getStations();
      noticeMessage = 'Restaurant station saved.';
    });
  }

  Future<void> saveOperationalSettings(Map<String, dynamic> settings) async {
    if (_demoMode) {
      operationalSettings = Map<String, dynamic>.from(settings);
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.updateOperationalSettings(settings);
      operationalSettings = await _api!.getOperationalSettings();
      noticeMessage = 'Operational settings saved.';
    });
  }

  Future<void> saveChannel({
    String? id,
    required String name,
    required int channelType,
    required int provider,
    required double commissionPercent,
    required double priceMarkupPercent,
    required int sortOrder,
    required bool isOnline,
    required bool isActive,
  }) async {
    if (_demoMode) {
      final updated = ChannelStatus(
        id: id ?? 'channel-${channels.length + 1}',
        name: name,
        provider: _demoProviderName(provider),
        providerCode: provider,
        channelType: channelType,
        orders: 0,
        sales: 0,
        commission: commissionPercent,
        priceMarkupPercent: priceMarkupPercent,
        sortOrder: sortOrder,
        online: isOnline,
        isActive: isActive,
        sync: 'Not synced',
      );
      channels = id == null
          ? [...channels, updated]
          : channels.map((item) => item.id == id ? updated : item).toList();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.saveChannel(
        id: id,
        name: name,
        channelType: channelType,
        provider: provider,
        commissionPercent: commissionPercent,
        priceMarkupPercent: priceMarkupPercent,
        sortOrder: sortOrder,
        isOnline: isOnline,
        isActive: isActive,
      );
      channels = await _api!.getChannels();
      noticeMessage = 'Restaurant channel saved.';
    });
  }

  Future<void> refreshPayroll() async {
    if (_demoMode) {
      _seedDemoPayroll();
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _loadPayrollData();
      noticeMessage = 'Payroll refreshed.';
    });
  }

  Future<PayrollEmployeeSaveResult> savePayrollEmployee({
    PayrollEmployee? employee,
    int? userId,
    required bool updateLoginAccess,
    required EmployeeLoginMode loginMode,
    required String loginUserName,
    required String loginEmailAddress,
    required String loginPhoneNumber,
    required String restaurantRoleName,
    required bool loginIsActive,
    required String staffCode,
    required String name,
    required String jobRole,
    required String department,
    required PayrollEmploymentType employmentType,
    required double basicSalary,
    required double hourlyRate,
    required double overtimeRate,
    required double fixedAllowance,
    required double fixedDeduction,
    required double serviceChargeWeight,
    required String bankAccountNumber,
    required String panNumber,
    required String ssfNumber,
    required DateTime joinedOn,
    required bool isActive,
  }) async {
    if (_demoMode) {
      noticeMessage = employee == null
          ? 'Demo staff member added.'
          : 'Demo staff member updated.';
      notifyListeners();
      return PayrollEmployeeSaveResult(
        employeeId: employee?.id ?? 'demo-new-employee',
        userId: loginMode == EmployeeLoginMode.none ? null : (userId ?? 99),
        userName: loginMode == EmployeeLoginMode.createNew ? loginUserName : '',
        temporaryPassword: loginMode == EmployeeLoginMode.createNew
            ? 'Demo@1234'
            : '',
      );
    }
    var result = const PayrollEmployeeSaveResult(employeeId: '');
    await _runBusy(() async {
      result = await _api!.savePayrollEmployee(
        id: employee?.id,
        userId: userId,
        updateLoginAccess: updateLoginAccess,
        loginMode: loginMode,
        loginUserName: loginUserName,
        loginEmailAddress: loginEmailAddress,
        loginPhoneNumber: loginPhoneNumber,
        restaurantRoleName: restaurantRoleName,
        loginIsActive: loginIsActive,
        staffCode: staffCode,
        name: name,
        jobRole: jobRole,
        department: department,
        employmentType: employmentType,
        basicSalary: basicSalary,
        hourlyRate: hourlyRate,
        overtimeRate: overtimeRate,
        fixedAllowance: fixedAllowance,
        fixedDeduction: fixedDeduction,
        serviceChargeWeight: serviceChargeWeight,
        bankAccountNumber: bankAccountNumber,
        panNumber: panNumber,
        ssfNumber: ssfNumber,
        joinedOn: joinedOn,
        isActive: isActive,
      );
      await _loadPayrollData();
      noticeMessage = employee == null
          ? 'Payroll employee added.'
          : 'Payroll employee updated.';
    });
    return result;
  }

  Future<void> payrollClockIn({
    String? employeeId,
    required String shiftName,
  }) async {
    if (_demoMode) {
      noticeMessage = 'Demo shift started.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.payrollClockIn(employeeId: employeeId, shiftName: shiftName);
      await _loadPayrollData();
      noticeMessage = 'Shift started.';
    });
  }

  Future<void> payrollClockOut({
    String? employeeId,
    int breakMinutes = 0,
  }) async {
    if (_demoMode) {
      noticeMessage = 'Demo shift completed.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.payrollClockOut(
        employeeId: employeeId,
        breakMinutes: breakMinutes,
      );
      await _loadPayrollData();
      noticeMessage = 'Shift completed.';
    });
  }

  Future<void> savePayrollAttendance({
    PayrollAttendance? attendance,
    required String employeeId,
    required DateTime workDate,
    DateTime? clockIn,
    DateTime? clockOut,
    required int breakMinutes,
    double? regularHours,
    double? overtimeHours,
    required PayrollAttendanceStatus status,
    required String shiftName,
    required String notes,
  }) async {
    if (_demoMode) {
      noticeMessage = 'Demo attendance saved.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.savePayrollAttendance(
        id: attendance?.id,
        employeeId: employeeId,
        workDate: workDate,
        clockIn: clockIn,
        clockOut: clockOut,
        breakMinutes: breakMinutes,
        regularHours: regularHours,
        overtimeHours: overtimeHours,
        status: status,
        shiftName: shiftName,
        notes: notes,
      );
      await _loadPayrollData();
      noticeMessage = 'Attendance saved.';
    });
  }

  Future<String> generatePayrollRun({
    required DateTime periodStart,
    required DateTime periodEnd,
    required double tipsPool,
    required double serviceChargePool,
    required String notes,
  }) async {
    if (_demoMode) {
      noticeMessage = 'Demo payroll generated.';
      notifyListeners();
      return 'demo-payroll';
    }
    var id = '';
    await _runBusy(() async {
      id = await _api!.generatePayrollRun(
        periodStart: periodStart,
        periodEnd: periodEnd,
        tipsPool: tipsPool,
        serviceChargePool: serviceChargePool,
        notes: notes,
      );
      await _loadPayrollData();
      noticeMessage = 'Draft payroll generated.';
    });
    return id;
  }

  Future<PayrollRunDetail> getPayrollRunDetail(String id) async {
    if (_demoMode) {
      final run = payrollRuns.firstWhere(
        (item) => item.id == id,
        orElse: () => payrollRuns.first,
      );
      return PayrollRunDetail(run: run, lines: myPayslips);
    }
    return _api!.getPayrollRun(id);
  }

  Future<void> approvePayrollRun(String id) async {
    await _payrollRunAction(
      demoMessage: 'Demo payroll approved.',
      liveMessage: 'Payroll approved.',
      action: () => _api!.approvePayrollRun(id),
    );
  }

  Future<void> markPayrollRunPaid(String id) async {
    await _payrollRunAction(
      demoMessage: 'Demo payroll marked paid.',
      liveMessage: 'Payroll marked paid.',
      action: () => _api!.markPayrollRunPaid(id),
    );
  }

  Future<void> deleteDraftPayrollRun(String id) async {
    await _payrollRunAction(
      demoMessage: 'Demo draft payroll removed.',
      liveMessage: 'Draft payroll removed.',
      action: () => _api!.deleteDraftPayrollRun(id),
    );
  }

  Future<void> _payrollRunAction({
    required String demoMessage,
    required String liveMessage,
    required Future<void> Function() action,
  }) async {
    if (_demoMode) {
      noticeMessage = demoMessage;
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await action();
      await _loadPayrollData();
      noticeMessage = liveMessage;
    });
  }

  Future<void> _loadPayrollData() async {
    if (_demoMode) {
      _seedDemoPayroll();
      return;
    }
    final api = _api!;
    payrollDashboard = await api.getPayrollDashboard();
    payrollEmployees = await api.getPayrollEmployees(
      includeInactive: hasPermission('Pages.Restaurant.Payroll.Staff'),
    );
    if (hasPermission('Pages.Restaurant.Payroll.Staff.Access')) {
      final accessOptions = await api.getPayrollStaffAccessOptions();
      payrollUserOptions = accessOptions.availableUsers;
      payrollRoleOptions = accessOptions.roles;
    } else {
      payrollUserOptions = [];
      payrollRoleOptions = [];
    }
    if (hasPermission('Pages.Restaurant.Payroll.Attendance')) {
      final today = DateTime.now();
      payrollAttendance = await api.getPayrollAttendance(
        from: DateTime(today.year, today.month, 1),
        to: today,
      );
    } else {
      payrollAttendance = [];
    }
    if (hasPermission('Pages.Restaurant.Payroll.Reports') ||
        hasPermission('Pages.Restaurant.Payroll.Process') ||
        hasPermission('Pages.Restaurant.Payroll.Approve')) {
      payrollRuns = await api.getPayrollRuns();
    } else {
      payrollRuns = [];
    }
    if (hasPermission('Pages.Restaurant.Payroll.OwnPayslip')) {
      myPayslips = await api.getMyPayslips();
    } else {
      myPayslips = [];
    }
  }

  void _seedDemoPayroll() {
    final employee = PayrollEmployee(
      id: 'demo-employee',
      userId: 1,
      loginUserName: 'demo.manager',
      loginEmailAddress: 'manager@example.com',
      restaurantRoleName: 'RestaurantManager',
      loginIsActive: true,
      staffCode: 'MGR-001',
      name: 'Demo Manager',
      jobRole: 'Restaurant Manager',
      department: 'Management',
      employmentType: PayrollEmploymentType.monthly,
      basicSalary: 55000,
      hourlyRate: 0,
      overtimeRate: 450,
      fixedAllowance: 3000,
      fixedDeduction: 500,
      serviceChargeWeight: 1.5,
      joinedOn: DateTime(2025, 1, 1),
      isActive: true,
    );
    final attendance = PayrollAttendance(
      id: 'demo-attendance',
      employeeId: employee.id,
      employeeName: employee.name,
      staffCode: employee.staffCode,
      workDate: DateTime.now(),
      clockIn: DateTime.now().subtract(const Duration(hours: 4)),
      breakMinutes: 30,
      regularHours: 0,
      overtimeHours: 0,
      status: PayrollAttendanceStatus.present,
      shiftName: 'Day shift',
      notes: '',
    );
    final run = PayrollRun(
      id: 'demo-run',
      runNumber: 'PAY-DEMO-001',
      periodStart: DateTime(DateTime.now().year, DateTime.now().month, 1),
      periodEnd: DateTime.now(),
      status: PayrollRunStatus.draft,
      tipsPool: 18000,
      serviceChargePool: 42000,
      totalGross: 286000,
      totalDeduction: 12500,
      totalNet: 273500,
      notes: 'Demo restaurant payroll',
      createdAt: DateTime.now(),
      employeeCount: 6,
    );
    payrollEmployees = [employee];
    payrollRoleOptions = const [
      PayrollRoleOption(
        name: 'RestaurantManager',
        displayName: 'Restaurant Manager',
      ),
      PayrollRoleOption(
        name: 'RestaurantCashier',
        displayName: 'Restaurant Cashier',
      ),
      PayrollRoleOption(
        name: 'RestaurantWaiter',
        displayName: 'Restaurant Waiter',
      ),
      PayrollRoleOption(
        name: 'RestaurantKitchen',
        displayName: 'Restaurant Kitchen',
      ),
      PayrollRoleOption(
        name: 'RestaurantInventory',
        displayName: 'Restaurant Inventory',
      ),
      PayrollRoleOption(
        name: 'RestaurantPayroll',
        displayName: 'Restaurant Payroll',
      ),
    ];
    payrollUserOptions = const [
      PayrollUserOption(id: 2, userName: 'demo.waiter', name: 'Demo Waiter'),
    ];
    payrollAttendance = [attendance];
    payrollRuns = [run];
    myPayslips = [];
    guestOrders = [];
    reservations = [];
    printJobs = [];
    payrollDashboard = PayrollDashboard(
      activeEmployeeCount: 6,
      presentToday: 5,
      openClockIns: 4,
      currentMonthGross: run.totalGross,
      currentMonthNet: run.totalNet,
      myProfile: employee,
      myTodayAttendance: attendance,
      recentRuns: [run],
      myRecentPayslips: const [],
    );
  }

  Future<void> _loadAuthenticatedData() async {
    final api = _api!;
    final savedSession = session;
    if (savedSession != null && savedSession.userId > 0) {
      profile = LoginProfile(
        userId: savedSession.userId,
        userName: savedSession.userName,
        name: savedSession.userName,
        tenantName: savedSession.tenancyName,
      );
      isAuthenticated = true;
    }
    profile = await api.getCurrentLoginProfile();
    _applyGrantedPermissions(await api.getGrantedPermissions());
    isAuthenticated = true;
    serverReachable = true;
    await _sessionStore.writeValue(
      _permissionCacheKey(),
      jsonEncode(
        permissions.where((p) => p == 'Pages.Restaurant.Pos').toList(),
      ),
    );
    await _loadRestaurantData();
    await _restoreLocalCartDraft();
    _startKdsRefresh();
    _startPermissionRefresh();
  }

  Future<bool> _refreshGrantedPermissions() {
    final active = _permissionRefreshInFlight;
    if (active != null) return active;
    final future = _performPermissionRefresh();
    _permissionRefreshInFlight = future;
    return future.whenComplete(() => _permissionRefreshInFlight = null);
  }

  Future<bool> _performPermissionRefresh() async {
    final api = _api;
    if (api == null) return false;
    return _applyGrantedPermissions(await api.getGrantedPermissions());
  }

  bool _applyGrantedPermissions(Set<String> grantedPermissions) {
    final next = Set<String>.unmodifiable(grantedPermissions);
    final changed = !setEquals(permissions, next);
    permissions = next;
    permissionsLoaded = true;
    if (changed) _clearRevokedModuleData();
    return changed;
  }

  void _clearRevokedModuleData() {
    if (!hasPermission('Pages.Restaurant.Setup') &&
        !hasPermission('Pages.Restaurant.Pos') &&
        !hasPermission('Pages.Restaurant.Kds')) {
      areas = [];
      tables = [];
      stations = [];
      devices = [];
      operationalSettings = {};
    }
    if (!hasPermission('Pages.Restaurant.Menu')) products = [];
    if (!hasPermission('Pages.Restaurant.Pos')) {
      openOrders = [];
      accountLedgers = [];
      salesLedgers = [];
      cart = [];
      currentOrderId = null;
      currentOrderTickets = [];
      orderDirty = false;
    }
    if (!hasPermission('Pages.Restaurant.Kds')) {
      _kdsTimer?.cancel();
      tickets = [];
    }
    if (!hasPermission('Pages.Restaurant.Inventory')) {
      inventory = [];
      rawMaterials = [];
      inventoryUnits = [];
      suppliers = [];
      supplierMappings = [];
      stockAdjustments = [];
      consumptionLedger = [];
      recipeCoverage = [];
    }
    if (!hasPermission('Pages.Restaurant.Channels')) {
      channels = [];
      aggregatorOrders = [];
      payouts = [];
    }
    if (!hasPermission('Pages.Restaurant.Reports')) {
      report = const ReportSummary();
      reportBundle = const RestaurantReportBundle();
    }
    if (!hasPermission('Pages.Restaurant.Payroll')) {
      payrollDashboard = null;
      payrollEmployees = [];
      payrollUserOptions = [];
      payrollRoleOptions = [];
      payrollAttendance = [];
      payrollRuns = [];
      myPayslips = [];
    }
    if (!hasPermission('Pages.Restaurant.Pos')) guestOrders = [];
    if (!hasPermission('Pages.Restaurant.Reservations')) reservations = [];
    if (!hasPermission('Pages.Restaurant.PrinterSetup')) printJobs = [];
  }

  Future<void> _loadRestaurantData() async {
    final api = _api!;
    final warnings = <String>[];

    Future<void> load(String label, Future<void> Function() action) async {
      try {
        await action();
        serverReachable = true;
      } on ApiException catch (error) {
        if (error.isUnauthorized && error.statusCode == 401) rethrow;
        serverReachable = false;
        warnings.add('$label: ${error.message}');
      } on TimeoutException {
        serverReachable = false;
        warnings.add('$label: the server did not respond.');
      }
    }

    if (hasPermission('Pages.Restaurant.Setup') ||
        hasPermission('Pages.Restaurant.Pos') ||
        hasPermission('Pages.Restaurant.Kds')) {
      await load('Restaurant setup', () async {
        final values = await Future.wait([
          api.getAreas(),
          api.getTables(),
          api.getStations(),
          api.getOperationalSettings(),
        ]);
        areas = values[0] as List<RestaurantAreaModel>;
        tables = values[1] as List<RestaurantTableModel>;
        stations = values[2] as List<RestaurantStationModel>;
        operationalSettings = values[3] as Map<String, dynamic>;
        if (!posContexts.any((item) => item.value == selectedPosContext)) {
          selectedPosContext = tables.isEmpty
              ? 'mode:takeaway'
              : tables.first.id;
        }
      });
      if (hasPermission('Pages.Restaurant.Pos')) {
        await load('Release capabilities', () async {
          releaseCapabilities = await api.getReleaseCapabilities();
          androidDraftRecoveryEnabled =
              releaseCapabilities.androidDraftRecoveryEnabled;
        });
      } else {
        androidDraftRecoveryEnabled = false;
      }
      if (hasPermission('Pages.Restaurant.Setup.Edit')) {
        await load('Restaurant devices', () async {
          devices = await api.getDevices();
        });
      }
    }

    if (hasPermission('Pages.Restaurant.Menu')) {
      await load('Menu', () async {
        products = await api.getPosMenu();
      });
    }
    if (hasPermission('Pages.Restaurant.Pos')) {
      await load('Open orders', () async {
        openOrders = await api.getOpenOrders(products);
        final selected = openOrders.where((order) {
          if (currentTableId != null) return order.tableId == currentTableId;
          return order.tableId == null && order.orderType == currentOrderType;
        });
        if (selected.isNotEmpty && cart.isEmpty) {
          currentOrderId = selected.first.id;
          currentOrderVersion = selected.first.rowVersion;
          cart = List<CartLine>.from(selected.first.items);
        }
      });
      await load('Billing ledgers', () async {
        final values = await Future.wait([
          api.getAccountLedgers(),
          api.getSalesLedgers(),
        ]);
        accountLedgers = values[0];
        salesLedgers = values[1];
      });
    }
    if (hasPermission('Pages.Restaurant.Kds')) {
      await load('KDS', () async {
        tickets = await api.getOpenTickets(products);
      });
    }
    if (hasPermission('Pages.Restaurant.Inventory')) {
      await load('Inventory', () async {
        final inventoryFuture = api.getLowStock();
        final rawMaterialFuture = api.getRawMaterials();
        final unitFuture = api.getInventoryUnits();
        final adjustmentFuture = api.getStockAdjustments();
        final consumptionFuture = api.getConsumptionLedger();
        final coverageFuture = api.getRecipeCoverage();
        inventory = await inventoryFuture;
        rawMaterials = await rawMaterialFuture;
        inventoryUnits = await unitFuture;
        stockAdjustments = await adjustmentFuture;
        consumptionLedger = await consumptionFuture;
        recipeCoverage = await coverageFuture;
        if (hasPermission('Pages.Restaurant.Inventory.SupplierMapping')) {
          final mappingFuture = api.getSupplierMappings();
          final supplierFuture = api.getSuppliers();
          supplierMappings = await mappingFuture;
          suppliers = await supplierFuture;
        }
      });
    }
    if (hasPermission('Pages.Restaurant.Channels')) {
      await load('Channels', () async {
        final channelFuture = api.getChannels();
        final orderFuture = api.getAggregatorOrders();
        channels = await channelFuture;
        aggregatorOrders = await orderFuture;
        if (hasPermission('Pages.Restaurant.Payouts')) {
          payouts = await api.getPayouts();
        }
      });
    }
    if (hasPermission('Pages.Restaurant.Reports')) {
      await load('Reports', () async {
        reportBundle = await api.getReportBundle(
          grantedPermissions: permissions,
        );
        report = reportBundle.summary;
      });
    }
    if (hasPermission('Pages.Restaurant.Payroll')) {
      await load('Payroll', _loadPayrollData);
    }
    if (hasPermission('Pages.Restaurant.Pos')) {
      await load('Guest QR orders', () async {
        guestOrders = await api.getPendingGuestOrders();
      });
    }
    if (hasPermission('Pages.Restaurant.Reservations')) {
      await load('Reservations', () async {
        final now = DateTime.now();
        reservations = await api.getReservations(
          from: now.subtract(const Duration(days: 1)),
          to: now.add(const Duration(days: 90)),
        );
      });
    }
    if (hasPermission('Pages.Restaurant.PrinterSetup')) {
      await load('Print queue', () async {
        printJobs = await api.getPrintJobs();
      });
    }
    errorMessage = warnings.isEmpty ? null : warnings.join('\n');
  }

  String _cartDraftKey() {
    final active = session;
    final userId = profile?.userId ?? active?.userId ?? 0;
    return 'restaurant-cart-draft:${active?.tenantId ?? 'host'}:$userId';
  }

  String _permissionCacheKey() => '${_cartDraftKey()}:pos-permission';

  Future<void> _persistLocalCartDraft({bool synced = false}) async {
    if (!androidDraftRecoveryEnabled) return;
    final active = session;
    final userId = profile?.userId ?? active?.userId ?? 0;
    if (active == null || userId <= 0) return;
    if (cart.isEmpty && currentOrderId == null) {
      await _sessionStore.deleteValue(_cartDraftKey());
      draftSavedAt = null;
      draftSynced = false;
      draftNeedsReview = false;
      return;
    }
    draftSavedAt = DateTime.now();
    draftSynced = synced;
    await _sessionStore.writeValue(
      _cartDraftKey(),
      jsonEncode({
        'version': 1,
        'savedAt': draftSavedAt!.toIso8601String(),
        'synced': synced,
        'orderDirty': orderDirty,
        'currentOrderId': currentOrderId,
        'currentOrderVersion': currentOrderVersion,
        'orderRequestId': draftOrderRequestId,
        'selectedPosContext': selectedPosContext,
        'lines': cart.map(_serializeCartLine).toList(),
      }),
    );
  }

  Future<void> _restoreLocalCartDraft() async {
    if (!androidDraftRecoveryEnabled) return;
    final active = session;
    final userId = profile?.userId ?? active?.userId ?? 0;
    if (active == null || userId <= 0) return;
    final rawPermission = await _sessionStore.readValue(_permissionCacheKey());
    if (!permissionsLoaded && rawPermission != null) {
      final cached = (jsonDecode(rawPermission) as List)
          .whereType<String>()
          .toSet();
      permissions = {...permissions, ...cached};
      permissionsLoaded = true;
    }
    final raw = await _sessionStore.readValue(_cartDraftKey());
    if (raw == null) return;
    final data = jsonDecode(raw) as Map<String, dynamic>;
    final orderId = data['currentOrderId'] as String?;
    currentOrderVersion = data['currentOrderVersion'] as String?;
    final hasCurrentServerOrder =
        orderId != null && openOrders.any((order) => order.id == orderId);
    if (data['synced'] == true &&
        hasCurrentServerOrder &&
        data['orderDirty'] != true) {
      return;
    }
    final restored = (data['lines'] as List? ?? const [])
        .map(
          (line) =>
              _deserializeCartLine(Map<String, dynamic>.from(line as Map)),
        )
        .toList();
    final reviewNotes = <String>[];
    cart = restored.map((line) {
      final matches = products.where((item) => item.id == line.product.id);
      if (matches.isEmpty) {
        reviewNotes.add(
          '${line.product.name} is no longer on the current menu',
        );
        return line;
      }
      final latest = matches.first;
      final variantId = line.variant?.id;
      final variantsNow = latest.variants.where((item) => item.id == variantId);
      final latestVariant = variantId == null || variantsNow.isEmpty
          ? line.variant
          : variantsNow.first;
      final oldUnit = line.unitPrice + line.modifierUnitTotal;
      final latestPrice = latestVariant == null
          ? latest.price
          : latestVariant.isAbsolutePrice
          ? latestVariant.priceDelta
          : latest.price + latestVariant.priceDelta;
      final newUnit = latestPrice + line.modifierUnitTotal;
      if ((oldUnit - newUnit).abs() > 0.009) {
        reviewNotes.add(
          '${latest.name}: ${oldUnit.toStringAsFixed(2)} → ${newUnit.toStringAsFixed(2)} per item',
        );
      }
      if (!latest.isActive || !latest.isAvailable) {
        reviewNotes.add('${latest.name} is currently unavailable');
      }
      return line.copyWith(product: latest, variant: latestVariant);
    }).toList();
    currentOrderId = orderId;
    draftOrderRequestId = data['orderRequestId'] as String?;
    selectedPosContext =
        data['selectedPosContext'] as String? ?? selectedPosContext;
    orderDirty = data['orderDirty'] == true;
    draftSynced = data['synced'] == true;
    draftNeedsReview = true;
    draftReviewMessage = reviewNotes.isEmpty
        ? 'Check the current menu and order details before dispatching.'
        : reviewNotes.join(' · ');
    draftSavedAt = DateTime.tryParse(data['savedAt'] as String? ?? '');
    noticeMessage =
        'Restored an on-device order draft. Review current prices and availability before sending it to the server.';
    notifyListeners();
  }

  Map<String, dynamic> _serializeCartLine(CartLine line) => {
    'product': _serializeProduct(line.product),
    'qty': line.qty,
    'orderItemId': line.orderItemId,
    'variantId': line.variant?.id,
    'modifiers': line.modifiers
        .map(
          (item) => {
            'id': item.id,
            'name': item.name,
            'priceDelta': item.priceDelta,
          },
        )
        .toList(),
    'status': line.status,
    'notes': line.notes,
    'discountAmount': line.discountAmount,
    'billedQty': line.billedQty,
    'unbilledQty': line.unbilledQty,
    'ticketItemId': line.ticketItemId,
    'unitName': line.unitName,
  };

  Map<String, dynamic> _serializeProduct(MenuProduct product) => {
    'id': product.id,
    'name': product.name,
    'category': product.category,
    'station': product.station,
    'price': product.price,
    'cost': product.cost,
    'stock': product.stock,
    'color': product.color.toARGB32(),
    'recipe': product.recipe,
    'productId': product.productId,
    'categoryId': product.categoryId,
    'stationId': product.stationId,
    'productType': product.productType,
    'isAvailable': product.isAvailable,
    'hasRecipe': product.hasRecipe,
    'variants': product.variants
        .map(
          (v) => {
            'id': v.id,
            'name': v.name,
            'priceDelta': v.priceDelta,
            'isAbsolutePrice': v.isAbsolutePrice,
            'isDefault': v.isDefault,
          },
        )
        .toList(),
    'modifierGroups': product.modifierGroups
        .map(
          (g) => {
            'id': g.id,
            'name': g.name,
            'minSelect': g.minSelect,
            'maxSelect': g.maxSelect,
            'isRequired': g.isRequired,
            'modifiers': g.modifiers
                .map(
                  (m) => {
                    'id': m.id,
                    'name': m.name,
                    'priceDelta': m.priceDelta,
                  },
                )
                .toList(),
          },
        )
        .toList(),
    'shortCode': product.shortCode,
    'description': product.description,
    'imageUrl': product.imageUrl,
    'preparationMinutes': product.preparationMinutes,
    'sortOrder': product.sortOrder,
    'isVeg': product.isVeg,
    'spiceLevel': product.spiceLevel,
    'isFeatured': product.isFeatured,
    'isActive': product.isActive,
    'unavailableUntil': product.unavailableUntil?.toIso8601String(),
  };

  CartLine _deserializeCartLine(Map<String, dynamic> raw) {
    final p = Map<String, dynamic>.from(raw['product'] as Map);
    final variants = (p['variants'] as List? ?? const []).map((item) {
      final v = Map<String, dynamic>.from(item as Map);
      return MenuVariant(
        id: v['id'] as String,
        name: v['name'] as String,
        priceDelta: (v['priceDelta'] as num).toDouble(),
        isAbsolutePrice: v['isAbsolutePrice'] as bool? ?? false,
        isDefault: v['isDefault'] as bool? ?? false,
      );
    }).toList();
    final groups = (p['modifierGroups'] as List? ?? const []).map((item) {
      final g = Map<String, dynamic>.from(item as Map);
      final mods = (g['modifiers'] as List? ?? const []).map((m) {
        final value = Map<String, dynamic>.from(m as Map);
        return MenuModifier(
          id: value['id'] as String,
          name: value['name'] as String,
          priceDelta: (value['priceDelta'] as num).toDouble(),
        );
      }).toList();
      return ModifierGroup(
        id: g['id'] as String,
        name: g['name'] as String,
        minSelect: g['minSelect'] as int? ?? 0,
        maxSelect: g['maxSelect'] as int? ?? 0,
        isRequired: g['isRequired'] as bool? ?? false,
        modifiers: mods,
      );
    }).toList();
    final product = MenuProduct(
      id: p['id'] as String,
      name: p['name'] as String,
      category: p['category'] as String? ?? '',
      station: p['station'] as String? ?? '',
      price: (p['price'] as num).toDouble(),
      cost: (p['cost'] as num).toDouble(),
      stock: (p['stock'] as num).toDouble(),
      color: Color(p['color'] as int? ?? 0xff607d8b),
      recipe: (p['recipe'] as List? ?? const []).cast<String>(),
      productId: p['productId'] as String? ?? '',
      categoryId: p['categoryId'] as String? ?? '',
      stationId: p['stationId'] as String?,
      productType: p['productType'] as int? ?? 0,
      isAvailable: p['isAvailable'] as bool? ?? false,
      hasRecipe: p['hasRecipe'] as bool? ?? false,
      variants: variants,
      modifierGroups: groups,
      shortCode: p['shortCode'] as String? ?? '',
      description: p['description'] as String? ?? '',
      imageUrl: p['imageUrl'] as String? ?? '',
      preparationMinutes: p['preparationMinutes'] as int? ?? 0,
      sortOrder: p['sortOrder'] as int? ?? 0,
      isVeg: p['isVeg'] as bool?,
      spiceLevel: p['spiceLevel'] as int?,
      isFeatured: p['isFeatured'] as bool? ?? false,
      isActive: p['isActive'] as bool? ?? true,
      unavailableUntil: DateTime.tryParse(
        p['unavailableUntil'] as String? ?? '',
      ),
    );
    final variantId = raw['variantId'] as String?;
    MenuVariant? variant;
    for (final item in variants) {
      if (item.id == variantId) {
        variant = item;
        break;
      }
    }
    final modifiers = (raw['modifiers'] as List? ?? const []).map((item) {
      final m = Map<String, dynamic>.from(item as Map);
      return MenuModifier(
        id: m['id'] as String,
        name: m['name'] as String,
        priceDelta: (m['priceDelta'] as num).toDouble(),
      );
    }).toList();
    return CartLine(
      product: product,
      qty: raw['qty'] as int,
      orderItemId: raw['orderItemId'] as String?,
      variant: variant,
      modifiers: modifiers,
      status: raw['status'] as int? ?? 0,
      notes: raw['notes'] as String? ?? '',
      discountAmount: (raw['discountAmount'] as num? ?? 0).toDouble(),
      billedQty: (raw['billedQty'] as num? ?? 0).toDouble(),
      unbilledQty: (raw['unbilledQty'] as num?)?.toDouble(),
      ticketItemId: raw['ticketItemId'] as String?,
      unitName: raw['unitName'] as String? ?? '',
    );
  }

  Future<void> reviewGuestOrder({
    required GuestOrderModel order,
    required bool approve,
    String rejectionReason = '',
  }) async {
    if (_demoMode) {
      guestOrders = guestOrders.where((item) => item.id != order.id).toList();
      noticeMessage = approve
          ? 'Guest order approved and sent to the kitchen.'
          : 'Guest order rejected.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.reviewGuestOrder(
        orderId: order.id,
        approve: approve,
        rejectionReason: rejectionReason,
      );
      guestOrders = await _api!.getPendingGuestOrders();
      noticeMessage = approve
          ? 'Guest order approved and sent to the kitchen.'
          : 'Guest order rejected.';
    });
  }

  Future<void> addWalkIn({
    required String guestName,
    required String phoneNumber,
    required int partySize,
    String notes = '',
  }) async {
    if (_demoMode) {
      noticeMessage = 'Walk-in added to the waitlist.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.addWalkIn(
        guestName: guestName,
        phoneNumber: phoneNumber,
        partySize: partySize,
        notes: notes,
      );
      await _reloadReservations();
      noticeMessage = 'Walk-in added to the waitlist.';
    });
  }

  Future<void> updateReservation({
    required RestaurantReservationRecord reservation,
    required int status,
    String? tableId,
    DateTime? endsAt,
  }) async {
    if (_demoMode) {
      noticeMessage = status == 4
          ? 'Guest seated. The table session is open.'
          : 'Reservation updated.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.updateReservation(
        id: reservation.id,
        status: status,
        tableId: tableId ?? reservation.tableId,
        endsAt: endsAt ?? reservation.endsAt,
      );
      await _reloadReservations();
      if (status == 4) await _refreshOrdersAndTickets();
      noticeMessage = status == 4
          ? 'Guest seated and table session opened.'
          : 'Reservation updated.';
    });
  }

  Future<void> retryPrintJob(RestaurantPrintJobRecord job) async {
    if (_demoMode) {
      noticeMessage = 'Print job added back to the ERP queue.';
      notifyListeners();
      return;
    }
    await _runBusy(() async {
      await _api!.retryPrintJob(job.id);
      printJobs = await _api!.getPrintJobs();
      noticeMessage = 'Print job added back to the ERP queue.';
    });
  }

  Future<void> retryPrintDelivery(RestaurantPrintJobRecord job, RestaurantPrintDeliveryRecord delivery) async {
    if (_demoMode) return;
    await _runBusy(() async {
      await _api!.retryPrintJob(job.id, deliveryId: delivery.id);
      printJobs = await _api!.getPrintJobs();
      noticeMessage = 'Print copy returned to ${delivery.deviceName}.';
    });
  }

  Future<List<Map<String, dynamic>>> getSharedPrinterRoutes() async =>
      _api?.getPrinterRoutes() ?? <Map<String, dynamic>>[];

  Future<List<Map<String, dynamic>>> getRegisteredPrintDevices() async =>
      _api?.getPrintDevices() ?? <Map<String, dynamic>>[];

  Future<String> getMobilePrintDeviceKey() async {
    var key = await _sessionStore.readValue('restaurantPrintDeviceKey');
    if (key != null && key.isNotEmpty) return key;
    final random = math.Random.secure();
    key = List.generate(16, (_) => random.nextInt(256).toRadixString(16).padLeft(2, '0')).join();
    await _sessionStore.writeValue('restaurantPrintDeviceKey', key);
    return key;
  }

  Future<Map<String, dynamic>> getMobilePrinterMappings() async {
    final stored = await _sessionStore.readValue('restaurantPrintRouteMappings');
    if (stored == null || stored.isEmpty) return <String, dynamic>{};
    try {
      final decoded = jsonDecode(stored);
      return decoded is Map<String, dynamic> ? decoded : <String, dynamic>{};
    } on FormatException {
      return <String, dynamic>{};
    }
  }

  Future<void> saveMobilePrinterMappings(Map<String, dynamic> mappings) async {
    await _sessionStore.writeValue('restaurantPrintRouteMappings', jsonEncode(mappings));
  }

  Future<void> startMobilePrintStation(Map<String, dynamic> mappings) async {
    if (_demoMode || _api == null || session == null) throw const ApiException('Sign in to configure mobile printing.');
    final enabledMappings = <String, dynamic>{
      for (final entry in mappings.entries)
        if (entry.value is Map && (entry.value as Map)['enabled'] != false) entry.key: entry.value,
    };
    final routeNames = enabledMappings.keys.toList();
    if (routeNames.isEmpty) throw const ApiException('Enable and configure at least one printer route first.');
    await saveMobilePrinterMappings(mappings);
    final key = await getMobilePrintDeviceKey();
    final registered = await _api!.registerPrintDevice(
      clientDeviceId: key,
      name: 'Android Print Station',
      routeNames: routeNames,
    );
    final serverId = _string(registered['id']);
    if (serverId.isEmpty) throw const ApiException('The Android print device could not be registered.');
    await _sessionStore.writeValue('restaurantPrintServerDeviceId', serverId);
    await _api!.setPrintDeviceEnabled(id: serverId, isEnabled: true);
    try {
      await AndroidPrintStationService.start(deviceId: key, routeMappings: enabledMappings);
    } catch (_) {
      await _api!.setPrintDeviceEnabled(id: serverId, isEnabled: false);
      rethrow;
    }
    mobilePrinterRunning = true;
    notifyListeners();
  }

  Future<void> stopMobilePrintStation({bool disableServer = true}) async {
    if (Platform.isAndroid) await AndroidPrintStationService.stop();
    mobilePrinterRunning = false;
    if (disableServer && _api != null) {
      final serverId = await _sessionStore.readValue('restaurantPrintServerDeviceId');
      if (serverId != null && serverId.isNotEmpty) {
        await _api!.setPrintDeviceEnabled(id: serverId, isEnabled: false);
      }
    }
    notifyListeners();
  }

  Future<bool> isMobilePrintStationRunning() async =>
      Platform.isAndroid && await FlutterForegroundTask.isRunningService;

  Future<void> _reloadReservations() async {
    final now = DateTime.now();
    reservations = await _api!.getReservations(
      from: now.subtract(const Duration(days: 1)),
      to: now.add(const Duration(days: 90)),
    );
  }

  Future<String> _saveCurrentOrder({
    required String customerName,
    required String customerPhone,
  }) async {
    final isCreate = currentOrderId == null;
    final requestSignature = jsonEncode({
      'orderId': currentOrderId,
      'orderType': currentOrderType,
      'tableId': currentTableId,
      'customerName': customerName,
      'customerPhone': customerPhone,
      'expectedOrderVersion': currentOrderVersion,
      'lines': cart.map(_serializeCartLine).toList(),
    });
    final clientRequestId = isCreate
        ? (draftOrderRequestId ?? _newClientRequestId())
        : await _stableOperationRequestId('order-edit', requestSignature);
    if (isCreate) {
      draftOrderRequestId = clientRequestId;
      await _persistLocalCartDraft(synced: false);
    }
    final id = await _api!.saveOrder(
      orderId: currentOrderId,
      orderType: currentOrderType,
      tableId: currentTableId,
      userId: profile?.userId ?? 0,
      customerName: customerName,
      customerPhone: customerPhone,
      items: cart,
      clientRequestId: clientRequestId,
      expectedOrderVersion: currentOrderVersion,
    );
    currentOrderId = id;
    draftOrderRequestId = null;
    orderDirty = false;
    draftSynced = true;
    draftNeedsReview = false;
    await _persistLocalCartDraft(synced: true);
    final latest = await _api!.getOrder(id, products);
    currentOrderVersion = latest.rowVersion;
    if (!isCreate) {
      await _sessionStore.deleteValue(_orderOperationKey('order-edit'));
    }
    return id;
  }

  String _orderOperationKey(String operation) {
    final active = session;
    final userId = profile?.userId ?? active?.userId ?? 0;
    return 'restaurant-order-op:${active?.tenantId ?? 'host'}:$userId:$operation';
  }

  Future<String> _stableOperationRequestId(
    String operation,
    String signature,
  ) async {
    final key = _orderOperationKey(operation);
    final raw = await _sessionStore.readValue(key);
    if (raw != null) {
      final stored = jsonDecode(raw) as Map<String, dynamic>;
      if (stored['signature'] == signature && stored['id'] is String) {
        return stored['id'] as String;
      }
    }
    final id = _newClientRequestId();
    await _sessionStore.writeValue(
      key,
      jsonEncode({'signature': signature, 'id': id}),
    );
    return id;
  }

  Future<void> _runVersionedKitchenMutation({
    required String operationType,
    required String actionKey,
    required Map<String, String> expectedOrderVersions,
    required Map<String, Object?> signature,
    required Future<void> Function(
      String requestId,
      Map<String, String> expectedOrderVersions,
    ) send,
  }) async {
    final storageKey = _orderOperationKey('kds:$operationType:$actionKey');
    final signatureJson = jsonEncode(signature);
    var requestId = '';
    var versions = expectedOrderVersions;
    final raw = await _sessionStore.readValue(storageKey);
    if (raw != null) {
      final stored = jsonDecode(raw) as Map<String, dynamic>;
      final storedRequestId = stored['id'] as String?;
      if (storedRequestId != null && storedRequestId.isNotEmpty) {
        final status = await _api!.getKdsOperationStatus(operationType, storedRequestId);
        final statusText = _string(status['status']);
        final sameAction = stored['signature'] == signatureJson;
        if (sameAction && statusText == 'Completed') {
          await _sessionStore.deleteValue(storageKey);
          await _refreshAfterKitchenOperation(operationType);
          return;
        }
        if (!sameAction && statusText == 'Completed') {
          await _sessionStore.deleteValue(storageKey);
          await _refreshAfterKitchenOperation(operationType);
          throw const ApiException(
            'The previous kitchen action completed. Refresh the order and retry the new action.',
          );
        }
        if (statusText != 'NotFound') {
          throw const ApiException(
            'The previous kitchen action could not be resolved. Check the connection and retry.',
          );
        }
        if (sameAction) {
          requestId = storedRequestId;
          final storedVersions = Map<String, dynamic>.from(
            stored['expectedOrderVersions'] as Map? ?? const {},
          );
          versions = storedVersions.map((key, value) => MapEntry(key, value.toString()));
        } else {
          await _sessionStore.deleteValue(storageKey);
        }
      }
    }

    if (requestId.isEmpty) {
      if (versions.isEmpty || versions.values.any((value) => value.isEmpty)) {
        throw const ApiException('The order version is unavailable. Refresh the kitchen screen.');
      }
      requestId = _newClientRequestId();
      await _sessionStore.writeValue(
        storageKey,
        jsonEncode({
          'id': requestId,
          'signature': signatureJson,
          'expectedOrderVersions': versions,
        }),
      );
    }

    try {
      await send(requestId, versions);
      await _sessionStore.deleteValue(storageKey);
    } on ApiException catch (error) {
      final details = error.message.toLowerCase();
      if (details.contains('restaurant.orderconflict') ||
          details.contains('changed on another device')) {
        await _sessionStore.deleteValue(storageKey);
        await _refreshAfterKitchenOperation(operationType);
      }
      rethrow;
    }
  }

  Future<void> _refreshAfterKitchenOperation(String operationType) async {
    if (operationType.startsWith('Kds')) {
      tickets = await _api!.getOpenTickets(products);
      return;
    }
    await _refreshOrdersAndTickets();
  }

  String _newClientRequestId() {
    final random = math.Random.secure();
    return 'flutter-${DateTime.now().microsecondsSinceEpoch}-${random.nextInt(1 << 32)}';
  }

  Future<void> _reloadCurrentOrder() async {
    final id = currentOrderId;
    if (id == null || _api == null) return;
    final order = await _api!.getOrder(id, products);
    currentOrderVersion = order.rowVersion;
    cart = List<CartLine>.from(order.items);
    final index = openOrders.indexWhere((item) => item.id == id);
    if (index >= 0) {
      final updated = List<RestaurantOrderModel>.from(openOrders);
      updated[index] = order;
      openOrders = updated;
    }
    currentOrderTickets = await _api!.getTicketsForOrder(id, products);
    orderDirty = false;
  }

  Future<void> _refreshOrdersAndTickets() async {
    final values = await Future.wait([
      _api!.getOpenOrders(products),
      if (hasPermission('Pages.Restaurant.Kds'))
        _api!.getOpenTickets(products)
      else
        Future.value(<KdsTicket>[]),
      _api!.getTables(),
    ]);
    openOrders = values[0] as List<RestaurantOrderModel>;
    tickets = values[1] as List<KdsTicket>;
    tables = values[2] as List<RestaurantTableModel>;
  }

  Future<void> _runBusy(Future<void> Function() action) async {
    if (busy) throw const ApiException('Another operation is in progress.');
    busy = true;
    errorMessage = null;
    noticeMessage = null;
    notifyListeners();
    try {
      await action();
    } on ApiException catch (error) {
      if (error.statusCode == null) serverReachable = false;
      errorMessage = error.message;
      rethrow;
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  void _attachAuthenticatedApi(StoredSession stored) {
    _api?.close();
    _api = RestaurantApi(
      AbpApiClient(
        baseUrl: stored.baseUrl,
        accessToken: stored.accessToken,
        tenantId: stored.tenantId,
        refreshAccessToken: _refreshAccessToken,
      ),
    );
  }

  Future<String?> _refreshAccessToken() {
    final active = _refreshInFlight;
    if (active != null) return active;
    final future = _performRefresh();
    _refreshInFlight = future;
    return future.whenComplete(() => _refreshInFlight = null);
  }

  Future<String?> _performRefresh() async {
    final stored = session;
    if (stored == null || stored.refreshToken.isEmpty) return null;
    final refreshClient = AbpApiClient(
      baseUrl: stored.baseUrl,
      tenantId: stored.tenantId,
    );
    try {
      final token = await RestaurantApi(
        refreshClient,
      ).refreshSession(stored.refreshToken);
      final updated = stored.copyWith(accessToken: token);
      session = updated;
      await _sessionStore.write(updated);
      return token;
    } catch (_) {
      return null;
    } finally {
      refreshClient.close();
    }
  }

  void _detachSession() {
    _kdsTimer?.cancel();
    _permissionTimer?.cancel();
    _api?.close();
    _api = null;
    session = null;
    profile = null;
    isAuthenticated = false;
    permissionsLoaded = false;
    permissions = {};
    requiresTwoFactor = false;
    twoFactorProviders = const [];
    _pendingTwoFactorUserId = null;
    _twoFactorRememberClientToken = null;
    areas = [];
    tables = [];
    stations = [];
    products = [];
    openOrders = [];
    tickets = [];
    inventory = [];
    rawMaterials = [];
    inventoryUnits = [];
    suppliers = [];
    supplierMappings = [];
    stockAdjustments = [];
    consumptionLedger = [];
    recipeCoverage = [];
    channels = [];
    aggregatorOrders = [];
    payouts = [];
    devices = [];
    currentOrderTickets = [];
    accountLedgers = [];
    salesLedgers = [];
    payrollDashboard = null;
    payrollEmployees = [];
    payrollUserOptions = [];
    payrollRoleOptions = [];
    payrollAttendance = [];
    payrollRuns = [];
    myPayslips = [];
    cart = [];
    currentOrderId = null;
    orderDirty = false;
    serverReachable = false;
    draftSynced = false;
    draftNeedsReview = false;
    draftReviewMessage = '';
    draftSavedAt = null;
  }

  void acknowledgeDraftReview() {
    draftNeedsReview = false;
    draftReviewMessage = '';
    unawaited(_persistLocalCartDraft(synced: false));
    notifyListeners();
  }

  void _startKdsRefresh() {
    _kdsTimer?.cancel();
    if (!hasPermission('Pages.Restaurant.Kds') &&
        !hasPermission('Pages.Restaurant.Pos') &&
        !hasPermission('Pages.Restaurant.PrinterSetup') &&
        !hasPermission('Pages.Restaurant.Reservations')) {
      return;
    }
    _kdsTimer = Timer.periodic(const Duration(seconds: 15), (_) async {
      if (_api == null || busy || refreshing) return;
      try {
        if (hasPermission('Pages.Restaurant.Kds')) {
          tickets = await _api!.getOpenTickets(products);
        }
        if (hasPermission('Pages.Restaurant.Pos')) {
          guestOrders = await _api!.getPendingGuestOrders();
        }
        if (hasPermission('Pages.Restaurant.Reservations')) {
          await _reloadReservations();
        }
        if (hasPermission('Pages.Restaurant.PrinterSetup')) {
          printJobs = await _api!.getPrintJobs();
        }
        notifyListeners();
      } catch (_) {
        // The visible manual refresh reports connectivity errors. A background
        // poll must not interrupt kitchen staff during a transient outage.
      }
    });
  }

  void _startPermissionRefresh() {
    _permissionTimer?.cancel();
    if (_demoMode || !isAuthenticated) return;
    _permissionTimer = Timer.periodic(const Duration(minutes: 1), (_) {
      unawaited(syncAccess(background: true));
    });
  }

  void _demoSendToKitchen() {
    final byStation = <String, List<CartLine>>{};
    for (final line in cart) {
      byStation.putIfAbsent(line.product.station, () => []).add(line);
    }
    var sequence = tickets.length + 1;
    final created = <KdsTicket>[];
    for (final entry in byStation.entries) {
      created.add(
        KdsTicket(
          id: 'KOT-${(210 + sequence).toString().padLeft(3, '0')}',
          table: selectedPosLabel,
          station: entry.key,
          channel: currentOrderType == 0 ? 'Dine-In' : 'Takeaway',
          minutes: 0,
          status: TicketStatus.queued,
          items: [
            for (var i = 0; i < entry.value.length; i++)
              entry.value[i].copyWith(
                ticketItemId: 'demo-ticket-item-$sequence-$i',
                status: 1,
              ),
          ],
        ),
      );
      sequence++;
    }
    tickets = [...created, ...tickets];
    cart = [];
    noticeMessage = 'Sent ${created.length} KOT/BOT ticket(s) to KDS.';
    notifyListeners();
  }

  String _demoProviderName(int provider) => switch (provider) {
    1 => 'Own online',
    2 => 'Foodmandu',
    3 => 'Pathao',
    4 => 'Bhojdeals',
    _ => 'Internal',
  };

  MenuProduct _copyMenuAvailability(
    MenuProduct product,
    bool available,
    DateTime? unavailableUntil,
  ) {
    return MenuProduct(
      id: product.id,
      name: product.name,
      category: product.category,
      station: product.station,
      price: product.price,
      cost: product.cost,
      stock: product.stock,
      color: product.color,
      recipe: product.recipe,
      productId: product.productId,
      categoryId: product.categoryId,
      stationId: product.stationId,
      productType: product.productType,
      isAvailable: available,
      hasRecipe: product.hasRecipe,
      variants: product.variants,
      modifierGroups: product.modifierGroups,
      shortCode: product.shortCode,
      description: product.description,
      imageUrl: product.imageUrl,
      preparationMinutes: product.preparationMinutes,
      sortOrder: product.sortOrder,
      isVeg: product.isVeg,
      spiceLevel: product.spiceLevel,
      isFeatured: product.isFeatured,
      isActive: product.isActive,
      unavailableUntil: unavailableUntil,
    );
  }

  @override
  void dispose() {
    _kdsTimer?.cancel();
    _api?.close();
    super.dispose();
  }
}
