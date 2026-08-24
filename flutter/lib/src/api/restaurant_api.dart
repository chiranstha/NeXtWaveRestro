part of '../../main.dart';

class RestaurantApi {
  RestaurantApi(this.client);

  final AbpApiClient client;

  static Future<(int?, String?)> resolveTenant(
    String baseUrl,
    String tenancyName,
  ) async {
    if (tenancyName.trim().isEmpty) return (null, null);
    final api = AbpApiClient(baseUrl: baseUrl);
    try {
      final result = _map(
        await api.post(
          '/api/services/app/Account/IsTenantAvailable',
          body: {'tenancyName': tenancyName.trim()},
        ),
      );
      final state = _integer(result['state']);
      if (state == 2) throw const ApiException('The tenant is inactive.');
      if (state != 1) throw const ApiException('Tenant was not found.');
      final id = _integer(result['tenantId']);
      return (
        id == 0 ? null : id,
        _nullableString(result['serverRootAddress']),
      );
    } finally {
      api.close();
    }
  }

  Future<AuthResult> authenticate({
    required String userName,
    required String password,
    String? twoFactorCode,
    String? twoFactorRememberClientToken,
  }) async {
    final result = _map(
      await client.post(
        '/api/TokenAuth/Authenticate',
        body: {
          'userNameOrEmailAddress': userName.trim(),
          'password': password,
          'rememberClient': true,
          if (twoFactorCode != null && twoFactorCode.isNotEmpty)
            'twoFactorVerificationCode': twoFactorCode,
          'twoFactorRememberClientToken': ?twoFactorRememberClientToken,
        },
      ),
    );
    final shouldResetPassword = _boolean(
      result['shouldResetPassword'] ?? result['passwordResetRequired'],
    );
    final resetCode = _nullableString(
      result['c'] ?? result['passwordResetCode'],
    );
    if (shouldResetPassword && resetCode == null) {
      throw const ApiException(
        'The server requires a password change but did not return a reset token.',
      );
    }
    return AuthResult(
      accessToken: _string(result['accessToken']),
      refreshToken: _string(result['refreshToken']),
      userId: _integer(result['userId']),
      shouldResetPassword: shouldResetPassword,
      resetCode: resetCode,
      requiresTwoFactor: _boolean(result['requiresTwoFactorVerification']),
      twoFactorProviders: _list(
        result['twoFactorAuthProviders'],
      ).map((item) => '$item').toList(),
      twoFactorRememberClientToken: _nullableString(
        result['twoFactorRememberClientToken'],
      ),
    );
  }

  Future<String> refreshSession(String refreshToken) async {
    final result = _map(
      await client.post(
        '/api/TokenAuth/RefreshToken',
        query: {'refreshToken': refreshToken},
      ),
    );
    final token = _string(result['accessToken']);
    if (token.isEmpty) {
      throw const ApiException('The server could not refresh this session.');
    }
    return token;
  }

  Future<void> sendTwoFactorCode({
    required int userId,
    required String provider,
  }) async {
    await client.post(
      '/api/TokenAuth/SendTwoFactorAuthCode',
      body: {'userId': userId, 'provider': provider},
    );
  }

  Future<void> resetPassword({
    required String resetCode,
    required String password,
  }) async {
    await client.post(
      '/api/services/app/Account/ResetPassword',
      body: {'c': resetCode, 'password': password},
      includeTenant: false,
    );
  }

  Future<LoginProfile> getCurrentLoginProfile() async {
    final result = _map(
      await client.get('/api/services/app/Session/GetCurrentLoginInformations'),
    );
    final user = _map(result['user']);
    final tenant = _map(result['tenant']);
    if (user.isEmpty) {
      throw const ApiException(
        'The saved login session has expired.',
        statusCode: 401,
      );
    }
    return LoginProfile(
      userId: _integer(user['id']),
      userName: _string(user['userName']),
      name: [
        _string(user['name']),
        _string(user['surname']),
      ].where((item) => item.isNotEmpty).join(' '),
      tenantName: _string(
        tenant['name'] ?? tenant['tenancyName'],
        fallback: 'Host',
      ),
    );
  }

  Future<Set<String>> getGrantedPermissions() async {
    final result = _map(await client.get('/AbpUserConfiguration/GetAll'));
    final auth = _map(result['auth']);
    final granted = _map(auth['grantedPermissions']);
    return granted.entries
        .where((entry) => _boolean(entry.value))
        .map((entry) => entry.key)
        .toSet();
  }

  Future<List<RestaurantAreaModel>> getAreas() async {
    return _list(
      await client.get('/api/services/app/RestaurantSetup/GetAreas'),
    ).map((value) {
      final item = _map(value);
      return RestaurantAreaModel(
        id: _string(item['id']),
        name: _string(item['name']),
        description: _string(item['description']),
        sortOrder: _integer(item['sortOrder']),
        isActive: _boolean(item['isActive'], fallback: true),
      );
    }).toList();
  }

  Future<List<RestaurantTableModel>> getTables() async {
    return _list(
      await client.get('/api/services/app/RestaurantSetup/GetTables'),
    ).map((value) {
      final item = _map(value);
      return RestaurantTableModel(
        id: _string(item['id']),
        name: _string(item['name']),
        code: _string(item['code']),
        areaId: _string(item['areaId']),
        areaName: _string(item['areaName']),
        capacity: _integer(item['capacity']),
        status: _integer(item['status']),
        sortOrder: _integer(item['sortOrder']),
        isActive: _boolean(item['isActive'], fallback: true),
      );
    }).toList();
  }

