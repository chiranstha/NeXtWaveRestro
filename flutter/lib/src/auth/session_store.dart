part of '../../main.dart';

class StoredSession {
  const StoredSession({
    required this.baseUrl,
    required this.accessToken,
    required this.refreshToken,
    required this.tenantId,
    required this.tenancyName,
    required this.userName,
    this.userId = 0,
  });

  final String baseUrl;
  final String accessToken;
  final String refreshToken;
  final int? tenantId;
  final String tenancyName;
  final String userName;
  final int userId;

  StoredSession copyWith({String? accessToken, String? refreshToken}) {
    return StoredSession(
      baseUrl: baseUrl,
      accessToken: accessToken ?? this.accessToken,
      refreshToken: refreshToken ?? this.refreshToken,
      tenantId: tenantId,
      tenancyName: tenancyName,
      userName: userName,
      userId: userId,
    );
  }
}

abstract class SessionStore {
  Future<StoredSession?> read();
  Future<void> write(StoredSession session);
  Future<void> clear();
  Future<String?> readValue(String key);
  Future<void> writeValue(String key, String value);
  Future<void> deleteValue(String key);
}

class SecureSessionStore implements SessionStore {
  const SecureSessionStore([
    this.storage = const FlutterSecureStorage(
      aOptions: AndroidOptions(encryptedSharedPreferences: true),
    ),
  ]);

  final FlutterSecureStorage storage;

  @override
  Future<StoredSession?> read() async {
    final values = await storage.readAll();
    final token = values['accessToken'] ?? '';
    if (token.isEmpty) return null;
    return StoredSession(
      baseUrl: values['baseUrl'] ?? AppConfig.defaultApiBaseUrl,
      accessToken: token,
      refreshToken: values['refreshToken'] ?? '',
      tenantId: int.tryParse(values['tenantId'] ?? ''),
      tenancyName: values['tenancyName'] ?? '',
      userName: values['userName'] ?? '',
      userId: int.tryParse(values['userId'] ?? '') ?? 0,
    );
  }

  @override
  Future<void> write(StoredSession session) async {
    await storage.write(key: 'baseUrl', value: session.baseUrl);
    await storage.write(key: 'accessToken', value: session.accessToken);
    await storage.write(key: 'refreshToken', value: session.refreshToken);
    await storage.write(key: 'tenantId', value: '${session.tenantId ?? ''}');
    await storage.write(key: 'tenancyName', value: session.tenancyName);
    await storage.write(key: 'userName', value: session.userName);
    await storage.write(key: 'userId', value: '${session.userId}');
  }

  @override
  Future<void> clear() async {
    for (final key in const [
      'baseUrl',
      'accessToken',
      'refreshToken',
      'tenantId',
      'tenancyName',
      'userName',
      'userId',
    ]) {
      await storage.delete(key: key);
    }
  }

  @override
  Future<String?> readValue(String key) => storage.read(key: key);

  @override
  Future<void> writeValue(String key, String value) =>
      storage.write(key: key, value: value);

  @override
  Future<void> deleteValue(String key) => storage.delete(key: key);
}

class MemorySessionStore implements SessionStore {
  StoredSession? value;
  final Map<String, String> values = {};

  @override
  Future<void> clear() async => value = null;

  @override
  Future<StoredSession?> read() async => value;

  @override
  Future<void> write(StoredSession session) async => value = session;

  @override
  Future<String?> readValue(String key) async => values[key];

  @override
  Future<void> writeValue(String key, String value) async =>
      values[key] = value;

  @override
  Future<void> deleteValue(String key) async => values.remove(key);
}
