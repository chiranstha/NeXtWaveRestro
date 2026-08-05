import { AppConsts } from '@shared/AppConsts';
import * as rtlDetect from 'rtl-detect';
import { StyleLoaderService } from '@shared/utils/style-loader.service';
import { ThemeHelper } from '@app/shared/layout/themes/ThemeHelper';
import { ThemeAssetContributorFactory } from './ThemeAssetContributorFactory';
export class DynamicResourcesHelper {
    static loadResources(callback: () => void): void {
        Promise.all([DynamicResourcesHelper.loadStyles()]).then(() => {
            callback();
        });
    }
    static loadStyles(): Promise<any> {
        const theme = ThemeHelper.getTheme();
        const isRtl = rtlDetect.isRtlLang(abp.localization.currentLanguage.name);
        if (isRtl) {
            document.documentElement.setAttribute('dir', 'rtl');
        }
        const skin = ThemeHelper.darkMode() ? 'dark' : 'light';
        document.documentElement.setAttribute('data-bs-theme', skin);
        const cssPostfix = isRtl ? '.rtl' : '';
        const dataTablePostfix = isRtl ? '-rtl' : '';
        // These assets exist in the repo as non-minified files. Requesting *.min.css causes 404s during development.
        const nonMinifiedCssExt = '.css';
        const styleLoaderService = new StyleLoaderService();
        const styleUrls = [
            `${AppConsts.appBaseUrl}/assets/metronic/themes/${theme}/css/style.bundle${cssPostfix.replace(
                '-',
                '.',
            )}.css`,
        ].concat(DynamicResourcesHelper.getAdditionalThemeAssets());
        DynamicResourcesHelper.setBodyAttributes();
        if (isRtl) {
            styleUrls.push(`${AppConsts.appBaseUrl}/assets/common/styles/abp-zero-template-rtl.css`);
        }
        styleLoaderService.loadArray(styleUrls).then(() => {
            abp.event.trigger('app.dynamic-styles-loaded');
        });
        return Promise.resolve(true);
    }
    static getAdditionalThemeAssets(): string[] {
        const assetContributor = ThemeAssetContributorFactory.getCurrent();
        if (!assetContributor) {
            return [];
        }
        return assetContributor.getAssetUrls();
    }
    static setBodyAttributes(): void {
        const assetContributor = ThemeAssetContributorFactory.getCurrent();
        if (!assetContributor) {
            return;
        }
        const attributes = assetContributor.getBodyAttributes();
        for (let i = 0; i < attributes.length; i++) {
            const attr = attributes[i];
            document.body.setAttribute(attr.name, attr.value);
        }
    }
}