  Future<List<RestaurantStationModel>> getStations() async {
    return _list(
      await client.get('/api/services/app/RestaurantSetup/GetStations'),
    ).map((value) {
      final item = _map(value);
      return RestaurantStationModel(
        id: _string(item['id']),
        name: _string(item['name']),
        type: _integer(item['stationType']),
        isActive: _boolean(item['isActive'], fallback: true),
      );
    }).toList();
  }

  Future<Map<String, dynamic>> getOperationalSettings() async {
    return _map(
      await client.get(
        '/api/services/app/RestaurantSetup/GetOperationalSettings',
      ),
    );
  }

  Future<void> saveArea({
    String? id,
    required String name,
    required String description,
    required int sortOrder,
    required bool isActive,
  }) async {
    await client.post(
      '/api/services/app/RestaurantSetup/CreateOrEditArea',
      body: {
        'id': ?id,
        'name': name,
        'description': description,
        'sortOrder': sortOrder,
        'isActive': isActive,
      },
    );
  }

  Future<void> deleteArea(String id) async {
    await client.post(
      '/api/services/app/RestaurantSetup/DeleteArea',
      body: {'id': id},
    );
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
    await client.post(
      '/api/services/app/RestaurantSetup/CreateOrEditTable',
      body: {
        'id': ?id,
        'name': name,
        'code': code,
        'capacity': capacity,
        'sortOrder': sortOrder,
        'status': status,
        'isActive': isActive,
        'areaId': areaId,
      },
    );
  }

  Future<void> saveStation({
    String? id,
    required String name,
    required int stationType,
    required bool isActive,
  }) async {
    await client.post(
      '/api/services/app/RestaurantSetup/CreateOrEditStation',
      body: {
        'id': ?id,
        'name': name,
        'stationType': stationType,
        'isActive': isActive,
      },
    );
  }

  Future<List<RestaurantDeviceModel>> getDevices() async {
    return _list(
      await client.get('/api/services/app/RestaurantSetup/GetDevices'),
    ).map((value) {
      final item = _map(value);
      return RestaurantDeviceModel(
        id: _string(item['id']),
        deviceCode: _string(item['deviceCode']),
        name: _string(item['name']),
        status: _integer(item['status']),
        lastPulledSeq: _integer(item['lastPulledSeq']),
        lastAcknowledgedSeq: _integer(item['lastAcknowledgedSeq']),
        hasConflict: _boolean(item['hasConflict']),
        lastSeenAt: DateTime.tryParse(_string(item['lastSeenAt'])),
        lastSyncAt: DateTime.tryParse(_string(item['lastSyncAt'])),
        lastSyncError: _string(item['lastSyncError']),
      );
    }).toList();
  }

  Future<void> updateOperationalSettings(Map<String, dynamic> settings) async {
    await client.post(
      '/api/services/app/RestaurantSetup/UpdateOperationalSettings',
      body: settings,
    );
  }

  Future<List<String>> getCategories() async {
    return _list(
      await client.get('/api/services/app/RestaurantMenu/GetCategories'),
    ).map((item) => _string(_map(item)['name'])).toList();
  }

  Future<List<MenuProduct>> getPosMenu() async {
    return _list(
      await client.get('/api/services/app/RestaurantMenu/GetPosMenu'),
    ).map(_menuProductFromJson).toList();
  }

  Future<void> setItemAvailability({
    required String menuItemId,
    required bool isAvailable,
    DateTime? unavailableUntil,
  }) async {
    await client.post(
      '/api/services/app/RestaurantMenu/SetItemAvailability',
      body: {
        'menuItemId': menuItemId,
        'isAvailable': isAvailable,
        'unavailableUntil': unavailableUntil?.toUtc().toIso8601String(),
      },
    );
  }

