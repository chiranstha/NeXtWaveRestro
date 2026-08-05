import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
@Component({
    selector: 'language-switch',
    templateUrl: './language-switch.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styles: ['.language-switch-btn { width: auto; height: auto; }'],
})
export class LanguageSwitchComponent extends AppComponentBase implements OnInit {
    currentLanguage: abp.localization.ILanguageInfo;
    languages: abp.localization.ILanguageInfo[] = [];

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.languages = abp.localization.languages.filter((l) => (<any>l).isDisabled === false);
        this.currentLanguage = abp.localization.currentLanguage;
    }
    changeLanguage(language: abp.localization.ILanguageInfo) {
        abp.utils.setCookieValue(
            'Abp.Localization.CultureName',
            language.name,
            new Date(new Date().getTime() + 5 * 365 * 86400000), // 5 year
            abp.appPath,
        );
        location.reload();
    }
}
