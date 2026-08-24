part of '../../main.dart';

class RestaurantPayrollScreen extends StatefulWidget {
  const RestaurantPayrollScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<RestaurantPayrollScreen> createState() =>
      _RestaurantPayrollScreenState();
}

class _RestaurantPayrollScreenState extends State<RestaurantPayrollScreen> {
  String view = 'Overview';

  RestaurantAppController get controller => widget.controller;

  @override
  Widget build(BuildContext context) {
    final dashboard = controller.payrollDashboard;
    final views = <String>[
      'Overview',
      if (controller.hasPermission('Pages.Restaurant.Payroll.Staff')) 'Staff',
      if (controller.hasPermission('Pages.Restaurant.Payroll.Attendance'))
        'Attendance',
      if (controller.hasPermission('Pages.Restaurant.Payroll.Reports') ||
          controller.hasPermission('Pages.Restaurant.Payroll.Process') ||
          controller.hasPermission('Pages.Restaurant.Payroll.Approve'))
        'Pay runs',
      if (controller.hasPermission('Pages.Restaurant.Payroll.OwnPayslip'))
        'My payslips',
    ];
    if (!views.contains(view)) view = views.first;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'Staff & Payroll',
          action: 'Restaurant shifts, wages, tips and service charge',
          icon: Icons.badge_rounded,
        ),
        const SizedBox(height: 12),
        if (dashboard == null)
          const _Panel(
            child: Center(
              child: Padding(
                padding: EdgeInsets.all(20),
                child: CircularProgressIndicator(),
              ),
            ),
          )
        else ...[
          _ResponsiveGrid(
            minTileWidth: 205,
            tileHeight: 144,
            children: [
              _KpiCard(
                title: 'Active staff',
                value: '${dashboard.activeEmployeeCount}',
                helper: '${dashboard.presentToday} present today',
                icon: Icons.groups_rounded,
                color: AppColors.primary,
              ),
              _KpiCard(
                title: 'Open shifts',
                value: '${dashboard.openClockIns}',
                helper: 'Clocked in, not clocked out',
                icon: Icons.punch_clock_rounded,
                color: AppColors.amber,
              ),
              _KpiCard(
                title: 'Month gross',
                value: money(dashboard.currentMonthGross),
                helper: 'Wages + OT + tips + service',
                icon: Icons.account_balance_wallet_rounded,
                color: AppColors.green,
              ),
              _KpiCard(
                title: 'Month net',
                value: money(dashboard.currentMonthNet),
                helper: 'After tax, advances and deductions',
                icon: Icons.payments_rounded,
                color: AppColors.teal,
              ),
            ],
          ),
          const SizedBox(height: 14),
          _Panel(
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                for (final item in views)
                  ChoiceChip(
                    selected: view == item,
                    label: Text(item),
                    onSelected: (_) => setState(() => view = item),
                  ),
                const SizedBox(width: 4),
                IconButton(
                  tooltip: 'Refresh payroll',
                  onPressed: controller.busy ? null : _refresh,
                  icon: const Icon(Icons.refresh_rounded),
                ),
                if (view == 'Staff' &&
                    controller.hasPermission('Pages.Restaurant.Payroll.Staff'))
                  FilledButton.icon(
                    onPressed: controller.busy ? null : () => _employeeDialog(),
                    icon: const Icon(Icons.person_add_alt_1_rounded),
                    label: const Text('Add staff'),
                  ),
                if (view == 'Attendance' &&
                    controller.hasPermission(
                      'Pages.Restaurant.Payroll.Attendance.Manage',
                    ))
                  FilledButton.icon(
                    onPressed: controller.busy
                        ? null
                        : () => _attendanceDialog(),
                    icon: const Icon(Icons.add_task_rounded),
                    label: const Text('Add attendance'),
                  ),
                if (view == 'Pay runs' &&
                    controller.hasPermission(
                      'Pages.Restaurant.Payroll.Process',
                    ))
                  FilledButton.icon(
                    onPressed: controller.busy ? null : _generatePayrollDialog,
                    icon: const Icon(Icons.calculate_rounded),
                    label: const Text('Generate payroll'),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          _buildView(dashboard),
        ],
      ],
    );
  }

  Widget _buildView(PayrollDashboard dashboard) => switch (view) {
    'Staff' => _staffView(),
    'Attendance' => _attendanceView(),
    'Pay runs' => _payRunsView(),
    'My payslips' => _payslipsView(),
    _ => _overview(dashboard),
  };

  Widget _overview(PayrollDashboard dashboard) {
    final profile = dashboard.myProfile;
    final attendance = dashboard.myTodayAttendance;
    return Column(
      children: [
        if (controller.hasPermission('Pages.Restaurant.Payroll.Attendance'))
          _Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const _PayrollSectionTitle(
                  title: 'My shift today',
                  subtitle: 'Self-service attendance linked to your login',
                  icon: Icons.punch_clock_rounded,
                ),
                const SizedBox(height: 12),
                if (profile == null)
                  const Text(
                    'Your login is not linked to a payroll employee. Ask the restaurant manager or payroll officer to link it.',
                    style: TextStyle(color: AppColors.muted, height: 1.4),
                  )
                else
                  Wrap(
                    spacing: 16,
                    runSpacing: 12,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      _StaffIdentity(employee: profile),
                      _PayrollTag(
                        label: attendance == null
                            ? 'Not clocked in'
                            : attendance.clockOut == null
                            ? 'On shift'
                            : 'Shift complete',
                        color: attendance == null
                            ? AppColors.muted
                            : attendance.clockOut == null
                            ? AppColors.green
                            : AppColors.primary,
                      ),
                      if (attendance?.clockIn != null)
                        Text('In ${_payrollTime(attendance!.clockIn!)}'),
                      if (attendance?.clockOut != null)
                        Text('Out ${_payrollTime(attendance!.clockOut!)}'),
                      if (attendance == null)
                        FilledButton.icon(
                          onPressed: controller.busy
                              ? null
                              : () => _clockInDialog(),
                          icon: const Icon(Icons.login_rounded),
                          label: const Text('Clock in'),
                        )
                      else if (attendance.clockOut == null)
                        FilledButton.icon(
                          onPressed: controller.busy ? null : _clockOutDialog,
                          icon: const Icon(Icons.logout_rounded),
                          label: const Text('Clock out'),
                        ),
                    ],
                  ),
              ],
            ),
          ),
        if (controller.payrollRuns.isNotEmpty) ...[
          const SizedBox(height: 12),
          _Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const _PayrollSectionTitle(
                  title: 'Recent payroll',
                  subtitle: 'Latest processing periods',
                  icon: Icons.receipt_long_rounded,
                ),
                const SizedBox(height: 8),
                for (final run in controller.payrollRuns.take(3)) _runTile(run),
              ],
            ),
          ),
        ],
        if (controller.myPayslips.isNotEmpty) ...[
          const SizedBox(height: 12),
          _Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const _PayrollSectionTitle(
                  title: 'Latest payslip',
                  subtitle: 'Your most recent approved payroll',
                  icon: Icons.request_quote_rounded,
                ),
                const SizedBox(height: 8),
                _payslipTile(controller.myPayslips.first),
              ],
            ),
          ),
        ],
      ],
    );
  }

  Widget _staffView() {
    final employees = controller.payrollEmployees;
    if (employees.isEmpty) {
      return const _Panel(
        child: Text('No payroll employees have been added yet.'),
      );
    }
    return _ResponsiveGrid(
      minTileWidth: 310,
      tileHeight: 210,
      children: [
        for (final employee in employees)
          _Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(child: _StaffIdentity(employee: employee)),
                    IconButton(
                      tooltip: 'Edit staff',
                      onPressed: controller.busy
                          ? null
                          : () => _employeeDialog(employee),
                      icon: const Icon(Icons.edit_outlined),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 6,
                  runSpacing: 6,
                  children: [
                    _PayrollTag(
                      label:
                          employee.employmentType ==
                              PayrollEmploymentType.monthly
                          ? 'Monthly'
                          : 'Hourly',
                      color: AppColors.primary,
                    ),
                    _PayrollTag(
                      label: employee.isActive ? 'Active' : 'Inactive',
                      color: employee.isActive
                          ? AppColors.green
                          : AppColors.muted,
                    ),
                    if (employee.userId != null)
                      _PayrollTag(
                        label: employee.restaurantRoleName.isEmpty
                            ? 'Login linked'
                            : _restaurantRoleDisplayName(
                                employee.restaurantRoleName,
                              ),
                        color: AppColors.teal,
                      ),
                    if (employee.loginIsActive == false)
                      const _PayrollTag(
                        label: 'Login disabled',
                        color: AppColors.red,
                      ),
                  ],
                ),
                const Spacer(),
                Text(
                  employee.employmentType == PayrollEmploymentType.monthly
                      ? '${money(employee.basicSalary)} / month'
                      : '${money(employee.hourlyRate)} / hour',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'OT ${money(employee.overtimeRate)}/hr · Service weight ${employee.serviceChargeWeight.toStringAsFixed(2)}',
                  style: const TextStyle(color: AppColors.muted),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _attendanceView() {
    final rows = controller.payrollAttendance;
    if (rows.isEmpty) {
      return const _Panel(
        child: Text('No attendance has been recorded for this month.'),
      );
    }
    return _Panel(
      child: Column(
        children: [
          for (final row in rows)
            ListTile(
              contentPadding: const EdgeInsets.symmetric(horizontal: 4),
              leading: CircleAvatar(
                backgroundColor: _attendanceColor(
                  row.status,
                ).withValues(alpha: 0.12),
                child: Icon(
                  Icons.punch_clock_rounded,
                  color: _attendanceColor(row.status),
                ),
              ),
              title: Text(
                row.employeeName,
                style: const TextStyle(fontWeight: FontWeight.w800),
              ),
              subtitle: Text(
                '${_payrollDate(row.workDate)} · ${row.shiftName.isEmpty ? _attendanceLabel(row.status) : row.shiftName} · ${row.regularHours.toStringAsFixed(2)}h + ${row.overtimeHours.toStringAsFixed(2)}h OT',
              ),
              trailing:
                  controller.hasPermission(
                    'Pages.Restaurant.Payroll.Attendance.Manage',
                  )
                  ? IconButton(
                      tooltip: 'Edit attendance',
                      onPressed: controller.busy
                          ? null
                          : () => _attendanceDialog(row),
                      icon: const Icon(Icons.edit_calendar_outlined),
                    )
                  : _PayrollTag(
                      label: _attendanceLabel(row.status),
                      color: _attendanceColor(row.status),
                    ),
            ),
        ],
      ),
    );
  }

  Widget _payRunsView() {
    if (controller.payrollRuns.isEmpty) {
      return const _Panel(
        child: Text('No payroll runs have been generated yet.'),
      );
    }
    return _Panel(
      child: Column(
        children: [for (final run in controller.payrollRuns) _runTile(run)],
      ),
    );
  }

  Widget _runTile(PayrollRun run) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 7),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                backgroundColor: _runColor(run.status).withValues(alpha: 0.12),
                child: Icon(
                  Icons.receipt_long_rounded,
                  color: _runColor(run.status),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      run.runNumber,
                      style: const TextStyle(fontWeight: FontWeight.w900),
                    ),
                    Text(
                      '${_payrollDate(run.periodStart)} – ${_payrollDate(run.periodEnd)} · ${run.employeeCount} staff · Net ${money(run.totalNet)}',
                      style: const TextStyle(color: AppColors.muted),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              _PayrollTag(
                label: _runLabel(run.status),
                color: _runColor(run.status),
              ),
            ],
          ),
          Align(
            alignment: Alignment.centerRight,
            child: Wrap(
              spacing: 2,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                IconButton(
                  tooltip: 'View payroll',
                  onPressed: () => _showRun(run),
                  icon: const Icon(Icons.visibility_outlined),
                ),
                if (run.status == PayrollRunStatus.draft &&
                    controller.hasPermission(
                      'Pages.Restaurant.Payroll.Approve',
                    ))
                  IconButton(
                    tooltip: 'Approve payroll',
                    onPressed: controller.busy ? null : () => _approveRun(run),
                    icon: const Icon(
                      Icons.verified_rounded,
                      color: AppColors.green,
                    ),
                  ),
                if (run.status == PayrollRunStatus.approved &&
                    controller.hasPermission(
                      'Pages.Restaurant.Payroll.Approve',
                    ))
                  IconButton(
                    tooltip: 'Mark paid',
                    onPressed: controller.busy ? null : () => _markPaid(run),
                    icon: const Icon(
                      Icons.payments_rounded,
                      color: AppColors.teal,
                    ),
                  ),
                if (run.status == PayrollRunStatus.draft &&
                    controller.hasPermission(
                      'Pages.Restaurant.Payroll.Process',
                    ))
                  IconButton(
                    tooltip: 'Delete draft',
                    onPressed: controller.busy ? null : () => _deleteRun(run),
                    icon: const Icon(
                      Icons.delete_outline_rounded,
                      color: AppColors.red,
                    ),
                  ),
              ],
            ),
          ),
          const Divider(height: 1),
        ],
      ),
    );
  }

  Widget _payslipsView() {
    if (controller.myPayslips.isEmpty) {
      return const _Panel(
        child: Text('No approved payslips are available for your login.'),
      );
    }
    return _Panel(
      child: Column(
        children: [
          for (final line in controller.myPayslips) _payslipTile(line),
        ],
      ),
    );
  }

  Widget _payslipTile(PayrollLine line) {
    final period = line.periodStart == null || line.periodEnd == null
        ? line.runNumber
        : '${_payrollDate(line.periodStart!)} – ${_payrollDate(line.periodEnd!)}';
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 4, vertical: 3),
      leading: const CircleAvatar(
        backgroundColor: Color(0xFFE8F5E9),
        child: Icon(Icons.request_quote_rounded, color: AppColors.green),
      ),
      title: Text(
        money(line.netPay),
        style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 17),
      ),
      subtitle: Text('$period · Gross ${money(line.grossPay)}'),
      trailing: IconButton(
        tooltip: 'Payslip breakdown',
        onPressed: () => _showPayslip(line),
        icon: const Icon(Icons.chevron_right_rounded),
      ),
    );
  }

  Future<void> _refresh() async {
    try {
      await controller.refreshPayroll();
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _clockInDialog() async {
    var shift = 'Day shift';
    final proceed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Start shift'),
        content: DropdownButtonFormField<String>(
          initialValue: shift,
          decoration: const InputDecoration(labelText: 'Shift'),
          items: const [
            DropdownMenuItem(value: 'Breakfast', child: Text('Breakfast')),
            DropdownMenuItem(value: 'Day shift', child: Text('Day shift')),
            DropdownMenuItem(value: 'Evening', child: Text('Evening')),
            DropdownMenuItem(value: 'Closing', child: Text('Closing')),
          ],
          onChanged: (value) => shift = value ?? shift,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Clock in'),
          ),
        ],
      ),
    );
    if (proceed != true) return;
    try {
      await controller.payrollClockIn(shiftName: shift);
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _clockOutDialog() async {
    final breakController = TextEditingController(text: '30');
    final proceed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Complete shift'),
        content: TextField(
          controller: breakController,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(labelText: 'Break minutes'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Clock out'),
          ),
        ],
      ),
    );
    if (proceed != true) return;
    try {
      await controller.payrollClockOut(
        breakMinutes: int.tryParse(breakController.text) ?? 0,
      );
    } on ApiException catch (error) {
      _showError(error.message);
    } finally {
      breakController.dispose();
    }
  }

  Future<void> _employeeDialog([PayrollEmployee? employee]) async {
    final formKey = GlobalKey<FormState>();
    final staffCode = TextEditingController(text: employee?.staffCode ?? '');
    final name = TextEditingController(text: employee?.name ?? '');
    final role = TextEditingController(text: employee?.jobRole ?? '');
    final department = TextEditingController(text: employee?.department ?? '');
    final basic = TextEditingController(text: '${employee?.basicSalary ?? 0}');
    final hourly = TextEditingController(text: '${employee?.hourlyRate ?? 0}');
    final overtime = TextEditingController(
      text: '${employee?.overtimeRate ?? 0}',
    );
    final allowance = TextEditingController(
      text: '${employee?.fixedAllowance ?? 0}',
    );
    final deduction = TextEditingController(
      text: '${employee?.fixedDeduction ?? 0}',
    );
    final weight = TextEditingController(
      text: '${employee?.serviceChargeWeight ?? 1}',
    );
    final bank = TextEditingController(text: employee?.bankAccountNumber ?? '');
    final pan = TextEditingController(text: employee?.panNumber ?? '');
    final ssf = TextEditingController(text: employee?.ssfNumber ?? '');
    var type = employee?.employmentType ?? PayrollEmploymentType.monthly;
    var active = employee?.isActive ?? true;
    var joinedOn = employee?.joinedOn ?? DateTime.now();
    final canManageLoginAccess = controller.hasPermission(
      'Pages.Restaurant.Payroll.Staff.Access',
    );
    var loginMode = employee?.userId == null
        ? EmployeeLoginMode.none
        : EmployeeLoginMode.linkExisting;
    var userId = employee?.userId;
    var loginIsActive = employee?.loginIsActive ?? true;
    var restaurantRoleName = employee?.restaurantRoleName.isNotEmpty == true
        ? employee!.restaurantRoleName
        : _recommendedRestaurantRole(role.text);
    var restaurantRoleChosenManually =
        employee?.restaurantRoleName.isNotEmpty == true;
    final loginUserName = TextEditingController(
      text: employee?.loginUserName ?? '',
    );
    final loginEmail = TextEditingController(
      text: employee?.loginEmailAddress ?? '',
    );
    final loginPhone = TextEditingController(
      text: employee?.loginPhoneNumber ?? '',
    );
    final userOptions = <PayrollUserOption>[
      if (employee?.userId != null)
        PayrollUserOption(
          id: employee!.userId!,
          userName: employee.loginUserName,
          name: employee.name,
          emailAddress: employee.loginEmailAddress,
          phoneNumber: employee.loginPhoneNumber,
          restaurantRoleName: employee.restaurantRoleName,
          isActive: employee.loginIsActive ?? true,
        ),
      ...controller.payrollUserOptions.where(
        (item) => item.id != employee?.userId,
      ),
    ];
    final roleOptions = <PayrollRoleOption>[
      if (employee?.restaurantRoleName.isNotEmpty == true &&
          !controller.payrollRoleOptions.any(
            (item) => item.name == employee!.restaurantRoleName,
          ))
        PayrollRoleOption(
          name: employee!.restaurantRoleName,
          displayName: _restaurantRoleDisplayName(employee.restaurantRoleName),
        ),
      ...controller.payrollRoleOptions,
    ];

    final saved = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(employee == null ? 'Add restaurant staff' : 'Edit staff'),
          content: SizedBox(
            width: 620,
            child: Form(
              key: formKey,
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: _requiredField(staffCode, 'Staff code'),
                        ),
                        const SizedBox(width: 10),
                        Expanded(child: _requiredField(name, 'Full name')),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Expanded(
                          child: _requiredField(
                            role,
                            'Job role',
                            onChanged: (value) {
                              if (canManageLoginAccess &&
                                  !restaurantRoleChosenManually) {
                                setDialogState(
                                  () => restaurantRoleName =
                                      _recommendedRestaurantRole(value),
                                );
                              }
                            },
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: _requiredField(department, 'Department'),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    const Divider(height: 28),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Text(
                        'App login & access',
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w900),
                      ),
                    ),
                    const SizedBox(height: 4),
                    if (!canManageLoginAccess)
                      Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          employee?.userId == null
                              ? 'No app login. Only an Admin or Restaurant Manager can grant access.'
                              : 'Linked to ${employee!.loginUserName}. Only an Admin or Restaurant Manager can change access.',
                          style: const TextStyle(
                            color: AppColors.muted,
                            height: 1.4,
                          ),
                        ),
                      )
                    else ...[
                      const Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          'Job role describes the employee. The selected app role controls permissions.',
                          style: TextStyle(color: AppColors.muted, height: 1.4),
                        ),
                      ),
                      const SizedBox(height: 10),
                      DropdownButtonFormField<EmployeeLoginMode>(
                        initialValue: loginMode,
                        decoration: const InputDecoration(
                          labelText: 'Login setup',
                        ),
                        items: const [
                          DropdownMenuItem(
                            value: EmployeeLoginMode.none,
                            child: Text('No restaurant app access'),
                          ),
                          DropdownMenuItem(
                            value: EmployeeLoginMode.createNew,
                            child: Text('Create a new login'),
                          ),
                          DropdownMenuItem(
                            value: EmployeeLoginMode.linkExisting,
                            child: Text('Link an existing login'),
                          ),
                        ],
                        onChanged: (value) => setDialogState(() {
                          loginMode = value ?? EmployeeLoginMode.none;
                          restaurantRoleName ??= _recommendedRestaurantRole(
                            role.text,
                          );
                          if (loginMode == EmployeeLoginMode.createNew &&
                              employee?.userId != null) {
                            userId = null;
                            loginUserName.clear();
                            loginEmail.clear();
                            loginPhone.clear();
                          }
                        }),
                      ),
                      if (loginMode == EmployeeLoginMode.linkExisting) ...[
                        const SizedBox(height: 10),
                        DropdownButtonFormField<int>(
                          isExpanded: true,
                          initialValue: userId,
                          decoration: const InputDecoration(
                            labelText: 'Existing login user',
                          ),
                          items: [
                            for (final user in userOptions)
                              DropdownMenuItem<int>(
                                value: user.id,
                                child: Text('${user.name} (${user.userName})'),
                              ),
                          ],
                          validator: (value) => value == null
                              ? 'Choose an existing login user'
                              : null,
                          onChanged: (value) => setDialogState(() {
                            userId = value;
                            final selected = userOptions
                                .where((item) => item.id == value)
                                .firstOrNull;
                            if (selected != null) {
                              loginIsActive = selected.isActive;
                              if (selected.restaurantRoleName.isNotEmpty) {
                                restaurantRoleName =
                                    selected.restaurantRoleName;
                              }
                            }
                          }),
                        ),
                      ],
                      if (loginMode == EmployeeLoginMode.createNew) ...[
                        const SizedBox(height: 10),
                        TextFormField(
                          controller: loginUserName,
                          decoration: const InputDecoration(
                            labelText: 'Username',
                            helperText: 'Used to sign in to the restaurant app',
                          ),
                          validator: (value) =>
                              value == null || value.trim().isEmpty
                              ? 'Username is required'
                              : null,
                        ),
                        const SizedBox(height: 10),
                        TextFormField(
                          controller: loginEmail,
                          keyboardType: TextInputType.emailAddress,
                          decoration: const InputDecoration(
                            labelText: 'Email (optional)',
                            helperText:
                                'Leave blank when the employee has no email',
                          ),
                          validator: (value) =>
                              value != null &&
                                  value.trim().isNotEmpty &&
                                  !value.contains('@')
                              ? 'Enter a valid email address'
                              : null,
                        ),
                        const SizedBox(height: 10),
                        TextFormField(
                          controller: loginPhone,
                          keyboardType: TextInputType.phone,
                          decoration: const InputDecoration(
                            labelText: 'Phone number (optional)',
                          ),
                        ),
                      ],
                      if (loginMode != EmployeeLoginMode.none) ...[
                        const SizedBox(height: 10),
                        DropdownButtonFormField<String>(
                          isExpanded: true,
                          initialValue: restaurantRoleName,
                          decoration: const InputDecoration(
                            labelText: 'Primary app role',
                          ),
                          items: [
                            for (final item in roleOptions)
                              DropdownMenuItem(
                                value: item.name,
                                child: Text(item.displayName),
                              ),
                          ],
                          validator: (value) => value == null || value.isEmpty
                              ? 'Choose an app role'
                              : null,
                          onChanged: (value) {
                            restaurantRoleName = value;
                            restaurantRoleChosenManually = value != null;
                          },
                        ),
                        SwitchListTile.adaptive(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Login active'),
                          subtitle: const Text(
                            'Inactive employees are always blocked from signing in.',
                          ),
                          value: loginIsActive && active,
                          onChanged: active
                              ? (value) =>
                                    setDialogState(() => loginIsActive = value)
                              : null,
                        ),
                      ],
                    ],
                    const SizedBox(height: 10),
                    DropdownButtonFormField<PayrollEmploymentType>(
                      initialValue: type,
                      decoration: const InputDecoration(
                        labelText: 'Employment type',
                      ),
                      items: const [
                        DropdownMenuItem(
                          value: PayrollEmploymentType.monthly,
                          child: Text('Monthly salary'),
                        ),
                        DropdownMenuItem(
                          value: PayrollEmploymentType.hourly,
                          child: Text('Hourly wage'),
                        ),
                      ],
                      onChanged: (value) =>
                          setDialogState(() => type = value ?? type),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Expanded(
                          child: _numberField(
                            type == PayrollEmploymentType.monthly
                                ? basic
                                : hourly,
                            type == PayrollEmploymentType.monthly
                                ? 'Monthly salary'
                                : 'Hourly rate',
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: _numberField(overtime, 'Overtime rate / hour'),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Expanded(child: _numberField(allowance, 'Allowance')),
                        const SizedBox(width: 10),
                        Expanded(child: _numberField(deduction, 'Deduction')),
                        const SizedBox(width: 10),
                        Expanded(
                          child: _numberField(weight, 'Service share weight'),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Expanded(child: _plainField(bank, 'Bank account')),
                        const SizedBox(width: 10),
                        Expanded(child: _plainField(pan, 'PAN number')),
                        const SizedBox(width: 10),
                        Expanded(child: _plainField(ssf, 'SSF number')),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        OutlinedButton.icon(
                          onPressed: () async {
                            final selected = await showDatePicker(
                              context: context,
                              firstDate: DateTime(1990),
                              lastDate: DateTime.now(),
                              initialDate: joinedOn,
                            );
                            if (selected != null) {
                              setDialogState(() => joinedOn = selected);
                            }
                          },
                          icon: const Icon(Icons.event_rounded),
                          label: Text('Joined ${_payrollDate(joinedOn)}'),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: SwitchListTile.adaptive(
                            contentPadding: EdgeInsets.zero,
                            title: const Text('Active'),
                            value: active,
                            onChanged: (value) =>
                                setDialogState(() => active = value),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () {
                if (formKey.currentState?.validate() ?? false) {
                  Navigator.pop(context, true);
                }
              },
              child: const Text('Save staff'),
            ),
          ],
        ),
      ),
    );
    var proceedWithSave = saved == true;
    if (proceedWithSave &&
        canManageLoginAccess &&
        employee?.userId != null &&
        loginMode == EmployeeLoginMode.none) {
      proceedWithSave = await _confirm(
        'Remove app access?',
        'This unlinks ${employee!.name}, removes restaurant operating roles and disables a login that has no other application role. Payroll history is kept.',
      );
    }
    if (proceedWithSave) {
      try {
        final result = await controller.savePayrollEmployee(
          employee: employee,
          userId: userId,
          updateLoginAccess: canManageLoginAccess,
          loginMode: loginMode,
          loginUserName: loginUserName.text.trim(),
          loginEmailAddress: loginEmail.text.trim(),
          loginPhoneNumber: loginPhone.text.trim(),
          restaurantRoleName: restaurantRoleName ?? '',
          loginIsActive: loginIsActive,
          staffCode: staffCode.text.trim(),
          name: name.text.trim(),
          jobRole: role.text.trim(),
          department: department.text.trim(),
          employmentType: type,
          basicSalary: double.tryParse(basic.text) ?? 0,
          hourlyRate: double.tryParse(hourly.text) ?? 0,
          overtimeRate: double.tryParse(overtime.text) ?? 0,
          fixedAllowance: double.tryParse(allowance.text) ?? 0,
          fixedDeduction: double.tryParse(deduction.text) ?? 0,
          serviceChargeWeight: double.tryParse(weight.text) ?? 1,
          bankAccountNumber: bank.text.trim(),
          panNumber: pan.text.trim(),
          ssfNumber: ssf.text.trim(),
          joinedOn: joinedOn,
          isActive: active,
        );
        if (result.temporaryPassword.isNotEmpty && mounted) {
          await _showTemporaryCredentials(result);
        }
      } on ApiException catch (error) {
        _showError(error.message);
      }
    }
    for (final item in [
      staffCode,
      name,
      role,
      department,
      basic,
      hourly,
      overtime,
      allowance,
      deduction,
      weight,
      bank,
      pan,
      ssf,
      loginUserName,
      loginEmail,
      loginPhone,
    ]) {
      item.dispose();
    }
  }

  String? _recommendedRestaurantRole(String jobRole) {
    final value = jobRole.toLowerCase();
    final preferred = value.contains('manager')
        ? 'RestaurantManager'
        : value.contains('payroll') || value.contains('hr')
        ? 'RestaurantPayroll'
        : value.contains('cashier') || value.contains('counter')
        ? 'RestaurantCashier'
        : value.contains('chef') ||
              value.contains('cook') ||
              value.contains('kitchen')
        ? 'RestaurantKitchen'
        : value.contains('inventory') ||
              value.contains('store') ||
              value.contains('purchase')
        ? 'RestaurantInventory'
        : 'RestaurantWaiter';
    final options = controller.payrollRoleOptions;
    if (options.any((item) => item.name == preferred)) return preferred;
    return options.firstOrNull?.name;
  }

  Future<void> _showTemporaryCredentials(
    PayrollEmployeeSaveResult result,
  ) async {
    await showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Employee login created'),
        content: SizedBox(
          width: 440,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Share these credentials securely. The temporary password is shown only once and must be changed after the first login.',
                style: TextStyle(color: AppColors.muted, height: 1.4),
              ),
              const SizedBox(height: 16),
              const Text(
                'Username',
                style: TextStyle(fontWeight: FontWeight.w800),
              ),
              SelectableText(result.userName),
              const SizedBox(height: 12),
              const Text(
                'Temporary password',
                style: TextStyle(fontWeight: FontWeight.w800),
              ),
              SelectableText(result.temporaryPassword),
            ],
          ),
        ),
        actions: [
          TextButton.icon(
            onPressed: () => Clipboard.setData(
              ClipboardData(
                text:
                    'Username: ${result.userName}\nTemporary password: ${result.temporaryPassword}',
              ),
            ),
            icon: const Icon(Icons.copy_rounded),
            label: const Text('Copy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext),
            child: const Text('Done'),
          ),
        ],
      ),
    );
  }

  Future<void> _attendanceDialog([PayrollAttendance? attendance]) async {
    if (controller.payrollEmployees.isEmpty) {
      _showError('Add payroll employees before recording attendance.');
      return;
    }
    var employeeId =
        attendance?.employeeId ?? controller.payrollEmployees.first.id;
    var date = attendance?.workDate ?? DateTime.now();
    var status = attendance?.status ?? PayrollAttendanceStatus.present;
    final shift = TextEditingController(
      text: attendance?.shiftName ?? 'Day shift',
    );
    final regular = TextEditingController(
      text: '${attendance?.regularHours ?? 8}',
    );
    final overtime = TextEditingController(
      text: '${attendance?.overtimeHours ?? 0}',
    );
    final breaks = TextEditingController(
      text: '${attendance?.breakMinutes ?? 30}',
    );
    final notes = TextEditingController(text: attendance?.notes ?? '');
    final saved = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text(
            attendance == null ? 'Add attendance' : 'Edit attendance',
          ),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  DropdownButtonFormField<String>(
                    initialValue: employeeId,
                    decoration: const InputDecoration(labelText: 'Employee'),
                    items: [
                      for (final employee in controller.payrollEmployees)
                        DropdownMenuItem(
                          value: employee.id,
                          child: Text(
                            '${employee.name} (${employee.staffCode})',
                          ),
                        ),
                    ],
                    onChanged: (value) => employeeId = value ?? employeeId,
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(
                        child: DropdownButtonFormField<PayrollAttendanceStatus>(
                          initialValue: status,
                          decoration: const InputDecoration(
                            labelText: 'Status',
                          ),
                          items: [
                            for (final item in PayrollAttendanceStatus.values)
                              DropdownMenuItem(
                                value: item,
                                child: Text(_attendanceLabel(item)),
                              ),
                          ],
                          onChanged: (value) => status = value ?? status,
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: () async {
                            final selected = await showDatePicker(
                              context: context,
                              firstDate: DateTime(2020),
                              lastDate: DateTime.now().add(
                                const Duration(days: 31),
                              ),
                              initialDate: date,
                            );
                            if (selected != null) {
                              setDialogState(() => date = selected);
                            }
                          },
                          icon: const Icon(Icons.event_rounded),
                          label: Text(_payrollDate(date)),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  _plainField(shift, 'Shift name'),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      Expanded(child: _numberField(regular, 'Regular hours')),
                      const SizedBox(width: 10),
                      Expanded(child: _numberField(overtime, 'Overtime hours')),
                      const SizedBox(width: 10),
                      Expanded(child: _numberField(breaks, 'Break minutes')),
                    ],
                  ),
                  const SizedBox(height: 10),
                  _plainField(notes, 'Notes'),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Save attendance'),
            ),
          ],
        ),
      ),
    );
    if (saved == true) {
      try {
        await controller.savePayrollAttendance(
          attendance: attendance,
          employeeId: employeeId,
          workDate: date,
          breakMinutes: int.tryParse(breaks.text) ?? 0,
          regularHours: double.tryParse(regular.text),
          overtimeHours: double.tryParse(overtime.text),
          status: status,
          shiftName: shift.text.trim(),
          notes: notes.text.trim(),
        );
      } on ApiException catch (error) {
        _showError(error.message);
      }
    }
    shift.dispose();
    regular.dispose();
    overtime.dispose();
    breaks.dispose();
    notes.dispose();
  }

  Future<void> _generatePayrollDialog() async {
    final now = DateTime.now();
    var start = DateTime(now.year, now.month, 1);
    var end = DateTime(now.year, now.month + 1, 0);
    final tips = TextEditingController(text: '0');
    final service = TextEditingController(text: '0');
    final notes = TextEditingController();
    final generated = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Generate restaurant payroll'),
          content: SizedBox(
            width: 520,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children: [
                    OutlinedButton.icon(
                      onPressed: () async {
                        final selected = await showDatePicker(
                          context: context,
                          firstDate: DateTime(2020),
                          lastDate: DateTime.now().add(
                            const Duration(days: 366),
                          ),
                          initialDate: start,
                        );
                        if (selected != null) {
                          setDialogState(() => start = selected);
                        }
                      },
                      icon: const Icon(Icons.first_page_rounded),
                      label: Text('From ${_payrollDate(start)}'),
                    ),
                    OutlinedButton.icon(
                      onPressed: () async {
                        final selected = await showDatePicker(
                          context: context,
                          firstDate: start,
                          lastDate: DateTime.now().add(
                            const Duration(days: 366),
                          ),
                          initialDate: end.isBefore(start) ? start : end,
                        );
                        if (selected != null) {
                          setDialogState(() => end = selected);
                        }
                      },
                      icon: const Icon(Icons.last_page_rounded),
                      label: Text('To ${_payrollDate(end)}'),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(child: _numberField(tips, 'Tips pool')),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _numberField(service, 'Service charge pool'),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                _plainField(notes, 'Notes'),
                const SizedBox(height: 10),
                const Text(
                  'Hourly wages use recorded hours. Monthly salaries are prorated for the selected period. Tips and service charge are distributed by staff weight among employees who attended.',
                  style: TextStyle(color: AppColors.muted, height: 1.4),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Generate draft'),
            ),
          ],
        ),
      ),
    );
    if (generated == true) {
      try {
        final id = await controller.generatePayrollRun(
          periodStart: start,
          periodEnd: end,
          tipsPool: double.tryParse(tips.text) ?? 0,
          serviceChargePool: double.tryParse(service.text) ?? 0,
          notes: notes.text.trim(),
        );
        if (id.isNotEmpty && mounted) {
          final match = controller.payrollRuns.where((item) => item.id == id);
          if (match.isNotEmpty) await _showRun(match.first);
        }
      } on ApiException catch (error) {
        _showError(error.message);
      }
    }
    tips.dispose();
    service.dispose();
    notes.dispose();
  }

  Future<void> _showRun(PayrollRun run) async {
    try {
      final detail = await controller.getPayrollRunDetail(run.id);
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('${run.runNumber} · ${_runLabel(run.status)}'),
          content: SizedBox(
            width: 760,
            height: math.min(560, MediaQuery.sizeOf(context).height * 0.7),
            child: ListView(
              children: [
                Wrap(
                  spacing: 16,
                  runSpacing: 8,
                  children: [
                    Text('Gross ${money(run.totalGross)}'),
                    Text('Deductions ${money(run.totalDeduction)}'),
                    Text(
                      'Net ${money(run.totalNet)}',
                      style: const TextStyle(fontWeight: FontWeight.w900),
                    ),
                  ],
                ),
                const Divider(height: 24),
                for (final line in detail.lines)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(
                      line.employeeName,
                      style: const TextStyle(fontWeight: FontWeight.w800),
                    ),
                    subtitle: Text(
                      '${line.staffCode} · Base ${money(line.basicPay)} · OT ${money(line.overtimePay)} · Tips ${money(line.tipsShare)} · Service ${money(line.serviceChargeShare)}',
                    ),
                    trailing: Text(
                      money(line.netPay),
                      style: const TextStyle(
                        fontWeight: FontWeight.w900,
                        color: AppColors.green,
                      ),
                    ),
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Close'),
            ),
          ],
        ),
      );
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _showPayslip(PayrollLine line) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(line.runNumber.isEmpty ? 'Payslip' : line.runNumber),
        content: SizedBox(
          width: 460,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _payslipRow('Base pay', line.basicPay),
              _payslipRow('Overtime', line.overtimePay),
              _payslipRow('Allowances', line.allowance),
              _payslipRow('Tips share', line.tipsShare),
              _payslipRow('Service charge share', line.serviceChargeShare),
              const Divider(),
              _payslipRow('Gross pay', line.grossPay, strong: true),
              _payslipRow('Tax', -line.taxDeduction),
              _payslipRow('Other deductions', -line.otherDeduction),
              _payslipRow('Advance recovery', -line.advanceRecovery),
              const Divider(),
              _payslipRow('Net pay', line.netPay, strong: true),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Close'),
          ),
        ],
      ),
    );
  }

  Widget _payslipRow(String label, double amount, {bool strong = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: TextStyle(fontWeight: strong ? FontWeight.w900 : null),
            ),
          ),
          Text(
            money(amount),
            style: TextStyle(fontWeight: strong ? FontWeight.w900 : null),
          ),
        ],
      ),
    );
  }

  Future<void> _approveRun(PayrollRun run) async {
    if (!await _confirm(
      'Approve ${run.runNumber}?',
      'Approved payroll can no longer be regenerated.',
    )) {
      return;
    }
    try {
      await controller.approvePayrollRun(run.id);
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _markPaid(PayrollRun run) async {
    if (!await _confirm(
      'Mark ${run.runNumber} paid?',
      'This records the payroll period as paid.',
    )) {
      return;
    }
    try {
      await controller.markPayrollRunPaid(run.id);
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<void> _deleteRun(PayrollRun run) async {
    if (!await _confirm(
      'Delete ${run.runNumber}?',
      'Only this draft and its calculated lines will be removed.',
    )) {
      return;
    }
    try {
      await controller.deleteDraftPayrollRun(run.id);
    } on ApiException catch (error) {
      _showError(error.message);
    }
  }

  Future<bool> _confirm(String title, String message) async {
    return await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: Text(title),
            content: Text(message),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Cancel'),
              ),
              FilledButton(
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Continue'),
              ),
            ],
          ),
        ) ??
        false;
  }

  TextFormField _requiredField(
    TextEditingController value,
    String label, {
    ValueChanged<String>? onChanged,
  }) {
    return TextFormField(
      controller: value,
      onChanged: onChanged,
      decoration: InputDecoration(labelText: label),
      validator: (text) =>
          text == null || text.trim().isEmpty ? 'Required' : null,
    );
  }

  TextFormField _plainField(TextEditingController value, String label) {
    return TextFormField(
      controller: value,
      decoration: InputDecoration(labelText: label),
    );
  }

  TextFormField _numberField(TextEditingController value, String label) {
    return TextFormField(
      controller: value,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      decoration: InputDecoration(labelText: label),
      validator: (text) {
        final number = double.tryParse(text ?? '');
        return number == null || number < 0 ? 'Enter 0 or more' : null;
      },
    );
  }

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: AppColors.red),
    );
  }
}

