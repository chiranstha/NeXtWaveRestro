part of '../../main.dart';

class GenericModuleScreen extends StatefulWidget {
  const GenericModuleScreen({required this.module, super.key});

  final ErpModule module;

  @override
  State<GenericModuleScreen> createState() => _GenericModuleScreenState();
}

class _GenericModuleScreenState extends State<GenericModuleScreen> {
  late ModuleWorkspaceData data;
  late List<List<String>> rows;

  @override
  void initState() {
    super.initState();
    _loadModule();
  }

  @override
  void didUpdateWidget(covariant GenericModuleScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.module.kind != widget.module.kind) {
      _loadModule();
    }
  }

  void _loadModule() {
    data = moduleWorkspaceFor(widget.module);
    rows = data.rows.map((row) => List<String>.from(row)).toList();
  }

  void _createDraftRow() {
    final row = draftRowFor(widget.module, data, rows.length + 1);
    setState(() => rows.insert(0, row));
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          '${data.primaryAction} created in ${widget.module.title}.',
        ),
      ),
    );
  }

  List<MetricValue> get metrics {
    if (data.metrics.isEmpty) {
      return [MetricValue('Rows', '${rows.length}', Icons.list_alt_rounded)];
    }
    return [
      MetricValue(
        data.metrics.first.label,
        '${rows.length}',
        data.metrics.first.icon,
      ),
      ...data.metrics.skip(1),
    ];
  }

  @override
  Widget build(BuildContext context) {
    final module = widget.module;
    final report = module.group.contains('Report');
    if (report) {
      return _ReportSuite(title: module.title, cards: _reportCardsFor(module));
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: module.title,
          action: module.description,
          icon: module.icon,
        ),
        const SizedBox(height: 12),
        _MetricBar(metrics: metrics, color: module.color),
        const SizedBox(height: 12),
        LayoutBuilder(
          builder: (context, constraints) {
            final wide = constraints.maxWidth >= 980;
            final list = _Panel(
              child: Column(
                children: [
                  _Toolbar(
                    title: data.listTitle,
                    button: data.primaryAction,
                    icon: Icons.add_rounded,
                    onPressed: _createDraftRow,
                  ),
                  _ErpTable(columns: data.columns, rows: rows),
                ],
              ),
            );
            final side = _ModuleActionPanel(
              module: module,
              data: data,
              onSave: _createDraftRow,
            );
            if (!wide) {
              return Column(children: [list, const SizedBox(height: 12), side]);
            }
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(flex: 7, child: list),
                const SizedBox(width: 12),
                Expanded(flex: 3, child: side),
              ],
            );
          },
        ),
      ],
    );
  }
}

class ModuleWorkspaceData {
  const ModuleWorkspaceData({
    required this.listTitle,
    required this.primaryAction,
    required this.columns,
    required this.rows,
    required this.metrics,
    required this.entryFields,
    required this.checklist,
  });

  final String listTitle;
  final String primaryAction;
  final List<String> columns;
  final List<List<String>> rows;
  final List<MetricValue> metrics;
  final List<String> entryFields;
  final List<String> checklist;
}

class MetricValue {
  const MetricValue(this.label, this.value, this.icon);

  final String label;
  final String value;
  final IconData icon;
}
