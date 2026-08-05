import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    EventEmitter,
    Injector,
    Input,
    OnDestroy,
    OnInit,
    Output,
    QueryList,
    TemplateRef,
    ViewChild,
    ViewChildren,
} from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UniversalDropdownDto, ProductServiceProxy, UnitsByProductIdDto } from '@shared/service-proxies/service-proxies';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { finalize, first } from 'rxjs/operators';
import { NgSelectComponent } from '@ng-select/ng-select';
import { Location } from '@angular/common';
import { Subject, takeUntil, timer } from 'rxjs';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-openingstock-product',
    templateUrl: './openingstockProduct.component.html',
    styleUrls: ['./openingStockProduct.component.css'],
    animations: [appModuleAnimation]

})
export class OpeningstockProductComponent extends AppComponentBase implements OnInit, OnDestroy {
    modalRef?: BsModalRef;
    @ViewChildren('productKey') productEvent: QueryList<NgSelectComponent>;
    @ViewChildren('productGroup', { read: ElementRef }) productGroupEvents: QueryList<ElementRef>;
    @ViewChildren('units') unitsEvent: QueryList<NgSelectComponent>;
    @ViewChildren('godOwns') godOwnsEvent: QueryList<NgSelectComponent>;
    @ViewChildren('racks') racksEvent: QueryList<NgSelectComponent>;
    @ViewChildren('rates', { read: ElementRef }) ratesEvents: QueryList<ElementRef>;
    @ViewChildren('qty', { read: ElementRef }) qtyEvents: QueryList<ElementRef>;
    @ViewChild('confirmationDialog') mnEvnt: TemplateRef<any>;
    @ViewChild('imeibtn', { read: ElementRef }) imeibtn: ElementRef;
    @ViewChild('typeIme', { read: ElementRef }) typeIme: ElementRef;

