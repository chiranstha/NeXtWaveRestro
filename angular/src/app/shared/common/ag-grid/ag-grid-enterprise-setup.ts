import { GridOptions, ModuleRegistry, provideGlobalGridOptions } from 'ag-grid-community';
import { AllEnterpriseModule as AllAgGridEnterpriseModule, LicenseManager } from 'ag-grid-enterprise';
import { AgChartsEnterpriseModule } from 'ag-charts-enterprise';
import { AppConsts } from '@shared/AppConsts';

let agGridEnterpriseConfigured = false;

const reportGridDefaults: GridOptions = {
    theme: 'legacy',
    animateRows: false,
    ensureDomOrder: false,
    rowBuffer: 10,
    suppressCellFocus: true,
    suppressColumnMoveAnimation: true,
    suppressColumnVirtualisation: false,
    enableContentVisibilityAuto: false,
    suppressRowHoverHighlight: false,
    suppressRowVirtualisation: false,
    suppressScrollOnNewData: true,
    valueCache: true,
};

export function setupAgGridEnterprise(): void {
    if (agGridEnterpriseConfigured) {
        return;
    }

    LicenseManager.setLicenseKey(globalThis.atob(AppConsts.agLicenseKey));
    ModuleRegistry.registerModules([AllAgGridEnterpriseModule.with(AgChartsEnterpriseModule)]);
    provideGlobalGridOptions(reportGridDefaults, 'deep');

    agGridEnterpriseConfigured = true;
}
