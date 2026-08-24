import { ChangeDetectionStrategy, ChangeDetectorRef, Component, HostListener, Input, OnInit, inject } from '@angular/core';
import { ThemesLayoutBaseComponent } from '../themes/themes-layout-base.component';
import {
    ChangeUserLanguageDto,
    FinancialYearSelectDto,
    FinancialYearsServiceProxy,
    ProfileServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AppConsts } from '@shared/AppConsts';
import { finalize } from 'rxjs';
@Component({
    selector: 'language-switch-dropdown',
    changeDetection: ChangeDetectionStrategy.Eager,
    templateUrl: './language-switch-dropdown.component.html',
    host: { class: 'd-flex align-items-center flex-shrink-0' },
})
export class LanguageSwitchDropdownComponent extends ThemesLayoutBaseComponent implements OnInit {
    private _profileServiceProxy = inject(ProfileServiceProxy);
    private _financialYearsServiceProxy = inject(FinancialYearsServiceProxy);
    private _changeDetector = inject(ChangeDetectorRef);
    @Input() isDropup = false;
    @Input() customStyle =
        'btn btn-icon btn-custom btn-icon-muted btn-active-light btn-active-color-primary w-35px h-35px w-md-40px h-md-40px position-relative';
    languages: abp.localization.ILanguageInfo[];
    currentLanguage: abp.localization.ILanguageInfo;
    canSelectUserFinancialYear = false;
    financialYears: FinancialYearSelectDto[] = [];
    currentFinancialYearName = '';
    selectedFinancialYearId: string | undefined;
    private persistedFinancialYearId: string | undefined;
    loadingFinancialYears = false;
    changingFinancialYear = false;
    financialYearMenuOpen = false;

    ngOnInit(): void {
        this.languages = this.localization.languages.filter((l) => l.isDisabled === false);
        this.currentLanguage = this.localization.currentLanguage;
        this.canSelectUserFinancialYear = this.isGranted('Pages.FinancialYears');

        if (this.canSelectUserFinancialYear) {
            this.loadUserFinancialYears();
        }
    }

    loadUserFinancialYears(): void {
        this.loadingFinancialYears = true;
        this._financialYearsServiceProxy
            .getAllFinancialYear()
            .pipe(
                finalize(() => {
                    this.loadingFinancialYears = false;
                    this._changeDetector.markForCheck();
                }),
            )
            .subscribe((financialYears) => {
                this.financialYears = financialYears || [];
                const selectedFinancialYear = this.financialYears.find((financialYear) => financialYear.active)
                    || this.financialYears[0];
                this.persistedFinancialYearId = selectedFinancialYear?.financialYearId;
                this.selectedFinancialYearId = this.persistedFinancialYearId;
                this.currentFinancialYearName = selectedFinancialYear?.financialYear || '';
                this._changeDetector.markForCheck();
            });
    }

    changeUserFinancialYear(financialYearId: string): void {
        if (
            !this.canSelectUserFinancialYear ||
            !financialYearId ||
            financialYearId === this.persistedFinancialYearId ||
            this.changingFinancialYear
        ) {
            return;
        }

        this.changingFinancialYear = true;
        this._financialYearsServiceProxy
            .changeFinancialYear(financialYearId)
            .pipe(
                finalize(() => {
                    this.changingFinancialYear = false;
                    this._changeDetector.markForCheck();
                }),
            )
            .subscribe({
                next: () => {
                    this.persistedFinancialYearId = financialYearId;
                    const selectedFinancialYear = this.financialYears.find(
                        (financialYear) => financialYear.financialYearId === financialYearId,
                    );
                    this.currentFinancialYearName = selectedFinancialYear?.financialYear || '';
                    localStorage.removeItem(AppConsts.suktasStorage.financialYear);
                    abp.event.trigger('app.financialYears.changed');
                    window.location.reload();
                },
                error: () => {
                    this.selectedFinancialYearId = this.persistedFinancialYearId;
                    this._changeDetector.markForCheck();
                },
            });
    }

    toggleFinancialYearMenu(): void {
        if (!this.loadingFinancialYears && !this.changingFinancialYear) {
            this.financialYearMenuOpen = !this.financialYearMenuOpen;
        }
    }

    @HostListener('document:click')
    closeFinancialYearMenu(): void {
        this.financialYearMenuOpen = false;
    }

    changeLanguage(languageName: string): void {
        const input = new ChangeUserLanguageDto();
        input.languageName = languageName;
        this._profileServiceProxy.changeLanguage(input).subscribe(() => {
            abp.utils.setCookieValue(
                'Abp.Localization.CultureName',
                languageName,
                new Date(new Date().getTime() + 5 * 365 * 86400000), //5 year
                abp.appPath,
            );
            window.location.reload();
        });
    }
}