  Future<List<Map<String, dynamic>>> getRecipe(String productId) async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantMenu/GetRecipe',
        query: {'productId': productId},
      ),
    ).map(_map).toList();
  }

  Future<Map<String, dynamic>> getRecipeCost(String productId) async {
    return _map(
      await client.get(
        '/api/services/app/RestaurantMenu/GetRecipeCost',
        query: {'productId': productId},
      ),
    );
  }

  Future<List<RestaurantOrderModel>> getOpenOrders(
    List<MenuProduct> products,
  ) async {
    return _list(
      await client.get('/api/services/app/RestaurantOrder/GetOpenOrdersForPos'),
    ).map((item) => _orderFromJson(_map(item), products)).toList();
  }

  Future<RestaurantOrderModel> getOrder(
    String orderId,
    List<MenuProduct> products,
  ) async {
    final result = _map(
      await client.get(
        '/api/services/app/RestaurantOrder/GetOrderForPos',
        query: {'id': orderId},
      ),
    );
    return _orderFromJson(result, products);
  }

  Future<String> saveOrder({
    required String? orderId,
    required int orderType,
    required String? tableId,
    required int userId,
    required String customerName,
    required String customerPhone,
    required List<CartLine> items,
  }) async {
    final result = await client.post(
      '/api/services/app/RestaurantOrder/CreateOrEditOrder',
      body: {
        'id': ?orderId,
        'orderType': orderType,
        'tableId': tableId,
        'deviceId': null,
        'source': 'Flutter POS',
        'clientRequestId': 'flutter-${DateTime.now().microsecondsSinceEpoch}',
        'waiterUserId': userId == 0 ? null : userId,
        'guestCount': 0,
        'customerName': customerName,
        'customerPhoneNo': customerPhone,
        'notes': '',
        'items': items.map(_orderItemPayload).toList(),
      },
    );
    return _string(result);
  }

  Future<void> sendToKitchen(String orderId) async {
    await client.post(
      '/api/services/app/RestaurantOrder/SendToKitchen',
      body: {'id': orderId},
    );
  }

  Future<void> applyDiscount(
    String orderId,
    double discount, {
    String? approvalPin,
  }) async {
    await client.post(
      '/api/services/app/RestaurantOrder/ApplyOrderDiscount',
      body: {
        'orderId': orderId,
        'discountAmount': discount,
        'approvalPin': approvalPin,
        'approvalNote': 'Flutter POS checkout',
      },
    );
  }

  Future<void> voidOrderItem({
    required String orderItemId,
    required String reason,
    String? approvalPin,
  }) async {
    await client.post(
      '/api/services/app/RestaurantOrder/VoidOrderItem',
      body: {
        'orderItemId': orderItemId,
        'reason': reason,
        'approvalPin': approvalPin,
        'approvalNote': 'Flutter POS',
      },
    );
  }

  Future<void> transferTable(String orderId, String tableId) async {
    await client.post(
      '/api/services/app/RestaurantOrder/TransferTable',
      body: {'orderId': orderId, 'newTableId': tableId},
    );
  }

  Future<String> splitOrder({
    required String sourceOrderId,
    required String? newTableId,
    required List<String> orderItemIds,
  }) async {
    return _string(
      await client.post(
        '/api/services/app/RestaurantOrder/SplitOrder',
        body: {
          'sourceOrderId': sourceOrderId,
          'newTableId': newTableId,
          'orderItemIds': orderItemIds,
        },
      ),
    );
  }

  Future<void> mergeOrders({
    required String targetOrderId,
    required List<String> sourceOrderIds,
  }) async {
    await client.post(
      '/api/services/app/RestaurantOrder/MergeOrders',
      body: {'targetOrderId': targetOrderId, 'sourceOrderIds': sourceOrderIds},
    );
  }

  Future<List<KdsTicket>> getTicketsForOrder(
    String orderId,
    List<MenuProduct> products,
  ) async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantOrder/GetTicketsForOrder',
        query: {'id': orderId},
      ),
    ).map((value) => _ticketFromJson(_map(value), products)).toList();
  }

  Future<KdsTicket> reprintTicket(
    String ticketId,
    List<MenuProduct> products, {
    String? approvalPin,
  }) async {
    return _ticketFromJson(
      _map(
        await client.post(
          '/api/services/app/RestaurantOrder/ReprintTicket',
          body: {
            'ticketId': ticketId,
            'approvalPin': approvalPin,
            'approvalNote': 'Flutter POS reprint',
          },
        ),
      ),
      products,
    );
  }

  Future<void> markTicketPrinted(String ticketId) async {
    await client.post(
      '/api/services/app/RestaurantOrder/MarkTicketPrinted',
      body: {'id': ticketId},
    );
  }

  Future<List<KdsTicket>> getOpenTickets(List<MenuProduct> products) async {
    return _list(
      await client.get('/api/services/app/RestaurantKds/GetOpenTickets'),
    ).map((value) => _ticketFromJson(_map(value), products)).toList();
  }

  Future<void> updateTicketStatus(
    String ticketId,
    int status, {
    String reason = '',
  }) async {
    await client.post(
      '/api/services/app/RestaurantKds/UpdateTicketStatus',
      body: {'ticketId': ticketId, 'status': status, 'cancelReason': reason},
    );
  }

  Future<void> updateTicketItemStatus({
    required String ticketItemId,
    required int status,
    String reason = '',
  }) async {
    await client.post(
      '/api/services/app/RestaurantKds/UpdateTicketItemStatus',
      body: {
        'ticketItemId': ticketItemId,
        'status': status,
        'cancelReason': reason,
      },
    );
  }

  Future<StockValidationResult> validateStock(
    String orderId, {
    List<BillLineSelection> billLines = const [],
  }) async {
    final result = _map(
      await client.post(
        '/api/services/app/RestaurantBilling/ValidateOrderStock',
        body: {
          'id': orderId,
          'billLines': billLines
              .map((line) => {'orderItemId': line.orderItemId, 'qty': line.qty})
              .toList(),
        },
      ),
    );
    return StockValidationResult(
      valid: _boolean(result['isValid']),
      items: _list(result['requirements']).map((value) {
        final item = _map(value);
        return StockRequirement(
          productName: _string(item['productName']),
          unitName: _string(item['unitName']),
          requiredQty: _number(item['requiredQty']),
          availableQty: _number(item['availableQty']),
          available: _boolean(item['isAvailable']),
        );
      }).toList(),
    );
  }

  Future<BillingResult> finalizeBill({
    required String orderId,
    required String dateMiti,
    required String salesAccountId,
    required String ledgerId,
    required String customerName,
    required String customerPhone,
    required int paymentMethod,
    required String? paymentLedgerId,
    required double tipAmount,
    required double? customerPaidAmount,
    required bool confirmNegativeStock,
    List<BillLineSelection> billLines = const [],
  }) async {
    final result = _map(
      await client.post(
        '/api/services/app/RestaurantBilling/FinalizeBill',
        body: {
          'orderId': orderId,
          'dateMiti': dateMiti,
          'salesAccountId': salesAccountId,
          'ledgerId': ledgerId,
          'customerName': customerName,
          'customerAddress': '',
          'customerVatNo': '',
          'customerPhoneNo': customerPhone,
          'paymentMethod': paymentMethod,
          'paymentMethodLedgerId': paymentLedgerId,
          'isPrint': false,
          'confirmNegativeStock': confirmNegativeStock,
          'tipAmount': tipAmount,
          'customerPaidAmount': customerPaidAmount,
          'billLines': billLines
              .map((line) => {'orderItemId': line.orderItemId, 'qty': line.qty})
              .toList(),
        },
      ),
    );
    return BillingResult(
      orderNo: _string(result['orderNo']),
      salesMasterId: _string(result['salesMasterId']),
      payable: _number(result['payableAmount']),
      returnAmount: _number(result['returnAmount']),
      isFullyBilled: _boolean(result['isFullyBilled'], fallback: true),
      remainingGrandTotal: _number(result['remainingGrandTotal']),
    );
  }

  Future<List<LedgerOption>> getAccountLedgers() async {
    return _ledgerOptions(
      await client.get(
        '/api/services/app/SalesMasters/GetAllAccountLedgerForTableDropdown',
      ),
    );
  }

  Future<List<LedgerOption>> getSalesLedgers() async {
    return _ledgerOptions(
      await client.get(
        '/api/services/app/SalesMasters/GetAllSalesAccountForTableDropdown',
      ),
    );
  }

  Future<List<InventoryItem>> getLowStock() async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantInventory/GetLowStockSuggestions',
      ),
    ).map((value) {
      final item = _map(value);
      return InventoryItem(
        productId: _string(item['productId']),
        unitId: _string(item['unitId']),
        name: _string(item['productName']),
        group: 'Low stock',
        unit: _string(item['unitName']),
        available: _number(item['availableQty']),
        reorder: _number(item['minimumStock']),
        maximum: _number(item['maximumStock']),
        pendingPurchase: _number(item['pendingPurchaseQty']),
        suggested: _number(item['suggestedQty']),
        cost: _number(item['rate']),
        supplier: _string(item['supplierName'], fallback: 'Not mapped'),
        missingSupplierMapping: _boolean(item['missingSupplierMapping']),
      );
    }).toList();
  }

  Future<List<UniversalOption>> getRawMaterials() async {
    return _universalOptions(
      await client.get('/api/services/app/RestaurantInventory/GetRawMaterials'),
    );
  }

  Future<List<UniversalOption>> getInventoryUnits() async {
    return _universalOptions(
      await client.get('/api/services/app/RestaurantInventory/GetUnits'),
    );
  }

  Future<List<UniversalOption>> getSuppliers() async {
    return _universalOptions(
      await client.get(
        '/api/services/app/Reporting/GetAllSuppliersForTableDropdown',
      ),
    );
  }

  Future<List<SupplierItemMapping>> getSupplierMappings() async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantInventory/GetSupplierItemMappings',
      ),
    ).map((value) {
      final item = _map(value);
      return SupplierItemMapping(
        id: _string(item['id']),
        productId: _string(item['productId']),
        productName: _string(item['productName']),
        supplierLedgerId: _string(item['supplierLedgerId']),
        supplierName: _string(item['supplierName']),
        supplierSku: _string(item['supplierSku']),
        unitId: _string(item['unitId']),
        unitName: _string(item['unitName']),
        rate: _number(item['rate']),
        leadTimeDays: _integer(item['leadTimeDays']),
        minimumOrderQty: _number(item['minimumOrderQty']),
        isPreferred: _boolean(item['isPreferred']),
        isActive: _boolean(item['isActive'], fallback: true),
      );
    }).toList();
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
    await client.post(
      '/api/services/app/RestaurantInventory/CreateOrEditSupplierItemMapping',
      body: {
        'id': ?id,
        'productId': productId,
        'supplierLedgerId': supplierLedgerId,
        'supplierSku': supplierSku,
        'unitId': unitId,
        'rate': rate,
        'leadTimeDays': leadTimeDays,
        'minimumOrderQty': minimumOrderQty,
        'isPreferred': isPreferred,
        'isActive': isActive,
      },
    );
  }

  Future<void> deleteSupplierMapping(String id) async {
    await client.post(
      '/api/services/app/RestaurantInventory/DeleteSupplierItemMapping',
      body: {'id': id},
    );
  }

  Future<int> generateDraftPurchaseOrders(List<String> productIds) async {
    final result = _map(
      await client.post(
        '/api/services/app/RestaurantInventory/GenerateDraftPurchaseOrders',
        body: {'productIds': productIds, 'dateMiti': null, 'dueDateMiti': null},
      ),
    );
    return _list(result['purchaseOrderIds']).length;
  }

  Future<void> createStockAdjustment({
    required int adjustmentType,
    required String dateMiti,
    required String description,
    required String productId,
    required String unitId,
    required double qty,
    required double? countedQty,
    required double rate,
    required String reason,
  }) async {
    await client.post(
      '/api/services/app/RestaurantInventory/CreateStockAdjustment',
      body: {
        'adjustmentType': adjustmentType,
        'dateMiti': dateMiti,
        'description': description,
        'lines': [
          {
            'productId': productId,
            'unitId': unitId,
            'qty': qty,
            'countedQty': countedQty,
            'rate': rate,
            'reason': reason,
          },
        ],
      },
    );
  }

  Future<List<StockAdjustmentRecord>> getStockAdjustments() async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantInventory/GetStockAdjustments',
      ),
    ).map((value) {
      final item = _map(value);
      return StockAdjustmentRecord(
        id: _string(item['id']),
        voucherNo: _string(item['voucherNo']),
        dateMiti: _string(item['dateMiti']),
        adjustmentType: _integer(item['adjustmentType']),
        description: _string(item['description']),
        userName: _string(item['createUserName']),
        totalAmount: _number(item['totalAmount']),
      );
    }).toList();
  }

  Future<List<ConsumptionRecord>> getConsumptionLedger() async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantInventory/GetConsumptionLedger',
      ),
    ).map((value) {
      final item = _map(value);
      return ConsumptionRecord(
        dateMiti: _string(item['dateMiti']),
        orderNo: _string(item['orderNo']),
        menuItemName: _string(item['menuItemName']),
        rawMaterialName: _string(item['rawMaterialName']),
        unitName: _string(item['unitName']),
        qty: _number(item['qty']),
        rate: _number(item['rate']),
        amount: _number(item['amount']),
      );
    }).toList();
  }

  Future<List<RecipeCoverageRecord>> getRecipeCoverage() async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantInventory/GetRecipeCoverage',
      ),
    ).map((value) {
      final item = _map(value);
      return RecipeCoverageRecord(
        menuItemId: _string(item['menuItemId']),
        productId: _string(item['productId']),
        productName: _string(item['productName']),
        categoryName: _string(item['categoryName']),
        hasRecipe: _boolean(item['hasRecipe']),
        activeRecipeLineCount: _integer(item['activeRecipeLineCount']),
        estimatedRecipeCost: _number(item['estimatedRecipeCost']),
        menuPrice: _number(item['menuPrice']),
        foodCostPercent: _number(item['foodCostPercent']),
        missingRawMaterialSetup: _boolean(item['missingRawMaterialSetup']),
      );
    }).toList();
  }

  Future<List<ChannelStatus>> getChannels() async {
    return _list(
      await client.get('/api/services/app/RestaurantChannel/GetChannels'),
    ).map((value) {
      final item = _map(value);
      return ChannelStatus(
        id: _string(item['id']),
        name: _string(item['name']),
        provider: _providerName(_integer(item['provider'])),
        orders: 0,
        sales: 0,
        commission: _number(item['commissionPercent']),
        online: _boolean(item['isOnline']),
        sync: _boolean(item['isActive']) ? 'Active' : 'Inactive',
        channelType: _integer(item['channelType']),
        providerCode: _integer(item['provider']),
        priceMarkupPercent: _number(item['defaultPriceMarkupPercent']),
        sortOrder: _integer(item['sortOrder']),
        isActive: _boolean(item['isActive'], fallback: true),
      );
    }).toList();
  }

  Future<List<RestaurantAggregatorOrderModel>> getAggregatorOrders() async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantChannel/GetAggregatorOrders',
      ),
    ).map((value) {
      final item = _map(value);
      return RestaurantAggregatorOrderModel(
        id: _string(item['id']),
        channel: _string(item['channelName']),
        externalOrderId: _string(item['externalOrderId']),
        customer: _string(item['customerName'], fallback: 'Guest'),
        amount: _number(item['expectedAmount']),
        status: _integer(item['status']),
        receivedAt: DateTime.tryParse(_string(item['receivedAt'])),
        channelId: _nullableString(item['channelId']),
        provider: _integer(item['provider']),
        customerPhone: _string(item['customerPhoneNo']),
        deliveryAddress: _string(item['deliveryAddress']),
        commissionAmount: _number(item['commissionAmount']),
        restaurantDiscountAmount: _number(item['restaurantDiscountAmount']),
        deliveryFeeAmount: _number(item['deliveryFeeAmount']),
        paidAmount: _number(item['paidAmount']),
        orderId: _nullableString(item['orderId']),
        orderNo: _string(item['orderNo']),
        statusMessage: _string(item['statusMessage']),
      );
    }).toList();
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
    await client.post(
      '/api/services/app/RestaurantChannel/CreateOrEditChannel',
      body: {
        'id': ?id,
        'name': name,
        'channelType': channelType,
        'provider': provider,
        'commissionPercent': commissionPercent,
        'defaultPriceMarkupPercent': priceMarkupPercent,
        'sortOrder': sortOrder,
        'isOnline': isOnline,
        'isActive': isActive,
      },
    );
  }

  Future<void> queueMenuPublish(String channelId) async {
    await client.post(
      '/api/services/app/RestaurantChannel/QueueMenuPublish',
      query: {'channelId': channelId},
    );
  }

  Future<void> acceptAggregatorOrder({
    required String aggregatorOrderId,
    required String menuItemId,
    required double qty,
  }) async {
    await client.post(
      '/api/services/app/RestaurantChannel/AcceptAggregatorOrder',
      body: {
        'aggregatorOrderId': aggregatorOrderId,
        'sendToKitchen': true,
        'lines': [
          {
            'menuItemId': menuItemId,
            'variantId': null,
            'qty': qty,
            'notes': '',
            'modifiers': <dynamic>[],
          },
        ],
      },
    );
  }

  Future<void> updateAggregatorOrderStatus({
    required String aggregatorOrderId,
    required int status,
    required String message,
  }) async {
    await client.post(
      '/api/services/app/RestaurantChannel/UpdateAggregatorOrderStatus',
      body: {
        'aggregatorOrderId': aggregatorOrderId,
        'status': status,
        'message': message,
      },
    );
  }

  Future<List<PayoutRecord>> getPayouts() async {
    return _list(
      await client.get('/api/services/app/RestaurantChannel/GetPayouts'),
    ).map((value) {
      final item = _map(value);
      final lines = _list(item['lines']);
      return PayoutRecord(
        id: _string(item['id']),
        channel: _string(item['channelName']),
        externalPayoutId: _string(item['externalPayoutId']),
        grossAmount: _number(item['grossAmount']),
        commissionAmount: _number(item['commissionAmount']),
        deductionsAmount: _number(item['deductionsAmount']),
        netPaidAmount: _number(item['netPaidAmount']),
        issueCount: lines.where((value) {
          final status = _integer(_map(value)['matchStatus']);
          return status == 2 || status == 3;
        }).length,
      );
    }).toList();
  }

  Future<ReportSummary> getReportSummary() async {
    final today = DateTime.now();
    final from = DateTime(today.year, today.month, today.day);
    final query = {
      'fromDate': from.toIso8601String(),
      'toDate': today.toIso8601String(),
    };
    final values = await Future.wait([
      client.get(
        '/api/services/app/RestaurantReports/GetPosSalesSummary',
        query: query,
      ),
      client.get(
        '/api/services/app/RestaurantReports/GetItemSales',
        query: query,
      ),
      client.get(
        '/api/services/app/RestaurantReports/GetSettlementReport',
        query: query,
      ),
    ]);
    final summary = _map(values[0]);
    final topItems = _list(values[1]).take(5).map((value) {
      final item = _map(value);
      return ReportLine(
        name: _string(item['productName']),
        quantity: _number(item['qty']),
        amount: _number(item['grandTotal']),
      );
    }).toList();
    final settlements = _list(values[2]).map((value) {
      final item = _map(value);
      return ReportLine(
        name: _string(item['paymentMethodName']),
        quantity: _number(item['orderCount']),
        amount: _number(item['grandTotal']),
      );
    }).toList();
    return ReportSummary(
      orderCount: _integer(summary['orderCount']),
      grossAmount: _number(summary['grossAmount']),
      discountAmount: _number(summary['discountAmount']),
      taxAmount: _number(summary['taxAmount']),
      grandTotal: _number(summary['grandTotal']),
      averageBill: _number(summary['averageBill']),
      topItems: topItems,
      settlements: settlements,
    );
  }

  Future<RestaurantReportBundle> getReportBundle({
    DateTime? fromDate,
    DateTime? toDate,
    Set<String> grantedPermissions = const {},
  }) async {
    final now = DateTime.now();
    final from = fromDate ?? DateTime(now.year, now.month, now.day);
    final to = toDate ?? now;
    final query = {
      'fromDate': from.toIso8601String(),
      'toDate': to.toIso8601String(),
    };
    const categoryPermissions = {
      'sales': 'Pages.Restaurant.Reports.Sales',
      'operations': 'Pages.Restaurant.Reports.Operations',
      'inventory': 'Pages.Restaurant.Reports.Inventory',
      'payroll': 'Pages.Restaurant.Reports.Payroll',
      'audit': 'Pages.Restaurant.Reports.AuditFinance',
    };
    final hasSpecificCategory = categoryPermissions.values.any(
      grantedPermissions.contains,
    );
    bool canView(String category) {
      return !hasSpecificCategory ||
          grantedPermissions.contains(categoryPermissions[category]);
    }

    final endpointCategories = <String, String>{
      'GetPosSalesSummary': 'sales',
      'GetItemSales': 'sales',
      'GetDailySalesSummary': 'sales',
      'GetItemSalesWithMargin': 'sales',
      'GetTableSales': 'operations',
      'GetWaiterSales': 'operations',
      'GetKotBotStatus': 'operations',
      'GetWaiterPerformance': 'operations',
      'GetTableTurnover': 'operations',
      'GetMaterialConsumption': 'inventory',
      'GetRecipeCosting': 'inventory',
      'GetFoodCosting': 'inventory',
      'GetWastageReport': 'inventory',
      'GetLowStockReport': 'inventory',
      'GetPayrollReport': 'payroll',
      'GetVoidCancelledAudit': 'audit',
      'GetDiscountReport': 'audit',
      'GetSettlementReport': 'audit',
    };
    final allowedEndpoints = endpointCategories.entries
        .where((entry) => canView(entry.value))
        .toList();
    final responses = await Future.wait([
      for (final entry in allowedEndpoints)
        client.get(
          '/api/services/app/RestaurantReports/${entry.key}',
          query: query,
        ),
    ]);
    final values = <String, dynamic>{
      for (var index = 0; index < allowedEndpoints.length; index++)
        allowedEndpoints[index].key: responses[index],
    };
    final summary = _map(values['GetPosSalesSummary']);
    final itemSales = _mapRows(values['GetItemSales']);
    final settlements = _mapRows(values['GetSettlementReport']);
    final payroll = _map(values['GetPayrollReport']);
    return RestaurantReportBundle(
      summary: ReportSummary(
        orderCount: _integer(summary['orderCount']),
        grossAmount: _number(summary['grossAmount']),
        discountAmount: _number(summary['discountAmount']),
        taxAmount: _number(summary['taxAmount']),
        grandTotal: _number(summary['grandTotal']),
        averageBill: _number(summary['averageBill']),
        topItems: itemSales.take(5).map((item) {
          return ReportLine(
            name: _string(item['productName']),
            quantity: _number(item['qty']),
            amount: _number(item['grandTotal']),
          );
        }).toList(),
        settlements: settlements.map((item) {
          return ReportLine(
            name: _string(item['paymentMethodName']),
            quantity: _number(item['orderCount']),
            amount: _number(item['grandTotal']),
          );
        }).toList(),
      ),
      materialConsumption: _mapRows(values['GetMaterialConsumption']),
      itemSales: itemSales,
      tableSales: _mapRows(values['GetTableSales']),
      waiterSales: _mapRows(values['GetWaiterSales']),
      dailySales: _mapRows(values['GetDailySalesSummary']),
      kotBotStatus: _mapRows(values['GetKotBotStatus']),
      itemMargins: _mapRows(values['GetItemSalesWithMargin']),
      waiterPerformance: _mapRows(values['GetWaiterPerformance']),
      tableTurnover: _mapRows(values['GetTableTurnover']),
      voidAudit: _mapRows(values['GetVoidCancelledAudit']),
      discounts: _mapRows(values['GetDiscountReport']),
      settlements: settlements,
      recipeCosting: _mapRows(values['GetRecipeCosting']),
      foodCosting: _mapRows(values['GetFoodCosting']),
      wastage: _mapRows(values['GetWastageReport']),
      lowStock: _mapRows(values['GetLowStockReport']),
      payrollSummary: _map(payroll['summary']),
      payrollRuns: _mapRows(payroll['runs']),
      payrollEmployeeCosts: _mapRows(payroll['employeeCosts']),
      payrollAttendance: _mapRows(payroll['attendance']),
    );
  }

  void close() => client.close();
}

