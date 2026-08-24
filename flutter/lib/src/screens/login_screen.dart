part of '../../main.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _tenantController = TextEditingController();
  final _userController = TextEditingController();
  final _passwordController = TextEditingController();
  final _twoFactorController = TextEditingController();
  bool _hidePassword = true;

  @override
  void dispose() {
    _tenantController.dispose();
    _userController.dispose();
    _passwordController.dispose();
    _twoFactorController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    await widget.controller.login(
      tenancyName: _tenantController.text,
      userName: _userController.text,
      password: _passwordController.text,
      twoFactorCode: widget.controller.requiresTwoFactor
          ? _twoFactorController.text.trim()
          : null,
    );
  }

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 460),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Align(
                          alignment: Alignment.centerLeft,
                          child: _LoginBrand(brand: controller.brand),
                        ),
                        const SizedBox(height: 26),
                        const Text(
                          'Restaurant staff sign in',
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          controller.brand.usesTenantName
                              ? 'Use the same tenant and staff account as the NextWave web ERP.'
                              : 'Sign in with your ${controller.brand.displayName} staff account.',
                          style: const TextStyle(
                            color: AppColors.muted,
                            height: 1.4,
                          ),
                        ),
                        const SizedBox(height: 22),
                        if (controller.brand.usesTenantName) ...[
                          TextFormField(
                            controller: _tenantController,
                            enabled: !controller.requiresTwoFactor,
                            textInputAction: TextInputAction.next,
                            decoration: const InputDecoration(
                              labelText: 'Tenant name',
                              hintText: 'Restaurant tenant',
                              prefixIcon: Icon(Icons.storefront_rounded),
                            ),
                            validator: (value) => value?.trim().isEmpty == true
                                ? 'Tenant name is required.'
                                : null,
                          ),
                          const SizedBox(height: 12),
                        ],
                        TextFormField(
                          controller: _userController,
                          enabled: !controller.requiresTwoFactor,
                          textInputAction: TextInputAction.next,
                          autocorrect: false,
                          decoration: const InputDecoration(
                            labelText: 'Username or email',
                            prefixIcon: Icon(Icons.person_rounded),
                          ),
                          validator: (value) => value?.trim().isEmpty == true
                              ? 'Username is required.'
                              : null,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _passwordController,
                          enabled: !controller.requiresTwoFactor,
                          obscureText: _hidePassword,
                          onFieldSubmitted: (_) => _submit(),
                          decoration: InputDecoration(
                            labelText: 'Password',
                            prefixIcon: const Icon(Icons.lock_rounded),
                            suffixIcon: IconButton(
                              onPressed: () => setState(
                                () => _hidePassword = !_hidePassword,
                              ),
                              icon: Icon(
                                _hidePassword
                                    ? Icons.visibility_rounded
                                    : Icons.visibility_off_rounded,
                              ),
                            ),
                          ),
                          validator: (value) => value?.isEmpty == true
                              ? 'Password is required.'
                              : null,
                        ),
                        if (controller.requiresTwoFactor) ...[
                          const SizedBox(height: 12),
                          if (controller.twoFactorProviders.isNotEmpty)
                            Wrap(
                              spacing: 8,
                              runSpacing: 8,
                              children: [
                                for (final provider
                                    in controller.twoFactorProviders)
                                  if (provider.toLowerCase().contains('google'))
                                    Chip(
                                      avatar: const Icon(
                                        Icons.phonelink_lock_rounded,
                                        size: 18,
                                      ),
                                      label: Text('Use $provider'),
                                    )
                                  else
                                    OutlinedButton.icon(
                                      onPressed: controller.busy
                                          ? null
                                          : () => controller.sendTwoFactorCode(
                                              provider,
                                            ),
                                      icon: const Icon(
                                        Icons.send_rounded,
                                        size: 18,
                                      ),
                                      label: Text('Send via $provider'),
                                    ),
                              ],
                            ),
                          const SizedBox(height: 12),
                          TextFormField(
                            controller: _twoFactorController,
                            autofocus: true,
                            keyboardType: TextInputType.number,
                            textInputAction: TextInputAction.done,
                            onFieldSubmitted: (_) => _submit(),
                            decoration: InputDecoration(
                              labelText: 'Verification code',
                              helperText: controller.twoFactorProviders.isEmpty
                                  ? null
                                  : 'Provider: ${controller.twoFactorProviders.join(', ')}',
                              prefixIcon: const Icon(
                                Icons.verified_user_rounded,
                              ),
                            ),
                            validator: (value) => value?.trim().isEmpty == true
                                ? 'Verification code is required.'
                                : null,
                          ),
                        ],
                        if (controller.noticeMessage != null) ...[
                          const SizedBox(height: 14),
                          _InlineMessage(
                            message: controller.noticeMessage!,
                            color: AppColors.primary,
                            icon: Icons.info_outline_rounded,
                          ),
                        ],
                        if (controller.errorMessage != null) ...[
                          const SizedBox(height: 14),
                          _InlineMessage(
                            message: controller.errorMessage!,
                            color: AppColors.red,
                            icon: Icons.error_outline_rounded,
                          ),
                        ],
                        const SizedBox(height: 20),
                        FilledButton.icon(
                          onPressed: controller.busy ? null : _submit,
                          icon: controller.busy
                              ? const SizedBox.square(
                                  dimension: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : Icon(
                                  controller.requiresTwoFactor
                                      ? Icons.verified_rounded
                                      : Icons.login_rounded,
                                ),
                          label: Text(
                            controller.requiresTwoFactor
                                ? 'Verify and continue'
                                : 'Sign in',
                          ),
                        ),
                        if (controller.requiresTwoFactor) ...[
                          const SizedBox(height: 8),
                          TextButton(
                            onPressed: controller.busy
                                ? null
                                : controller.cancelTwoFactor,
                            child: const Text('Use a different account'),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _LoginBrand extends StatelessWidget {
  const _LoginBrand({required this.brand});

  final AppBrand brand;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 46,
          height: 46,
          decoration: BoxDecoration(
            color: AppColors.primary,
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(Icons.restaurant_rounded, color: Colors.white),
        ),
        const SizedBox(width: 11),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              brand.displayName,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900),
            ),
            Text(
              brand.productName,
              style: const TextStyle(color: AppColors.muted, fontSize: 12),
            ),
          ],
        ),
      ],
    );
  }
}

class _InlineMessage extends StatelessWidget {
  const _InlineMessage({
    required this.message,
    required this.color,
    required this.icon,
  });

  final String message;
  final Color color;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withValues(alpha: 0.2)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 19),
          const SizedBox(width: 9),
          Expanded(child: Text(message)),
        ],
      ),
    );
  }
}
