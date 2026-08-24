import { isDevMode, NgModule } from '@angular/core';
import { AgGridModule } from 'ag-grid-angular';
import { ModuleRegistry, ValidationModule } from 'ag-grid-community';
import { setupAgGridEnterprise } from './ag-grid-enterprise-setup';
import { GridLayoutDefaultsDirective } from './grid-layout-defaults.directive';

setupAgGridEnterprise();

if (isDevMode()) {
    ModuleRegistry.registerModules([ValidationModule]);
}

@NgModule({
    imports: [AgGridModule],
    declarations: [GridLayoutDefaultsDirective],
    exports: [AgGridModule, GridLayoutDefaultsDirective],
})
export class AgGridFeatureModule {}
