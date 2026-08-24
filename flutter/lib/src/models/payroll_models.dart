part of '../../main.dart';

enum PayrollEmploymentType { monthly, hourly }

enum PayrollAttendanceStatus { present, late, leave, absent }

enum PayrollRunStatus { draft, approved, paid }

enum EmployeeLoginMode { none, linkExisting, createNew }

class PayrollEmployee {
  const PayrollEmployee({
    required this.id,
    required this.staffCode,
    required this.name,
    required this.jobRole,
    required this.department,
    required this.employmentType,
    required this.basicSalary,
    required this.hourlyRate,
    required this.overtimeRate,
    required this.fixedAllowance,
    required this.fixedDeduction,
    required this.serviceChargeWeight,
    required this.joinedOn,
    required this.isActive,
    this.userId,
    this.loginUserName = '',
    this.loginEmailAddress = '',
    this.loginPhoneNumber = '',
    this.restaurantRoleName = '',
    this.loginIsActive,
    this.bankAccountNumber = '',
    this.panNumber = '',
    this.ssfNumber = '',
  });

  factory PayrollEmployee.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollEmployee(
      id: _string(item['id']),
      userId: item['userId'] == null ? null : _integer(item['userId']),
      loginUserName: _string(item['loginUserName']),
      loginEmailAddress: _string(item['loginEmailAddress']),
      loginPhoneNumber: _string(item['loginPhoneNumber']),
      restaurantRoleName: _string(item['restaurantRoleName']),
      loginIsActive: item['loginIsActive'] == null
          ? null
          : _boolean(item['loginIsActive']),
      staffCode: _string(item['staffCode']),
      name: _string(item['name'], fallback: 'Staff member'),
      jobRole: _string(item['jobRole']),
      department: _string(item['department']),
      employmentType: _integer(item['employmentType']) == 1
          ? PayrollEmploymentType.hourly
          : PayrollEmploymentType.monthly,
      basicSalary: _number(item['basicSalary']),
      hourlyRate: _number(item['hourlyRate']),
      overtimeRate: _number(item['overtimeRate']),
      fixedAllowance: _number(item['fixedAllowance']),
      fixedDeduction: _number(item['fixedDeduction']),
      serviceChargeWeight: _number(item['serviceChargeWeight']),
      bankAccountNumber: _string(item['bankAccountNumber']),
      panNumber: _string(item['panNumber']),
      ssfNumber: _string(item['ssfNumber']),
      joinedOn:
          DateTime.tryParse(_string(item['joinedOn']))?.toLocal() ??
          DateTime.now(),
      isActive: _boolean(item['isActive'], fallback: true),
    );
  }

  final String id;
  final int? userId;
  final String loginUserName;
  final String loginEmailAddress;
  final String loginPhoneNumber;
  final String restaurantRoleName;
  final bool? loginIsActive;
  final String staffCode;
  final String name;
  final String jobRole;
  final String department;
  final PayrollEmploymentType employmentType;
  final double basicSalary;
  final double hourlyRate;
  final double overtimeRate;
  final double fixedAllowance;
  final double fixedDeduction;
  final double serviceChargeWeight;
  final String bankAccountNumber;
  final String panNumber;
  final String ssfNumber;
  final DateTime joinedOn;
  final bool isActive;
}

class PayrollUserOption {
  const PayrollUserOption({
    required this.id,
    required this.userName,
    required this.name,
    this.emailAddress = '',
    this.phoneNumber = '',
    this.restaurantRoleName = '',
    this.isActive = true,
  });

  factory PayrollUserOption.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollUserOption(
      id: _integer(item['id']),
      userName: _string(item['userName']),
      name: _string(item['name'], fallback: _string(item['userName'])),
      emailAddress: _string(item['emailAddress']),
      phoneNumber: _string(item['phoneNumber']),
      restaurantRoleName: _string(item['restaurantRoleName']),
      isActive: _boolean(item['isActive'], fallback: true),
    );
  }

  final int id;
  final String userName;
  final String name;
  final String emailAddress;
  final String phoneNumber;
  final String restaurantRoleName;
  final bool isActive;
}

class PayrollRoleOption {
  const PayrollRoleOption({required this.name, required this.displayName});

  factory PayrollRoleOption.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollRoleOption(
      name: _string(item['name']),
      displayName: _string(
        item['displayName'],
        fallback: _string(item['name']),
      ),
    );
  }

  final String name;
  final String displayName;
}

class PayrollStaffAccessOptions {
  const PayrollStaffAccessOptions({
    required this.roles,
    required this.availableUsers,
  });

