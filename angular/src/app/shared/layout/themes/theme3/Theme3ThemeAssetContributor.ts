import { NameValuePair } from '@shared/utils/name-value-pair';
import { IThemeAssetContributor } from '../ThemeAssetContributor';

export class Theme3ThemeAssetContributor implements IThemeAssetContributor {
    public getAssetUrls(): string[] {
        return ['/assets/common/styles/themes/theme3/metronic-customize.css'];
    }

    public getMenuWrapperStyle(): string {
        return '';
    }

    public getSubheaderStyle(): string {
        return 'text-dark fw-bold my-1 me-5';
    }

    public getFooterStyle(): string {
        return 'footer py-4 d-flex flex-lg-column ';
    }

    getBodyAttributes(): NameValuePair[] {
        return [];
    }

    getAppModuleBodyClass(): string {
        return 'app-theme3 header-fixed header-tablet-and-mobile-fixed toolbar-enabled';
    }
}
