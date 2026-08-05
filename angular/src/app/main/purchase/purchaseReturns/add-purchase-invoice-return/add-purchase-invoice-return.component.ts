import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DocumentDetailsDto,
    FileParameter,
    IdAndNumberDto,
    PurchaseMasterTaxTableDto,
    PurchaseReturnAccountLedgerTableDto,
    PurchaseReturnDetailDto,
    PurchaseReturnsServiceProxy,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { Location } from '@angular/common';

import { finalize, first } from 'rxjs/operators';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { NgSelectComponent } from '@ng-select/ng-select';
import { lastValueFrom } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-purchase-invoice-return',
    templateUrl: './add-purchase-invoice-return.component.html',
    styleUrls: ['./add-purchase-invoice-return.component.css'],
    animations: [appModuleAnimation]

})
export class AddPurchaseInvoiceReturnComponent extends AppComponentBase implements OnInit {
    @ViewChild('purchase') purchaseEvent: NgSelectComponent;
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('mode') modeEvent: NgSelectComponent;
    @ViewChild('invoice') invoiceEvent: NgSelectComponent;
    @ViewChild('return') returnEvent: NgSelectComponent;
    @ViewChild('reAmt', { read: ElementRef }) reAmtEvent: ElementRef;
    @ViewChild('transport', { read: ElementRef }) transportEvent: ElementRef;
    @ViewChild('lrn', { read: ElementRef }) lrnEvent: ElementRef;
    @ViewChild('dec', { read: ElementRef }) decEvent: ElementRef;
    @ViewChild('returnTax') returnTaxEvent: NgSelectComponent;
    @ViewChildren('qtys', { read: ElementRef }) qtyEvent: QueryList<ElementRef>;
    @ViewChildren('unit') unitEvent: QueryList<NgSelectComponent>;
    @ViewChildren('rates', { read: ElementRef }) rateEvent: QueryList<ElementRef>;
    @ViewChildren('discount', { read: ElementRef }) discountEvent: QueryList<ElementRef>;
    @ViewChildren('disAmt', { read: ElementRef }) disAmtEvent: QueryList<ElementRef>;
    @ViewChildren('tax') taxEvent: QueryList<NgSelectComponent>;
    @ViewChildren('totalAmt', { read: ElementRef }) totalAmtEvent: QueryList<ElementRef>;
    @ViewChild('fileInput') fileInput;
    modalRef?: BsModalRef;
    expand: boolean;
    imageFile: FileParameter;
    documents: DocumentDetailsDto[];
    imageId: number;
    files: File;
    imageDocs: string | ArrayBuffer;
    pdfDocs: string | ArrayBuffer;
    imageType: string;
    acceptedExtension: ['.jpg', '.jpeg', '.png', '.pdf'];
    imageView = true;
    pdfView = true;
    // FILEUPLOADER END
    form: FormGroup;
    againstMode: FormGroup;
    id: string;
    ledgerId: string;
    formLoader = true;
    saving = false;
    isManual = false;
    automaticInput = false;
    shortcuts: ShortcutInput[] = [];
    isPrint = false;
    disabledAddIme = true;
    hideSpecUnit = true;
    allAccountLedgers: PurchaseReturnAccountLedgerTableDto[];
    allExpenseLedgers: PurchaseReturnAccountLedgerTableDto[];
    allCashorBank: PurchaseReturnAccountLedgerTableDto[];
    title = 'Add Purchase Return';
    serialNumber = 0;
    allTaxes: PurchaseMasterTaxTableDto[];
    purchaseMasterVouchername: string;
    addNew: any = [{ id: null, displayName: 'Add New' }];
    // Return Type
    returnType = [
        { id: 0, displayName: 'PartyWise' },
        { id: 1, displayName: 'ProductWise' },
        { id: 2, displayName: 'RateDifference' },
        { id: 3, displayName: 'NA' },
    ];
    invoiceType = [
        { id: 0, displayName: 'N/A' },
        { id: 1, displayName: 'Tax Invoice' },
        { id: 2, displayName: 'Invoice' },
    ];
    productcodes = false;
    allInvoices: IdAndNumberDto[];
    specificUnits: any;

    againstType = [
        { id: 0, displayName: 'N/A' },
        { id: 1, displayName: 'Purchase Invoice' },
    ];
    showTable = false;
    hideTable = true;

    invoiceVoucher = true;
    settings: TenantSettingsEditDto = undefined;
    units = false;
    discountPercent = false;
    discountAmount = false;
    taxes = false;
    maxQty: number;
    voucherNo: string;
    taxIdForParty: string;
    increaseDecreaseBool = false;
    returnTaxData: any;
    displayedRow: DocumentDetailsDto[];
    selectProductUnit: string;
    validQuantity: boolean;
    purchaseAcc: UniversalDropdownDto[];
    allProduct: UniversalDropdownDto[];

    constructor(
        private fb: FormBuilder,
        injector: Injector,
        private _purchaseReturnsServiceProxy: PurchaseReturnsServiceProxy,
        private route: ActivatedRoute,
        private modalService: BsModalService,
        private _location: Location,
        private router: Router,
        private _tenantSettingsService: TenantSettingsServiceProxy
    ) {
        super(injector);
        this.getSetting();
    }

    //   purchase Detail array
    get purchaseReturnDetail() {
        return this.form.get('purchaseReturnDetail') as FormArray;
    }

    //   Additional Cost array
    get additionalCosts() {
        return this.form.get('additionalCosts') as FormArray;
    }

