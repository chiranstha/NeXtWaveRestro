part of '../../main.dart';

class RestaurantSetupScreen extends StatelessWidget {
  const RestaurantSetupScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  bool get canEdit => controller.hasPermission('Pages.Restaurant.Setup.Edit');
  bool get canCreate =>
      controller.hasPermission('Pages.Restaurant.Setup.Create');
  bool get canDelete =>
      controller.hasPermission('Pages.Restaurant.Setup.Delete');

  @override
  Widget build(BuildContext context) {
    final settings = controller.operationalSettings;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Restaurant Setup',
          action: canEdit
              ? 'Tenant configuration · editable'
              : 'Tenant configuration',
          icon: Icons.settings_suggest_rounded,
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 250,
          tileHeight: 176,
          children: [
            _setupListCard(
              context,
              icon: Icons.layers_rounded,
              title: 'Areas',
              value: '${controller.areas.length}',
              color: AppColors.primary,
              add: canCreate ? () => _areaDialog(context) : null,
              children: [
                for (final area in controller.areas.take(4))
                  _rowAction(
                    context,
                    area.name,
                    area.isActive,
                    edit: canEdit ? () => _areaDialog(context, area) : null,
                    delete: canDelete ? () => _deleteArea(context, area) : null,
                  ),
              ],
            ),
            _setupListCard(
              context,
              icon: Icons.table_bar_rounded,
              title: 'Tables',
              value: '${controller.tables.length}',
              color: AppColors.green,
              add: canCreate ? () => _tableDialog(context) : null,
              children: [
                for (final table in controller.tables.take(4))
                  _rowAction(
                    context,
                    '${table.displayName} · ${table.areaName}',
                    table.isActive,
                    edit: canEdit ? () => _tableDialog(context, table) : null,
                  ),
              ],
            ),
            _setupListCard(
              context,
              icon: Icons.soup_kitchen_rounded,
              title: 'Stations',
              value: '${controller.stations.length}',
              color: AppColors.amber,
              add: canCreate ? () => _stationDialog(context) : null,
              children: [
                for (final station in controller.stations.take(4))
                  _rowAction(
                    context,
                    '${station.name} · ${station.type == 1 ? 'Bar' : 'Kitchen'}',
                    station.isActive,
                    edit: canEdit
                        ? () => _stationDialog(context, station)
                        : null,
                  ),
              ],
            ),
            _setupListCard(
              context,
              icon: Icons.tune_rounded,
              title: 'Operations',
              value: '${settings.length}',
              color: AppColors.violet,
              add: canEdit ? () => _settingsDialog(context) : null,
              children: [
                Text(
                  'VAT ${_value(settings, 'vatPercent')}% · service ${_value(settings, 'serviceChargePercent')}%',
                ),
                Text(
                  'Stock: ${_value(settings, 'negativeStockStatus', fallback: 'Warn')}',
                ),
                Text(
                  'Tables: ${_value(settings, 'tableWorkflow', fallback: 'TableSession')}',
                ),
                Text(
                  'Ticket printing: ${_yesNo(settings['ticketPrintingEnabled'])}',
                ),
              ],
            ),
          ],
        ),
        if (canEdit) ...[
          const SizedBox(height: 14),
          _Panel(
            child: Column(
              children: [
                const _Toolbar(
                  title: 'Registered devices',
                  button: 'Sync health',
                  icon: Icons.devices_other_rounded,
                ),
                if (controller.devices.isEmpty)
                  const Padding(
                    padding: EdgeInsets.all(20),
                    child: Text('No restaurant devices are registered.'),
                  )
                else
                  _ErpTable(
                    columns: const [
                      'Device',
                      'Code',
                      'Status',
                      'Pulled seq',
                      'Ack seq',
                      'Conflict',
                      'Last error',
                    ],
                    rows: [
                      for (final device in controller.devices)
                        [
                          device.name,
                          device.deviceCode,
                          _deviceStatus(device.status),
                          '${device.lastPulledSeq}',
                          '${device.lastAcknowledgedSeq}',
                          device.hasConflict ? 'Yes' : 'No',
                          device.lastSyncError,
                        ],
                    ],
                  ),
              ],
            ),
          ),
        ],
      ],
    );
  }

  Widget _setupListCard(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String value,
    required Color color,
    required List<Widget> children,
    VoidCallback? add,
  }) {
    return _Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: color),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w900),
                ),
              ),
              Text(
                value,
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w900,
                  color: color,
                ),
              ),
              if (add != null)
                IconButton(
                  tooltip: title == 'Operations'
                      ? 'Edit settings'
                      : 'Add $title',
                  onPressed: add,
                  icon: Icon(
                    title == 'Operations'
                        ? Icons.edit_outlined
                        : Icons.add_circle_outline_rounded,
                  ),
                ),
            ],
          ),
          const Divider(),
          if (children.isEmpty)
            const Text(
              'Nothing configured',
              style: TextStyle(color: AppColors.muted),
            )
          else
            ...children,
        ],
      ),
    );
  }

  Widget _rowAction(
    BuildContext context,
    String label,
    bool active, {
    VoidCallback? edit,
    VoidCallback? delete,
  }) {
    return Row(
      children: [
        Icon(
          active ? Icons.check_circle_outline : Icons.pause_circle_outline,
          size: 15,
          color: active ? AppColors.green : AppColors.muted,
        ),
        const SizedBox(width: 5),
        Expanded(
          child: Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 12),
          ),
        ),
        if (edit != null)
          InkWell(
            onTap: edit,
            child: const Padding(
              padding: EdgeInsets.all(3),
              child: Icon(Icons.edit_outlined, size: 16),
            ),
          ),
        if (delete != null)
          InkWell(
            onTap: delete,
            child: const Padding(
              padding: EdgeInsets.all(3),
              child: Icon(
                Icons.delete_outline_rounded,
                size: 16,
                color: AppColors.red,
              ),
            ),
          ),
      ],
    );
  }

  Future<void> _areaDialog(
    BuildContext context, [
    RestaurantAreaModel? area,
  ]) async {
    final name = TextEditingController(text: area?.name ?? '');
    final description = TextEditingController(text: area?.description ?? '');
    final sort = TextEditingController(text: '${area?.sortOrder ?? 0}');
    var active = area?.isActive ?? true;
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(area == null ? 'Add area' : 'Edit area'),
          content: SizedBox(
            width: 480,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: name,
                  autofocus: true,
                  decoration: const InputDecoration(labelText: 'Name'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: description,
                  decoration: const InputDecoration(labelText: 'Description'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: sort,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Sort order'),
                ),
                SwitchListTile(
                  value: active,
                  onChanged: (value) => setDialogState(() => active = value),
                  title: const Text('Active'),
                ),
              ],
            ),
          ),
          actions: _formActions(context),
        ),
      ),
    );
    if (accepted == true && name.text.trim().isNotEmpty) {
      if (!context.mounted) return;
      await _call(
        context,
        () => controller.saveArea(
          id: area?.id,
          name: name.text.trim(),
          description: description.text.trim(),
          sortOrder: int.tryParse(sort.text) ?? 0,
          isActive: active,
        ),
      );
    }
    name.dispose();
    description.dispose();
    sort.dispose();
  }

  Future<void> _tableDialog(
    BuildContext context, [
    RestaurantTableModel? table,
  ]) async {
    if (controller.areas.isEmpty) {
      _message(context, 'Create an area first.');
      return;
    }
    var areaId = controller.areas.any((item) => item.id == table?.areaId)
        ? table!.areaId
        : controller.areas.first.id;
    final name = TextEditingController(text: table?.name ?? '');
    final code = TextEditingController(text: table?.code ?? '');
    final capacity = TextEditingController(text: '${table?.capacity ?? 4}');
    final sort = TextEditingController(text: '${table?.sortOrder ?? 0}');
    var status = table?.status ?? 0;
    var active = table?.isActive ?? true;
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(table == null ? 'Add table' : 'Edit table'),
          content: SizedBox(
            width: 500,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  DropdownButtonFormField<String>(
                    initialValue: areaId,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Area'),
                    items: [
                      for (final area in controller.areas)
                        DropdownMenuItem(
                          value: area.id,
                          child: Text(area.name),
                        ),
                    ],
                    onChanged: (value) =>
                        setDialogState(() => areaId = value ?? areaId),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: name,
                    decoration: const InputDecoration(labelText: 'Name'),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: code,
                    decoration: const InputDecoration(labelText: 'Code'),
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: capacity,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Capacity',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: sort,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Sort order',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: DropdownButtonFormField<int>(
                          initialValue: status,
                          decoration: const InputDecoration(
                            labelText: 'Status',
                          ),
                          items: const [
                            DropdownMenuItem(
                              value: 0,
                              child: Text('Available'),
                            ),
                            DropdownMenuItem(value: 1, child: Text('Occupied')),
                            DropdownMenuItem(value: 2, child: Text('Reserved')),
                            DropdownMenuItem(value: 3, child: Text('Cleaning')),
                            DropdownMenuItem(value: 4, child: Text('Inactive')),
                          ],
                          onChanged: (value) =>
                              setDialogState(() => status = value ?? 0),
                        ),
                      ),
                    ],
                  ),
                  SwitchListTile(
                    value: active,
                    onChanged: (value) => setDialogState(() => active = value),
                    title: const Text('Active'),
                  ),
                ],
              ),
            ),
          ),
          actions: _formActions(context),
        ),
      ),
    );
    if (accepted == true && name.text.trim().isNotEmpty) {
      if (!context.mounted) return;
      await _call(
        context,
        () => controller.saveTable(
          id: table?.id,
          name: name.text.trim(),
          code: code.text.trim(),
          capacity: int.tryParse(capacity.text) ?? 1,
          sortOrder: int.tryParse(sort.text) ?? 0,
          status: status,
          isActive: active,
          areaId: areaId,
        ),
      );
    }
    name.dispose();
    code.dispose();
    capacity.dispose();
    sort.dispose();
  }

  Future<void> _stationDialog(
    BuildContext context, [
    RestaurantStationModel? station,
  ]) async {
    final name = TextEditingController(text: station?.name ?? '');
    var type = station?.type ?? 0;
    var active = station?.isActive ?? true;
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(station == null ? 'Add station' : 'Edit station'),
          content: SizedBox(
            width: 450,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: name,
                  decoration: const InputDecoration(labelText: 'Name'),
                ),
                const SizedBox(height: 10),
                DropdownButtonFormField<int>(
                  initialValue: type,
                  decoration: const InputDecoration(labelText: 'Station type'),
                  items: const [
                    DropdownMenuItem(value: 0, child: Text('Kitchen / KOT')),
                    DropdownMenuItem(value: 1, child: Text('Bar / BOT')),
                  ],
                  onChanged: (value) => setDialogState(() => type = value ?? 0),
                ),
                SwitchListTile(
                  value: active,
                  onChanged: (value) => setDialogState(() => active = value),
                  title: const Text('Active'),
                ),
              ],
            ),
          ),
          actions: _formActions(context),
        ),
      ),
    );
    if (accepted == true && name.text.trim().isNotEmpty) {
      if (!context.mounted) return;
      await _call(
        context,
        () => controller.saveStation(
          id: station?.id,
          name: name.text.trim(),
          stationType: type,
          isActive: active,
        ),
      );
    }
    name.dispose();
  }

  Future<void> _settingsDialog(BuildContext context) async {
    final source = controller.operationalSettings;
    final vat = TextEditingController(text: '${source['vatPercent'] ?? 0}');
    final service = TextEditingController(
      text: '${source['serviceChargePercent'] ?? 0}',
    );
    final pin = TextEditingController(text: '${source['managerPin'] ?? ''}');
    var requirePin = source['requireManagerPinForSensitiveActions'] == true;
    var printing = source['ticketPrintingEnabled'] == true;
    var availability = source['channelAvailabilityEnabled'] == true;
    var negative = '${source['negativeStockStatus'] ?? 'Warn'}';
    var tableWorkflow = '${source['tableWorkflow'] ?? 'TableSession'}';
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Operational settings'),
          content: SizedBox(
            width: 540,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: vat,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(labelText: 'VAT %'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: service,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'Service charge %',
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  DropdownButtonFormField<String>(
                    initialValue: negative,
                    decoration: const InputDecoration(
                      labelText: 'Negative stock',
                    ),
                    items: const [
                      DropdownMenuItem(value: 'Allow', child: Text('Allow')),
                      DropdownMenuItem(value: 'Warn', child: Text('Warn')),
                      DropdownMenuItem(value: 'Block', child: Text('Block')),
                    ],
                    onChanged: (value) =>
                        setDialogState(() => negative = value ?? 'Warn'),
                  ),
                  const SizedBox(height: 10),
                  DropdownButtonFormField<String>(
                    initialValue: tableWorkflow,
                    decoration: const InputDecoration(
                      labelText: 'Table workflow',
                    ),
                    items: const [
                      DropdownMenuItem(
                        value: 'TableSession',
                        child: Text('Table session'),
                      ),
                      DropdownMenuItem(
                        value: 'PerOrder',
                        child: Text('Per order'),
                      ),
                    ],
                    onChanged: (value) => setDialogState(
                      () => tableWorkflow = value ?? 'TableSession',
                    ),
                  ),
                  SwitchListTile(
                    value: requirePin,
                    onChanged: (value) =>
                        setDialogState(() => requirePin = value),
                    title: const Text(
                      'Require manager PIN for sensitive actions',
                    ),
                  ),
                  if (requirePin)
                    TextField(
                      controller: pin,
                      obscureText: true,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(
                        labelText: 'Manager PIN',
                      ),
                    ),
                  SwitchListTile(
                    value: printing,
                    onChanged: (value) =>
                        setDialogState(() => printing = value),
                    title: const Text('Ticket printing enabled'),
                  ),
                  SwitchListTile(
                    value: availability,
                    onChanged: (value) =>
                        setDialogState(() => availability = value),
                    title: const Text('Channel availability enabled'),
                  ),
                ],
              ),
            ),
          ),
          actions: _formActions(context),
        ),
      ),
    );
    if (accepted == true) {
      if (!context.mounted) return;
      await _call(
        context,
        () => controller.saveOperationalSettings({
          'vatPercent': double.tryParse(vat.text) ?? 0,
          'serviceChargePercent': double.tryParse(service.text) ?? 0,
          'requireManagerPinForSensitiveActions': requirePin,
          'managerPin': pin.text.trim(),
          'negativeStockStatus': negative,
          'ticketPrintingEnabled': printing,
          'channelAvailabilityEnabled': availability,
          'tableWorkflow': tableWorkflow,
        }),
      );
    }
    vat.dispose();
    service.dispose();
    pin.dispose();
  }

  Future<void> _deleteArea(
    BuildContext context,
    RestaurantAreaModel area,
  ) async {
    final accepted = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete area?'),
        content: Text(area.name),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Keep'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (accepted == true) {
      if (!context.mounted) return;
      await _call(context, () => controller.deleteArea(area.id));
    }
  }

  List<Widget> _formActions(BuildContext context) => [
    TextButton(
      onPressed: () => Navigator.pop(context, false),
      child: const Text('Back'),
    ),
    FilledButton(
      onPressed: () => Navigator.pop(context, true),
      child: const Text('Save'),
    ),
  ];

  Future<void> _call(
    BuildContext context,
    Future<void> Function() action,
  ) async {
    try {
      await action();
    } on ApiException catch (error) {
      if (context.mounted) _message(context, error.message);
    }
  }

  void _message(BuildContext context, String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

String _readableKey(String value) => value
    .replaceAllMapped(
      RegExp(r'([a-z])([A-Z])'),
      (match) => '${match[1]} ${match[2]}',
    )
    .replaceAll('_', ' ');

String _value(
  Map<String, dynamic> values,
  String key, {
  String fallback = '0',
}) => values[key] == null ? fallback : '${values[key]}';

String _yesNo(dynamic value) => value == true ? 'Yes' : 'No';

String _deviceStatus(int status) => switch (status) {
  0 => 'Active',
  1 => 'Blocked',
  _ => 'Status $status',
};
