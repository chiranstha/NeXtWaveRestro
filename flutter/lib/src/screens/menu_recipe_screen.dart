part of '../../main.dart';

class MenuRecipeScreen extends StatelessWidget {
  const MenuRecipeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const _SectionHeader(
          title: 'Menu & Recipes',
          action: 'Item, variant, add-on, recipe',
          icon: Icons.menu_book_rounded,
        ),
        const SizedBox(height: 12),
        _ResponsiveGrid(
          minTileWidth: 280,
          tileHeight: 160,
          children: [
            for (final item in menuProducts) _RecipeCard(product: item),
          ],
        ),
      ],
    );
  }
}
