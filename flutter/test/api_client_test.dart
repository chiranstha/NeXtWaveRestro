import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:restaurant_erp/main.dart';

void main() {
  test('API client refreshes once and retries a 401 request', () async {
    var requests = 0;
    var refreshes = 0;
    final httpClient = MockClient((request) async {
      requests++;
      if (request.headers['authorization'] == 'Bearer old-token') {
        return http.Response(
          jsonEncode({
            'success': false,
            'error': {'message': 'Expired'},
          }),
          401,
          headers: {'content-type': 'application/json'},
        );
      }
      expect(request.headers['authorization'], 'Bearer new-token');
      return http.Response(
        jsonEncode({
          'success': true,
          'result': {'value': 42},
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });
    final client = AbpApiClient(
      baseUrl: 'https://restaurant.example',
      accessToken: 'old-token',
      refreshAccessToken: () async {
        refreshes++;
        return 'new-token';
      },
      httpClient: httpClient,
    );

    final result = await client.get('/secured') as Map<String, dynamic>;

    expect(result['value'], 42);
    expect(requests, 2);
    expect(refreshes, 1);
    client.close();
  });

  test('two-factor delivery uses the existing TokenAuth contract', () async {
    late http.Request captured;
    final httpClient = MockClient((request) async {
      captured = request;
      return http.Response(
        jsonEncode({'success': true, 'result': null}),
        200,
        headers: {'content-type': 'application/json'},
      );
    });
    final client = AbpApiClient(
      baseUrl: 'https://restaurant.example',
      tenantId: 7,
      httpClient: httpClient,
    );

    await RestaurantApi(
      client,
    ).sendTwoFactorCode(userId: 19, provider: 'Email');

    expect(captured.url.path, '/api/TokenAuth/SendTwoFactorAuthCode');
    expect(captured.headers['abp-tenantid'], '7');
    expect(jsonDecode(captured.body), {'userId': 19, 'provider': 'Email'});
    client.close();
  });

  test('authentication preserves the encrypted forced-reset token', () async {
    final httpClient = MockClient((request) async {
      return http.Response(
        jsonEncode({
          'success': true,
          'result': {'shouldResetPassword': true, 'c': 'encrypted-reset-token'},
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });
    final client = AbpApiClient(
      baseUrl: 'https://restaurant.example',
      tenantId: 7,
      httpClient: httpClient,
    );

    final result = await RestaurantApi(client).authenticate(
      userName: 'manager@restaurant.example',
      password: 'temporary-password',
    );

    expect(result.shouldResetPassword, isTrue);
    expect(result.resetCode, 'encrypted-reset-token');
    expect(result.accessToken, isEmpty);
    client.close();
  });

  test('forced-reset response without a token is rejected', () async {
    final httpClient = MockClient((request) async {
      return http.Response(
        jsonEncode({
          'success': true,
          'result': {'shouldResetPassword': true},
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });
    final client = AbpApiClient(
      baseUrl: 'https://restaurant.example',
      tenantId: 7,
      httpClient: httpClient,
    );

    await expectLater(
      RestaurantApi(client).authenticate(
        userName: 'manager@restaurant.example',
        password: 'temporary-password',
      ),
      throwsA(
        isA<ApiException>().having(
          (error) => error.message,
          'message',
          contains('reset token'),
        ),
      ),
    );
    client.close();
  });

  test('password reset sends encrypted c without a tenant header', () async {
    late http.Request captured;
    final httpClient = MockClient((request) async {
      captured = request;
      return http.Response(
        jsonEncode({
          'success': true,
          'result': {'canLogin': true},
        }),
        200,
        headers: {'content-type': 'application/json'},
      );
    });
    final client = AbpApiClient(
      baseUrl: 'https://restaurant.example',
      tenantId: 7,
      httpClient: httpClient,
    );

    await RestaurantApi(
      client,
    ).resetPassword(resetCode: 'encrypted-c', password: 'NewPassword!1');

    expect(captured.url.path, '/api/services/app/Account/ResetPassword');
    expect(captured.headers.containsKey('abp-tenantid'), isFalse);
    expect(jsonDecode(captured.body), {
      'c': 'encrypted-c',
      'password': 'NewPassword!1',
    });
    client.close();
  });
}
