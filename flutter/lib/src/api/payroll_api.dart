part of '../../main.dart';

extension RestaurantPayrollApi on RestaurantApi {
  Future<PayrollDashboard> getPayrollDashboard() async {
    return PayrollDashboard.fromJson(
      await client.get('/api/services/app/RestaurantPayroll/GetDashboard'),
    );
  }

  Future<List<PayrollEmployee>> getPayrollEmployees({
    bool includeInactive = false,
  }) async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantPayroll/GetEmployees',
        query: {'includeInactive': includeInactive},
      ),
    ).map(PayrollEmployee.fromJson).toList();
  }

  Future<PayrollStaffAccessOptions> getPayrollStaffAccessOptions() async {
    return PayrollStaffAccessOptions.fromJson(
      await client.get(
        '/api/services/app/RestaurantPayroll/GetStaffAccessOptions',
      ),
    );
  }

  Future<PayrollEmployeeSaveResult> savePayrollEmployee({
    String? id,
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
    return PayrollEmployeeSaveResult.fromJson(
      await client.post(
        '/api/services/app/RestaurantPayroll/CreateOrEditEmployee',
        body: {
          'id': ?id,
          'userId': ?userId,
          'updateLoginAccess': updateLoginAccess,
          'loginMode': loginMode.index,
          'loginUserName': loginUserName,
          'loginEmailAddress': loginEmailAddress,
          'loginPhoneNumber': loginPhoneNumber,
          'restaurantRoleName': restaurantRoleName,
          'loginIsActive': loginIsActive,
          'staffCode': staffCode,
          'name': name,
          'jobRole': jobRole,
          'department': department,
          'employmentType': employmentType.index,
          'basicSalary': basicSalary,
          'hourlyRate': hourlyRate,
          'overtimeRate': overtimeRate,
          'fixedAllowance': fixedAllowance,
          'fixedDeduction': fixedDeduction,
          'serviceChargeWeight': serviceChargeWeight,
          'bankAccountNumber': bankAccountNumber,
          'panNumber': panNumber,
          'ssfNumber': ssfNumber,
          'joinedOn': joinedOn.toIso8601String(),
          'isActive': isActive,
        },
      ),
    );
  }

  Future<List<PayrollAttendance>> getPayrollAttendance({
    required DateTime from,
    required DateTime to,
    String? employeeId,
  }) async {
    return _list(
      await client.get(
        '/api/services/app/RestaurantPayroll/GetAttendance',
        query: {
          'from': from.toIso8601String(),
          'to': to.toIso8601String(),
          if (employeeId != null && employeeId.isNotEmpty)
            'employeeId': employeeId,
        },
      ),
    ).map(PayrollAttendance.fromJson).toList();
  }

  Future<PayrollAttendance> payrollClockIn({
    String? employeeId,
    required String shiftName,
    int breakMinutes = 0,
  }) async {
    return PayrollAttendance.fromJson(
      await client.post(
        '/api/services/app/RestaurantPayroll/ClockIn',
        body: {
          'employeeId': ?employeeId,
          'at': DateTime.now().toIso8601String(),
          'shiftName': shiftName,
          'breakMinutes': breakMinutes,
        },
      ),
    );
  }

  Future<PayrollAttendance> payrollClockOut({
    String? employeeId,
    int breakMinutes = 0,
  }) async {
    return PayrollAttendance.fromJson(
      await client.post(
        '/api/services/app/RestaurantPayroll/ClockOut',
        body: {
          'employeeId': ?employeeId,
          'at': DateTime.now().toIso8601String(),
          'breakMinutes': breakMinutes,
        },
      ),
    );
  }

  Future<void> savePayrollAttendance({
    String? id,
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
    await client.post(
      '/api/services/app/RestaurantPayroll/SaveAttendance',
      body: {
        'id': ?id,
        'employeeId': employeeId,
        'workDate': workDate.toIso8601String(),
        'clockIn': clockIn?.toIso8601String(),
        'clockOut': clockOut?.toIso8601String(),
        'breakMinutes': breakMinutes,
        'regularHours': regularHours,
        'overtimeHours': overtimeHours,
        'status': status.index,
        'shiftName': shiftName,
        'notes': notes,
      },
    );
  }

  Future<List<PayrollRun>> getPayrollRuns() async {
    return _list(
      await client.get('/api/services/app/RestaurantPayroll/GetPayrollRuns'),
    ).map(PayrollRun.fromJson).toList();
  }

  Future<PayrollRunDetail> getPayrollRun(String id) async {
    return PayrollRunDetail.fromJson(
      await client.get(
        '/api/services/app/RestaurantPayroll/GetPayrollRun',
        query: {'id': id},
      ),
    );
  }

  Future<List<PayrollLine>> getMyPayslips() async {
    return _list(
      await client.get('/api/services/app/RestaurantPayroll/GetMyPayslips'),
    ).map(PayrollLine.fromJson).toList();
  }

  Future<String> generatePayrollRun({
    required DateTime periodStart,
    required DateTime periodEnd,
    required double tipsPool,
    required double serviceChargePool,
    required String notes,
    List<Map<String, dynamic>> adjustments = const [],
  }) async {
    return _string(
      await client.post(
        '/api/services/app/RestaurantPayroll/GeneratePayrollRun',
        body: {
          'periodStart': periodStart.toIso8601String(),
          'periodEnd': periodEnd.toIso8601String(),
          'tipsPool': tipsPool,
          'serviceChargePool': serviceChargePool,
          'notes': notes,
          'adjustments': adjustments,
        },
      ),
    );
  }

  Future<void> approvePayrollRun(String id) async {
    await client.post(
      '/api/services/app/RestaurantPayroll/ApprovePayrollRun',
      body: {'id': id},
    );
  }

  Future<void> markPayrollRunPaid(String id) async {
    await client.post(
      '/api/services/app/RestaurantPayroll/MarkPayrollRunPaid',
      body: {'id': id},
    );
  }

  Future<void> deleteDraftPayrollRun(String id) async {
    await client.post(
      '/api/services/app/RestaurantPayroll/DeleteDraftPayrollRun',
      body: {'id': id},
    );
  }
}
