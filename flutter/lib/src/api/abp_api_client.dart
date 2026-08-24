part of '../../main.dart';

class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode, this.details});

  final String message;
  final int? statusCode;
  final String? details;

  bool get isUnauthorized => statusCode == 401 || statusCode == 403;

  @override
  String toString() => message;
}

class AbpApiClient {
  AbpApiClient({
    required String baseUrl,
    this.accessToken,
    this.tenantId,
    this.refreshAccessToken,
    http.Client? httpClient,
  }) : baseUrl = AppConfig.normalizeBaseUrl(baseUrl),
       _httpClient = httpClient ?? http.Client();

  final String baseUrl;
  String? accessToken;
  final int? tenantId;
  final Future<String?> Function()? refreshAccessToken;
  final http.Client _httpClient;

  Future<dynamic> get(
    String path, {
    Map<String, Object?> query = const {},
  }) async {
    return _send(() => _httpClient.get(_uri(path, query), headers: _headers()));
  }

  Future<dynamic> post(
    String path, {
    Object? body,
    Map<String, Object?> query = const {},
    bool includeTenant = true,
  }) async {
    return _send(
      () => _httpClient.post(
        _uri(path, query),
        headers: _headers(contentType: true, includeTenant: includeTenant),
        body: jsonEncode(body ?? <String, Object?>{}),
      ),
    );
  }

  Future<dynamic> _send(
    Future<http.Response> Function() request, {
    bool retried = false,
  }) async {
    final response = await request().timeout(const Duration(seconds: 30));
    if (response.statusCode == 401 && !retried && refreshAccessToken != null) {
      final refreshed = await refreshAccessToken!();
      if (refreshed != null && refreshed.isNotEmpty) {
        accessToken = refreshed;
        return _send(request, retried: true);
      }
    }
    return _decode(response);
  }

  Uri _uri(String path, Map<String, Object?> query) {
    final normalizedPath = path.startsWith('/') ? path : '/$path';
    final values = <String, String>{};
    for (final entry in query.entries) {
      if (entry.value == null || '${entry.value}'.isEmpty) continue;
      values[entry.key] = '${entry.value}';
    }
    return Uri.parse(
      '$baseUrl$normalizedPath',
    ).replace(queryParameters: values.isEmpty ? null : values);
  }

  Map<String, String> _headers({
    bool contentType = false,
    bool includeTenant = true,
  }) {
    return {
      'Accept': 'application/json',
      if (contentType) 'Content-Type': 'application/json',
      if (accessToken != null && accessToken!.isNotEmpty)
        'Authorization': 'Bearer $accessToken',
      if (includeTenant && tenantId != null) 'Abp-TenantId': '$tenantId',
    };
  }

  dynamic _decode(http.Response response) {
    dynamic decoded;
    if (response.body.trim().isNotEmpty) {
      try {
        decoded = jsonDecode(response.body);
      } on FormatException {
        if (response.statusCode >= 200 && response.statusCode < 300) {
          return response.body;
        }
      }
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw _exceptionFrom(decoded, response.statusCode);
    }

    if (decoded is Map<String, dynamic> && decoded.containsKey('success')) {
      if (decoded['success'] != true) {
        throw _exceptionFrom(decoded, response.statusCode);
      }
      return decoded['result'];
    }
    return decoded;
  }

  ApiException _exceptionFrom(dynamic decoded, int statusCode) {
    if (decoded is Map<String, dynamic>) {
      final error = decoded['error'];
      if (error is Map<String, dynamic>) {
        return ApiException(
          _string(error['message'], fallback: 'Request failed'),
          statusCode: statusCode,
          details: _nullableString(error['details']),
        );
      }
      return ApiException(
        _string(decoded['message'], fallback: 'Request failed'),
        statusCode: statusCode,
      );
    }
    return ApiException('Request failed ($statusCode)', statusCode: statusCode);
  }

  void close() => _httpClient.close();
}

String _string(dynamic value, {String fallback = ''}) {
  final text = value?.toString().trim() ?? '';
  return text.isEmpty ? fallback : text;
}

String? _nullableString(dynamic value) {
  final text = value?.toString().trim() ?? '';
  return text.isEmpty ? null : text;
}

double _number(dynamic value) {
  if (value is num) return value.toDouble();
  return double.tryParse('$value') ?? 0;
}

int _integer(dynamic value) {
  if (value is num) return value.toInt();
  return int.tryParse('$value') ?? 0;
}

bool _boolean(dynamic value, {bool fallback = false}) {
  if (value is bool) return value;
  if (value is num) return value != 0;
  if (value is String) return value.toLowerCase() == 'true';
  return fallback;
}

Map<String, dynamic> _map(dynamic value) {
  return value is Map<String, dynamic> ? value : <String, dynamic>{};
}

List<dynamic> _list(dynamic value) => value is List ? value : const [];