    shortcuts: ShortcutInput[] = [];
    @Input() serialNo: number;
    @Output() openingStockSave: EventEmitter<any> = new EventEmitter<any>();
    editI: number;
    form: FormGroup;
    id: string;
    serialNumber = 0;
    saving: boolean;
    duplicate: boolean;
    rowid: number;
    allProduct: UniversalDropdownDto[];
    allUnits: UnitsByProductIdDto[];
    imeiResult: any;
    private _destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private _proxy: ProductServiceProxy,
        private fb: FormBuilder,
        private _location: Location,
        private modalService: BsModalService
    ) {
        super(injector);
    }

    get products() {
        return this.form.get('products') as FormArray;
    }

    ngOnInit() {
        this.createForm();
        this.getAllProducts()
    }

    getAllProducts() {
        this._proxy
            .getAllOpeningProductForTableDropdown()
            .pipe(takeUntil(this._destroy$))
            .subscribe((data) => {
                this.allProduct = data;
            });
    }

    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }

    getAllUnits(id) {
        this._proxy
            .getAllUnitByProductForTableDropdown(id)
            .pipe(takeUntil(this._destroy$))
            .subscribe((data) => {
                this.allUnits = data;
            });
    }

    calculateTotalByInput(abcd, inputname) {
        if (inputname === 'imeiCalc') {
            let qtys = abcd.get('openingQty').value;
            const rates = abcd.get('rate').value;
            if (qtys === null || qtys === undefined || qtys === '' || qtys <= 0) {
                qtys = 0;
            }
            if (qtys == 0) {
                abcd.get('qty').markAsDirty();
                this.notify.warn(this.l('Please enter quantity'));
            } else if (rates <= 0) {
                abcd.get('rate').markAsDirty();
                this.notify.warn(this.l('Please enter Rate'));
            }
        }
    }


    removeproductsForm(i, form) {
        const control = form.controls.products;
        if (control.length > 1) {
            control.removeAt(i);
        }
        setTimeout(() => {
            this.checkIfDuplicate();
        }, 500);
    }

    openDialog(dialog: TemplateRef<any>, i?: number): void {
        this.rowid = i;
        this.modalRef = this.modalService.show(dialog);
    }

    keySave(event: boolean) {
        console.log('Key save is clicked');
        if (event === true && this.form.valid && this.products.length > 0) {
            console.log('AAA');
            this.save();
        } else {
            return;
        }
    }

    save() {
        console.log('Save is called');
        this.saving = false;
        if (this.form.valid) {
            this.saving = true;
            this._proxy
                .createNonOpeningStockProduct(this.form.getRawValue())
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    })
                )
                .subscribe(() => {
                    this.notify.info(this.l('Saved Successfully'));
                    this.ngOnInit();
                });
        } else {
            this.notify.error('Form is invalid !!');
        }
    }

    close() {
        if (this.serialNo) {
            this.form.reset();
            this.openingStockSave.emit(null);
            this.ngOnInit();
        } else {
            this.form.reset();
            this._location.back();
            this.ngOnInit();
        }
    }

    keyClose(event: boolean) {
        if (event === true) {
            this.close();
        } else {
            return;
        }
    }

    next(e, i) {
        e.preventDefault();
        if (e.which === 13) {
            if (this.editI === i) {
                if (this.products.controls[i].valid) {
                    //     this.gonext(i);
                }
            } else {
                //    this.gonext(i);
            }
        } else if (e.key === 'Alt') {
            this.message.confirm('', this.l('Do you want to Save ?'), (isConfirmed) => {
                if (isConfirmed == true) {
                    console.log('Key s is clicked');
                    this.save();
                }
            });
        }
    }

    keyEventsReceipt(): void {
        this.checkIfDuplicate();
        this.enableAllproducts();
        if (this.products.valid && !this.duplicate) {
            for (const control of this.products.controls) {
                control.disable();
            }
            this.addproductsForm();
        }
    }

    addproductsForm() {
        const control = <FormArray>this.form.controls.products;
        control.push(this.createproducts());
        setTimeout(() => {
            this.productEvent.first.open();
        });
    }

    enableproductsFormGroup(i, form) {
        this.editI = i;
        timer(100)
            .pipe(first())
            .subscribe(() => this.productEvent.toArray()[i].open());
        const control = form.controls.products.controls[i];
        control.enable();
        control.get('unitId')?.disable();
    }

    formQty(e, i) {
        console.log('Qty event');
        if (e.which === 13) {
            e.preventDefault();
            this.unitsEvent.toArray()[i].open();
        }
        console.log('Qty event1');
    }

    gotoRate(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            this.unitsEvent.toArray()[i].close();
            this.ratesEvents.toArray()[i].nativeElement.focus();
        }
    }

    unitConversion(rate: number, abcd: FormGroup) {
        if (rate !== undefined && rate !== null) {
            abcd.get('rate').setValue(rate);
        }
    }

    gotGodOwn(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            this.keyEventsReceipt();
        }
    }

    fromProduct(e, i, abcd) {
        if (e.which === 13) {
            e.preventDefault();
            const id = abcd.get('productId').value;
            this._proxy.getProductForView(id).subscribe((result) => {
                //      let ime = result.isAllowSerialNo;
                //  this.toAimei(ime, i);
            });
        }
    }


    gotoQty(i) {
        if (i == 0) {
            this.productEvent.first.close();
            this.qtyEvents.first.nativeElement.focus();
        } else {
            this.productEvent.last.close();
            this.qtyEvents.last.nativeElement.focus();
        }
    }


    openForm(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.addproductsForm();
        }
    }

    checkIfDuplicate() {
        if (this.products.controls.length > 1) {
            for (let i = 0; i < this.products.controls.length; i++) {
                for (let j = i + 1; j < this.products.controls.length; j++) {
                    if (
                        this.products.controls[i].get('productId').value == this.products.controls[j].get('productId').value
                    ) {
                        if (
                            this.products.controls[i].get('unitId').value == this.products.controls[j].get('unitId').value
                        ) {
                            this.duplicate = true;
                            this.notify.error('Same product with same unit cannot be added twice.');
                            break;
                        } else {
                            this.duplicate = false;
                        }
                    } else {
                        this.duplicate = false;
                    }
                }
                if (this.duplicate) {
                    break;
                }
            }
        } else {
            this.duplicate = false;
        }
    }

    createIme(item: any = {}) {
        return this.fb.group({
            imeiNumber: [item.imeiNumber],
            id: [item.id ? item.id : this.emptyGuId],
        });
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            products: this.fb.array(
                (() => {
                    if (!item.products) {
                        return [];
                    }
                    return item.products.map((item) => this.createproducts(item));
                })()
            ),
        });
    }

    createproducts(item: any = {}): FormGroup {
        return this.fb.group({
            productId: [item.productId, Validators.required],
            unitId: [{ value: item.unitId ? item.unitId : this.emptyGuId, disabled: true }, Validators.required],
            openingQty: [item.openingQty ? item.openingQty : 0, Validators.required],
            rate: [item.rate ? item.rate : 0, Validators.required],
        });
    }

    getproducts(form) {
        return form.controls.products.controls;
    }

    enableAllproducts() {
        this.products.controls.forEach((element) => {
            element.enable();
            element.get('unitId')?.disable();
        });
    }

    filterproduct() {
        this.allProduct.forEach((element) => {
            this.products.controls.forEach((data) => {
                if (data.get('productId').value == element.id) {
                    this.filtering(data.get('productId').value);
                }
            });
        });
    }

    filtering(id) {
        const items = this.allProduct.filter((x) => x.id !== id);
        this.allProduct = items;
    }


    onGetProduct(id: string, abcd: FormGroup, i: number) {
        this.getAllUnits(id);
        this._proxy.getProductForView(id).pipe(takeUntil(this._destroy$)).subscribe((result) => {
            abcd.get('openingQty').setValue(1);
            abcd.get('unitId').setValue(result.unitId);
            abcd.get('rate').setValue(result.rate);
        });
        setTimeout(() => {
            this.checkIfDuplicate();
        }, 800);
    }
}
