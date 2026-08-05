import { BsDatepickerConfig, BsDaterangepickerConfig, BsLocaleService } from 'ngx-bootstrap/datepicker';
import { NgxBootstrapLocaleMappingService } from 'assets/ngx-bootstrap/ngx-bootstrap-locale-mapping.service';
import { defineLocale } from 'ngx-bootstrap/chronos';
import { ThemeHelper } from '@app/shared/layout/themes/ThemeHelper';
import { enGbLocale, esLocale, ptBrLocale, viLocale, zhCnLocale } from 'ngx-bootstrap/chronos';

export class NgxBootstrapDatePickerConfigService {

    static getDaterangepickerConfig(): BsDaterangepickerConfig {
        return Object.assign(new BsDaterangepickerConfig(), {
            containerClass: 'theme-default',
            // Use ISO-like date format for inputs: YYYY-MM-DD
            // ngx-bootstrap expects format tokens similar to moment: 'YYYY-MM-DD'
            rangeInputFormat: 'YYYY-MM-DD'
        });
    }

    static getDatepickerConfig(): BsDatepickerConfig {
        return Object.assign(new BsDatepickerConfig(), {
            containerClass: 'theme-default',
            // Ensure the date input shows yyyy-MM-dd and parses accordingly
            dateInputFormat: 'YYYY-MM-DD'
        });
    }

    static getDatepickerLocale(): BsLocaleService {
        let localeService = new BsLocaleService();
        const localeToUse = new NgxBootstrapLocaleMappingService().map(abp.localization.currentLanguage.name).toLowerCase();
        localeService.use(localeToUse);
        return localeService;
    }

    static registerNgxBootstrapDatePickerLocales(): Promise<boolean> {
        const currentAbpLocale = (abp.localization.currentLanguage.name || '').toLowerCase();
        
        // Always register en-gb locale as it's the default fallback
        defineLocale('en-gb', enGbLocale);
        
        if (currentAbpLocale === 'en' || currentAbpLocale.startsWith('en-')) {
            return Promise.resolve(true);
        }

        const supportedLocale = new NgxBootstrapLocaleMappingService().map(abp.localization.currentLanguage.name).toLowerCase();
        const moduleLocaleName = new NgxBootstrapLocaleMappingService().getModuleName(abp.localization.currentLanguage.name);

        const localeDataByModuleName: Record<string, any> = {
            enGb: enGbLocale,
            es: esLocale,
            ptBr: ptBrLocale,
            vi: viLocale,
            zhCn: zhCnLocale,
        };

        const localeData = localeDataByModuleName[moduleLocaleName] ?? enGbLocale;
        defineLocale(supportedLocale, localeData);
        return Promise.resolve(true);
    }
}
