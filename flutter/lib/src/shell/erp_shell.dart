part of '../../main.dart';

class ErpShell extends StatefulWidget {
  const ErpShell({required this.controller, super.key});

  final RestaurantAppController controller;

  @override
  State<ErpShell> createState() => _ErpShellState();
}

class _ErpShellState extends State<ErpShell> {
  ModuleKind selected = ModuleKind.dashboard;
  String query = '';

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.controller,
      builder: (context, _) {
        final controller = widget.controller;
        final available = controller.visibleModules;
        if (!available.any((module) => module.kind == selected)) {
          selected = ModuleKind.dashboard;
        }
        final activeModule = available.firstWhere(
          (module) => module.kind == selected,
          orElse: () => modules.first,
        );
        final filtered = available.where((module) {
          return module.title.toLowerCase().contains(query.toLowerCase());
        }).toList();
        final compact = MediaQuery.sizeOf(context).width < 900;

        return Scaffold(
          drawer: compact
              ? Drawer(
                  child: SafeArea(
                    child: _ModuleNavigation(
                      selected: selected,
                      modules: filtered,
                      profile: controller.profile,
                      brand: controller.brand,
                      onSelected: (kind) {
                        Navigator.pop(context);
                        setState(() => selected = kind);
                      },
                    ),
                  ),
                )
              : null,
          body: Row(
            children: [
              if (!compact)
                SizedBox(
                  width: 264,
                  child: _ModuleNavigation(
                    selected: selected,
                    modules: filtered,
                    profile: controller.profile,
                    brand: controller.brand,
                    onSelected: (kind) => setState(() => selected = kind),
                  ),
                ),
              Expanded(
                child: Column(
                  children: [
                    _TopBar(
                      module: activeModule,
                      compact: compact,
                      controller: controller,
                      onQueryChanged: (value) => setState(() => query = value),
                    ),
                    if (controller.errorMessage != null)
                      _ShellMessage(
                        message: controller.errorMessage!,
                        color: AppColors.red,
                        icon: Icons.error_outline_rounded,
                      )
                    else if (controller.noticeMessage != null)
                      _ShellMessage(
                        message: controller.noticeMessage!,
                        color: AppColors.teal,
                        icon: Icons.check_circle_outline_rounded,
                      ),
                    Expanded(
                      child: _ScreenFrame(
                        child: _buildSelectedScreen(controller, available),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildSelectedScreen(
    RestaurantAppController controller,
    List<ErpModule> available,
  ) {
    return switch (selected) {
      ModuleKind.dashboard => DashboardScreen(
        controller: controller,
        modules: available,
        onOpen: (kind) => setState(() => selected = kind),
      ),
      ModuleKind.pos => PosBillingScreen(controller: controller),
      ModuleKind.kds => KdsScreen(controller: controller),
      ModuleKind.restaurantInventory => RestaurantInventoryScreen(
        controller: controller,
      ),
      ModuleKind.menuRecipes => MenuRecipeScreen(controller: controller),
      ModuleKind.channels => ChannelsScreen(controller: controller),
      ModuleKind.restaurantSetup => RestaurantSetupScreen(
        controller: controller,
      ),
      ModuleKind.restaurantReports => RestaurantReportsScreen(
        controller: controller,
      ),
      ModuleKind.restaurantPayroll => RestaurantPayrollScreen(
        controller: controller,
      ),
      ModuleKind.guestOrders => RestaurantGuestOrdersScreen(
        controller: controller,
      ),
      ModuleKind.reservations => RestaurantReservationsScreen(
        controller: controller,
      ),
      ModuleKind.printQueue => RestaurantPrintQueueScreen(
        controller: controller,
      ),
      _ => const SizedBox.shrink(),
    };
  }
}

class _TopBar extends StatelessWidget {
  const _TopBar({
    required this.module,
    required this.compact,
    required this.controller,
    required this.onQueryChanged,
  });

  final ErpModule module;
  final bool compact;
  final RestaurantAppController controller;
  final ValueChanged<String> onQueryChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: compact ? 72 : 64,
      padding: EdgeInsets.symmetric(horizontal: compact ? 8 : 16, vertical: 8),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(bottom: BorderSide(color: AppColors.border)),
      ),
      child: Row(
        children: [
          if (compact)
            Builder(
              builder: (context) => IconButton(
                tooltip: 'Menu',
                onPressed: () => Scaffold.of(context).openDrawer(),
                icon: const Icon(Icons.menu_rounded),
              ),
            ),
          Container(
            width: 42,
            height: 42,
            decoration: BoxDecoration(
              color: module.color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(module.icon, color: module.color),
          ),
          const SizedBox(width: 11),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  module.title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: AppColors.text,
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  controller.profile?.tenantName ?? 'Restaurant',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: AppColors.muted, fontSize: 12),
                ),
              ],
            ),
          ),
          if (!compact) ...[
            SizedBox(
              width: 260,
              child: TextField(
                onChanged: onQueryChanged,
                decoration: const InputDecoration(
                  hintText: 'Search restaurant module',
                  prefixIcon: Icon(Icons.search_rounded),
                  isDense: true,
                ),
              ),
            ),
            const SizedBox(width: 10),
          ],
          IconButton(
            tooltip: 'Refresh',
            onPressed: controller.refreshing ? null : controller.refreshAll,
            icon: controller.refreshing
                ? const SizedBox.square(
                    dimension: 19,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.refresh_rounded),
          ),
          PopupMenuButton<String>(
            tooltip: 'Account',
            onSelected: (value) {
              if (value == 'logout') unawaited(controller.logout());
            },
            itemBuilder: (context) => [
              PopupMenuItem(
                enabled: false,
                child: Text(
                  controller.profile?.name ??
                      controller.profile?.userName ??
                      'Staff user',
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
              const PopupMenuDivider(),
              const PopupMenuItem(
                value: 'logout',
                child: ListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.logout_rounded),
                  title: Text('Sign out'),
                ),
              ),
            ],
            child: const Padding(
              padding: EdgeInsets.all(8),
              child: CircleAvatar(
                radius: 18,
                backgroundColor: Color(0xFFE8F0FE),
                child: Icon(Icons.person_rounded, color: AppColors.primary),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ModuleNavigation extends StatelessWidget {
  const _ModuleNavigation({
    required this.selected,
    required this.modules,
    required this.profile,
    required this.brand,
    required this.onSelected,
  });

  final ModuleKind selected;
  final List<ErpModule> modules;
  final LoginProfile? profile;
  final AppBrand brand;
  final ValueChanged<ModuleKind> onSelected;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.primaryDark,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(18, 22, 18, 16),
            child: Row(
              children: [
                Container(
                  width: 42,
                  height: 42,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(
                    Icons.restaurant_rounded,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(width: 11),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        brand.displayName,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 20,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const Text(
                        'Restaurant staff app',
                        style: TextStyle(
                          color: Color(0xFFAFC1DB),
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const Divider(color: Color(0xFF2B405E), height: 1),
          Expanded(
            child: ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 14),
              itemCount: modules.length,
              itemBuilder: (context, index) {
                final module = modules[index];
                final active = selected == module.kind;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 5),
                  child: ListTile(
                    selected: active,
                    selectedTileColor: Colors.white.withValues(alpha: 0.12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                    leading: Icon(
                      module.icon,
                      color: active ? Colors.white : const Color(0xFFAFC1DB),
                    ),
                    title: Text(
                      module.title,
                      style: TextStyle(
                        color: active ? Colors.white : const Color(0xFFD8E1EE),
                        fontWeight: active ? FontWeight.w900 : FontWeight.w700,
                      ),
                    ),
                    onTap: () => onSelected(module.kind),
                  ),
                );
              },
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(14),
            child: Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.07),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    profile?.name.isNotEmpty == true
                        ? profile!.name
                        : profile?.userName ?? 'Staff',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    profile?.tenantName ?? '',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Color(0xFFAFC1DB),
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ShellMessage extends StatelessWidget {
  const _ShellMessage({
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
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      color: color.withValues(alpha: 0.08),
      child: Row(
        children: [
          Icon(icon, color: color, size: 18),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(color: color, fontWeight: FontWeight.w700),
            ),
          ),
        ],
      ),
    );
  }
}

class _ScreenFrame extends StatelessWidget {
  const _ScreenFrame({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: child,
    );
  }
}
