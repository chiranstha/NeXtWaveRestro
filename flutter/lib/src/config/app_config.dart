part of '../../main.dart';

class AppBrand {
  const AppBrand({
    required this.key,
    required this.displayName,
    required this.productName,
    required this.tenantId,
  });

  final String key;
  final String displayName;
  final String productName;

  /// A positive tenant ID permanently binds a tenant-branded app to that
  /// tenant. NextWave is the platform brand and resolves a tenant name at
  /// sign-in instead.
  final int tenantId;

  bool get isNextWave => key.trim().toLowerCase() == 'nextwave';
  bool get usesTenantName => isNextWave;
  int? get fixedTenantId => tenantId > 0 ? tenantId : null;

  String? get configurationError {
    if (!usesTenantName && fixedTenantId == null) {
      return 'This branded app is missing its tenant configuration.';
    }
    return null;
  }
}

class AppConfig {
  static const defaultApiBaseUrl = String.fromEnvironment(
    'APP_BASE_URL',
    defaultValue: 'https://localhost:44305',
  );

  static const brand = AppBrand(
    key: String.fromEnvironment('APP_BRAND', defaultValue: 'nextwave'),
    displayName: String.fromEnvironment(
      'APP_BRAND_NAME',
      defaultValue: 'NextWave',
    ),
    productName: String.fromEnvironment(
      'APP_PRODUCT_NAME',
      defaultValue: 'Restaurant ERP',
    ),
    tenantId: int.fromEnvironment('APP_TENANT_ID', defaultValue: 0),
  );

  static String normalizeBaseUrl(String value) {
    var normalized = value.trim();
    while (normalized.endsWith('/')) {
      normalized = normalized.substring(0, normalized.length - 1);
    }
    return normalized;
  }
}
