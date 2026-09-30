part of '../../main.dart';

Future<void> printConfiguredPrinterBytes(Map<String, dynamic> route, List<int> bytes) async {
  final kind = route['kind'];
  if (kind == 'network') {
    final host = '${route['host'] ?? ''}'.trim();
    final port = int.tryParse('${route['port'] ?? 9100}') ?? 9100;
    if (host.isEmpty) throw const ApiException('Enter the printer IP address for this route.');
    Socket? socket;
    try {
      socket = await Socket.connect(host, port, timeout: const Duration(seconds: 15));
      socket.add(bytes);
      await socket.flush().timeout(const Duration(seconds: 30));
      await socket.close();
      return;
    } catch (_) {
      socket?.destroy();
      rethrow;
    }
  }

  if (kind != 'bluetooth' && kind != 'ble') throw const ApiException('Choose a supported ESC/POS printer connection.');
  final address = '${route['address'] ?? route['deviceId'] ?? ''}';
  if (address.isEmpty) throw const ApiException('Choose a paired Bluetooth printer for this route.');
  const type = thermal.PrinterType.bluetooth;
  final printer = thermal.BluetoothPrinterInput(
    address: address,
    name: '${route['printerName'] ?? 'Bluetooth printer'}',
    isBle: kind == 'ble',
    autoConnect: true,
  );
  final manager = thermal.PrinterManager.instance;
  try {
    final connected = await manager.connect(type: type, model: printer).timeout(const Duration(seconds: 20));
    if (!connected) throw const ApiException('Could not connect to the configured Bluetooth printer.');
    final sent = await manager.send(type: type, bytes: bytes).timeout(const Duration(seconds: 30));
    if (!sent) throw const ApiException('The printer did not accept the print data.');
  } finally {
    await manager.disconnect(type: type);
  }
}

class AndroidPrintStationService {
  static void initialize() {
    if (!Platform.isAndroid) return;
    FlutterForegroundTask.init(
      androidNotificationOptions: AndroidNotificationOptions(
        channelId: 'nextwave_print_station',
        channelName: 'NextWave Print Station',
        channelDescription: 'Prints restaurant jobs to configured local printers.',
        onlyAlertOnce: true,
      ),
      iosNotificationOptions: IOSNotificationOptions(
        showNotification: false,
        playSound: false,
      ),
      foregroundTaskOptions: ForegroundTaskOptions(
        eventAction: ForegroundTaskEventAction.repeat(3000),
        autoRunOnBoot: true,
        autoRunOnMyPackageReplaced: true,
        allowWakeLock: true,
        allowWifiLock: true,
        allowAutoRestart: true,
      ),
    );
  }

  static Future<void> start({
    required String deviceId,
    required Map<String, dynamic> routeMappings,
  }) async {
    if (!Platform.isAndroid) throw const ApiException('Direct printing is currently available on Android.');
    initialize();
    final notificationPermission = await FlutterForegroundTask.checkNotificationPermission();
    if (notificationPermission != NotificationPermission.granted) {
      final requested = await FlutterForegroundTask.requestNotificationPermission();
      if (requested != NotificationPermission.granted) {
        throw const ApiException('Allow notifications so Android can show the active print service.');
      }
    }
    if (routeMappings.values.any((value) {
      final route = value is Map ? value : const <String, dynamic>{};
      return route['kind'] == 'bluetooth' || route['kind'] == 'ble';
    })) {
      final connect = await Permission.bluetoothConnect.request();
      if (!connect.isGranted) throw const ApiException('Allow Nearby devices access before starting Bluetooth printing.');
    }
    await FlutterForegroundTask.saveData(key: 'print.deviceId', value: deviceId);
    await FlutterForegroundTask.saveData(key: 'print.routes', value: jsonEncode(routeMappings));
    await FlutterForegroundTask.saveData(key: 'print.enabled', value: true);
    if (await FlutterForegroundTask.isRunningService) {
      await FlutterForegroundTask.restartService();
    } else {
      final result = await FlutterForegroundTask.startService(
        serviceId: 3021,
        serviceTypes: const [ForegroundServiceTypes.connectedDevice],
        notificationTitle: 'NextWave printing is active',
        notificationText: 'Waiting for kitchen, bar, and receipt jobs',
        callback: startMobilePrintTask,
      );
      if (result is ServiceRequestFailure) throw ApiException('Android could not start the print service: ${result.error}');
    }
  }

