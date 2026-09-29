export class ThemeHelper {
    public static getTheme(): string {
        if (!abp.session?.userId) {
            return 'default';
        }
        const theme = (abp.setting?.get ? abp.setting.get('App.UiManagement.Theme') : 'default')?.toLowerCase() || 'default';
        return ['default', 'theme8', 'theme11'].includes(theme) ? theme : 'default';
    }
    public static darkMode(): boolean {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.DarkMode`)?.toLowerCase() === 'true'
            : false;
    }
    public static getAsideSkin(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Left.AsideSkin`)?.toLowerCase()
            : 'light';
    }
    public static getAllowAsideMinimizing(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Left.AllowAsideMinimizing`)?.toLowerCase()
            : 'true';
    }
    public static getDefaultMinimizedAside(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Left.DefaultMinimizedAside`)?.toLowerCase()
            : 'false';
    }
    public static getHoverableAside(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Left.HoverableAside`)?.toLowerCase()
            : 'true';
    }
    public static getFixedAside(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Left.FixedAside`)?.toLowerCase()
            : 'true';
    }
    public static getDesktopFixedHeader(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Header.DesktopFixedHeader`)?.toLowerCase()
            : 'true';
    }
    public static getMobileFixedHeader(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Header.MobileFixedHeader`)?.toLowerCase()
            : 'false';
    }
    public static getDesktopFixedToolbar(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Toolbar.DesktopFixedToolbar`)?.toLowerCase()
            : 'true';
    }
    public static getMobileFixedToolbar(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Toolbar.MobileFixedToolbar`)?.toLowerCase()
            : 'false';
    }
    public static getDesktopFixedFooter(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Footer.DesktopFixedFooter`)?.toLowerCase()
            : 'false';
    }
    public static getMobileFixedFooter(): string {
        return abp.setting?.get
            ? abp.setting.get(`${ThemeHelper.getTheme()}.App.UiManagement.Footer.MobileFixedFooter`)?.toLowerCase()
            : 'false';
    }
}
