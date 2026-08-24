import 'dart:async';
import 'dart:convert';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show setEquals;
import 'package:flutter/services.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:nepali_utils/nepali_utils.dart';

part 'src/config/app_config.dart';
part 'src/theme/app_colors.dart';
part 'src/models/erp_models.dart';
part 'src/models/payroll_models.dart';
part 'src/api/abp_api_client.dart';
part 'src/api/restaurant_api.dart';
part 'src/api/payroll_api.dart';
part 'src/auth/session_store.dart';
part 'src/state/restaurant_app_controller.dart';
part 'src/data/modules.dart';
part 'src/shell/erp_shell.dart';
part 'src/screens/login_screen.dart';
part 'src/screens/password_reset_screen.dart';
part 'src/screens/dashboard_screen.dart';
part 'src/screens/pos_billing_screen.dart';
part 'src/screens/kds_screen.dart';
part 'src/screens/restaurant_inventory_screen.dart';
part 'src/screens/menu_recipe_screen.dart';
part 'src/screens/channels_screen.dart';
part 'src/screens/restaurant_setup_screen.dart';
part 'src/screens/restaurant_reports_screen.dart';
part 'src/screens/restaurant_payroll_screen.dart';
part 'src/screens/generic_module_screen.dart';
part 'src/widgets/erp_widgets.dart';
part 'src/logic/erp_helpers.dart';
part 'src/data/seed_data.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(RestaurantErpApp());
}

class RestaurantErpApp extends StatelessWidget {
  RestaurantErpApp({RestaurantAppController? controller, super.key})
    : controller = controller ?? RestaurantAppController.live();

  final RestaurantAppController controller;

  @override
  Widget build(BuildContext context) {
    const seed = Color(0xFF1769E0);
    final scheme = ColorScheme.fromSeed(
      seedColor: seed,
      brightness: Brightness.light,
      surface: AppColors.surface,
    );

    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: '${controller.brand.displayName} ${controller.brand.productName}',
      theme: ThemeData(
        colorScheme: scheme,
        scaffoldBackgroundColor: AppColors.background,
        fontFamily: 'Arial',
        useMaterial3: true,
        appBarTheme: const AppBarTheme(
          elevation: 0,
          centerTitle: false,
          backgroundColor: AppColors.surface,
          foregroundColor: AppColors.text,
        ),
        cardTheme: CardThemeData(
          elevation: 0,
          color: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
            side: const BorderSide(color: AppColors.border),
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.border),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.border),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.primary, width: 1.4),
          ),
        ),
      ),
      home: _AppGate(controller: controller),
    );
  }
}

class _AppGate extends StatefulWidget {
  const _AppGate({required this.controller});

  final RestaurantAppController controller;

  @override
  State<_AppGate> createState() => _AppGateState();
}

class _AppGateState extends State<_AppGate> with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    unawaited(widget.controller.initialize());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      unawaited(widget.controller.syncAccess(background: true));
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.controller,
      builder: (context, _) {
        if (!widget.controller.initialized) {
          return _BootstrapScreen(brand: widget.controller.brand);
        }
        if (widget.controller.passwordResetCode != null) {
          return PasswordResetScreen(controller: widget.controller);
        }
        if (!widget.controller.isAuthenticated) {
          return LoginScreen(controller: widget.controller);
        }
        return ErpShell(controller: widget.controller);
      },
    );
  }
}

class _BootstrapScreen extends StatelessWidget {
  const _BootstrapScreen({required this.brand});

  final AppBrand brand;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Icon(Icons.restaurant_rounded, size: 58, color: AppColors.primary),
            SizedBox(height: 18),
            CircularProgressIndicator(),
            SizedBox(height: 12),
            Text('Opening ${brand.displayName} ${brand.productName}...'),
          ],
        ),
      ),
    );
  }
}
