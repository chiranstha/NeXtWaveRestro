import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DocumentDetailsDto,
    FileParameter,
    PurchaseReturnUnitsQtyDto,
    SalesReturnMasterAccountLedgerTableDto,
    SalesReturnMastersServiceProxy,
    TaxDto,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';

import { Location } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Subject } from 'rxjs';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-sales-return',
    templateUrl: './add-sales-return.component.html',
    styleUrls: ['./add-sales-return.component.css'],
    animations: [appModuleAnimation]

})
export class AddSalesReturnComponent extends AppComponentBase implements OnInit {
    @ViewChild('salesacc') salesaccEvent: NgSelectComponent;
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('mode') modeEvent: NgSelectComponent;
    @ViewChild('salesInvoice') salesInvoiceEvent: NgSelectComponent;
    @ViewChild('bill', { read: ElementRef }) billEvent: ElementRef;
    @ViewChild('totTax', { read: ElementRef }) totTaxEvent: ElementRef;
    @ViewChild('netTot', { read: ElementRef }) netTotEvent: ElementRef;
    @ViewChild('grandTot', { read: ElementRef }) grandTotEvent: ElementRef;
    @ViewChild('trnscompany', { read: ElementRef }) trnsEvent: ElementRef;
    @ViewChild('lrno', { read: ElementRef }) lrnoEvent: ElementRef;
    @ViewChild('desc', { read: ElementRef }) descEvent: ElementRef;
    @ViewChild('return') returnEvent: NgSelectComponent;
    @ViewChild('returnAmt', { read: ElementRef }) returnAmtEvent: ElementRef;
    @ViewChild('returnTax') returnTaxEvent: NgSelectComponent;
    @ViewChildren('product') productEvent: QueryList<NgSelectComponent>;
    @ViewChildren('qtys', { read: ElementRef }) qtyEvent: QueryList<ElementRef>;
    @ViewChildren('rate', { read: ElementRef }) rateEvent: QueryList<ElementRef>;
    @ViewChildren('unit') unitEvent: QueryList<NgSelectComponent>;
    @ViewChildren('discount', { read: ElementRef }) discountEvent: QueryList<ElementRef>;
    @ViewChildren('disAmt', { read: ElementRef }) disAmtEvent: QueryList<ElementRef>;
    @ViewChildren('tax') taxEvent: QueryList<NgSelectComponent>;
    @ViewChildren('totalAmt', { read: ElementRef }) totalAmtEvent: QueryList<ElementRef>;
    @ViewChild('fileInput') fileInput;
    modalRef?: BsModalRef;
    imageFile: FileParameter;
    files: File;
    documents: DocumentDetailsDto[];

    allAccountLedgers: SalesReturnMasterAccountLedgerTableDto[];
    title = 'Add Sales Return';
    imageDocs: string | ArrayBuffer;
    pdfDocs: string | ArrayBuffer;
    imageType: string;
    voucherName: string;
    specificUnits: any;

    acceptedExtension: ['.jpg', '.jpeg', '.png', '.pdf'];
    imageView = true;
    pdfView = true;
    expand: boolean;
    saving: boolean;
    isPrint = false;
    automaticInput = false;
    showTable = false;
    hideTable = true;
    isManual = false;
    formLoader = true;
    destroy$: Subject<void> = new Subject<void>();
    imageId: string;

    productId: string;
    serialNumber = 0;
    taxIdForParty: string;
    id: string;
    maxQty = 0;
    settings: TenantSettingsEditDto = undefined;
    shortcuts: ShortcutInput[] = [];
    form: FormGroup;
    againstMode: FormGroup;
    imeiForm: FormGroup;
    additionalItems: FormArray;
    debitCredut = [
        { id: 0, name: 'Dr' },
        { id: 1, name: 'Cr' },
    ];
    againstType = [
        { id: 0, displayName: 'N/A' },
        { id: 1, displayName: 'Sales Invoice' },
    ];
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
    invoiceVoucher = true;
    isTaxAmount = true;
    allTaxes: TaxDto[];
    displayedRow: DocumentDetailsDto[];
    returnTaxData: any;
    allUnitsByproductTemp: PurchaseReturnUnitsQtyDto[] = [];
    selectProductUnit: any;
    units = false;
    discountPercent = false;
    discountAmount = false;
    taxes = false;
    invoiceNumbers: UniversalDropdownDto[];
    allCashorBank: UniversalDropdownDto[];
    allExpenseLedgers: UniversalDropdownDto[];
    allProducts: UniversalDropdownDto[];
    allSalesMasters: UniversalDropdownDto[];
    allSalesAccounts: any;