    get qtyValidation() {
        return true;
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.getSettingsForPrint();
        this.createForm();
        this.createAgainstModeForm();
        this.getReturnTaxId();
        this.form.get('dateMiti').setValue(this.today);

        this.onLedger();
        this.changeVoucher();
        if (this.id) {
            this.title = 'Edit Purchase Return';
            this.getData();
            this._purchaseReturnsServiceProxy.getPurchaseReturnForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                if (result.purchaseReturnDetail) {
                    this.hideTable = false;
                    this.showTable = true;
                }
                this.onLedger();
                this.purchaseMasterVouchername = result.purchaseMasterVoucherNo;
                //   this.againstMode.get('returnType').setValue(1);
                this.form.get('returnType').setValue(result.returnType);
                this.changeVoucher();
            });
        } else {
            this.form.get('returnType').setValue(1);
        }
        // setTimeout(() => {
        //     this.branchEvent.open();
        // }, 1300);
        this._purchaseReturnsServiceProxy.getAllTaxForTableDropdown().subscribe((res) => {
            this.allTaxes = res;
        });
        //this.allTaxes = this._purchaseReturnsServiceProxy.getAllTaxForTableDropdown();

        this.formLoader = false;
    }

    fromUnit(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            this.unitEvent.toArray()[i].close();
            this.rateEvent.toArray()[i].nativeElement.focus();
        }
    }

    getSettingsForPrint(): void {
        this._tenantSettingsService.getAllSettings().subscribe((result: TenantSettingsEditDto) => {
            this.settings = result;
            // if (this.settings.allSettingsBundleDto.tickPrintAfterSave === true) {
            //     this.isPrint = true;
            // }
        });
    }

    getPurchaseAccount() {
        this._purchaseReturnsServiceProxy.getAllPurchaseAccountForTableDropdown().subscribe((data) => {
            this.purchaseAcc = data;
            this.form.controls['purchaseAccount'].setValue(this.purchaseAcc[0].id);

            this.formLoader = false;
        });
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            voucherNo: [item.voucherNo ? item.voucherNo : '', Validators.required],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            description: [item.description],
            purchaseAccount: [item.purchaseAccount ? item.purchaseAccount : 0, Validators.required],
            totalTax: [item.totalTax ? item.totalTax : 0, Validators.required],
            totalAmount: [item.totalAmount ? item.totalAmount : 0, Validators.required],
            grandTotal: [item.grandTotal ? item.grandTotal : 0, Validators.required],
            netAmount: [item.netAmount ? item.netAmount : 0],
            totalTaxableAmount: [item.totalTaxableAmount ? item.totalTaxableAmount : 0],
            totalDiscount: [item.totalDiscount ? item.totalDiscount : 0, Validators.required],
            lrNo: [item.lrNo],
            invoiceType: [item.invoiceType ? item.invoiceType : 0],
            transportationCompany: [item.transportationCompany],
            voucherName: [item.voucherName ? item.voucherName : 'Purchase Returmn'],
            purchaseMasterId: [item.purchaseMasterId ? item.purchaseMasterId : this.emptyGuId, Validators.required],
            vendorInvoiceNo: [item.vendorInvoiceNo ? item.vendorInvoiceNo : ''],
            ledgerId: [item.ledgerId ? item.ledgerId : this.emptyGuId, Validators.required],
            returnType: [item.returnType],
            // returnLedgerId: [item.returnLedgerId ? item.returnLedgerId : this.emptyGuId],
            returnTaxId: [item.returnTaxId ? item.returnTaxId : this.emptyGuId],
            returnAmount: [item.returnAmount ? item.returnAmount : 0],
            returnTaxAmount: [item.returnTaxAmount ? item.returnTaxAmount : 0],
            debitOrCreditNote: [item.debitOrCreditNote ? item.debitOrCreditNote : false],
            purchaseReturnDetail: this.fb.array(
                (() => {
                    if (!item.purchaseReturnDetail) {
                        return [];
                    }
                    return item.purchaseReturnDetail.map((item) => this.createpurchaseMasterDetails(item));
                })()
            ),

            id: [item.id ? item.id : this.emptyGuId],
        });

        this.form.get('ledgerId').valueChanges.subscribe((data) => {
            // if (data) {

            this.getInvoiceNumber(data);
            this.form.get('purchaseMasterId').setValue(this.emptyGuId);
            // this.createAgainst();
            this.purchaseReturnDetail.clear();
            this.totalPriceCalculation();

            //   }
        });
        // this.form.get("purchaseMasterId").valueChanges.subscribe((data) => {
        //     if (data) {
        //         let branchId = this.form.get("branchId").value;
        //         if (branchId) {
        //             this.getProductByNumber(data);
        //         }
        //     }
        // });
    }

    // }
    onChangePurchaseMasterId(event) {
        if (this.form.get('returnType').value !== 0) {
            this.getProductByNumber(event.id);
        }
    }

    getProductByNumber(data: string) {
        this._purchaseReturnsServiceProxy.getPurchaseInvoiceByInvoiceNumber(data).subscribe((data) => {
            this.fillForm(data);
        });
    }

    fillForm(result: PurchaseReturnDetailDto[]) {
        (this.form.get('purchaseReturnDetail') as FormArray).clear();
        result.forEach((item) => {
            this.addDetailForm(item);
            this.totalPriceCalculation();
        });
    }

    //   remove detail form
    removepurchaseReturnDetailForm(i, form) {
        const control = form.controls.purchaseReturnDetail;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalPriceCalculation();
    }

    addDetailForm(item: any = {}) {
        const control = <FormArray>this.form.controls.purchaseReturnDetail;
        control.push(this.createpurchaseMasterDetails(item));
    }

    createpurchaseMasterDetails(item: any = {}) {
        return this.fb.group({
            purchaseDetailsId: [item.purchaseDetailsId ? item.purchaseDetailsId : this.emptyGuId],
            qty: [item.qty ? item.qty : 0, Validators.required],
            rate: [item.rate ? item.rate : 0, Validators.required],
            discount: [item.discount ? item.discount : 0],
            discountPer: [item.discountPer ? item.discountPer : 0],
            taxAmount: [item.taxAmount ? item.taxAmount : 0],
            taxValue: [item.taxValue ? item.taxValue : 0],
            grossAmount: [item.grossAmount ? item.grossAmount : 0],
            netAmount: [item.netAmount ? item.netAmount : 0],
            amount: [item.amount ? item.amount : 0],
            productId: [item.productId ? item.productId : 0, Validators.required],
            productName: [item.productName ? item.productName : 0, Validators.required],
            productCode: [item.productCode ? item.productCode : 'N/A', Validators.required],
            unitId: [{ value: item.unitId ? item.unitId : this.emptyGuId, disabled: false }, Validators.required],
            taxId: [{ value: item.taxId ? item.taxId : this.emptyGuId, disabled: false }, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
        });
    }

    unitConversion(formGroup: FormGroup) {
        const unitsList = formGroup.get('unitsList')?.value || [];
        const selectedUnit = unitsList.find((x) => x.unitId === formGroup.get('unitId')?.value);

        if (selectedUnit?.rate !== undefined) {
            formGroup.get('rate')?.setValue(selectedUnit.rate);
            this.calculateTotalByInput(formGroup, 'rate');
        }
    }

    getpurchaseReturnDetail(form) {
        return form.controls.purchaseReturnDetail.controls;
    }

    changeUnit(rate, qty, abcd) {
        abcd.get('rate').setValue(rate);
        this.maxQty = qty;
        this.calculateTotalByInput(abcd, 'rate');
    }

    onLedger() {
        this.getAllProduct();
        this.getPurchaseAccount();
        if (!this.id) {
            this._purchaseReturnsServiceProxy.getPurchaseReturnVoucherNo().subscribe((x) => {
                this.form.get('voucherNo').setValue(x);
            });
        }
        this._purchaseReturnsServiceProxy.getAllExpensesLedgerForTableDropdown().subscribe((data) => {
            this.allExpenseLedgers = data;
        });
        this._purchaseReturnsServiceProxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allCashorBank = data;
            // if (!this.id) {
            //     this.form.get("cashOrBank").setValue(this.allCashorBank[0].id);
            // }
        });

        this._purchaseReturnsServiceProxy.getAllPurchaseAccountForTableDropdown().subscribe((res) => {
            this.purchaseAcc = res;
        });
        this.formLoader = false;
        this.getAccountLedger();
    }

    getVoucherNumber() {
        this._purchaseReturnsServiceProxy.getPurchaseReturnVoucherNo().subscribe((x) => {
            this.form.get('voucherNo').setValue(x);
        });
    }

    removeAdditionalCostForm(x, form) {
        const control = form.controls.additionalCosts;
        control.removeAt(x);
    }

    getInvoiceNumber(data: string) {
        this._purchaseReturnsServiceProxy.getInvoiceNumber(data).subscribe((data) => {
            this.allInvoices = data;
        });
    }

    setAccountLedger() {
        this.form.controls['ledgerId'].setValue(null);
        this.form.controls['purchaseMasterId'].setValue(null);
    }

    save() {
        this.saving = false;
        if (this.form.valid) {
            if (this.id) {
                this.message.confirm('', this.l('Do you want to Update ?'), (isConfirm) => {
                    if (isConfirm) {
                        this.CreatOrEdit();
                    }
                });
            } else {
                this.CreatOrEdit();
            }
        } else {
            this.notify.error('Form is invalid !!');
        }
    }

    CreatOrEdit() {
        this.saving = true;
        this._purchaseReturnsServiceProxy
            .createOrEdit(this.form.getRawValue())
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe((data) => {
                if (this.isPrint === true) {
                    if (this.id) {
                        this.notify.info(this.l('Updated Successfully'));
                        this.router.navigate([`app/main/purchase/pdf/4/${  data}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/purchase/pdf/4/${  data}`]);
                    }
                } else {
                    if (this.id) {
                        this._location.back();
                        this.notify.info(this.l('Updated Successfully'));
                    } else {
                        this._location.back();
                        this.notify.info(this.l('Saved Successfully'));
                    }
                }
            });
    }

    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfir) => {
                if (isConfir) {
                    this._location.back();
                }
            });
        } else {
            this._location.back();
        }
    }

    keyEventsDetail(): void {
        if (this.form.get('purchaseReturnDetail').valid) {
            this.addDetailForm();
        }
    }

    // get product
    onGetProduct(id: string, abcd, i: number) {
        if (!id) {
            return;
        } else {
            this.hideSpecUnit = false;

            this._purchaseReturnsServiceProxy.getProductById(id).subscribe((result) => {
                const unitList = abcd.get('unitsList') as FormArray;
                unitList.clear();

                abcd.get('productCode').setValue(result.productCode);
                abcd.get('unitId').setValue(result.unitsList[0].unitId);
                abcd.get('rate').setValue(result.unitsList[0].rate);
                this.specificUnits = result.unitsList;
                abcd.get('qty').setValue(1);
                abcd.get('qty').enable();
                abcd.get('taxId').setValue(result.taxId);
                this.calculateTotalByInput(abcd, 'qty');
            });
        }
    }

    calcqty(abcd) {
        abcd.get('qty').setValue(abcd.get('imeiList').length);
        abcd.get('qty').disable();
        this.calculateTotalByInput(abcd, 'imeiCalc');
    }

    changeInQty(abcd) {
        if (abcd.get('productType').value !== 2) {
            setTimeout(() => {
                const event = abcd.get('qty').value;
                if (this.qtyValidationType === 'Block' || this.qtyValidationType === 'Warn') {
                    if (this.maxQty < event) {
                        if (this.qtyValidationType === 'Block') {
                            this.validQuantity = true;
                            abcd.get('qty').setErrors(true);
                            abcd.get('qty').markAsTouched();
                            this.notify.error(this.l('Quantity Exceeded The Stock Quantity...'));
                        } else {
                            if (event < 1) {
                                this.notify.error(this.l('Invalid Quantity Entry'));
                            } else {
                                this.notify.error(this.l('Quantity Exceeded The Stock Quantity'));
                            }
                        }
                    } else {
                        if (this.qtyValidationType === 'Block') {
                            this.validQuantity = false;
                            abcd.get('qty').setErrors(false);
                        }
                    }
                }
            }, 500);
        }
    }

    roundToTwo(num) {
        return Math.round((num + Number.EPSILON) * 100) / 100;
    }

    calculateTotalByInput(abcd, inputname) {
        if (inputname === 'imeiCalc') {
            let qtys = abcd.get('qty').value;
            const rates = abcd.get('rate').value;
            let percentage = abcd.get('discountPer').value;
            if (qtys === null || qtys === undefined || qtys === '' || qtys <= 0) {
                qtys = 0;
            }
            if (percentage >= 100) {
                this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
            } else if (percentage === null || percentage === undefined || percentage === '' || percentage < 0) {
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
                percentage = 0;
            } else if (qtys === 0) {
                const taxId = abcd.get('taxId').value;
                const taxRate = this.allTaxes.filter((data) => data.id === taxId).map((data) => data.rate)[0];
                const amount = qtys * rates;
                const discountAmt = (amount * percentage) / 100;
                const netX = amount - discountAmt;
                const taxAmount = netX * (taxRate / 100);
                const grossX = netX + taxAmount;
                abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                this.totalPriceCalculation();
                abcd.get('qty').markAsDirty();
                this.notify.warn(this.l('Please enter quantity'));
            } else if (rates <= 0) {
                abcd.get('rate').markAsDirty();
                this.notify.warn(this.l('Please enter Rate'));
            } else {
                const taxId = abcd.get('taxId').value;
                const taxRate = this.allTaxes.filter((data) => data.id === taxId).map((data) => data.rate)[0];
                const amount = qtys * rates;
                const discountAmt = (amount * percentage) / 100;
                const netX = amount - discountAmt;
                const taxAmount = netX * (taxRate / 100);
                const grossX = netX + taxAmount;
                abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                this.totalPriceCalculation();
            }
        }
        if (inputname === 'rate') {
            const rates = abcd.get('rate').value;
            const qty = abcd.get('qty').value;
            let percentage = abcd.get('discountPer').value;
            if (qty === 0) {
                abcd.get('qty').markAsDirty();
                this.notify.warn(this.l('Please enter quantity'));
                this.setAllZero(abcd);
            } else if (rates <= 0) {
                abcd.get('rate').markAsDirty();
                abcd.get('grossAmount').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
                abcd.get('grossAmount').patchValue(0, { emitEvent: false });
                abcd.get('netAmount').patchValue(0, { emitEvent: false });
                abcd.get('taxAmount').patchValue(0, { emitEvent: false });
                abcd.get('amount').patchValue(0, { emitEvent: false });
                this.notify.warn(this.l('Please enter Rate'));
            } else if (this.allTaxes === null) {
                this._purchaseReturnsServiceProxy.getAllTaxForTableDropdown().subscribe((data) => {
                    this.allTaxes = data;
                });
            } else if (percentage === null || percentage === undefined || percentage === '') {
                this.notify.warn(this.l('Please enter Discount Percent'));
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
                percentage = 0;
            } else if (percentage >= 100) {
                abcd.get('discountPer').dirty = true;
                this.notify.warn(this.l('Value of Discount Percentage must be less than 100'));
                abcd.get('discountPer').reset();
            } else {
                const taxId = abcd.get('taxId').value;
                const taxRate = this.allTaxes.filter((data) => data.id === taxId).map((data) => data.rate)[0];
                const amount = qty * rates;
                const discountAmt = (amount * percentage) / 100;
                const netX = amount - discountAmt;
                const taxAmount = netX * (taxRate / 100);
                const grossX = netX + taxAmount;
                abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                this.totalPriceCalculation();
            }
        }
        if (inputname === 'totalamt') {
            const amounts = abcd.get('amount').value;
            const tax = abcd.get('taxId').value;
            let percentage = abcd.get('discountPer').value;
            const qty = abcd.get('qty').value;
            if (this.allTaxes === null) {
                this._purchaseReturnsServiceProxy.getAllTaxForTableDropdown().subscribe((data) => {
                    this.allTaxes = data;
                });
            } else if (percentage >= 100) {
                this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
            } else if (percentage === null || percentage === undefined || percentage === '' || percentage < 0) {
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
                percentage = 0;
            } else {
                const taxRate = this.allTaxes.filter((data) => data.id === tax).map((data) => data.rate)[0];

                if (amounts === null || amounts === '' || amounts <= 0) {
                    return;
                }
                const netValuey = 1 + taxRate / 100;
                const netX = amounts / netValuey;
                abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
                const taxAmount = this.roundToTwo(netX * (taxRate / 100));
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                const grossValueY = 1 - percentage / 100;
                const grossX = netX / grossValueY;
                abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                const discountAmount = this.roundToTwo(grossX * (percentage / 100));
                abcd.get('discount').patchValue(discountAmount);
                const rate = this.roundToTwo(grossX / qty);
                abcd.get('rate').patchValue(rate, { emitEvent: false });
                this.totalPriceCalculation();
            }
        }

        if (inputname === 'qty') {
            let qtys = abcd.get('qty').value;
            const rates = abcd.get('rate').value;

            if (qtys === null || qtys === undefined || qtys === '' || qtys <= 0) {
                qtys = 0;
                this.setAllZero(abcd);
            }
            let percentage = abcd.get('discountPer').value;
            if (percentage >= 100) {
                this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
            } else if (percentage === null || percentage === undefined || percentage === '' || percentage < 0) {
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
                percentage = 0;
            } else if (qtys === 0) {
                abcd.get('qty').markAsDirty();
                this.notify.warn(this.l('Please enter quantity'));
                this.setAllZero(abcd);

            } else if (rates <= 0) {
                abcd.get('rate').markAsDirty();
                this.notify.warn(this.l('Please enter Rate'));
                this.setAllZero(abcd);
            } else {
                const taxId = abcd.get('taxId').value;
                const taxRate = this.allTaxes.filter((data) => data.id === taxId).map((data) => data.rate)[0];
                const amount = qtys * rates;
                const discountAmt = (amount * percentage) / 100;
                const netX = amount - discountAmt;
                const taxAmount = netX * (taxRate / 100);
                const grossX = netX + taxAmount;
                abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                this.totalPriceCalculation();
            }
        }

        if (inputname === 'taxId') {
            const tax = abcd.get('taxId').value;
            const qtys = abcd.get('qty').value;
            const rates = abcd.get('rate').value;
            if (tax === null || tax === undefined || tax === '') {
                this.notify.warn(this.l('Please select tax'));
            } else {
                const taxRate = this.allTaxes.find((x) => x.id === tax).rate;
                let percentage = abcd.get('discountPer').value;
                if (qtys === null || qtys === undefined || qtys === '' || qtys <= 0) {
                    abcd.get('qty').markAsDirty();
                    this.notify.warn(this.l('Please enter quantity'));
                } else if (percentage >= 100) {
                    this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
                    abcd.get('discountPer').patchValue(0, { emitEvent: false });
                    abcd.get('discount').patchValue(0, { emitEvent: false });
                } else if (percentage === null || percentage === undefined || percentage === '') {
                    abcd.get('discountPer').patchValue(0, { emitEvent: false });
                    abcd.get('discount').patchValue(0, { emitEvent: false });
                    percentage = 0;
                } else if (qtys === 0) {
                    abcd.get('qty').markAsDirty();
                    this.notify.warn(this.l('Please enter quantity'));
                } else if (rates <= 0) {
                    abcd.get('rate').markAsDirty();
                    this.notify.warn(this.l('Please enter Rate'));
                } else {
                    const amount = qtys * rates;
                    const discountAmt = (amount * percentage) / 100;
                    const netX = amount - discountAmt;
                    const taxAmount = netX * (taxRate / 100);
                    const grossX = netX + taxAmount;
                    abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                    abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                    abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
                    abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                    abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
                }
            }
        }

        if (inputname === 'discountPer') {
            let qtys = abcd.get('qty').value;
            const rates = abcd.get('rate').value;
            if (qtys === null || qtys === undefined || qtys === '' || qtys <= 0) {
                qtys = 0;
            }
            let percentage = abcd.get('discountPer').value;
            if (percentage >= 100) {
                this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
            } else if (percentage === null || percentage === undefined || percentage === '' || percentage < 0) {
                abcd.get('discountPer').patchValue(0, { emitEvent: false });
                abcd.get('discount').patchValue(0, { emitEvent: false });
                percentage = 0;
            } else if (qtys === 0) {
                abcd.get('qty').markAsDirty();
                this.notify.warn(this.l('Please enter quantity'));
            } else if (rates <= 0) {
                abcd.get('rate').markAsDirty();
                this.notify.warn(this.l('Please enter Rate'));
            } else if (this.allTaxes === null) {
                this._purchaseReturnsServiceProxy.getAllTaxForTableDropdown().subscribe((data) => {
                    this.allTaxes = data;
                });
            } else {
                const amount = qtys * rates;
                const discountAmt = (amount * percentage) / 100;
                const netAmount = amount - discountAmt;
                const taxId = abcd.get('taxId').value;
                const taxRate = this.allTaxes.filter((data) => data.id === taxId).map((data) => data.rate)[0];
                const taxAmount = netAmount * (taxRate / 100);
                abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                abcd.get('netAmount').patchValue(this.roundToTwo(netAmount), { emitEvent: false });
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                abcd.get('amount').patchValue(this.roundToTwo(netAmount + taxAmount), { emitEvent: false });
                this.totalPriceCalculation();
            }
        }
        if (inputname === 'discount') {
            const gross = abcd.get('grossAmount').value;
            const x = abcd.get('discount').value;
            let qtys = abcd.get('qty').value;
            const rates = abcd.get('rate').value;
            if (qtys === null || qtys === undefined || qtys === '' || qtys <= 0) {
                qtys = 0;
            }
            if (qtys <= 0) {
                abcd.get('qty').markAsDirty();
                this.notify.warn(this.l('Please enter quantity'));
            } else if (rates <= 0) {
                abcd.get('rate').markAsDirty();
                this.notify.warn(this.l('Please enter Rate'));
            } else if (this.allTaxes === null) {
                this._purchaseReturnsServiceProxy.getAllTaxForTableDropdown().subscribe((data) => {
                    this.allTaxes = data;
                });
            } else {
                const dp = (x / gross) * 100;
                abcd.get('discountPer').patchValue(this.roundToTwo(dp), { emitEvent: false });
                const amount = qtys * rates;
                if (dp >= amount) {
                    abcd.get('discount').markAsDirty();
                    this.notify.warn(this.l('Discount Amount is greate than Amount'));
                    abcd.get('discount').patchValue(0, { emitEvent: false });
                    abcd.get('discountPer').patchValue(0, { emitEvent: false });
                }
                const taxId = abcd.get('taxId').value;
                const taxRate = this.allTaxes.filter((data) => data.id === taxId).map((data) => data.rate)[0];

                const discountAmt = (amount * dp) / 100;
                const netAmount = amount - discountAmt;
                const taxAmount = netAmount * (taxRate / 100);
                abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
                abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
                abcd.get('netAmount').patchValue(this.roundToTwo(netAmount), { emitEvent: false });
                abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
                abcd.get('amount').patchValue(this.roundToTwo(netAmount + taxAmount), { emitEvent: false });
                this.totalPriceCalculation();
            }
        }
        this.totalPriceCalculation();
    }

    setAllZero(abcd) {
        abcd.get('discount').setValue(0);
        abcd.get('grossAmount').setValue(0);
        abcd.get('netAmount').setValue(0);
        abcd.get('taxAmount').setValue(0);
        abcd.get('amount').setValue(0);
    }

    // shortcut keys
    // 1 branch

    totalPriceCalculation() {
        let totalAmount = 0;
        let taxAmount = 0;
        let discountAmount = 0;
        let netAmount = 0;
        let taxableAmount = 0;
        let grandTotal = 0;
        for (let i = 0; i < this.purchaseReturnDetail.length; i++) {
            taxableAmount =
                this.purchaseReturnDetail.controls[i].get('taxAmount').value > 0
                    ? this.purchaseReturnDetail.controls[i].get('netAmount').value + taxableAmount
                    : taxableAmount;
            taxAmount = this.purchaseReturnDetail.controls[i].get('taxAmount').value + taxAmount;
            netAmount = this.purchaseReturnDetail.controls[i].get('netAmount').value + netAmount;
            totalAmount = this.purchaseReturnDetail.controls[i].get('grossAmount').value + totalAmount;
            discountAmount = this.purchaseReturnDetail.controls[i].get('discount').value + discountAmount;
            grandTotal = this.purchaseReturnDetail.controls[i].get('amount').value + grandTotal;
        }
        this.form.get('totalTax').setValue(this.roundToTwo(taxAmount));
        this.form.get('totalAmount').setValue(this.roundToTwo(totalAmount));
        this.form.get('totalDiscount').setValue(this.roundToTwo(discountAmount));
        this.form.get('grandTotal').setValue(this.roundToTwo(grandTotal));
        this.form.get('netAmount').setValue(this.roundToTwo(netAmount));
        this.form.get('totalTaxableAmount').setValue(this.roundToTwo(taxableAmount));
    }

    //   }
    setTax(id, rate, abcd) {
        abcd.get('taxId').setValue(id);
        abcd.get('taxValue').setValue(rate / 100);
        this.totalPriceCalculation();
    }

    //   2 ledger
    getAccountLedger() {
        this._purchaseReturnsServiceProxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allAccountLedgers = data;
            if (!this.id) {
                this.form.get('ledgerId').setValue(this.allAccountLedgers[0].id);
            }
        });
    }

    keyClose(event: boolean) {
        if (event === true) {
            // if (!this._dialogService.hasOpenDialogs()) {
            //     this.close();
            // }
            // else {
            //     this._dialogService.dismissAll();
            // }
        } else {
            return;
        }
    }

    keySave(event: boolean) {
        if (event === true) {
            if (this.form.valid || this.form.get('grandTotal').value < 0) {
                this.checkSave();
            } else {
                this.notify.error(this.l('Form Invalid'));
            }
        } else {
            return;
        }
    }

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._purchaseReturnsServiceProxy.delete(id).subscribe(() => {
                    this._location.back();
                    this.notify.success(this.l('Deleted Successfully'));
                });
            }
        });
    }

    goToDate(e) {
        if (e.which === 13) {
            // this.branchEvent.close();
            document.getElementById('npDatePicker').focus();
        }
    }

    goToPurchase(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.purchaseEvent.open();
        }
    }

    goToLedger(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.purchaseEvent.close();
            this.ledgerEvent.open();
        }
    }

    goToRetuenType(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.close();
            this.returnEvent.open();
        }
    }

    fromreturnType(e) {
        const id = this.form.get('returnType').value;
        if (e.which === 13) {
            e.preventDefault();
            this.returnEvent.close();
            setTimeout(() => {
                if (id === 0) {
                    this.reAmtEvent.nativeElement.focus();
                } else {
                    this.modeEvent.open();
                }
            }, 200);
        }
    }

    fromAgainstMode(e) {
        const id = this.againstMode.get('modeId').value;
        if (e.which === 13) {
            e.preventDefault();
            this.modeEvent.close();
            setTimeout(() => {
                if (id > 0) {
                    this.invoiceEvent.open();
                }
            }, 200);
        }
    }

    fromQtyt(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.units) {
                if (i === 0) {
                    this.unitEvent.first.open();
                } else {
                    this.unitEvent.last.open();
                }
            } else {
                if (i === 0) {
                    this.rateEvent.first.nativeElement.focus();
                } else {
                    this.rateEvent.last.nativeElement.focus();
                }
            }
        }
    }

    goFromUnit(e, i) {
        if (e.which === 13) {
            e.preventDefault();

            if (i === 0) {
                this.unitEvent.first.close();
                this.rateEvent.first.nativeElement.focus();
            } else {
                this.unitEvent.last.close();
                this.rateEvent.last.nativeElement.focus();
            }

        }
    }

    fromRate(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.discountPercent) {
                if (i === 0) {
                    this.discountEvent.first.nativeElement.focus();
                } else {
                    this.discountEvent.last.nativeElement.focus();
                }
            } else if (this.discountAmount) {
                if (i === 0) {
                    this.disAmtEvent.first.nativeElement.focus();
                } else {
                    this.disAmtEvent.last.nativeElement.focus();
                }
            } else {
                if (i === 0) {
                    this.totalAmtEvent.first.nativeElement.focus();
                } else {
                    this.totalAmtEvent.last.nativeElement.focus();
                }
            }
        }
    }

    fromDiscountPercenr(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.discountAmount) {
                if (i === 0) {
                    this.disAmtEvent.first.nativeElement.focus();
                } else {
                    this.disAmtEvent.last.nativeElement.focus();
                }
            } else {
                if (i === 0) {
                    this.totalAmtEvent.first.nativeElement.focus();
                } else {
                    this.totalAmtEvent.last.nativeElement.focus();
                }
            }
        }
    }

    froDiscountAmt(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (i === 0) {
                this.totalAmtEvent.first.nativeElement.focus();
            } else {
                this.totalAmtEvent.last.nativeElement.focus();
            }
        }
    }

    formTotal(e) {
        if (e.key === 'Alt') {
            e.preventDefault();
            this.purchaseReturnDetail.disable();
            this.transportEvent.nativeElement.focus();
        }
    }

    formTranspotCom(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.lrnEvent.nativeElement.focus();
        }
    }

    fronLrno(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.decEvent.nativeElement.focus();
        }
    }

    fromDescription(e) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.id) {
                this.save();
            } else {
                if (this.form.valid) {
                    this.message.confirm('', this.l('Do you want to Save ?'), (isConfirm) => {
                        if (isConfirm) {
                            this.save();
                        }
                    });
                } else {
                    this.save();
                }
            }
        }
    }

    fromReturnAmt(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.returnTaxEvent.open();
        }
    }

    fromReturnTax(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.returnTaxEvent.close();
            // setTimeout(()=>{
            //     this.transportEvent.nativeElement.focus();
            // },200);
            this.transportEvent.nativeElement.focus();
        }
    }

    changeVoucher() {
        if (!this.id) {
            const vouchergenerate = this._purchaseReturnsServiceProxy.getVoucherGenerateType();
            vouchergenerate.pipe(first()).subscribe((x) => {
                if (x === 'Automatic') {
                    this.automaticInput = true;
                    this.getVoucherNumber();
                } else {
                    this.automaticInput = false;
                    if (x === 'Manual') {
                        this.isManual = true;
                    } else {
                        this.isManual = false;
                    }
                }
            });
        }
    }

    // getReturnLedgerId() {
    //     this._purchaseReturnsServiceProxy.getAllPartyWiseAccountLedgerForTableDropdown().subscribe((result) => {
    //         this.returnLedgerIdData = result;
    //         this.form.get("returnLedgerId").setValue(this.returnLedgerIdData[0].id);
    //     });
    // }

    async checkSave() {
        this.saving = true;
        if (this.isManual) {
            const voucherN = this.form.get('voucherNo').value;
            await lastValueFrom(this._purchaseReturnsServiceProxy.getCheckVoucherNo(voucherN)).then((value) => {
                if (!value) {
                    this.save();
                } else {
                    alert('Voucher Number already Exists');
                }
            });
        } else {
            this.save();
        }
    }

    // after the return type is changed to party wise
    changePartyWiseCalculation(id?: string) {
        const {value} = this.form.get('returnType');
        if (value === 0) {
            // this.form.get("totalAmount").setValue(this.form.get("returnAmount").value);
            // this.form.get("taxAmount").setValue(this.form.get("returnTaxAmount").value);
            // this.form.get("grandTotal").setValue(this.form.get("returnAmount").value - this.form.get("returnTaxAmount").value);
            if (id) {
                this.taxIdForParty = id;
            }

            const taxRate = this.returnTaxData.filter((data) => data.id === this.taxIdForParty).map((data) => data.rate);
            const taxAmount = this.form.get('returnAmount').value * (taxRate / 100);
            this.form.get('returnTaxAmount').setValue(taxAmount);
            this.form.get('totalTax').setValue(this.form.get('returnTaxAmount').value);
            this.form.get('totalAmount').setValue(this.form.get('returnAmount').value);
            this.form.get('totalDiscount').setValue(0);
            this.form
                .get('grandTotal')
                .setValue(this.form.get('returnAmount').value + this.form.get('returnTaxAmount').value);
            this.form.get('netAmount').setValue(this.form.get('returnAmount').value);
            if (taxAmount === 0) {
                this.form.get('totalTaxableAmount').setValue(0);
            } else {
                this.form.get('totalTaxableAmount').setValue(this.form.get('returnAmount').value);
            }
        }
    }

    // For Return Tax Id
    getReturnTaxId() {
        this._purchaseReturnsServiceProxy.getAllTaxAccountLedgerForTableDropdown().subscribe((data) => {
            this.returnTaxData = data;
            this.taxIdForParty = data[0].id;
            this.form.get('returnTaxId').setValue(this.returnTaxData[0].ledgerId);
        });
    }

    // FOR FILEUPLOADER START
    onSingleImage(event) {
        const formData = new FormData();
        const purchase = this.id;
        formData.append('file', event.target.files[0]);
        if (event.target.files?.length) {
            const [file] = event.target.files;
            this.imageFile = { data: event.target.files[0], fileName: file.name };
            const extensionType = file.type;
            if (
                extensionType === 'image/jpeg' ||
                extensionType === 'image/png' ||
                extensionType === 'image/jpg' ||
                extensionType === 'application/pdf'
            ) {
                this._purchaseReturnsServiceProxy.uploadImageNew(purchase, this.imageFile).subscribe(() => {
                    this.getData();
                });
            } else {
                this.notify.info('Only jpeg, jpg, png, pdf files are allowed');
            }
        }
    }

    getData() {
        this._purchaseReturnsServiceProxy.getAllDocuments(this.id).subscribe((result) => {
            this.documents = result;
            this.displayedRow = this.documents;
        });
    }

    getImages() {
        // const dialogRef = this._dialogService.open(template, { responsivePadding: true });
        // this._purchaseReturnsServiceProxy.getImage(id).subscribe((result) => {
        //     this.imageType = result.fileType;
        //     if (this.imageType === '.pdf') {
        //         this.pdfView = true;
        //         this.imageView = false;
        //         const base64_string = result.image;
        //         this.pdfDocs = 'data:application/pdf;base64,' + base64_string;
        //     } else {
        //         this.pdfView = false;
        //         this.imageView = true;
        //         const base64_string = result.image;
        //         this.imageDocs = 'data:image/png;base64,' + base64_string;
        //     }
        // });
    }

    deleteFile(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._purchaseReturnsServiceProxy.deleteFile(id).subscribe(() => {
                    this.getData();
                });
            }
        });
    }

    openExpand(event) {
        if (event === true) {
            this.expand = true;
        } else {
            this.expand = false;
        }
    }

    // FOR FILEUPLOADER END

    onChangeReturnType() {
        this.form.get('purchaseMasterId').setValue(this.emptyGuId);
        this.purchaseReturnDetail.clear();
        this.totalPriceCalculation();
        this.form.get('returnAmount').setValue(0);
        this.form.get('returnTaxAmount').setValue(0);
        this.changePartyWiseCalculation();
        // if (id === 0) {
        //     // this.againstMode.get("modeId").setValue(0);
        //     this.form.get("purchaseMasterId").setValue(0);
        //     this.purchaseReturnDetail.clear();
        //     this.totalPriceCalculation();
        // }else{
        //     this.form.get('returnAmount').setValue(0);
        //     this.form.get('returnTaxAmount').setValue(0);
        //     this.changePartyWiseCalculation();
        // }
    }

    incOrDec(form) {
        form.get('debitOrCreditNote').setValue(!form.get('debitOrCreditNote').value);
    }

    selectNa(id) {
        this.hideTable = true;
        this.showTable = false;
        //    this.allInvoices = [];
        (this.form.get('purchaseReturnDetail') as FormArray).clear();
        if (id === 3) {
            this.invoiceVoucher = false;
            this.form.get('purchaseMasterId').setValue(this.emptyGuId);
            this.showTable = true;
            this.hideTable = false;
            this.addDetailForm();
        } else {
            this.showTable = false;
            this.hideTable = true;
            this.invoiceVoucher = true;
        }
    }

    showHideTable() {
        if (this.form.get('returnType').value === 0) {
            this.showTable = false;
            this.hideTable = false;
        }
        // *ngIf="this.form.get('returnType').value != 0 && purchaseReturnDetail.length != 0"
        if (this.form.get('returnType').value === 1 || this.form.get('returnType').value === 2) {
            this.showTable = true;
            this.hideTable = false;
        }
        if (this.form.get('returnType').value === 3) {
            this.showTable = true;
            this.hideTable = false;
            (this.form.get('purchaseReturnDetail') as FormArray).clear();
            this.addDetailForm();
            this.units = true;
            this.taxes = true;
            // this.isTaxAmount = false;
        }
    }

    getAllProduct() {
        this._purchaseReturnsServiceProxy.getAllProductForTableDropdown().subscribe((data) => {
            this.allProduct = data;
        });
    }

    shownodetails() {
        this.showTable = false;
        this.hideTable = true;
        this.form.get('returnType').setValue(1);
        this.invoiceVoucher = true;
    }

    createAgainstModeForm() {
        this.againstMode = this.fb.group({
            modeId: [this.emptyGuId]
        });
    }
}
