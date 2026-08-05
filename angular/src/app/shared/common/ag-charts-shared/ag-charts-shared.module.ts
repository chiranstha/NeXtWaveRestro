import { NgModule, Injectable } from '@angular/core';
import { AgChartsModule } from 'ag-charts-angular';
import { LicenseManager, ModuleRegistry, AllEnterpriseModule } from 'ag-charts-enterprise';
@Injectable({
    providedIn: 'root',
})
export class AgChartsConfigService {
    private static initialized = false;
    static initializeOnce(): void {
        if (!this.initialized) {
            LicenseManager.setLicenseKey(
                atob(
                    'RG93bmxvYWREZXZUb29sc0NPTVtGVUxMXVtCT1RIXVt2MzNdX05ERXdNak0xT0RRd01EQXdNQT09NGVhNDRkMTY3OGJmZDM4ZDA2MmZmYTRkZDY0YWJiODc=',
                ),
            );
            ModuleRegistry.registerModules(AllEnterpriseModule);
            this.initialized = true;
        }
    }
}
@NgModule({
    imports: [AgChartsModule],
    exports: [AgChartsModule],
    providers: [AgChartsConfigService],
})
export class AgChartsSharedModule {
    constructor() {
        AgChartsConfigService.initializeOnce();
    }
}
