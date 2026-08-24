part of '../../main.dart';

class PasswordResetScreen extends StatefulWidget {
  const PasswordResetScreen({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<PasswordResetScreen> createState() => _PasswordResetScreenState();
}

class _PasswordResetScreenState extends State<PasswordResetScreen> {
  final _formKey = GlobalKey<FormState>();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _hidden = true;

  @override
  void dispose() {
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    await widget.controller.resetRequiredPassword(_passwordController.text);
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
                        _LoginBrand(brand: controller.brand),
                        const SizedBox(height: 26),
                        const Text(
                          'Choose a new password',
                          style: TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'The ERP requires this account to change its password before opening restaurant operations.',
                          style: TextStyle(color: AppColors.muted, height: 1.4),
                        ),
                        const SizedBox(height: 20),
                        TextFormField(
                          controller: _passwordController,
                          obscureText: _hidden,
                          decoration: InputDecoration(
                            labelText: 'New password',
                            prefixIcon: const Icon(Icons.password_rounded),
                            suffixIcon: IconButton(
                              onPressed: () =>
                                  setState(() => _hidden = !_hidden),
                              icon: Icon(
                                _hidden
                                    ? Icons.visibility_rounded
                                    : Icons.visibility_off_rounded,
                              ),
                            ),
                          ),
                          validator: (value) => (value?.length ?? 0) < 6
                              ? 'Use at least 6 characters.'
                              : null,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _confirmController,
                          obscureText: _hidden,
                          onFieldSubmitted: (_) => _submit(),
                          decoration: const InputDecoration(
                            labelText: 'Confirm password',
                            prefixIcon: Icon(Icons.password_rounded),
                          ),
                          validator: (value) =>
                              value != _passwordController.text
                              ? 'Passwords do not match.'
                              : null,
                        ),
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
                              : const Icon(Icons.check_rounded),
                          label: const Text('Change password'),
                        ),
                        TextButton(
                          onPressed: controller.busy
                              ? null
                              : controller.cancelPasswordReset,
                          child: const Text('Back to sign in'),
                        ),
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