class RestaurantAggregatorOrderModel {
  const RestaurantAggregatorOrderModel({
    required this.id,
    required this.channel,
    required this.externalOrderId,
    required this.customer,
    required this.amount,
    required this.status,
    required this.receivedAt,
    this.channelId,
    this.provider = 0,
    this.customerPhone = '',
    this.deliveryAddress = '',
    this.commissionAmount = 0,
    this.restaurantDiscountAmount = 0,
    this.deliveryFeeAmount = 0,
    this.paidAmount = 0,
    this.orderId,
    this.orderNo = '',
    this.statusMessage = '',
  });

  final String id;
  final String channel;
  final String externalOrderId;
  final String customer;
  final double amount;
  final int status;
  final DateTime? receivedAt;
  final String? channelId;
  final int provider;
  final String customerPhone;
  final String deliveryAddress;
  final double commissionAmount;
  final double restaurantDiscountAmount;
  final double deliveryFeeAmount;
  final double paidAmount;
  final String? orderId;
  final String orderNo;
  final String statusMessage;
}

MenuProduct _menuProductFromJson(dynamic value) {
  final item = _map(value);
  final variants = _list(item['variants'])
      .where((value) {
        return _boolean(_map(value)['isActive'], fallback: true);
      })
      .map((value) {
        final variant = _map(value);
        return MenuVariant(
          id: _string(variant['id']),
          name: _string(variant['name']),
          priceDelta: _number(variant['priceDelta']),
          isAbsolutePrice: _boolean(variant['isAbsolutePrice']),
          isDefault: _boolean(variant['isDefault']),
        );
      })
      .toList();
  final groups = _list(item['modifierGroups'])
      .where((value) {
        return _boolean(_map(value)['isActive'], fallback: true);
      })
      .map((value) {
        final group = _map(value);
        return ModifierGroup(
          id: _string(group['id']),
          name: _string(group['name']),
          minSelect: _integer(group['minSelect']),
          maxSelect: math.max(1, _integer(group['maxSelect'])),
          isRequired: _boolean(group['isRequired']),
          modifiers: _list(group['modifiers'])
              .where((value) {
                return _boolean(_map(value)['isActive'], fallback: true);
              })
              .map((value) {
                final modifier = _map(value);
                return MenuModifier(
                  id: _string(modifier['id']),
                  name: _string(modifier['name']),
                  priceDelta: _number(modifier['priceDelta']),
                );
              })
              .toList(),
        );
      })
      .toList();
  return MenuProduct(
    id: _string(item['id']),
    productId: _string(item['productId']),
    categoryId: _string(item['categoryId']),
    name: _string(item['displayName'] ?? item['productName']),
    category: _string(item['categoryName'], fallback: 'Menu'),
    stationId: _nullableString(item['stationId']),
    station: _string(item['stationName'], fallback: 'Counter'),
    productType: _integer(item['productType']),
    price: _number(item['price']),
    cost: 0,
    stock: _boolean(item['isAvailable']) ? double.infinity : 0,
    color: _parseColor(item['colorHex']),
    recipe: const [],
    isAvailable: _boolean(item['isAvailable']),
    hasRecipe: _boolean(item['hasRecipe']),
    variants: variants,
    modifierGroups: groups,
    shortCode: _string(item['shortCode']),
    description: _string(item['description']),
    imageUrl: _string(item['imageUrl']),
    preparationMinutes: _integer(item['preparationMinutes']),
    sortOrder: _integer(item['sortOrder']),
    isVeg: item['isVeg'] == null ? null : _boolean(item['isVeg']),
    spiceLevel: item['spiceLevel'] == null
        ? null
        : _integer(item['spiceLevel']),
    isFeatured: _boolean(item['isFeatured']),
    isActive: _boolean(item['isActive'], fallback: true),
    unavailableUntil: DateTime.tryParse(_string(item['unavailableUntil'])),
  );
}

