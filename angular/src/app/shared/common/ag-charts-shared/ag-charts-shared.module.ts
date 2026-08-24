import { NgModule, Injectable } from '@angular/core';
import { AgCharts, AgGauge } from 'ag-charts-angular';
import { AllEnterpriseModule, LicenseManager, ModuleRegistry } from 'ag-charts-enterprise';
import { AppConsts } from '@shared/AppConsts';

@Injectable({
    providedIn: 'root'
})
export class AgChartsConfigService {
    private static initialized = false;

    static initializeOnce(): void {
        if (!this.initialized) {
            // The Enterprise bundle includes Community modules, so existing Community charts
            // continue to work while Enterprise-only dashboard features are enabled once.
            // Enterprise Bundle keys are valid for both AG Grid and AG Charts.
            // A dedicated chart key, when supplied by deployment configuration, takes precedence.
            const licenseKey = AppConsts.agChartsLicenseKey?.trim() || globalThis.atob(AppConsts.agLicenseKey);
            if (licenseKey) {
                LicenseManager.setLicenseKey(licenseKey);
            }

            ModuleRegistry.registerModules(AllEnterpriseModule);
            this.initialized = true;
        }
    }
}

@NgModule({
    imports: [AgCharts, AgGauge],
    exports: [AgCharts, AgGauge],
    providers: [AgChartsConfigService]
})
export class AgChartsSharedModule {
    constructor() {
        AgChartsConfigService.initializeOnce();
    }
}
