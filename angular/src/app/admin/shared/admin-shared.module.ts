import { NgModule } from '@angular/core';
import { RoleComboComponent } from '@app/admin/shared/role-combo.component';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { PermissionTreeComponent } from '@app/admin/shared/permission-tree.component';
import { PermissionTreeModalComponent } from '@app/admin/shared/permission-tree-modal.component';
import { PermissionComboComponent } from '@app/admin/shared/permission-combo.component';
import { OrganizationUnitsTreeComponent } from '@app/admin/shared/organization-unit-tree.component';
import { FeatureTreeComponent } from '@app/admin/shared/feature-tree.component';
import { EditionComboComponent } from '@app/admin/shared/edition-combo.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TreeModule } from '@shared/ui-compat';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { CommonModule } from '@angular/common';
import { UtilsModule } from '@shared/utils/utils.module';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { TableModule } from '@shared/ui-compat';
import { DragDropModule } from '@shared/ui-compat';
import { ContextMenuModule } from '@shared/ui-compat';
import { PaginatorModule } from '@shared/ui-compat';
import { AutoCompleteModule } from '@shared/ui-compat';
import { EditorModule } from '@shared/ui-compat';
import { InputMaskModule } from '@shared/ui-compat';
import { Angular2CountoModule } from '@awaismirza/angular2-counto';
import { IMaskModule } from 'angular-imask';
import { SelectModule } from '@shared/ui-compat';

import { FileUploadModule } from 'ng2-file-upload';
import { FileUploadModule as AppFileUploadModule } from '@shared/ui-compat';

import {
    AllEnterpriseModule,
    ClientSideRowModelModule,
    ColumnsToolPanelModule,
    FiltersToolPanelModule,
    LicenseManager,
    ModuleRegistry,
    SetFilterModule,
    ValidationModule,
} from 'ag-grid-enterprise';
import { AgGridModule } from 'ag-grid-angular';
import { PaginationComponent } from './pagination.component';
import { ActionCellRendererComponent } from './action-cell-renderer.component';
ModuleRegistry.registerModules([
    AllEnterpriseModule,
    ClientSideRowModelModule,
    ColumnsToolPanelModule,
    FiltersToolPanelModule,
    SetFilterModule,
    ValidationModule,
]);
LicenseManager.setLicenseKey(
    atob('RG93bmxvYWREZXZUb29sc19DT01fTkRFd01qTTBOVGd3TURBd01BPT01OTE1OGI1MjI1NDAwODc5YTEyYTk2NjM0NTQ0ZjViNg=='),
);
@NgModule({
    imports: [
        AppSharedModule,
        ReactiveFormsModule,
        TreeModule,
        TooltipModule,
        FormsModule,
        CommonModule,
        UtilsModule,
        AppCommonModule,
        TableModule,
        TreeModule,
        DragDropModule,
        ContextMenuModule,
        PaginatorModule,
        AutoCompleteModule,
        EditorModule,
        InputMaskModule,
        Angular2CountoModule,
        IMaskModule,
        NgScrollbarModule,
        SelectModule,
        FileUploadModule,
        AppFileUploadModule,
        AgGridModule,
        RoleComboComponent,
        PermissionTreeComponent,
        PermissionTreeModalComponent,
        PermissionComboComponent,
        OrganizationUnitsTreeComponent,
        FeatureTreeComponent,
        EditionComboComponent,
        PaginationComponent,
        ActionCellRendererComponent,
    ],
    exports: [
        RoleComboComponent,
        PermissionTreeComponent,
        PermissionTreeModalComponent,
        PermissionComboComponent,
        OrganizationUnitsTreeComponent,
        FeatureTreeComponent,
        EditionComboComponent,
        PaginationComponent,
        ActionCellRendererComponent,
        UtilsModule,
        AppCommonModule,
        TableModule,
        TreeModule,
        DragDropModule,
        ContextMenuModule,
        PaginatorModule,
        AutoCompleteModule,
        EditorModule,
        InputMaskModule,
        Angular2CountoModule,
        IMaskModule,
        NgScrollbarModule,
        SelectModule,
        AppSharedModule,
        ReactiveFormsModule,
        TreeModule,
        TooltipModule,
        FormsModule,
        CommonModule,
        FileUploadModule,
        AppFileUploadModule,
        AgGridModule,
    ],
})
export class AdminSharedModule {}
