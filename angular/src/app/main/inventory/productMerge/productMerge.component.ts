import { ChangeDetectionStrategy, Component, Injector, OnInit, ViewChild } from '@angular/core';
import { ProductMergeServiceProxy, UniversalDropdownDto } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'product-Merge',
    templateUrl: './productMerge.component.html',
    animations: [appModuleAnimation]
})
export class ProductMergeComponent extends AppComponentBase implements OnInit {
    @ViewChild('oldProduct') oldProduct: NgSelectComponent;
    @ViewChild('newProduct') newProduct: NgSelectComponent;
    form: FormGroup;
    allProducts: UniversalDropdownDto[];

    title: string = 'Product Merge';
    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private _proxy: ProductMergeServiceProxy
    ) {
        super(injector);
    }

    gotoNewProduct(e) {
        if (e.which == 13) {
            e.preventDefault();
            this.oldProduct.close();
            this.newProduct.open();
        }
    }

    ngOnInit(): void {
        this.createForm();
        this.getAllProducts();
    }

    createForm() {
        this.form = this.fb.group({
            oldProductId: this.emptyGuId,
            newProductId: this.emptyGuId
        });
    }

    getAllProducts() {
        this._proxy.getAllProductDropdown().subscribe((res) => {
            this.allProducts = res;
        })
    }

    save() {
        this._proxy.createProductMerge(this.form.getRawValue()).subscribe(() => {
            this.notify.info('Saved Successfully');
            this.close();
            this.form.reset();
        })
    }

    close() {
        console.log('Closed');
    }

    onChange() {
        const productId = this.form.get('oldProductId').value;
        this.GetAllProductDropdown(productId);
    }

    GetAllProductDropdown(id) {
        this._proxy.getAllProductDropdownByProductId(id).subscribe((data) => {
            this.allProducts = data.filter(
                x => x.id !== id);
        });
    }
}