RestaurantOrderModel _orderFromJson(
  Map<String, dynamic> item,
  List<MenuProduct> products,
) {
  return RestaurantOrderModel(
    id: _string(item['id']),
    orderNo: _string(item['orderNo']),
    orderType: _integer(item['orderType']),
    status: _integer(item['status']),
    tableId: _nullableString(item['tableId']),
    tableName: _string(item['tableName']),
    customerName: _string(item['customerName']),
    customerPhone: _string(item['customerPhoneNo']),
    grandTotal: _number(item['grandTotal']),
    remainingGrandTotal: _number(item['remainingGrandTotal']),
    items: _list(item['items']).map((value) {
      final line = _map(value);
      final menuItemId = _string(line['menuItemId']);
      final productId = _string(line['productId']);
      final found = products.where(
        (product) => product.id == menuItemId || product.productId == productId,
      );
      final product = found.isNotEmpty
          ? found.first
          : MenuProduct(
              id: menuItemId,
              productId: productId,
              name: _string(
                line['itemNameSnapshot'] ?? line['productName'],
                fallback: 'Menu item',
              ),
              category: 'Order',
              station: _string(line['stationName'], fallback: 'Counter'),
              price: _number(line['rate']),
              cost: 0,
              stock: double.infinity,
              color: AppColors.primary,
              recipe: const [],
            );
      final variantId = _nullableString(line['variantId']);
      MenuVariant? variant;
      if (variantId != null) {
        final matches = product.variants.where((item) => item.id == variantId);
        variant = matches.isNotEmpty
            ? matches.first
            : MenuVariant(
                id: variantId,
                name: _string(line['variantNameSnapshot']),
                priceDelta: _number(line['rate']),
                isAbsolutePrice: true,
                isDefault: false,
              );
      }
      final modifiers = _list(line['modifiers']).map((value) {
        final modifier = _map(value);
        return MenuModifier(
          id: _string(modifier['modifierId']),
          name: _string(modifier['modifierNameSnapshot']),
          priceDelta: _number(modifier['priceDelta']),
        );
      }).toList();
      return CartLine(
        product: product,
        qty: _number(line['qty']).round(),
        orderItemId: _nullableString(line['id']),
        variant: variant,
        modifiers: modifiers,
        status: _integer(line['status']),
        notes: _string(line['notes']),
        discountAmount: _number(line['discountAmount']),
        billedQty: _number(line['billedQty']),
        unbilledQty: _number(line['unbilledQty']),
        unitName: _string(line['unitName']),
      );
    }).toList(),
  );
}

