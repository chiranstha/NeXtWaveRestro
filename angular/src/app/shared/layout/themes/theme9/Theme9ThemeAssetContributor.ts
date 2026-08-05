import { IThemeAssetContributor } from '../ThemeAssetContributor';
import { NameValuePair } from '@shared/utils/name-value-pair';
export class Theme9ThemeAssetContributor implements IThemeAssetContributor {
    public getAssetUrls(): string[] {
        return [];
    }
    public getMenuWrapperStyle(): string {
        return 'header-menu-wrapper header-menu-wrapper-left';
    }
    public getSubheaderStyle(): string {
        return 'text-dark fw-bold my-1 me-5';
    }
    public getFooterStyle(): string {
        return 'footer py-4 d-flex flex-lg-column';
    }
    getBodyAttributes(): NameValuePair[] {
        return [];
    }
    getAppModuleBodyClass(): string {
        return 'header-fixed header-tablet-and-mobile-fixed aside-fixed aside-secondary-disabled';
    }
}
