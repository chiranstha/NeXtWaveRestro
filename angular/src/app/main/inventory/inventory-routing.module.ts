import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { UnitComponent } from './units/units.component';
import { AddUnitComponent } from './units/addUnit/addUnit.component';
import { ProductGroupsComponent } from './productGroups/productGroups.component';
import { AddProductGroupsComponent } from './productGroups/addProductGroups/addProductGroups.component';
import { OpeningstockProductComponent } from './openingStockProduct/openingStockProduct.component';
import { ProductsComponent } from './products/products.component';
import { AddProductComponent } from './products/addProduct/addProduct.component';
import { ProductMergeComponent } from './productMerge/productMerge.component';

@NgModule({
    imports: [RouterModule.forChild([
        {
            path: '',
            children: [
                // { path: 'accountGroups', component: AccountGroupsComponent },
                { path: 'units', component: UnitComponent, data: { permission: 'Pages.Units' } },
                { path: 'units/add', component: AddUnitComponent, data: { permission: 'Pages.Units.Create' } },
                { path: 'units/edit/:id', component: AddUnitComponent, data: { permission: 'Pages.Units.Edit' } },
                {
                    path: 'productGroups',
                    component: ProductGroupsComponent,
                    data: { permission: 'Pages.ProductGroups' }
                },
                {
                    path: 'productGroups/add',
                    component: AddProductGroupsComponent,
                    data: { permission: 'Pages.ProductGroups.Create' }
                },
                {
                    path: 'productGroups/edit/:id',
                    component: AddProductGroupsComponent,
                    data: { permission: 'Pages.ProductGroups.Edit' }
                },
                { path: 'products', component: ProductsComponent, data: { permission: 'Pages.Products' } },
                {
                    path: 'products/add',
                    component: AddProductComponent,
                    data: { permission: 'Pages.Products.Create' }
                },
                {
                    path: 'products/edit/:pid',
                    component: AddProductComponent,
                    data: { permission: 'Pages.Products.Edit' }
                },

                { path: 'openingStock', component: OpeningstockProductComponent, data: { permission: 'Pages.ProductGroups' } },
                { path: 'productMerge', component: ProductMergeComponent, data: { Permissions: 'Pages.Units' } },

            ]
        }
    ])],
    exports: [RouterModule]
})

export class InventoryRoutingModule {

}