import 'dart:math' as math;

import 'package:flutter/material.dart';

part 'src/theme/app_colors.dart';
part 'src/models/erp_models.dart';
part 'src/data/modules.dart';
part 'src/shell/erp_shell.dart';
part 'src/screens/dashboard_screen.dart';
part 'src/screens/pos_billing_screen.dart';
part 'src/screens/kds_screen.dart';
part 'src/screens/restaurant_inventory_screen.dart';
part 'src/screens/menu_recipe_screen.dart';
part 'src/screens/channels_screen.dart';
part 'src/screens/restaurant_setup_screen.dart';
part 'src/screens/restaurant_reports_screen.dart';
part 'src/screens/generic_module_screen.dart';
part 'src/widgets/erp_widgets.dart';
part 'src/logic/erp_helpers.dart';
part 'src/data/seed_data.dart';

void main() {
  runApp(const RestaurantErpApp());
}

class RestaurantErpApp extends StatelessWidget {
  const RestaurantErpApp({super.key});

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
      title: 'NextWave Restaurant ERP',
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
      home: const ErpShell(),
    );
  }
}