  factory PayrollStaffAccessOptions.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollStaffAccessOptions(
      roles: _list(item['roles']).map(PayrollRoleOption.fromJson).toList(),
      availableUsers: _list(
        item['availableUsers'],
      ).map(PayrollUserOption.fromJson).toList(),
    );
  }

  final List<PayrollRoleOption> roles;
  final List<PayrollUserOption> availableUsers;
}

class PayrollEmployeeSaveResult {
  const PayrollEmployeeSaveResult({
    required this.employeeId,
    this.userId,
    this.userName = '',
    this.temporaryPassword = '',
  });

  factory PayrollEmployeeSaveResult.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollEmployeeSaveResult(
      employeeId: _string(item['employeeId']),
      userId: item['userId'] == null ? null : _integer(item['userId']),
      userName: _string(item['userName']),
      temporaryPassword: _string(item['temporaryPassword']),
    );
  }

  final String employeeId;
  final int? userId;
  final String userName;
  final String temporaryPassword;
}

class PayrollAttendance {
  const PayrollAttendance({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.staffCode,
    required this.workDate,
    required this.breakMinutes,
    required this.regularHours,
    required this.overtimeHours,
    required this.status,
    required this.shiftName,
    required this.notes,
    this.clockIn,
    this.clockOut,
  });

  factory PayrollAttendance.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollAttendance(
      id: _string(item['id']),
      employeeId: _string(item['employeeId']),
      employeeName: _string(item['employeeName'], fallback: 'Staff member'),
      staffCode: _string(item['staffCode']),
      workDate:
          DateTime.tryParse(_string(item['workDate']))?.toLocal() ??
          DateTime.now(),
      clockIn: DateTime.tryParse(_string(item['clockIn']))?.toLocal(),
      clockOut: DateTime.tryParse(_string(item['clockOut']))?.toLocal(),
      breakMinutes: _integer(item['breakMinutes']),
      regularHours: _number(item['regularHours']),
      overtimeHours: _number(item['overtimeHours']),
      status:
          PayrollAttendanceStatus.values[_integer(
            item['status'],
          ).clamp(0, PayrollAttendanceStatus.values.length - 1)],
      shiftName: _string(item['shiftName']),
      notes: _string(item['notes']),
    );
  }

  final String id;
  final String employeeId;
  final String employeeName;
  final String staffCode;
  final DateTime workDate;
  final DateTime? clockIn;
  final DateTime? clockOut;
  final int breakMinutes;
  final double regularHours;
  final double overtimeHours;
  final PayrollAttendanceStatus status;
  final String shiftName;
  final String notes;
}

class PayrollRun {
  const PayrollRun({
    required this.id,
    required this.runNumber,
    required this.periodStart,
    required this.periodEnd,
    required this.status,
    required this.tipsPool,
    required this.serviceChargePool,
    required this.totalGross,
    required this.totalDeduction,
    required this.totalNet,
    required this.notes,
    required this.createdAt,
    required this.employeeCount,
  });

  factory PayrollRun.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollRun(
      id: _string(item['id']),
      runNumber: _string(item['runNumber']),
      periodStart:
          DateTime.tryParse(_string(item['periodStart']))?.toLocal() ??
          DateTime.now(),
      periodEnd:
          DateTime.tryParse(_string(item['periodEnd']))?.toLocal() ??
          DateTime.now(),
      status:
          PayrollRunStatus.values[_integer(
            item['status'],
          ).clamp(0, PayrollRunStatus.values.length - 1)],
      tipsPool: _number(item['tipsPool']),
      serviceChargePool: _number(item['serviceChargePool']),
      totalGross: _number(item['totalGross']),
      totalDeduction: _number(item['totalDeduction']),
      totalNet: _number(item['totalNet']),
      notes: _string(item['notes']),
      createdAt:
          DateTime.tryParse(_string(item['createdAt']))?.toLocal() ??
          DateTime.now(),
      employeeCount: _integer(item['employeeCount']),
    );
  }

  final String id;
  final String runNumber;
  final DateTime periodStart;
  final DateTime periodEnd;
  final PayrollRunStatus status;
  final double tipsPool;
  final double serviceChargePool;
  final double totalGross;
  final double totalDeduction;
  final double totalNet;
  final String notes;
  final DateTime createdAt;
  final int employeeCount;
}

