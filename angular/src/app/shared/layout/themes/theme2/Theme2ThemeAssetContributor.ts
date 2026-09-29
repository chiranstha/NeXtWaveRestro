import { IThemeAssetContributor } from '../ThemeAssetContributor';
import { NameValuePair } from '@shared/utils/name-value-pair';

export class Theme2ThemeAssetContributor implements IThemeAssetContributor {
    public getAssetUrls(): string[] {
        return ['/assets/common/styles/themes/theme2/metronic-customize.css'];
    }

    public getMenuWrapperStyle(): string {
        return 'header-menu-wrapper header-menu-wrapper-left';
    }

    public getSubheaderStyle(): string {
        return 'text-dark fw-bold my-2 me-5';
    }

    public getFooterStyle(): string {
        return 'footer py-4 d-flex flex-lg-column mt-2';
    }

    getBodyAttributes(): NameValuePair[] {
        return [];
    }

    getAppModuleBodyClass(): string {
        return 'app-theme2 header-tablet-and-mobile-fixed aside-enabled';
    }
}