  static Future<void> stop() async {
    if (!Platform.isAndroid) return;
    initialize();
    await FlutterForegroundTask.saveData(key: 'print.enabled', value: false);
    if (await FlutterForegroundTask.isRunningService) await FlutterForegroundTask.stopService();
  }
}

@pragma('vm:entry-point')
void startMobilePrintTask() {
  FlutterForegroundTask.setTaskHandler(MobilePrintTaskHandler());
}

class MobilePrintTaskHandler extends TaskHandler {
  bool _busy = false;
  String? _deviceId;
  Map<String, dynamic> _routes = <String, dynamic>{};
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
  );

  @override
  Future<void> onStart(DateTime timestamp, TaskStarter starter) async {
    await _readConfiguration();
  }

  @override
  void onRepeatEvent(DateTime timestamp) {
    if (!_busy) unawaited(_poll());
  }

  @override
  Future<void> onDestroy(DateTime timestamp, bool isTimeout) async {}

  @override
  void onReceiveData(Object data) {
    if (data is Map && data['enabled'] == false) {
      unawaited(FlutterForegroundTask.stopService());
    }
    unawaited(_readConfiguration());
  }

  Future<void> _readConfiguration() async {
    _deviceId = await FlutterForegroundTask.getData(key: 'print.deviceId') as String?;
    final routeJson = await FlutterForegroundTask.getData(key: 'print.routes') as String?;
    try {
      final value = routeJson == null ? null : jsonDecode(routeJson);
      _routes = value is Map<String, dynamic> ? value : <String, dynamic>{};
    } on FormatException {
      _routes = <String, dynamic>{};
    }
    final enabled = await FlutterForegroundTask.getData(key: 'print.enabled');
    if (enabled != true || _deviceId == null || _routes.isEmpty) {
      unawaited(FlutterForegroundTask.stopService());
    }
  }

  Future<void> _poll() async {
    _busy = true;
    try {
      final enabled = await FlutterForegroundTask.getData(key: 'print.enabled');
      if (enabled != true || _deviceId == null) {
        await _readConfiguration();
        return;
      }
      final job = await _post('/api/services/app/RestaurantGuestOperations/ClaimPrintJob', {'agentId': _deviceId});
      if (job == null || job['id'] == null || job['deliveryId'] == null) return;
      final deliveryId = '${job['deliveryId']}';
      final completed = await _completedIds();
      if (completed.contains(deliveryId)) {
        await _report(job, printed: true);
        return;
      }
      final route = _routes['${job['routeName']}'];
      try {
        if (route is! Map) throw const ApiException('This phone has no printer mapped to the job route.');
        await _printBytes(route.cast<String, dynamic>(), base64Decode('${job['payloadBase64']}'));
        await _rememberCompleted(deliveryId);
        await _report(job, printed: true);
        FlutterForegroundTask.updateService(notificationText: 'Printed ${job['routeName']}');
      } catch (error) {
        await _report(job, printed: false, error: error.toString());
        FlutterForegroundTask.updateService(notificationText: 'Print failed: ${job['routeName']}');
      }
    } catch (error) {
      FlutterForegroundTask.updateService(notificationText: 'Waiting for ERP connection');
    } finally {
      _busy = false;
    }
  }

  Future<Set<String>> _completedIds() async {
    final value = await FlutterForegroundTask.getData(key: 'print.completed');
    if (value is! String || value.isEmpty) return <String>{};
    try {
      return (jsonDecode(value) as List).map((item) => '$item').toSet();
    } on FormatException {
      return <String>{};
    }
  }

  Future<void> _rememberCompleted(String id) async {
    final ids = await _completedIds();
    ids.add(id);
    final recent = ids.toList().reversed.take(300).toList();
    await FlutterForegroundTask.saveData(key: 'print.completed', value: jsonEncode(recent));
  }

  Future<void> _report(Map<String, dynamic> job, {required bool printed, String? error}) async {
    await _post('/api/services/app/RestaurantGuestOperations/ReportPrintJob', {
      'id': job['id'],
      'deliveryId': job['deliveryId'],
      'leaseToken': job['leaseToken'],
      'agentId': _deviceId,
      'agentJobId': 'android-${job['deliveryId']}',
      'status': printed ? 2 : 3,
      if (error != null) 'error': error.length > 500 ? error.substring(0, 500) : error,
    });
  }

  Future<void> _printBytes(Map<String, dynamic> route, List<int> bytes) =>
      printConfiguredPrinterBytes(route, bytes);

  Future<dynamic> _post(String path, Map<String, dynamic> body) async {
    final baseUrl = await _storage.read(key: 'baseUrl');
    if (baseUrl == null || baseUrl.trim().isEmpty) throw const ApiException('ERP server URL is missing.');
    final normalizedBase = AppConfig.normalizeBaseUrl(baseUrl);
    final tenantId = await _storage.read(key: 'tenantId');
    Future<http.Response> send(String token) => http.post(
      Uri.parse('$normalizedBase$path'),
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        if (token.isNotEmpty) 'Authorization': 'Bearer $token',
        if (tenantId != null && tenantId.isNotEmpty) 'Abp-TenantId': tenantId,
      },
      body: jsonEncode(body),
    ).timeout(const Duration(seconds: 30));

    var token = await _storage.read(key: 'accessToken') ?? '';
    var response = await send(token);
    if (response.statusCode == 401) {
      final refreshToken = await _storage.read(key: 'refreshToken');
      if (refreshToken == null || refreshToken.isEmpty) throw const ApiException('Sign in again to continue mobile printing.');
      final refreshed = await http.post(
        Uri.parse('$normalizedBase/api/TokenAuth/RefreshToken').replace(queryParameters: {'refreshToken': refreshToken}),
        headers: {'Accept': 'application/json', 'Content-Type': 'application/json'},
        body: '{}',
      ).timeout(const Duration(seconds: 30));
      if (refreshed.statusCode < 200 || refreshed.statusCode >= 300) throw const ApiException('Print station authentication expired.');
      final refreshBody = jsonDecode(refreshed.body);
      final refreshResult = refreshBody is Map ? refreshBody['result'] ?? refreshBody : refreshBody;
      token = refreshResult is Map ? '${refreshResult['accessToken'] ?? ''}' : '';
      if (token.isEmpty) throw const ApiException('ERP could not refresh the print station session.');
      await _storage.write(key: 'accessToken', value: token);
      response = await send(token);
    }
    dynamic decoded;
    if (response.body.trim().isNotEmpty) decoded = jsonDecode(response.body);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      final error = decoded is Map ? decoded['error'] : null;
      final message = error is Map ? error['message'] : decoded is Map ? decoded['message'] : null;
      throw ApiException('$message' == 'null' ? 'Print request failed (${response.statusCode}).' : '$message');
    }
    return decoded is Map && decoded.containsKey('result') ? decoded['result'] : decoded;
  }
}