KdsTicket _ticketFromJson(
  Map<String, dynamic> item,
  List<MenuProduct> products,
) {
  final sentAt = DateTime.tryParse(_string(item['sentAt']))?.toLocal();
  final minutes = sentAt == null
      ? 0
      : math.max(0, DateTime.now().difference(sentAt).inMinutes);
  return KdsTicket(
    id: _string(item['ticketNo']),
    serverId: _string(item['id']),
    orderId: _string(item['orderId']),
    orderNo: _string(item['orderNo']),
    ticketType: _integer(item['ticketType']),
    purpose: _integer(item['purpose']),
    table: _string(item['tableName'], fallback: _string(item['orderNo'])),
    station: _string(item['stationName'], fallback: 'Kitchen'),
    channel: _integer(item['ticketType']) == 1 ? 'BOT' : 'KOT',
    minutes: minutes,
    status: _ticketStatus(_integer(item['status'])),
    items: _list(item['items']).map((value) {
      final line = _map(value);
      final product = MenuProduct(
        id: _string(line['orderItemId']),
        productId: '',
        name: _string(
          line['itemNameSnapshot'] ?? line['productName'],
          fallback: 'Item',
        ),
        category: 'Ticket',
        station: _string(item['stationName'], fallback: 'Kitchen'),
        price: 0,
        cost: 0,
        stock: double.infinity,
        color: AppColors.amber,
        recipe: const [],
      );
      return CartLine(
        product: product,
        qty: _number(line['qty']).round(),
        ticketItemId: _nullableString(line['id']),
        status: _integer(line['status']),
        notes: _string(line['notes']),
        unitName: _string(line['unitName']),
      );
    }).toList(),
  );
}