    constructor(
        private fb: FormBuilder,
        injector: Injector,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        private _location: Location,
        private _proxy: SalesReturnMastersServiceProxy,
        private route: ActivatedRoute,
        private modalService: BsModalService,
        private router: Router
    ) {
        super(injector);
        this.getSetting();
    }

    //   sales Detail array
    get salesReturnDetail() {
        return this.form.get('salesReturnDetail') as FormArray;
    }

    //   Additional Cost array
    get additionalCosts() {
        return this.form.get('additionalCosts') as FormArray;
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.createForm();
        this.createAgainstModeForm();
        //      this.getSettingsForPrint();
        this.getReturnTaxId();
        if (!this.id) {
            this.form.get('dateMiti').setValue(this.today);
        }
        this.onLedger();
        if (this.id) {
            this.title = 'Edit Sales Return';
            this.getData();
            this._proxy.getSalesReturnMasterForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                if (result.salesReturnDetail) {
                    this.hideTable = false;
                    this.showTable = true;
                }

                this.voucherName = result.salesMasterVoucherNo;
                this.againstMode.get('modeId').setValue(1);
                this.againstMode.get('modeId').disable();
                //     this.onLedger(result.branchId);
            });
        } else {
            this.form.get('returnType').setValue(1);
        }

        this._proxy.getAllTaxAccountLedgerForTableDropdown().subscribe((res) => {
            this.allTaxes = res;
        });
    }

    unitConversion(formGroup) {

        const unitList = formGroup.get('unitsList').value;

        const selectedUnit = unitList.find(x =>
            x.unitId == formGroup.get('unitId').value
        );

        if (!selectedUnit) {
            return;
        }

        formGroup.get('rate').setValue(selectedUnit.rate);

        this.calculateTotalByInput(formGroup, 'rate');
    }


    changeInQty(abcd) {
        const unitList = abcd.get('unitsList') as FormArray;
        if (!unitList || unitList.length === 0) {
            return;
        }

        const selectedUnit = unitList.controls.find(control =>
            control.get('unitId').value === abcd.get('unitId').value
        )?.value;

        if (!selectedUnit) {
            return;
        }

        const selectedQty = selectedUnit.qty;
        const returntypeId = this.form.get('returnType').value;

        if (returntypeId == 1) {
            const qty = abcd.get('qty').value;
            if (qty > selectedQty) {
                abcd.get('qty').setValue(selectedQty);
                this.notify.warn(`Quantity should not be greater than ${  this.maxQty}`);
            }
        }
    }

    createForm(item: any = {}) {

        this.form = this.fb.group({
            voucherNo: [{ value: item.voucherNo ? item.voucherNo : 1, disabled: this.id }, Validators.required],
            salesAccountId: [item.salesAccountId ? item.salesAccountId : this.emptyGuId],
            description: [item.description],
            totalAmount: [item.totalAmount ? item.totalAmount : 0],
            billDiscount: [item.billDiscount ? item.billDiscount : 0],
            netAmount: [item.netAmount ? item.netAmount : 0],
            taxableAmount: [item.taxableAmount ? item.taxableAmount : 0],
            taxAmount: [item.taxAmount ? item.taxAmount : 0],
            valueAddedTax: [item.valueAddedTax ? item.valueAddedTax : 0],
            grandTotal: [item.grandTotal ? item.grandTotal : 0, Validators.required],
            lrNo: [item.lrNo ? item.lrNo : ''],
            transportationCompany: [item.transportationCompany ? item.transportationCompany : ''],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            discount: [item.discount ? item.discount : 0],
            ledgerId: [item.ledgerId ? item.ledgerId : 1, Validators.required],
            returnType: [item.returnType],
            debitOrCreditNote: [item.debitOrCreditNote ? item.debitOrCreditNote : false],
            salesMasterId: [item.salesMasterId],
            invoiceType: [item.invoiceType ? item.invoiceType : 0],
            salesReturnDetail: this.fb.array(
                (() => {
                    if (!item.salesReturnDetail) {
                        return [];
                    }
                    return item.salesReturnDetail.map((item) => this.createDetails(item));
                })()
            ),
            returnTaxId: [item.returnTaxId ? item.returnTaxId : this.emptyGuId],
            returnAmount: [item.returnAmount ? item.returnAmount : 0],
            returnTaxAmount: [item.returnTaxAmount ? item.returnTaxAmount : 0],
            id: [item.id ? item.id : this.emptyGuId],
        });

        this.form.get('ledgerId').valueChanges.subscribe((data) => {
            // if (data) {

            this.getAllSalesInvoice(data);
            this.form.get('salesMasterId').setValue(this.emptyGuId);
            // this.createAgainst();
            this.salesReturnDetail.clear();
            this.totalPriceCalculation();
        });


        // this.form.get('ledgerId').valueChanges.subscribe((data) => {
        //     if (data) {
        //         this.getAllSalesInvoice();
        //     }
        // });
    }

    // getInvoiceNumber(ledgerId) {
    //     if (ledgerId !== null) {
    //         this.form.get('salesMasterId').setValue(this.emptyGuId);
    //         this.salesReturnDetail.clear();
    //         this.totalPriceCalculation();
    //         this.form.get('returnAmount').setValue(0);
    //         this.changePartyWiseCalculation();
    //         this._proxy.getSalesInvoiceIdByLedgerId(ledgerId).subscribe((data) => {
    //             this.invoiceNumbers = data;
    //         });
    //     }
    // }

    createDetails(item: any = {}) {
        return this.fb.group({
            qty: [item.qty ? item.qty : 0, Validators.required],
            rate: [item.rate ? item.rate : 0, Validators.required],

            discount: [item.discount ? item.discount : 0],
            discountPer: [item.discountPer ? item.discountPer : 0],

            taxAmount: [{ value: item.taxAmount ? item.taxAmount : 0, disabled: true }, Validators.required],
            taxId: [item.taxId ? item.taxId : null, Validators.required],
            taxValue: [item.taxValue],

            grossAmount: [item.grossAmount ? item.grossAmount : 0],
            netAmount: [item.netAmount ? item.netAmount : 0],
            amount: [item.amount ? item.amount : 0],

            productId: [item.productId ? item.productId : null, Validators.required],

            unitId: [item.unitId ? item.unitId : null, Validators.required],
            unitsList: this.fb.array([]),
            id: [item.id ? item.id : null],
            salesDetailId: [item.salesDetailId ? item.salesDetailId : this.emptyGuId],
        });
    }

    createAdditionalCost(item: any = {}) {
        return this.fb.group({
            drOrCr: [item.drOrCr ? item.drOrCr : 0],
            amount: [item.amount ? item.amount : 0],
            cashOrBank: [item.cashOrBank ? item.cashOrBank : 1, Validators.required],
            ledgerId: [item.ledgerId ? item.ledgerId : 1, Validators.required],
        });
    }

    onChangeSalesMasters(id) {
        if (this.form.get('returnType').value != 0) {
            this._proxy.getSalesDetailsByMasterId(id).subscribe((data) => {
                this.getSalesDetailsById(data);
            });
        }

    }

    getSalesDetailsById(data) {

        const control = this.form.get('salesReturnDetail') as FormArray;

        control.clear();

        control.enable();

        data.forEach((items, index) => {

            this.addDetailForm(items);

            const row = control.at(index);

            if (items.productId) {

                this._proxy.getProductById(items.productId).subscribe((result) => {

                    const unitList = row.get('unitsList') as FormArray;

                    unitList.clear();

                    if (result.unitsList && result.unitsList.length > 0) {

                        result.unitsList.forEach((x) => {

                            unitList.push(
                                this.fb.group({
                                    unitId: [x.unitId],
                                    unitName: [x.unitName],
                                    rate: [x.rate],
                                    qty: [x.qty],
                                })
                            );
                        });

                        if (!row.get('unitId').value) {

                            row.get('unitId').setValue(result.unitsList[0].unitId);
                        }
                    }
                });
            }

            this.totalPriceCalculation();
        });
    }


    getDetail(form) {
        return form.controls.salesReturnDetail.controls;
    }

    getImeiList(form) {
        return form.controls.imeiList.controls;
    }

    getsalesReturnDetail(form) {
        return form.controls.salesReturnDetail.controls;
    }

    getAdditionalCost(form) {
        return form.controls.additionalCosts.controls;
    }

    onLedger() {
        this._proxy.getAllExpensesLedgerForTableDropdown().subscribe((data) => {
            this.allExpenseLedgers = data;
        });
        this._proxy.getAllCashOrBankForTableDropdown().subscribe((data) => {
            this.allCashorBank = data;
            if (!this.id) {
            }
        });
        this.getAllSalesAccounts();
        this.getAllProduct();
        if (!this.id) {
            this._proxy.getVoucherGenerateType().subscribe((x) => {
                if (x == 'Automatic') {
                    this.automaticInput = true;

                    this._proxy.getSalesReturnVoucherNo().subscribe((x) => {
                        this.form.get('voucherNo').setValue(x);
                    });
                } else {
                    this.automaticInput = false;
                    if (x == 'Manually') {
                        this.isManual = true;
                    } else {
                        this.isManual = false;
                    }
                }
            });
        }
        this.getAllAccountLedgers();
    }
    getAllSalesAccounts() {
        this._proxy.getSalesAccountForTableDropdown().subscribe(data => {
            this.allSalesAccounts = data;
            if (!this.id) {
                this.form.get('salesAccountId').setValue(data[0].id);
            }
        });
    }

    getAllAccountLedgers() {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe(data => {
            this.allAccountLedgers = data;
            this.formLoader = false;
            if (!this.id) {
                this.form.get('ledgerId').setValue(data[0].id);
            } else {
                setTimeout(() => {
                    document.getElementById('npDatePicker').focus();

                }, 300);
            }

        });
    }

    ngOnDestroy(): void {
        //Called once, before the instance is destroyed.
        //Add 'implements OnDestroy' to the class.
        this.destroy$.next();
        this.destroy$.complete();
    }

    onGetProduct(id: string, abcd: FormGroup, i?: number) {

        if (!id) {
            return;
        }

        this._proxy.getProductById(id).subscribe((result) => {

            if (!result) {
                return;
            }

            // units list form array
            const unitList = abcd.get('unitsList') as FormArray;

            // clear previous units
            unitList.clear();

            // add units from api
            if (result.unitsList && result.unitsList.length > 0) {

                result.unitsList.forEach((x) => {

                    unitList.push(
                        this.fb.group({
                            unitId: [x.unitId],
                            unitName: [x.unitName],
                            rate: [x.rate],
                            qty: [x.qty],
                            productId: [x.productId]
                        })
                    );
                });

                // set default first unit
                abcd.get('unitId').setValue(result.unitsList[0].unitId);

                // set default rate
                abcd.get('rate').setValue(result.unitsList[0].rate);
            }

            // set qty
            abcd.get('qty').setValue(1);

            // set tax
            abcd.get('taxId').setValue(result.taxId);

            // recalculate totals
            this.calculateTotalByInput(abcd, 'qty');

            // force ui refresh
            abcd.updateValueAndValidity();

        }, error => {

            console.error(error);

            this.notify.error('Failed to load product');
        });
    }

    getAllProduct() {
        this._proxy.getAllProduct().subscribe(data => {
            this.allProducts = data;
        });
    }

    keyEventsDetail(): void {

        if (this.form.get('salesReturnDetail').valid) {

            this.addDetailForm();
        }
    }

    addDetailForm(item: any = {}) {
        const control = <FormArray>this.form.controls.salesReturnDetail;
        control.push(this.createDetails(item));
    }


    removeIME(i, j) {
        const control = <FormArray>this.form.get('salesReturnDetail')['controls'][i].get('imeiList');

        control.removeAt(j);
    }


    //   remove detail form
    removeReturnDetailForm(i, form) {
        const control = form.controls.salesReturnDetail;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalPriceCalculation();
    }

    removeAdditionalCostForm(x, form) {
        const control = form.controls.additionalCosts;
        control.removeAt(x);
    }

    calcqty(abcd) {
        abcd.get('qty').setValue(abcd.get('imeiList').length);

        abcd.get('qty').disable();

        this.calculateTotalByInput(abcd, 'qty');
    }

    // calculateTotalByInput(abcd, inputname) {
    //     // Cache commonly accessed form values to avoid repetitive property access
    //     const formValues = {
    //         qty: Number(abcd.get('qty').value) || 0,
    //         rate: Number(abcd.get('rate').value) || 0,
    //         discountPer: Number(abcd.get('discountPer').value) || 0,
    //         discount: Number(abcd.get('discount').value) || 0,
    //         taxId: abcd.get('taxId').value,
    //         isAllowSerialNo: abcd.get('isAllowSerialNo').value
    //     };

    //     // Early validation for discount percentage
    //     if (formValues.discountPer >= 100 || formValues.discountPer < 0) {
    //         if (formValues.discountPer >= 100) {
    //             this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
    //         }
    //         abcd.patchValue({
    //             discountPer: 0,
    //             discount: 0
    //         }, { emitEvent: false });
    //         return;
    //     }

    //     // Get the tax rate once with proper validation
    //     const selectedTax = this.allTaxes?.find(tax => tax.id === formValues.taxId);
    //     const taxRate = selectedTax?.rate || 0;
    //     const taxRatePercent = taxRate / 100;

    //     // Calculate amount based on input type
    //     const amount = inputname === 'totalamt' ?
    //         (Number(abcd.get('amount').value) || 0) :
    //         formValues.qty * formValues.rate;

    //     // For invalid rate values, reset the form values
    //     if (inputname === 'rate' && formValues.rate <= 0) {
    //         abcd.get('rate').markAsDirty();
    //         abcd.patchValue({
    //             grossAmount: 0,
    //             discount: 0,
    //             netAmount: 0,
    //             taxAmount: 0,
    //             amount: 0
    //         }, { emitEvent: false });
    //         this.totalPriceCalculation();
    //         return;
    //     }

    //     // Handle 'qty' input type with validation
    //     if (inputname === 'qty') {
    //         if (formValues.qty <= 0) {
    //             abcd.get('qty').markAsDirty();
    //         }
    //     }

    //     // Calculate base values
    //     const discountAmt = (amount * formValues.discountPer) / 100;
    //     const netAmount = amount - discountAmt;
    //     const taxAmount = netAmount * taxRatePercent;
    //     const totalAmount = netAmount + taxAmount;

    //     // Handle discount amount validation
    //     if (inputname === 'discount') {
    //         if (formValues.discount > amount) {
    //             abcd.get('discount').markAsDirty();
    //             this.notify.warn(this.l('Discount Amount is greater than Amount'));
    //             abcd.patchValue({
    //                 discount: 0,
    //                 discountPer: 0
    //             }, { emitEvent: false });
    //             this.totalPriceCalculation();
    //             return;
    //         }
    //         // Recalculate discount percentage when discount amount is entered directly
    //         formValues.discountPer = amount > 0 ? (formValues.discount / amount) * 100 : 0;
    //     }

    //     // Handle total amount calculation (working backward)
    //     if (inputname === 'totalamt') {
    //         const netValuey = 1 + taxRatePercent;
    //         const netX = amount / netValuey;
    //         const grossValueY = 1 - formValues.discountPer / 100;
    //         const grossX = netX / grossValueY;
    //         const newDiscountAmt = grossX * (formValues.discountPer / 100);
    //         const newTaxAmount = netX * taxRatePercent;
    //         const newRate = formValues.qty > 0 ? grossX / formValues.qty : 0;

    //         // Update form with calculated values
    //         abcd.patchValue({
    //             netAmount: this.roundToTwo(netX),
    //             taxAmount: this.roundToTwo(newTaxAmount),
    //             grossAmount: this.roundToTwo(grossX),
    //             discount: this.roundToTwo(newDiscountAmt),
    //             rate: this.roundToThree(newRate)
    //         }, { emitEvent: false });
    //     } else {
    //         // Update form with calculated values for other input types
    //         abcd.patchValue({
    //             grossAmount: this.roundToTwo(amount),
    //             discount: this.roundToTwo(discountAmt),
    //             netAmount: this.roundToTwo(netAmount),
    //             taxAmount: this.roundToTwo(taxAmount),
    //             amount: this.roundToTwo(totalAmount)
    //         }, { emitEvent: false });
    //     }

    //     // Calculate the total price once after all updates
    //     this.totalPriceCalculation();
    // }


    calculateTotalByInput(abcd, inputname) {

        const qty = Number(abcd.get('qty').value || 0);

        const rate = Number(abcd.get('rate').value || 0);

        let discountPer = Number(abcd.get('discountPer').value || 0);

        if (discountPer >= 100) {

            discountPer = 0;

            abcd.get('discountPer').setValue(0);
        }

        const taxId = abcd.get('taxId').value;

        let taxRate = 0;

        if (taxId) {

            const taxObj = this.allTaxes.find(x => x.id == taxId);

            if (taxObj) {

                taxRate = taxObj.rate;
            }
        }

        const grossAmount = qty * rate;

        const discountAmount = (grossAmount * discountPer) / 100;

        const netAmount = grossAmount - discountAmount;

        const taxAmount = (netAmount * taxRate) / 100;

        const totalAmount = netAmount + taxAmount;

        abcd.get('grossAmount').setValue(
            this.roundToTwo(grossAmount),
            { emitEvent: false }
        );

        abcd.get('discount').setValue(
            this.roundToTwo(discountAmount),
            { emitEvent: false }
        );

        abcd.get('netAmount').setValue(
            this.roundToTwo(netAmount),
            { emitEvent: false }
        );

        abcd.get('taxAmount').setValue(
            this.roundToTwo(taxAmount),
            { emitEvent: false }
        );

        abcd.get('amount').setValue(
            this.roundToTwo(totalAmount),
            { emitEvent: false }
        );

        this.totalPriceCalculation();
    }

    roundToTwo(num) {
        return Math.round((num + Number.EPSILON) * 100) / 100;
    }

    roundToThree(num) {
        return Math.round((num + Number.EPSILON) * 1000) / 1000;
    }

    totalPriceCalculation() {

        let gross = 0;
        let discount = 0;
        let taxable = 0;
        let tax = 0;
        let grand = 0;

        this.salesReturnDetail.controls.forEach((x: FormGroup) => {

            gross += Number(x.get('grossAmount').value || 0);

            discount += Number(x.get('discount').value || 0);

            taxable += Number(x.get('netAmount').value || 0);

            tax += Number(x.get('taxAmount').value || 0);

            grand += Number(x.get('amount').value || 0);
        });

        this.form.get('totalAmount').setValue(this.roundToTwo(gross));

        this.form.get('billDiscount').setValue(this.roundToTwo(discount));

        this.form.get('taxableAmount').setValue(this.roundToTwo(taxable));

        this.form.get('taxAmount').setValue(this.roundToTwo(tax));

        this.form.get('grandTotal').setValue(this.roundToTwo(grand));
    }

    // onAgainstChange(event) {
    //     if (event.id === 0) {
    //         this.form.get('salesMasterId').disable();
    //         (this.form.get('salesReturnDetail') as FormArray).clear();
    //         // this.addDetailForm();
    //         this.form.get('salesMasterId').setValue(this.emptyGuId);
    //         this.form.get('totalAmount').setValue(0);
    //     } else {
    //         this.form.get('salesMasterId').enable();
    //         this.getAllSalesInvoice();
    //     }
    // }

    getAllSalesInvoice(ledgerId: string) {
        //   const ledId = this.form.get('ledgerId').value;
        this._proxy.getSalesInvoiceIdByLedgerId(ledgerId).subscribe(data => {
            this.allSalesMasters = data;
        });
    }

    checkAndSave() {
        this.saving = true;
        if (this.isManual) {
            const vNo = this.form.get('voucherNo').value;
            this._proxy.getCheckVoucherNo(vNo).subscribe((value) => {
                if (!value) {
                    this.save();
                } else {
                    this.saving = false;
                    alert('Voucher Number Already Exists !!!');
                }
            });
        } else {
            this.save();
        }
    }

    save() {
        if (this.form.valid) {
            if (this.id) {
                this.message.confirm('', this.l('Do you want Update ?'), (isConfrim) => {
                    if (isConfrim) {
                        this.CreatOREdit();
                    }
                });
            } else {
                this.CreatOREdit();
            }
        } else {
            this.notify.error('Form is invalid !!');
        }
    }

    CreatOREdit() {
        this.saving = true;
        this._proxy
            .createOrEdit(this.form.getRawValue())
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe((data) => {
                if (this.isPrint === true) {
                    if (this.id) {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/sales/pdf/4/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/sales/pdf/4/${data}`]);
                    }
                } else {
                    if (this.id) {
                        this.ngOnInit();
                        this._location.back();
                        this.notify.info(this.l('Updated Successfully'));
                    } else {
                        this.ngOnInit();
                        this._location.back();
                        this.notify.info(this.l('Saved Successfully'));
                    }
                }
            });
    }

    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want Cancel ?'), (isConfrim) => {
                if (isConfrim) {
                    this.ngOnInit();
                    this._location.back();
                }
            });
        } else {
            this.ngOnInit();
            this._location.back();
        }
    }

    changeUnit(rate, abcd) {
        abcd.get('rate').setValue(rate);
        this.calculateTotalByInput(abcd, 'rate');

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
            this.save();
        } else {
            return;
        }
    }


    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy.delete(id).subscribe(() => {
                    this.ngOnInit();
                    this._location.back();
                    this.notify.success('Deleted Successfully');
                });
            }
        });
    }

    onSaveOnInput(event: any) {
        if (event.which === 13) {
            event.preventDefault();
            if (this.id) {
                this.save();
            } else {
                if (this.form.valid) {
                    this.message.confirm('', this.l('Do you want to Save ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.save();
                        }
                    });
                } else {
                    this.save();
                }

            }
        }
    }

    // getReturnLedgerId() {
    //     this._proxy.getAllPartyWiseAccountLedgerForTableDropdown().subscribe((result) => {
    //         this.returnLedgerIdData = result;
    //         this.form.get("returnLedgerId").setValue(this.returnLedgerIdData[0].id);
    //     })
    // }


    changePartyWiseCalculation(id?: string) {
        if (id) {
            this.taxIdForParty = id;
        }

        if (!this.returnTaxData || !this.taxIdForParty) {
            return;
        }

        const selectedTax = this.returnTaxData.find((data) => data.id == this.taxIdForParty);
        const taxRate = selectedTax ? selectedTax.rate : 0;
        const returnAmount = this.form.get('returnAmount')?.value || 0;
        const taxAmount = returnAmount * (taxRate / 100);

        this.form.get('returnTaxAmount').setValue(taxAmount);
        this.form.get('taxAmount').setValue(taxAmount);
        this.form.get('totalAmount').setValue(returnAmount);
        this.form.get('billDiscount').setValue(0);
        this.form.get('grandTotal').setValue(returnAmount + taxAmount);
        this.form.get('netAmount').setValue(returnAmount);

        if (taxAmount == 0) {
            this.form.get('taxableAmount').setValue(0);
        } else {
            this.form.get('taxableAmount').setValue(returnAmount);
        }
    }

    incOrDec(form) {
        form.get('debitOrCreditNote').setValue(!form.get('debitOrCreditNote').value);
    }

    changeReturnType() {
        this.form.get('salesMasterId').setValue(this.emptyGuId);
        this.salesReturnDetail.clear();
        this.totalPriceCalculation();
        this.form.get('returnAmount').setValue(0);
        this.form.get('returnTaxAmount').setValue(0);
        this.changePartyWiseCalculation();
        // if (id == 0) {
        //     this.againstMode.get('modeId').setValue(this.emptyGuId);
        //     this.form.get('salesMasterId').setValue(this.emptyGuId);
        //     this.salesReturnDetail.clear();
        //     this.totalPriceCalculation();
        // } else {
        //     this.form.get('returnAmount').setValue(0);
        //     this.form.get('returnTaxAmount').setValue(0);
        //     this.changePartyWiseCalculation();
        // }
        // if (!this.id) {
        //     this.form.get("returnAmount").setValue(0);
        //     this.changePartyWiseCalculation();
        // }
    }

    getReturnTaxId() {
        this._proxy.getAllTaxAccountLedgerForTableDropdown().subscribe((data) => {
            this.returnTaxData = data;
            if (data && data.length > 0) {
                this.taxIdForParty = data[0].id;
                this.form.get('returnTaxId').setValue(data[0].ledgerId);
            }
        });
    }

    // FOR FILEUPLOADER START
    onSingleImage(event) {
        const formData = new FormData();
        const salesReturnId = this.id;
        formData.append('file', event.target.files[0]);
        if (event.target.files?.length) {
            const [file] = event.target.files;
            this.imageFile = { data: event.target.files[0], fileName: file.name, };
            const extensionType = file.type;
            if (extensionType == 'image/jpeg' || extensionType == 'image/png' || extensionType == 'image/jpg' || extensionType == 'application/pdf') {
                this._proxy.uploadImageNew(salesReturnId, this.imageFile).subscribe(() => {
                    this.getData();
                });
            } else {
                this.notify.info('Only jpeg, jpg, png, pdf files are allowed');
            }

        }
    }

    getData() {
        if (!this.id) {
            return;
        }

        this._proxy.getAllDocuments(this.id).subscribe((result) => {
            this.documents = result;
            this.displayedRow = this.documents;
            if (this.documents && this.documents.length > 0) {
                this.imageId = this.documents[0].id;
            }
        });
    }

    getImages() {
        //  const dialogRef = this._dialogService.open(template, { responsivePadding: true });
        // this._proxy.getImage(id).subscribe((result) => {
        //     this.imageType = result.fileType;
        //     if (this.imageType == '.pdf') {
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
                this._proxy.deleteFile(id).subscribe(() => {
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

    fromDate(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.salesaccEvent.open();
        }
    }

    fromSalesAc(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.salesaccEvent.close();
            this.ledgerEvent.open();
        }
    }

    fromCashParty(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.close();
            this.returnEvent.open();
        }
    }

    fromReturnType(e) {
        const id = this.form.get('returnType').value;
        if (e.which === 13) {
            e.preventDefault();
            this.returnEvent.close();
            if (id > 0) {
                this.modeEvent.open();
            } else {
                this.returnAmtEvent.nativeElement.focus();
            }
        }
    }

    fromAgainstMode(e) {
        if (!this.againstMode) {
            return;
        }

        const id = this.againstMode.get('modeId')?.value;
        if (e.which === 13) {
            e.preventDefault();
            this.modeEvent.close();
            if (id > 0) {
                this.salesInvoiceEvent.open();
            } else {
                if (this.productEvent && this.productEvent.first) {
                    this.productEvent.first.open();
                }
            }
        }
    }

    fromSalesInvoice(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.salesInvoiceEvent.close();

        }
    }

    fromTransCop(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.lrnoEvent.nativeElement.focus();
        }
    }

    fromLern(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.descEvent.nativeElement.focus();
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
            this.trnsEvent.nativeElement.focus();
        }
    }

    selectNa(id) {
        this.hideTable = true;
        this.showTable = false;
        (this.form.get('salesReturnDetail') as FormArray).clear();
        if (id == 3) {
            this.invoiceVoucher = false;
            this.form.get('salesMasterId').setValue(this.emptyGuId);
            this.showTable = true;
            this.hideTable = false;
            this.isTaxAmount = false;
            this.addDetailForm();
        } else {
            this.showTable = false;
            this.hideTable = true;
            this.invoiceVoucher = true;
            this.isTaxAmount = true;
        }
    }

    showHideTable() {
        if (this.form.get('returnType').value == 0) {
            this.showTable = false;
            this.hideTable = false;

        }
        if ((this.form.get('returnType').value == 1) || (this.form.get('returnType').value == 2)) {
            this.showTable = true;
            this.hideTable = false;
        }
        if (this.form.get('returnType').value == 3) {
            this.showTable = true;
            this.hideTable = false;
            (this.form.get('salesReturnDetail') as FormArray).clear();
            this.addDetailForm();
            this.units = true;
            this.taxes = true;
            this.isTaxAmount = false;
        }
        // this.showTable=true;
        // this.hideTable = false;
        // this.keyEventsDetail();
    }

    createAgainstModeForm() {
        this.againstMode = this.fb.group({
            modeId: [this.emptyGuId]
        });
    }
}
