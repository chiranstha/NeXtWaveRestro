import { PermissionCheckerService, FeatureCheckerService, LocalizationService, MessageService, AbpMultiTenancyService, NotifyService, SettingService, } from 'abp-ng2-module';
import { ChangeDetectorRef, Directive, Injector, OnDestroy, inject, OnInit } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { AppConsts } from '@shared/AppConsts';
import { AppUrlService } from '@shared/common/nav/app-url.service';
import { AppSessionService } from '@shared/common/session/app-session.service';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { DataTableHelper } from 'shared/helpers/DataTableHelper';
import { AgGridTableHelper } from 'shared/helpers/AgGridTableHelper';
import {
    ReportingServiceProxy,
    UiCustomizationSettingsDto,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import '@shared/service-proxies/tenant-login-info-dto-extensions';
import { NgxSpinnerService } from 'ngx-spinner';
import { NgxSpinnerTextService } from '@app/shared/ngx-spinner-text.service';
import { NepaliDatepickerPrivateService } from '@app/shared/common/nepalidatepicker/services/nepali-datepicker-angular-private.service';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { FormArray } from '@angular/forms';
import { finalize, shareReplay, tap } from 'rxjs/operators';
interface AbpEventSubscription {
    eventName: string;
    callback: (...args: unknown[]) => void;
}

@Directive()
export abstract class AppComponentBase implements OnDestroy, OnInit {
    private static financialYearRequest$: any = null;

    [key: string]: any;
    emptyguId = '00000000-0000-0000-0000-000000000000';
    emptyGuId = this.emptyguId;
    localizationSourceName = AppConsts.localization.defaultLocalizationSourceName;
    screenHeight = window.innerHeight - 200;
    localization = inject(LocalizationService);
    permission = inject(PermissionCheckerService);
    feature = inject(FeatureCheckerService);
    notify = inject(NotifyService);
    setting = inject(SettingService);
    message = inject(MessageService);
    multiTenancy = inject(AbpMultiTenancyService);
    appSession = inject(AppSessionService);
    dataTableHelper = new DataTableHelper();
    agGridTableHelper = new AgGridTableHelper();
    ui = inject(AppUiCustomizationService);
    appUrlService = inject(AppUrlService);
    spinnerService = inject(NgxSpinnerService);
    eventSubscriptions: AbpEventSubscription[] = [];
    private ngxSpinnerTextService = inject(NgxSpinnerTextService);
    private readonly changeDetectorRef = inject(ChangeDetectorRef, { optional: true });
    //ag grid
    totalRecords: number = 0;
    currentPage: number = 0;
    pageSize: number = 50;
    pageSizeOptions: number[] = [];
    nepaliDateService = inject(NepaliDatepickerPrivateService);
    baseurl = AppConsts.appBaseUrl;
    today: string;
    isPrint = false;
    fromMiti: string;
    toMiti: string;
    reportingService = inject(ReportingServiceProxy);
    getAllBranchList: UniversalDropdownDto[];
    localStorageService = inject(LocalStorageService);
    textfilledbull: boolean;
    numberfillednull: boolean;
    qtyValidationType = '';

    constructor(_injector?: Injector) {}

    protected setGridRowData<T>(
        gridApi: { setGridOption: (key: any, value: any) => void } | null | undefined,
        rows: T[] | null | undefined,
    ): T[] {
        const rowData = rows ?? [];
        gridApi?.setGridOption?.('rowData', rowData);
        this.changeDetectorRef?.markForCheck();
        return rowData;
    }

    protected markViewForCheck(): void {
        this.changeDetectorRef?.markForCheck();
    }

    get currentTheme(): UiCustomizationSettingsDto {
        return this.appSession.theme;
    }
    checkNumberfilled(filename, form) {
        const data = form.get(filename).value;
        if (data == null) {
            this.numberfillednull = true;
        } else {
            this.numberfillednull = false;
        }
    }
    checkTeaxtfilled(filename, form) {
        const data = form.get(filename).value;
        if (data === '') {
            this.textfilledbull = true;
        } else {
            this.textfilledbull = false;
        }
    }
    get appLogoSkin(): string {
        if (this.currentTheme.isTopMenuUsed || this.currentTheme.isTabMenuUsed) {
            return this.currentTheme.baseSettings.layout.darkMode ? 'light' : 'dark';
        }
        return this.currentTheme.baseSettings.menu.asideSkin;
    }
    get defaultContainer(): string {
        if (this.currentTheme.baseSettings.theme === 'default') {
            return 'default-container';
        }
        return '';
    }
    get containerClass(): string {
        let containerClass = 'app-container';
        // Add container type based on layout
        if (this.layoutType === 'fluid') {
            containerClass += ' container-fluid';
        } else if (this.layoutType === 'fixed' || this.layoutType === 'fluid-xxl') {
            containerClass += ' container-xxl';
        } else {
            containerClass += ' container';
        }
        // Add theme class if dark mode is active
        if (document.documentElement.getAttribute('data-bs-theme') === 'dark') {
            containerClass += ' dark-theme';
        }
        return containerClass;
    }
    get footerContainerClass(): string {
        if (this.appSession.theme.baseSettings.footer.footerWidthType === 'fluid') {
            return 'app-footer-container container-fluid';
        } else if (
            this.appSession.theme.baseSettings.footer.footerWidthType === 'fixed' ||
            this.appSession.theme.baseSettings.layout.layoutType === 'fluid-xxl'
        ) {
            return 'app-footer-container container-xxl';
        }
        return 'app-container container';
    }
    get layoutType(): string {
        return this.appSession.theme.baseSettings.layout.layoutType;
    }
    ngOnDestroy(): void {
        this.unSubscribeAllEvents();
    }
    checkFormArrdisableOnEdit(formArrName: FormArray, formGroupIndx: number) {
        if (formGroupIndx >= 0) {
            for (let i = 0; i < formArrName.length; i++) {
                formArrName.controls.at(i).disable();
            }
            formArrName.controls.at(formArrName.length - 1).enable();
        }
    }
    protected _prepareSelectOptions(labelMapping: Record<number, string>): { id: number; displayName: string }[] {
        return Object.keys(labelMapping)
            .filter((key) => !Number.isNaN(Number(key)))
            .map((key) => ({
                id: Number(key),
                displayName: labelMapping[Number(key)],
            }));
    }
    titleCase(form: AbstractControl, str: string, name: string) {
        const separateWord = str.split(' ');
        for (let i = 0; i < separateWord.length; i++) {
            separateWord[i] = separateWord[i].charAt(0).toUpperCase() + separateWord[i].substring(1);
        }
        const final = separateWord.join(' ');
        form.get(name).setValue(final);
    }
    uppercase(form: AbstractControl, str: string, name: string) {
        form.get(name)?.setValue((str || '').toUpperCase());
    }
    gotoNarration(event?: { which?: number; preventDefault?: () => void }) {
        if (event?.which === 13) {
            event.preventDefault?.();
        }
    }
    flattenDeep(array) {
        return array.reduce(
            (acc, val) => (Array.isArray(val) ? acc.concat(this.flattenDeep(val)) : acc.concat(val)),
            [],
        );
    }
    l(key: string, ...args: unknown[]): string {
        args.unshift(key);
        args.unshift(this.localizationSourceName);
        return this.ls.apply(this, args);
    }
    ls(sourcename: string, key: string, ...args: unknown[]): string {
        let localizedText = this.localization.localize(key, sourcename);
        if (!localizedText) {
            localizedText = key;
        }
        if (!args?.length) {
            return localizedText;
        }
        args.unshift(localizedText);
        return abp.utils.formatString.apply(this, this.flattenDeep(args));
    }
    isGranted(permissionName: string): boolean {
        return this.permission.isGranted(permissionName);
    }
    isGrantedAny(...permissions: string[]): boolean {
        if (!permissions) {
            return false;
        }
        for (const permission of permissions) {
            if (this.isGranted(permission)) {
                return true;
            }
        }
        return false;
    }
    s(key: string): string {
        return abp.setting?.get ? abp.setting.get(key) : '';
    }
    appRootUrl(): string {
        return this.appUrlService.appRootUrl;
    }
    showMainSpinner(text?: string): void {
        this.ngxSpinnerTextService.setText(text);
        void this.spinnerService.show();
    }
    hideMainSpinner(): void {
        void this.spinnerService.hide();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.getSetting();
    }
    getSetting() {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        if (abp.session.tenantId) {
            this.loadFinancialYearData();
        }
    }
    // Load financial year data
    private loadFinancialYearData() {
        const cachedFinancialYear = localStorage.getItem(AppConsts.suktasStorage.financialYear);
        if (cachedFinancialYear) {
            try {
                const financialYearData = JSON.parse(cachedFinancialYear);
                if (
                    financialYearData.fromDate === null ||
                    financialYearData.toDate === null ||
                    financialYearData.fromDate === '' ||
                    financialYearData.toDate === ''
                ) {
                    this.fetchFinancialYearFromApi();
                    return;
                }
                // Safely assign miti values if they exist
                if (financialYearData.fromMiti) {
                    this.fromMiti = financialYearData.fromMiti;
                }
                if (financialYearData.toMiti) {
                    this.toMiti = financialYearData.toMiti;
                }
            } catch (error) {
                console.error('Error parsing financial year data:', error);
                this.fetchFinancialYearFromApi();
            }
        } else {
            this.fetchFinancialYearFromApi();
        }
    }
    // Fetch financial year from API
    private fetchFinancialYearFromApi() {
        if (!AppComponentBase.financialYearRequest$) {
            AppComponentBase.financialYearRequest$ = this.reportingService.getFinancialYears().pipe(
                tap((res) => this.cacheFinancialYear(res)),
                finalize(() => {
                    AppComponentBase.financialYearRequest$ = null;
                }),
                shareReplay({ bufferSize: 1, refCount: false }),
            );
        }

        AppComponentBase.financialYearRequest$.subscribe({
            next: (res) => {
                this.applyFinancialYear(res);
            },
            error: (error) => {
                console.error('Error fetching financial year data:', error);
            },
        });
    }
    private cacheFinancialYear(res: { fromMiti?: string; toMiti?: string }) {
        const financialYear = {
            ...(res.fromMiti && { fromMiti: res.fromMiti }),
            ...(res.toMiti && { toMiti: res.toMiti }),
        };
        localStorage.setItem(AppConsts.suktasStorage.financialYear, JSON.stringify(financialYear));
    }
    private applyFinancialYear(res: { fromMiti?: string; toMiti?: string }) {
        if (res.fromMiti) {
            this.fromMiti = res.fromMiti;
        }
        if (res.toMiti) {
            this.toMiti = res.toMiti;
        }
        this.markViewForCheck();
    }
    protected calculatePageSizeOptions(): void {
        const baseSizes = [
            50, 100, 250, 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 10000, 20000, 50000, 100000,
            200000, 500000, 1000000,
        ];
        this.pageSizeOptions = baseSizes.filter((size) => size <= this.totalRecords);
        if (!this.pageSizeOptions.includes(this.totalRecords)) {
            const index = this.pageSizeOptions.length;
            while (index > 0 && this.pageSizeOptions[index - 1] > this.totalRecords) {
                this.pageSizeOptions.push(this.pageSizeOptions[index - 1] * 2);
            }
        }
        this.pageSizeOptions.push(this.totalRecords);
    }
    protected subscribeToEvent(eventName: string, callback: (...args: unknown[]) => void): void {
        abp.event.on(eventName, callback);
        this.eventSubscriptions.push({
            eventName,
            callback,
        });
    }
    private unSubscribeAllEvents() {
        this.eventSubscriptions.forEach((s) => abp.event.off(s.eventName, s.callback));
        this.eventSubscriptions = [];
    }
}

