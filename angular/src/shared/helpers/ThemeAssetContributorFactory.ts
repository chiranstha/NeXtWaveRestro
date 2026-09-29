import { IThemeAssetContributor } from '@app/shared/layout/themes/ThemeAssetContributor';
import { ThemeHelper } from '@app/shared/layout/themes/ThemeHelper';
import { DefaultThemeAssetContributor } from '@app/shared/layout/themes/default/DefaultThemeAssetContributor';
import { Theme8ThemeAssetContributor } from '@app/shared/layout/themes/theme8/Theme8ThemeAssetContributor';
import { Theme11ThemeAssetContributor } from '@app/shared/layout/themes/theme11/Theme11ThemeAssetContributor';
import { Theme2ThemeAssetContributor } from '@app/shared/layout/themes/theme2/Theme2ThemeAssetContributor';
import { Theme3ThemeAssetContributor } from '@app/shared/layout/themes/theme3/Theme3ThemeAssetContributor';
export class ThemeAssetContributorFactory {
    static getCurrent(): IThemeAssetContributor {
        const theme = ThemeHelper.getTheme();
        if (theme === 'default') {
            return new DefaultThemeAssetContributor();
        }
        if (theme === 'theme8') {
            return new Theme8ThemeAssetContributor();
        }
        if (theme === 'theme2') {
            return new Theme2ThemeAssetContributor();
        }
        if (theme === 'theme3') {
            return new Theme3ThemeAssetContributor();
        }
        if (theme === 'theme11') {
            return new Theme11ThemeAssetContributor();
        }
        return null;
    }
}