class AndroidPrintStationScreen extends StatefulWidget {
  const AndroidPrintStationScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<AndroidPrintStationScreen> createState() => _AndroidPrintStationScreenState();
}

class _AndroidPrintStationScreenState extends State<AndroidPrintStationScreen> {
  final thermal.PrinterManager _printerManager = thermal.PrinterManager.instance;
  final Map<String, TextEditingController> _hosts = {};
  final Map<String, TextEditingController> _ports = {};
  final Map<String, TextEditingController> _addresses = {};
  List<Map<String, dynamic>> _routes = [];
  List<Map<String, dynamic>> _devices = [];
  Map<String, dynamic> _mappings = {};
  List<thermal.PrinterDevice> _foundPrinters = [];
  String _scanRouteName = '';
  String _scanningKind = 'bluetooth';
  bool _loading = true;
  bool _saving = false;
  bool _running = false;
  String? _error;
  String? _message;

  @override
  void initState() {
    super.initState();
    AndroidPrintStationService.initialize();
    _load();
  }

  Future<void> _load() async {
    try {
      final values = await Future.wait([
        widget.controller.getSharedPrinterRoutes(),
        widget.controller.getMobilePrinterMappings(),
        widget.controller.getRegisteredPrintDevices(),
        widget.controller.isMobilePrintStationRunning(),
      ]);
      if (!mounted) return;
      _routes = values[0] as List<Map<String, dynamic>>;
      _mappings = Map<String, dynamic>.from(values[1] as Map<String, dynamic>);
      _devices = values[2] as List<Map<String, dynamic>>;
      _running = values[3] as bool;
      for (final route in _routes) {
        final name = '${route['name']}';
        final mapping = _mapping(name);
        _hosts[name] = TextEditingController(text: '${mapping['host'] ?? ''}');
        _ports[name] = TextEditingController(text: '${mapping['port'] ?? 9100}');
        _addresses[name] = TextEditingController(text: '${mapping['address'] ?? mapping['deviceId'] ?? ''}');
      }
    } catch (error) {
      _error = error.toString();
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Map<String, dynamic> _mapping(String name) {
    final value = _mappings[name];
    if (value is! Map) return <String, dynamic>{'kind': 'network', 'enabled': false};
    final mapping = Map<String, dynamic>.from(value);
    final hasPrinter = mapping['kind'] == 'network'
        ? '${mapping['host'] ?? ''}'.trim().isNotEmpty
        : '${mapping['address'] ?? mapping['deviceId'] ?? ''}'.trim().isNotEmpty;
    mapping['enabled'] = mapping['enabled'] == true || (mapping['enabled'] == null && hasPrinter);
    return mapping;
  }

  Future<void> _discover(String name, String kind) async {
    setState(() { _error = null; _scanRouteName = name; _foundPrinters = []; });
    try {
      final scan = await Permission.bluetoothScan.request();
      final connect = await Permission.bluetoothConnect.request();
      if (!scan.isGranted || !connect.isGranted) throw const ApiException('Allow Nearby devices access to find Bluetooth printers.');
      final found = await _printerManager.discovery(
        type: thermal.PrinterType.bluetooth,
        isBle: kind == 'ble',
      ).toList().timeout(const Duration(seconds: 10));
      if (mounted) {
        setState(() {
          _foundPrinters = found;
          _scanningKind = kind;
          _message = found.isEmpty ? 'No paired or nearby printers found.' : 'Choose a printer below.';
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    }
  }

  void _selectPrinter(String routeName, thermal.PrinterDevice printer) {
    final current = _mapping(routeName);
    final value = <String, dynamic>{
      'kind': _scanningKind,
      'printerName': printer.name,
      'enabled': true,
      if (printer.address != null) 'address': printer.address,
    };
    setState(() {
      _mappings[routeName] = {...current, ...value};
      _addresses[routeName]?.text = printer.address ?? '';
      _scanRouteName = '';
    });
  }

  Future<void> _saveMappings({bool start = false}) async {
    setState(() { _saving = true; _error = null; _message = null; });
    try {
      for (final route in _routes) {
        final name = '${route['name']}';
        final mapping = _mapping(name);
        if (mapping['enabled'] != true) {
          _mappings[name] = mapping;
          continue;
        }
        if (mapping['kind'] == 'network') {
          mapping['host'] = _hosts[name]?.text.trim() ?? '';
          mapping['port'] = int.tryParse(_ports[name]?.text.trim() ?? '') ?? 9100;
          mapping['printerName'] = name;
          if ((mapping['host'] as String).isEmpty) throw ApiException('Enter a printer IP for route $name.');
          if ((mapping['port'] as int) < 1 || (mapping['port'] as int) > 65535) throw ApiException('Enter a valid printer port for route $name.');
        } else {
          final address = _addresses[name]?.text.trim() ?? '';
          if (address.isEmpty) throw ApiException('Choose a paired printer for route $name.');
          mapping['address'] = address;
        }
        _mappings[name] = mapping;
      }
      await widget.controller.saveMobilePrinterMappings(_mappings);
      if (start) {
        await widget.controller.startMobilePrintStation(_mappings);
        _running = true;
        _devices = await widget.controller.getRegisteredPrintDevices();
        _message = 'Android print station is active. It will print its own copy of matching jobs.';
      } else {
        _message = 'Printer routes saved on this phone.';
      }
    } catch (error) {
      _error = error.toString();
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _stop() async {
    setState(() { _saving = true; _error = null; });
    try {
      await widget.controller.stopMobilePrintStation();
      _running = false;
      _devices = await widget.controller.getRegisteredPrintDevices();
      _message = 'Android print station stopped and disabled.';
    } catch (error) {
      _error = error.toString();
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _test(String name) async {
    try {
      final mapping = _mapping(name);
      if (mapping['kind'] == 'bluetooth' || mapping['kind'] == 'ble') {
        final permission = await Permission.bluetoothConnect.request();
        if (!permission.isGranted) throw const ApiException('Allow Nearby devices access to test this printer.');
      }
      final host = _hosts[name]?.text.trim() ?? '';
      final port = int.tryParse(_ports[name]?.text.trim() ?? '') ?? 9100;
      final address = _addresses[name]?.text.trim() ?? '';
      if (mapping['kind'] == 'network') {
        mapping['host'] = host;
        mapping['port'] = port;
      } else if (mapping['kind'] == 'bluetooth') {
        mapping['address'] = address;
      } else {
        mapping['address'] = address;
      }
      final testPayload = utf8.encode('\x1b@\x1ba\x01NEXTWAVE ERP\nANDROID PRINTER TEST\nRoute: $name\n\n\n\x1dV\x00');
      await _sendTest(mapping, testPayload);
      if (mounted) setState(() { _error = null; _message = 'Test print sent to $name.'; });
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    }
  }

  Future<void> _sendTest(Map<String, dynamic> mapping, List<int> bytes) async {
    await printConfiguredPrinterBytes(mapping, bytes);
  }

  @override
  void dispose() {
    for (final controller in [..._hosts.values, ..._ports.values, ..._addresses.values]) {
      controller.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (!Platform.isAndroid) return const Scaffold(body: Center(child: Text('Direct mobile printing is currently available on Android.')));
    return Scaffold(
      appBar: AppBar(title: const Text('Android printer setup')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Card(child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(_running ? 'Print station active' : 'Direct printing is off', style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(height: 8),
                    const Text('This phone receives a separate copy for each route mapped below. The foreground service stays active with a notification while the app is closed.'),
                    const SizedBox(height: 12),
                    Wrap(spacing: 8, runSpacing: 8, children: [
                      OutlinedButton.icon(onPressed: _saving ? null : () => _saveMappings(), icon: const Icon(Icons.save_outlined), label: const Text('Save routes')),
                      FilledButton.icon(onPressed: _saving ? null : () => _saveMappings(start: true), icon: const Icon(Icons.play_arrow), label: Text(_running ? 'Restart printing' : 'Start printing')),
                      if (_running) OutlinedButton.icon(onPressed: _saving ? null : _stop, icon: const Icon(Icons.stop), label: const Text('Stop printing')),
                    ]),
                  ]),
                )),
                if (_error != null) _alert(_error!, true),
                if (_message != null) _alert(_message!, false),
                if (_routes.isEmpty) const Card(child: Padding(padding: EdgeInsets.all(16), child: Text('Create shared printer routes from the web Print station first.'))),
                for (final route in _routes) _routeCard(route),
                if (_devices.isNotEmpty) ...[
                  const Padding(padding: EdgeInsets.only(top: 12, bottom: 6), child: Text('Registered print devices', style: TextStyle(fontWeight: FontWeight.bold))),
                  for (final device in _devices) ListTile(
                    leading: Icon('${device['platform']}' == 'Android' ? Icons.phone_android : Icons.desktop_windows),
                    title: Text('${device['name']}'),
                    subtitle: Text('${device['platform']} · ${((device['routeNames'] as List?) ?? const []).join(', ')}'),
                    trailing: Text(device['isEnabled'] == true ? 'Enabled' : 'Disabled'),
                  ),
                ],
              ],
            ),
    );
  }

  Widget _routeCard(Map<String, dynamic> route) {
    final name = '${route['name']}';
    final mapping = _mapping(name);
    final kind = '${mapping['kind'] ?? 'network'}';
    return Card(
      margin: const EdgeInsets.only(top: 10),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('${route['displayName'] ?? name} · $name', style: Theme.of(context).textTheme.titleSmall),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            value: mapping['enabled'] == true,
            title: const Text('Print this route on this phone'),
            onChanged: (enabled) => setState(() => _mappings[name] = {...mapping, 'enabled': enabled}),
          ),
          const SizedBox(height: 10),
          DropdownButtonFormField<String>(
            initialValue: kind,
            decoration: const InputDecoration(labelText: 'Connection type'),
            items: const [
              DropdownMenuItem(value: 'network', child: Text('Wi-Fi / Ethernet printer')),
              DropdownMenuItem(value: 'bluetooth', child: Text('Bluetooth Classic')),
              DropdownMenuItem(value: 'ble', child: Text('Bluetooth Low Energy')),
            ],
            onChanged: (value) => setState(() => _mappings[name] = {...mapping, 'kind': value}),
          ),
          if (kind == 'network') ...[
            const SizedBox(height: 8),
            TextField(controller: _hosts[name], decoration: const InputDecoration(labelText: 'Printer IP address')),
            const SizedBox(height: 8),
            TextField(controller: _ports[name], keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Port', hintText: '9100')),
          ] else ...[
            const SizedBox(height: 8),
            TextField(controller: _addresses[name], decoration: InputDecoration(labelText: kind == 'ble' ? 'BLE printer device ID' : 'Bluetooth printer address')),
            Wrap(spacing: 8, children: [
              OutlinedButton.icon(onPressed: () => _discover(name, kind), icon: const Icon(Icons.bluetooth_searching), label: const Text('Find printer')),
            ]),
            if (_scanRouteName == name && _foundPrinters.isNotEmpty)
              for (final printer in _foundPrinters) ListTile(
                dense: true,
                title: Text(printer.name),
                subtitle: Text(printer.address ?? ''),
                trailing: TextButton(onPressed: () => _selectPrinter(name, printer), child: const Text('Select')),
              ),
          ],
          const SizedBox(height: 10),
          OutlinedButton.icon(onPressed: () => _test(name), icon: const Icon(Icons.print_outlined), label: const Text('Test printer')),
        ]),
      ),
    );
  }

  Widget _alert(String text, bool isError) => Container(
    margin: const EdgeInsets.only(top: 10),
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(color: isError ? Colors.red.shade50 : Colors.green.shade50, borderRadius: BorderRadius.circular(8)),
    child: Text(text, style: TextStyle(color: isError ? Colors.red.shade900 : Colors.green.shade900)),
  );
}