Map<String, dynamic> _orderItemPayload(CartLine line) {
  return {
    if (line.orderItemId != null) 'id': line.orderItemId,
    'menuItemId': line.product.id,
    'variantId': line.variant?.id,
    'productId': line.product.productId,
    'stationId': line.product.stationId,
    'qty': line.qty,
    'rate': line.unitPrice,
    'discountAmount': line.discountAmount,
    'notes': line.notes,
    'modifiers': line.modifiers
        .map((modifier) => {'modifierId': modifier.id, 'qty': 1})
        .toList(),
  };
}

List<LedgerOption> _ledgerOptions(dynamic value) {
  return _list(value).map((value) {
    final item = _map(value);
    return LedgerOption(
      id: _string(item['id']),
      name: _string(item['displayName'] ?? item['name'], fallback: 'Ledger'),
    );
  }).toList();
}

List<UniversalOption> _universalOptions(dynamic value) {
  return _list(value).map((value) {
    final item = _map(value);
    return UniversalOption(
      id: _string(item['id']),
      name: _string(item['displayName'] ?? item['name'], fallback: 'Item'),
    );
  }).toList();
}

List<Map<String, dynamic>> _mapRows(dynamic value) {
  return _list(value).map(_map).toList();
}

TicketStatus _ticketStatus(int status) {
  return switch (status) {
    0 => TicketStatus.queued,
    1 => TicketStatus.preparing,
    2 => TicketStatus.ready,
    _ => TicketStatus.bumped,
  };
}

Color _parseColor(dynamic value) {
  final text = _string(value).replaceFirst('#', '');
  final parsed = int.tryParse(text, radix: 16);
  if (parsed == null) return AppColors.primary;
  return Color(text.length <= 6 ? 0xFF000000 | parsed : parsed);
}

String _providerName(int provider) => switch (provider) {
  1 => 'Own online',
  2 => 'Foodmandu',
  3 => 'Pathao',
  4 => 'Bhojdeals',
  _ => 'Internal',
};