class PayrollLine {
  const PayrollLine({
    required this.id,
    required this.employeeId,
    required this.employeeName,
    required this.staffCode,
    required this.jobRole,
    required this.employmentType,
    required this.workedHours,
    required this.overtimeHours,
    required this.basicPay,
    required this.overtimePay,
    required this.allowance,
    required this.tipsShare,
    required this.serviceChargeShare,
    required this.grossPay,
    required this.taxDeduction,
    required this.otherDeduction,
    required this.advanceRecovery,
    required this.netPay,
    required this.notes,
    required this.runNumber,
    this.periodStart,
    this.periodEnd,
    this.runStatus,
  });

  factory PayrollLine.fromJson(dynamic value) {
    final item = _map(value);
    final status = item['runStatus'];
    return PayrollLine(
      id: _string(item['id']),
      employeeId: _string(item['employeeId']),
      employeeName: _string(item['employeeName'], fallback: 'Staff member'),
      staffCode: _string(item['staffCode']),
      jobRole: _string(item['jobRole']),
      employmentType: _integer(item['employmentType']) == 1
          ? PayrollEmploymentType.hourly
          : PayrollEmploymentType.monthly,
      workedHours: _number(item['workedHours']),
      overtimeHours: _number(item['overtimeHours']),
      basicPay: _number(item['basicPay']),
      overtimePay: _number(item['overtimePay']),
      allowance: _number(item['allowance']),
      tipsShare: _number(item['tipsShare']),
      serviceChargeShare: _number(item['serviceChargeShare']),
      grossPay: _number(item['grossPay']),
      taxDeduction: _number(item['taxDeduction']),
      otherDeduction: _number(item['otherDeduction']),
      advanceRecovery: _number(item['advanceRecovery']),
      netPay: _number(item['netPay']),
      notes: _string(item['notes']),
      runNumber: _string(item['runNumber']),
      periodStart: DateTime.tryParse(_string(item['periodStart']))?.toLocal(),
      periodEnd: DateTime.tryParse(_string(item['periodEnd']))?.toLocal(),
      runStatus: status == null
          ? null
          : PayrollRunStatus.values[_integer(
              status,
            ).clamp(0, PayrollRunStatus.values.length - 1)],
    );
  }

  final String id;
  final String employeeId;
  final String employeeName;
  final String staffCode;
  final String jobRole;
  final PayrollEmploymentType employmentType;
  final double workedHours;
  final double overtimeHours;
  final double basicPay;
  final double overtimePay;
  final double allowance;
  final double tipsShare;
  final double serviceChargeShare;
  final double grossPay;
  final double taxDeduction;
  final double otherDeduction;
  final double advanceRecovery;
  final double netPay;
  final String notes;
  final String runNumber;
  final DateTime? periodStart;
  final DateTime? periodEnd;
  final PayrollRunStatus? runStatus;
}

class PayrollRunDetail {
  const PayrollRunDetail({required this.run, required this.lines});

  factory PayrollRunDetail.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollRunDetail(
      run: PayrollRun.fromJson(item),
      lines: _list(item['lines']).map(PayrollLine.fromJson).toList(),
    );
  }

  final PayrollRun run;
  final List<PayrollLine> lines;
}

class PayrollDashboard {
  const PayrollDashboard({
    required this.activeEmployeeCount,
    required this.presentToday,
    required this.openClockIns,
    required this.currentMonthGross,
    required this.currentMonthNet,
    required this.recentRuns,
    required this.myRecentPayslips,
    this.myProfile,
    this.myTodayAttendance,
  });

  factory PayrollDashboard.fromJson(dynamic value) {
    final item = _map(value);
    return PayrollDashboard(
      activeEmployeeCount: _integer(item['activeEmployeeCount']),
      presentToday: _integer(item['presentToday']),
      openClockIns: _integer(item['openClockIns']),
      currentMonthGross: _number(item['currentMonthGross']),
      currentMonthNet: _number(item['currentMonthNet']),
      myProfile: item['myProfile'] == null
          ? null
          : PayrollEmployee.fromJson(item['myProfile']),
      myTodayAttendance: item['myTodayAttendance'] == null
          ? null
          : PayrollAttendance.fromJson(item['myTodayAttendance']),
      recentRuns: _list(item['recentRuns']).map(PayrollRun.fromJson).toList(),
      myRecentPayslips: _list(
        item['myRecentPayslips'],
      ).map(PayrollLine.fromJson).toList(),
    );
  }

  final int activeEmployeeCount;
  final int presentToday;
  final int openClockIns;
  final double currentMonthGross;
  final double currentMonthNet;
  final PayrollEmployee? myProfile;
  final PayrollAttendance? myTodayAttendance;
  final List<PayrollRun> recentRuns;
  final List<PayrollLine> myRecentPayslips;
}
