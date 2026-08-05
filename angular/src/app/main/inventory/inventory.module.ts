import { CommonModule } from '@angular/common';
import { CUSTOM_ELEMENTS_SCHEMA, NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppCommonModule } from '@app/shared/common/app-common.module';
import { UtilsModule } from '@shared/utils/utils.module';
import {
    BsDatepickerModule
} from 'ngx-bootstrap/datepicker';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { ModalModule } from 'ngx-bootstrap/modal';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { AutoCompleteModule } from '@shared/ui-compat';
import { EditorModule } from '@shared/ui-compat';
import { FileUploadModule } from '@shared/ui-compat';
import { InputMaskModule } from '@shared/ui-compat';
import { PaginatorModule } from '@shared/ui-compat';
import { TableModule } from '@shared/ui-compat';
import { NgSelectModule } from '@ng-select/ng-select';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { InventoryRoutingModule } from './inventory-routing.module';
import { UnitComponent } from './units/units.component';
import { AddUnitComponent } from './units/addUnit/addUnit.component';
import { ProductGroupsComponent } from './productGroups/productGroups.component';
import { AddProductGroupsComponent } from './productGroups/addProductGroups/addProductGroups.component';
import { OpeningstockProductComponent } from './openingStockProduct/openingStockProduct.component';
import { ProductsComponent } from './products/products.component';
import { AddProductComponent } from './products/addProduct/addProduct.component';
import { ProductMergeComponent } from './productMerge/productMerge.component';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';

@NgModule({
    imports: [
        InventoryRoutingModule,
        FileUploadModule,
        AutoCompleteModule,
        PaginatorModule,
        EditorModule,
        InputMaskModule,
        TableModule,
        SubHeaderComponent,
        CommonModule,
        ModalModule,
        TooltipModule,
        AppCommonModule,
        UtilsModule,
        ReactiveFormsModule,
        FormsModule,
        BsDatepickerModule,
        BsDropdownModule,
        PopoverModule,
        NgSelectModule,
        BsDatepickerModule,
        AdminSharedModule,
        AppSharedModule,
    ],
    declarations: [
        UnitComponent,
        AddUnitComponent,
        ProductGroupsComponent,
        AddProductGroupsComponent,
        OpeningstockProductComponent,
        ProductsComponent,
        AddProductComponent,
        ProductMergeComponent
    ],
    providers: [],
    schemas: [CUSTOM_ELEMENTS_SCHEMA, NO_ERRORS_SCHEMA]
})

export class InventoryModule {
}