class _StaffIdentity extends StatelessWidget {
  const _StaffIdentity({required this.employee});

  final PayrollEmployee employee;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        CircleAvatar(
          backgroundColor: AppColors.primary.withValues(alpha: 0.12),
          child: Text(
            employee.name.isEmpty ? '?' : employee.name[0].toUpperCase(),
            style: const TextStyle(
              color: AppColors.primary,
              fontWeight: FontWeight.w900,
            ),
          ),
        ),
        const SizedBox(width: 10),
        Flexible(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                employee.name,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontWeight: FontWeight.w900),
              ),
              Text(
                '${employee.staffCode} · ${employee.jobRole.isEmpty ? employee.department : employee.jobRole}',
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(color: AppColors.muted, fontSize: 12),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _PayrollSectionTitle extends StatelessWidget {
  const _PayrollSectionTitle({
    required this.title,
    required this.subtitle,
    required this.icon,
  });

  final String title;
  final String subtitle;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, color: AppColors.primary),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontWeight: FontWeight.w900,
                  fontSize: 16,
                ),
              ),
              Text(
                subtitle,
                style: const TextStyle(color: AppColors.muted, fontSize: 12),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _PayrollTag extends StatelessWidget {
  const _PayrollTag({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.11),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 11.5,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

String _payrollDate(DateTime value) =>
    '${value.year}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';

String _payrollTime(DateTime value) =>
    '${value.hour.toString().padLeft(2, '0')}:${value.minute.toString().padLeft(2, '0')}';

String _attendanceLabel(PayrollAttendanceStatus status) => switch (status) {
  PayrollAttendanceStatus.present => 'Present',
  PayrollAttendanceStatus.late => 'Late',
  PayrollAttendanceStatus.leave => 'Leave',
  PayrollAttendanceStatus.absent => 'Absent',
};

Color _attendanceColor(PayrollAttendanceStatus status) => switch (status) {
  PayrollAttendanceStatus.present => AppColors.green,
  PayrollAttendanceStatus.late => AppColors.amber,
  PayrollAttendanceStatus.leave => AppColors.primary,
  PayrollAttendanceStatus.absent => AppColors.red,
};

String _restaurantRoleDisplayName(String roleName) => switch (roleName) {
  'RestaurantManager' => 'Restaurant Manager',
  'RestaurantCashier' => 'Restaurant Cashier',
  'RestaurantWaiter' => 'Restaurant Waiter',
  'RestaurantKitchen' => 'Restaurant Kitchen',
  'RestaurantInventory' => 'Restaurant Inventory',
  'RestaurantPayroll' => 'Restaurant Payroll',
  _ => roleName,
};

String _runLabel(PayrollRunStatus status) => switch (status) {
  PayrollRunStatus.draft => 'Draft',
  PayrollRunStatus.approved => 'Approved',
  PayrollRunStatus.paid => 'Paid',
};

Color _runColor(PayrollRunStatus status) => switch (status) {
  PayrollRunStatus.draft => AppColors.amber,
  PayrollRunStatus.approved => AppColors.primary,
  PayrollRunStatus.paid => AppColors.green,
};
