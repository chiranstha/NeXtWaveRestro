import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnInit, QueryList, TemplateRef, ViewChild, ViewChildren } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DocumentDetailsDto,
    PurchaseDetailDto,
    PurchaseMasterAccountLedgerTableDto,
    PurchaseMastersServiceProxy,
    PurchaseMasterTaxTableDto,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { Location } from '@angular/common';

import { AgainstMode, ModeType } from './against-mode.service';
import { debounceTime, finalize, first } from 'rxjs/operators';
import { lastValueFrom, Observable, timer } from 'rxjs';
import { HttpClient, HttpEventType, HttpResponse } from '@angular/common/http';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { NgSelectComponent } from '@ng-select/ng-select';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-purchase-invoice',
    templateUrl: './add-purchase-invoice.component.html',
    styleUrls: ['./add-purchase-invoice.component.css'],
    animations: [appModuleAnimation]

})
export class AddPurchaseInvoiceComponent extends AppComponentBase implements OnInit {
    @ViewChild('submitButton') submitButton: ElementRef;
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('purchaseAcs') purchaseEvent: NgSelectComponent;
    @ViewChild('remarks', { read: ElementRef }) remarksEvent: ElementRef;
    @ViewChild('vendorNo', { read: ElementRef }) vendorNoEvent: ElementRef;
    @ViewChild('voucherType') voucherTypeEvent: NgSelectComponent;
    @ViewChild('orderNo') orderNoEvent: NgSelectComponent;
    @ViewChild('creditPeriods', { read: ElementRef }) creditPeriodEvent: ElementRef;
    @ViewChild('purchaseType') purchaseTypeEvent: NgSelectComponent;
    // Details
    @ViewChildren('qty', { read: ElementRef }) qtyEvent: QueryList<ElementRef>;
    @ViewChildren('rates', { read: ElementRef }) rateEvent: QueryList<ElementRef>;
    @ViewChildren('total', { read: ElementRef }) totalEvent: QueryList<ElementRef>;
    @ViewChildren('productKey') productEvent: QueryList<NgSelectComponent>;
    @ViewChildren('unit') unitEvent: QueryList<NgSelectComponent>;
    @ViewChild('lrno', { read: ElementRef }) lrnoEvent: ElementRef;
    @ViewChild('descrip', { read: ElementRef }) descripEvent: ElementRef;
    @ViewChildren('discountPers', { read: ElementRef }) discountPersEvent: QueryList<ElementRef>;
    @ViewChild('transportcomp', { read: ElementRef }) transportcompEvent: ElementRef;
    @ViewChildren('dis', { read: ElementRef }) disEvent: QueryList<ElementRef>;
    @ViewChildren('taxess') taxessEvent: QueryList<NgSelectComponent>;

    modalRef?: BsModalRef;
    // file uploader
    title = 'Create Purchase Invoice';
    // FILEUPLOADER END
    shortcuts: ShortcutInput[] = [];

    //form
    form: FormGroup;
    // additionalForm: FormGroup;
    imeiForm: FormGroup;
    //variable
    id: string;
    isManual = false;
    automaticInput = false;
    productId: string;
    ledgerId: string;
    saving = false;
    expanded: boolean;
    hideSpecUnit = true;
    disabledAddIme = false;
    purchaseOrderMasterDescription = '';
    ledgerName = '';
    branchName = '';
    dataFilled = true;
    allAccountLedgers$: Observable<PurchaseMasterAccountLedgerTableDto[]>;
    allCashorBank: PurchaseMasterAccountLedgerTableDto[];
    // allBranchs$: Observable<PurchaseMasterBranchTableDto[]>;
    allTaxes: PurchaseMasterTaxTableDto[];
    allTaxes$: Observable<PurchaseMasterTaxTableDto[]>;
    purchaseAccount$: Observable<UniversalDropdownDto[]>;

    aganstView = true;
    ledgerLoading: boolean;
    editAgainstView = false;
    voucherNo: number;

    unitId: number;
    // allGodOwns: MaterialReceiptDetailGodOwnTableDto[];
    purchaseDetailList: PurchaseDetailDto[];
    // allUnits: MaterialReceiptMasterBranchTableDto[];
    // allRacks: PurchaseMasterRackTableDto[];
    displayMode: boolean;
    formLoader = true;
    selectedAgainstMode: AgainstMode = { id: 0, name: 'N/A' };
    units = false;
    discountPercent = false;
    discountAmount = false;
    taxes = false;
    // againstMode: AgainstMode[];
    againstMode = [
        { id: 0, name: 'N/A' },
        { id: 1, name: 'Purchase Order' },
        { id: 2, name: 'Material Receipt' },
    ];
    modeType: ModeType[];

    disableIme = false;
    accountledgerId: string;
    addNew: any = [{ id: null, displayName: 'Add New' }];
    debitCredut = [
        { id: 0, name: 'Dr' },
        { id: 1, name: 'Cr' },
    ];
    againstType = [
        {
            id: 0,
            name: 'Purchase Order',
        }
    ];
    order: boolean;
    fileName: string;
    orderOrReceiptVoucherName: string;
    settings: TenantSettingsEditDto = undefined;
    percentDone = 0;
    formDetailRowId: number | null = null;
    // imeiToArray:FormControl;

    width: any = 0;
    mainFile: string | ArrayBuffer;
    messages: string;
    allProducts$: Observable<UniversalDropdownDto[]>;
    allUnits: UniversalDropdownDto[] = [];
    additionalItems: FormArray;

    storingTableDataChecks: any;

    displayedRow: DocumentDetailsDto[];
    serialNumber = 0;

    constructor(
        private fb: FormBuilder,
        injector: Injector,
        private route: ActivatedRoute,
        private http: HttpClient,
        private router: Router,
        private modalService: BsModalService,
        private _purchaseMasterServiceProxy: PurchaseMastersServiceProxy,
        private _location: Location,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        // private _selectService: AgainstModeService
    ) {
        super(injector);
        this.getSetting();
    }

    //   purchase Detail array
    get purchaseDetail() {
        return this.form.get('purchaseDetail') as FormArray;
    }

    customSearchFn(term: string, item: PurchaseMasterAccountLedgerTableDto) {
        term = term.toLocaleLowerCase();
        const searchItem = `${item.displayName}/${item.mobileNo || ''}/${item.panNo || ''}`;
        return searchItem.toLowerCase().indexOf(term) > -1 || searchItem.toLowerCase() === term;
    }

    getTerm() {
    }

    searchFilterAccWise(term: string, item: PurchaseMasterAccountLedgerTableDto) {
        term = term.toLowerCase();

        return item.displayName.toLowerCase().indexOf(term) > -1;
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.createForm();
        this.changeVoucher();

        if (!this.id) {
            this.form.get('dateMiti').setValue(this.today);
        }
        this.fetchAll();
        if (this.id) {
            this.title = 'Edit Purchase Invoice';
            this._purchaseMasterServiceProxy.getPurchaseMasterForEdit(this.id).subscribe((result) => {

                this.formLoader = false;
                this.changeVoucher();
                this.aganstView = false;
                this.editAgainstView = true;
                this.createForm(result);
            });
        }

        this._purchaseMasterServiceProxy.getAllTaxForTableDropdown().subscribe((result) => {
            this.allTaxes = result;
        });
        this.watchPurchaseDetailChanges();
    }

    watchPurchaseDetailChanges() {
        this.purchaseDetail.valueChanges
            .pipe(debounceTime(50))
            .subscribe(() => {
                this.totalPriceCalculation();
            });
    }

    getSettingsForPrint(): void {
        this._tenantSettingsService.getAllSettings().subscribe((result: TenantSettingsEditDto) => {
            this.settings = result;
            // if (this.settings.allSettingsBundleDto.tickPrintAfterSave === true) {
            //     this.isPrint = true;
            // }
        });
    }

    onSelect(againstid) {
        if (againstid === 0) {
            this.modeType = null;
            this.form.get('orderOrReceiptId').reset();
            this.form.get('againstId').reset();
            this.purchaseDetail.reset();
        } else {
            //       this.modeType = this._selectService.getModeType().filter((item) => item.againstid === againstid);
            this.form.get('againstId').reset();
            this.purchaseDetail.reset();
        }
    }

    // unitConversion(formGroup) {
    //     if (this.form.value.invoiceTypeEnum !== 1) {

    //         try {
    //             // Cache form controls
    //             const controls = {
    //                 unitsList: formGroup.get('unitsList') as FormArray,
    //                 unitId: formGroup.get('unitId'),
    //                 productId: formGroup.get('productId'),
    //                 rate: formGroup.get('rate')
    //             };

    //             if (!controls.unitId?.value || !controls.productId?.value) {
    //                 return;
    //             }

    //             // Get selected unit
    //             const selectedUnit = controls.unitsList.controls.find(control =>
    //                 control.get('unitId')?.value === controls.unitId?.value
    //             )?.value as PurchaseReturnUnitsQtyDto;

    //             if (!selectedUnit) {
    //                 this.notify.warn('No unit selected');
    //                 return;
    //             }

    //             // Update form and related data
    //             const { rate } = selectedUnit;

    //             if (controls.rate.value === 0) {
    //                 controls.rate.setValue(rate);
    //             }

    //             this.calculateTotalByInput(formGroup, 'rate');

    //         } catch (error) {
    //             console.error('Error in handleUnitSelection:', error);
    //             this.notify.error('Failed to process unit selection');
    //         }


    //         this.calculateTotalByInput(formGroup, 'rate');
    //     }
    //     this.totalPriceCalculation();
    // }


    // get product
    onGetProduct(id: string, abcd: FormGroup, i: number) {
        if (!id) {
            return;
        } else {
            this.hideSpecUnit = false;
            this._purchaseMasterServiceProxy.getProductForView(id).subscribe((result) => {
                abcd.get('id').setValue(this.emptyGuId);
                const unitList = abcd.get('unitsList') as FormArray;
                unitList.clear();
                const selectedUnit = this.allUnits.find(x => x.id === result.unitId);
                if (selectedUnit) {
                    unitList.push(
                        this.fb.group({
                            unitId: selectedUnit.id,
                            unitName: selectedUnit.displayName
                        })
                    );
                }
                abcd.get('unitId').setValue(result.unitId);
                abcd.get('taxId').setValue(result.taxId);
                abcd.get('rate').setValue(result.rate);
                abcd.get('taxValue').setValue(result.taxRate);
                abcd.get('qty').setValue(1);
                const qty = abcd.get('qty').value;
                abcd.get('taxAmount').setValue(result.taxRate * result.rate * qty);
                abcd.get('discount').setValue(0);
                abcd.get('discountPercent').setValue(0);
                abcd.get('netAmount').setValue(result.rate * qty);
                abcd.get('grossAmount').setValue(abcd.get('qty').value * abcd.get('rate').value);
                abcd.get('amount').setValue(result.rate + abcd.get('taxAmount').value * qty);
                this.calculateRow(abcd);
            });
        }
        // this.totalPriceCalculation();
    }

    createForm(item: any = {}) {

        this.form = this.fb.group({
            voucherNo: [item.voucherNo ? item.voucherNo : this.voucherNo, Validators.required],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            vendorInvoiceNo: [item.vendorInvoiceNo, Validators.required],
            creditPeriod: [item.creditPeriod ? item.creditPeriod : 0, Validators.required],
            narration: [item.narration ? item.narration : ''],
            totalTax: [item.totalTax ? item.totalTax : 0, Validators.required],
            totalAmount: [item.totalAmount ? item.totalAmount : 0, Validators.required],
            billDiscount: [item.billDiscount ? item.billDiscount : 0, Validators.required],
            grandTotal: [item.grandTotal ? item.grandTotal : 0, Validators.required],
            taxableAmount: [item.taxableAmount ? item.taxableAmount : 0],
            netAmount: [item.netAmount ? item.netAmount : 0],
            lrNo: [item.lrNo ? item.lrNo : ''],
            againstId: [item.againstId ? item.againstId : 0],
            orderOrReceiptId: [item.orderOrReceiptId ? item.orderOrReceiptId : this.emptyGuId, Validators.required],
            purchaseAccountId: [item.purchaseAccountId ? item.purchaseAccountId : 1, Validators.required],
            ledgerId: [item.ledgerId ? item.ledgerId : this.emptyGuId, Validators.required],
            purchaseDetail: this.fb.array(
                (() => {
                    if (!item.purchaseDetail) {
                        return [this.createpurchaseMasterDetails()];
                    }
                    return item.purchaseDetail.map((item) => this.createpurchaseMasterDetails(item));
                })()
            ),
            id: [item.id ? item.id : this.emptyGuId],
        });
    }

    onChangeAccLedger(creditPeriod) {
        if (this.form.value.againstId !== 0) {
            this.form.get('againstId').setValue(this.emptyGuId);
            this.dataFilled = true;
            this.form.get('orderOrReceiptId').setValue(this.emptyGuId);
            this.purchaseDetail.clear();
            this.addDetailForm();
        }
        this.form.get('creditPeriod').setValue(creditPeriod);
    }

    onChangeOrderOrReceipt(data) {
        if (data && data !== null) {
            const branchId = this.form.get('branchId').value;
            const modeId = this.form.get('againstId').value;
            if (branchId && branchId !== null && modeId && modeId !== null) {
                //       this.getProductByNumber(data, modeId, branchId);
            }
        }
    }

    createpurchaseMasterDetails(item: any = {}) {
        return this.fb.group({
            id: new FormControl(item.id ? item.id : this.emptyGuId),
            qty: new FormControl(item.qty ? item.qty : 0, Validators.required),
            discount: new FormControl(item.discount ? item.discount : 0),
            discountPercent: new FormControl(item.discountPercent ? item.discountPercent : 0),
            taxAmount: new FormControl(item.taxAmount ? item.taxAmount : 0),
            grossAmount: new FormControl(item.grossAmount ? item.grossAmount : 0),
            netAmount: new FormControl(item.netAmount ? item.netAmount : 0),
            orderDetailId: new FormControl(item.orderDetailId ? item.orderDetailId : this.emptyGuId),
            amount: new FormControl(item.amount ? item.amount : 0, Validators.required),
            productId: new FormControl({ value: item.productId ? item.productId : null, disabled: item.id }, Validators.required),
            unitId: new FormControl(item.unitId ? item.unitId : this.emptyGuId, Validators.required),
            taxId: new FormControl(item.taxId, Validators.required),
            taxValue: [item.taxValue ? item.taxValue : 0],
            rate: new FormControl(item.rate ? item.rate : 0),
            unitsList: this.fb.array([])
        });
    }

    getPurchaseDetail(form) {
        return form.controls.purchaseDetail.controls;
    }

    //   remove detail form
    removePurchaseDetailForm(i, form) {
        const control = form.controls.purchaseDetail;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalPriceCalculation();
    }

    keyEventsDetail(): void {

        if (this.form.get('purchaseDetail').valid) {
            this.addDetailForm();
        }
    }



    addDetailForm(item: any = {}) {
        const control = <FormArray>this.form.controls.purchaseDetail;
        control.push(this.createpurchaseMasterDetails(item));
    }

    fillForm(result: PurchaseDetailDto[], modeId: number) {
        (this.form.get('purchaseDetail') as FormArray).clear();

        result.forEach((item, index) => {
            this.addDetailForm(item);
            if (modeId === 1) {
                const bid = this.form.get('branchId').value;
            }
        });
        // this.addDetailForm();
        this.totalPriceCalculation();
    }

    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this._location.back();
                }
            });
        } else {
            this._location.back();
        }
    }

    changePercent(abcd) {
        const qty = abcd.get('qty').value;
        const importRate = abcd.get('importRate').value;
        const control = abcd.controls.customLedgerList.controls;
        control.forEach((element) => {
            const calcPercent = (element.get('amount').value / (qty * importRate)) * 100;
            element.get('percent').setValue(calcPercent);
        });
    }

    changeInEffectLedger(custom) {
        const ledger = custom.get('ledgerId').value;
        const effectLedger = custom.get('effectLedgerId').value;
        if (ledger === effectLedger) {
            custom.get('effectLedgerId').setErrors(true);
        } else {
            custom.get('effectLedgerId').setErrors(false);
        }
    }

    onEnterPressed(e, i) {
        const keyCode = e.which || e.keyCode;
        if (keyCode === 13) {
            e.preventDefault();
        }
    }


    tableDataIntoObj(data) {
        const obj = { data };
        const result = Object.entries(obj).reduce((acc, curr) => {
            const [key, value] = curr;

            return {
                ...acc,
                [key]: value.map((item) => ({ id: 0, imeiNumber: item })),
            };
        }, {});

    }

    removeDataFromTable(xy, i, ime) {
        const control = <FormArray>this.form.get('purchaseDetail')['controls'][i].get('imeiList');

        if (this.id) {
            if (control.controls[xy].get('id').value === this.emptyGuId) {
                control.removeAt(xy);
            } else {
                // this._purchaseMasterServiceProxy
                //     .getImeiCheckForDelete(this.productCod, ime.get('imeiNumber').value)
                //     .subscribe((data) => {
                //         if (data === true) {
                //             control.removeAt(xy);
                //         }
                //     });
            }
        } else {
            control.removeAt(xy);
        }
    }

    hide() {
        const button = document.getElementById('hide');
        button.style.display = 'none';
    }

    getAllPurchaseAccount() {
        this.purchaseAccount$ = this._purchaseMasterServiceProxy.getAllPurchaseAccountForTableDropdown();
        this.purchaseAccount$.subscribe((data) => {
            if (!this.id) {
                if (data.length > 0) {
                    this.form.get('purchaseAccountId').setValue(data[0].id);
                }
            }
        });

    }

    openDialog(dialog: TemplateRef<any>, abcd): void {
        this.disabledAddIme = false;
        this.modalRef = this.modalService.show(dialog, Object.assign({}, { class: 'gray modal-lg' }));

    }


    taxValueSet(i: number, value: number) {
        const tax =
            (value / 100) *
            (this.purchaseDetail.controls[i].get('qty').value * this.purchaseDetail.controls[i].get('rate').value);
        //
        // const tax = (value/100)*gross;

        this.purchaseDetail.controls[i].get('taxAmount').setValue(tax);
        this.totalPriceCalculation();
    }


    calculateRow(abcd: FormGroup) {
        const qty = +abcd.get('qty').value || 0;
        const rate = +abcd.get('rate').value || 0;
        const discountPercent = +abcd.get('discountPercent').value || 0;
        const taxId = abcd.get('taxId').value;

        if (!qty || !rate) {
            this.setAllZero(abcd);
            return;
        }

        const taxRate = this.allTaxes?.find(x => x.id === taxId)?.rate || 0;

        const gross = qty * rate;
        const discount = (gross * discountPercent) / 100;
        const net = gross - discount;
        const tax = (net * taxRate) / 100;
        const total = net + tax;

        abcd.patchValue({
            grossAmount: this.roundToTwo(gross),
            discount: this.roundToTwo(discount),
            netAmount: this.roundToTwo(net),
            taxAmount: this.roundToTwo(tax),
            amount: this.roundToTwo(total)
        }, { emitEvent: false });
        this.totalPriceCalculation();
    }


    // calculateTotalByInput(abcd, inputname) {
    //     if (inputname === 'imeiCalc') {
    //         let qtys = abcd.get('qty').value;
    //         let rates = abcd.get('rate').value;
    //         let percentage = abcd.get('discountPercent').value;
    //         if (qtys == null || qtys === undefined || qtys === '' || qtys <= 0) {
    //             qtys = 0;
    //         }
    //         if (percentage > 100) {
    //             this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //         } else if (percentage == null || percentage === undefined || percentage === '' || percentage < 0) {
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //             percentage = 0;
    //         } else if (qtys === 0) {
    //             abcd.get('qty').markAsDirty();
    //             abcd.get('grossAmount').setValue(0);
    //             abcd.get('netAmount').setValue(0);
    //             this.notify.warn(this.l('Please enter quantity'));
    //         } else if (rates <= 0) {
    //             abcd.get('rate').markAsDirty();
    //             abcd.get('grossAmount').setValue(0);
    //             abcd.get('netAmount').setValue(0);
    //             this.notify.warn(this.l('Please enter Rate'));
    //         } else {
    //             const taxId = abcd.get('taxId').value;
    //             const taxRate = this.allTaxes.find((x) => x.id === taxId).rate;

    //             let amount = qtys * rates;
    //             let discountAmt = (amount * percentage) / 100;
    //             let netX = amount - discountAmt;
    //             const taxAmount = netX * (taxRate / 100);
    //             let grossX = netX + taxAmount;
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
    //             abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
    //             abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
    //             abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
    //             abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
    //         }
    //     }
    //     if (inputname === 'rate') {
    //         let qty = abcd.get('qty').value;
    //         let rates = abcd.get('rate').value;
    //         let percentage = abcd.get('discountPercent').value;
    //         if (qty === 0) {
    //             abcd.get('qty').markAsDirty();
    //             this.notify.warn(this.l('Please enter quantity'));
    //             this.setAllZero(abcd);
    //         } else if (rates <= 0) {
    //             abcd.get('rate').markAsDirty();
    //             this.notify.warn(this.l('Please enter Rate'));
    //             this.setAllZero(abcd);
    //         } else if (this.allTaxes == null) {
    //             this._purchaseMasterServiceProxy.getAllTaxForTableDropdown().subscribe((data) => {
    //                 this.allTaxes = data;
    //             });
    //         } else if (percentage == null || percentage === undefined || percentage === '') {
    //             this.notify.warn(this.l('Please enter Discount Percent'));
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //             percentage = 0;
    //         } else if (percentage > 100) {
    //             abcd.get('discountPercent').dirty = true;
    //             this.notify.warn(this.l('Value of Discount Percentage must be less than 100'));
    //             abcd.get('discountPercent').reset();
    //         } else {
    //             const taxId = abcd.get('taxId').value;
    //             const taxRate = this.allTaxes.find((x) => x.id === taxId).rate;

    //             let amount = qty * rates;
    //             let discountAmt = (amount * percentage) / 100;
    //             let netX = amount - discountAmt;
    //             const taxAmount = netX * (taxRate / 100);
    //             let grossX = netX + taxAmount;
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
    //             abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
    //             abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
    //             abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
    //             abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
    //         }
    //     }
    //     if (inputname === 'totalamt') {
    //         const tax = abcd.get('taxId').value;
    //         let percentage = abcd.get('discountPercent').value;
    //         const qty = abcd.get('qty').value;
    //         if (this.allTaxes == null) {
    //             this._purchaseMasterServiceProxy.getAllTaxForTableDropdown().subscribe((data) => {
    //                 this.allTaxes = data;
    //             });
    //         } else if (percentage > 100) {
    //             this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //         } else if (percentage == null || percentage === undefined || percentage === '' || percentage < 0) {
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //             percentage = 0;
    //         } else {
    //             const taxRate = this.allTaxes.find((x) => x.id === tax).rate;
    //             let amounts = abcd.get('amount').value;
    //             if (amounts == null || amounts === undefined || amounts === '' || amounts <= 0) {
    //                 return;
    //             }
    //             let netValuey = 1 + taxRate / 100;
    //             let netX = amounts / netValuey;
    //             abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
    //             const taxAmount = this.roundToTwo(netX * (taxRate / 100));
    //             abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
    //             let grossValueY = 1 - percentage / 100;
    //             let grossX = netX / grossValueY;
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
    //             const discountAmount = this.roundToTwo(grossX * (percentage / 100));
    //             abcd.get('discount').patchValue(discountAmount);
    //             const rate = this.roundToTwo(grossX / qty);
    //             abcd.get('rate').patchValue(rate, { emitEvent: false });
    //         }
    //     }

    //     if (inputname === 'qty') {
    //         let qtys = abcd.get('qty').value;
    //         let rates = abcd.get('rate').value;
    //         if (qtys == null || qtys === undefined || qtys === '' || qtys <= 0) {
    //             qtys = 0;
    //             this.setAllZero(abcd);
    //         }
    //         let percentage = abcd.get('discountPercent').value;
    //         if (percentage > 100) {
    //             this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //         } else if (percentage == null || percentage === undefined || percentage === '' || percentage < 0) {
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //             percentage = 0;
    //         } else if (qtys === 0) {
    //             abcd.get('qty').markAsDirty();
    //             this.notify.warn(this.l('Please enter quantity'));
    //             this.setAllZero(abcd);
    //         } else if (rates <= 0) {
    //             abcd.get('rate').markAsDirty();
    //             this.notify.warn(this.l('Please enter Rate'));
    //             this.setAllZero(abcd);
    //         } else {
    //             let grossAmt = qtys * rates;
    //             let discountAmt = (grossAmt * percentage) / 100;
    //             const netAmount = grossAmt - discountAmt;
    //             const taxId = abcd.get('taxId').value;
    //             const taxRate = this.allTaxes.find((x) => x.id === taxId).rate;
    //             const taxAmount = netAmount * (taxRate / 100);
    //             let grandTot = netAmount + taxAmount;
    //             abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(grossAmt), { emitEvent: false });
    //             abcd.get('netAmount').patchValue(this.roundToTwo(netAmount), { emitEvent: false });
    //             abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
    //             abcd.get('amount').patchValue(this.roundToTwo(grandTot), { emitEvent: false });
    //         }
    //     }

    //     if (inputname === 'taxId') {
    //         const tax = abcd.get('taxId').value;
    //         let qtys = abcd.get('qty').value;
    //         let rates = abcd.get('rate').value;
    //         if (tax == null || tax === undefined || tax === '') {
    //             this.notify.warn(this.l('Please select tax'));
    //         } else {
    //             const taxRate = this.allTaxes.find((x) => x.id === tax).rate;
    //             let percentage = abcd.get('discountPercent').value;
    //             if (qtys == null || qtys === undefined || qtys === '' || qtys <= 0) {
    //                 abcd.get('qty').markAsDirty();
    //                 this.notify.warn(this.l('Please enter quantity'));
    //             } else if (percentage > 100) {
    //                 this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
    //                 abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //                 abcd.get('discount').patchValue(0, { emitEvent: false });
    //             } else if (percentage == null || percentage === undefined || percentage === '') {
    //                 abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //                 abcd.get('discount').patchValue(0, { emitEvent: false });
    //                 percentage = 0;
    //             } else if (qtys === 0) {
    //                 abcd.get('qty').markAsDirty();
    //                 this.notify.warn(this.l('Please enter quantity'));
    //             } else if (rates <= 0) {
    //                 abcd.get('rate').markAsDirty();
    //                 this.notify.warn(this.l('Please enter Rate'));
    //             } else {
    //                 let amount = qtys * rates;
    //                 let discountAmt = (amount * percentage) / 100;
    //                 let netX = amount - discountAmt;

    //                 const taxAmount = netX * (taxRate / 100);
    //                 let grossX = netX + taxAmount;
    //                 abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
    //                 abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
    //                 abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
    //                 abcd.get('taxAmount').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
    //                 abcd.get('amount').patchValue(this.roundToTwo(grossX), { emitEvent: false });
    //             }
    //         }
    //     }

    //     if (inputname === 'discountPer') {
    //         let qtys = abcd.get('qty').value;
    //         let rates = abcd.get('rate').value;
    //         if (qtys == null || qtys === undefined || qtys === '' || qtys <= 0) {
    //             qtys = 0;
    //         }
    //         let percentage = abcd.get('discountPercent').value;
    //         if (percentage > 100) {
    //             this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //         } else if (percentage == null || percentage === undefined || percentage === '' || percentage < 0) {
    //             abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             abcd.get('discount').patchValue(0, { emitEvent: false });
    //             percentage = 0;
    //         } else if (qtys === 0) {
    //             abcd.get('qty').markAsDirty();
    //             this.notify.warn(this.l('Please enter quantity'));
    //         } else if (rates <= 0) {
    //             abcd.get('rate').markAsDirty();
    //             this.notify.warn(this.l('Please enter Rate'));
    //         } else {
    //             const taxId = abcd.get('taxId').value;
    //             const taxRate = this.allTaxes.find((x) => x.id === taxId).rate;

    //             let amount = qtys * rates;
    //             let discountAmt = (amount * percentage) / 100;
    //             let netX = amount - discountAmt;
    //             const taxAmount = netX * (taxRate / 100);
    //             abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
    //             abcd.get('netAmount').patchValue(this.roundToTwo(netX), { emitEvent: false });
    //             abcd.get('taxAmount').patchValue(taxAmount, { emitEvent: false });
    //             abcd.get('amount').patchValue(this.roundToTwo(netX + abcd.get('taxAmount').value), { emitEvent: false });
    //         }
    //     }
    //     if (inputname === 'discount') {
    //         const gross = abcd.get('grossAmount').value;
    //         const x = abcd.get('discount').value;
    //         let qtys = abcd.get('qty').value;
    //         let rates = abcd.get('rate').value;
    //         if (qtys == null || qtys === undefined || qtys === '' || qtys <= 0) {
    //             qtys = 0;
    //         }
    //         if (qtys <= 0) {
    //             abcd.get('qty').markAsDirty();
    //             this.notify.warn(this.l('Please enter quantity'));
    //         } else if (rates <= 0) {
    //             abcd.get('rate').markAsDirty();
    //             this.notify.warn(this.l('Please enter Rate'));
    //         } else {
    //             let dp = (x / gross) * 100;
    //             abcd.get('discountPercent').patchValue(this.roundToTwo(dp), { emitEvent: false });
    //             let amount = qtys * rates;
    //             if (dp >= amount) {
    //                 abcd.get('discount').markAsDirty();
    //                 this.notify.warn(this.l('Discount Amount is greate than Amount'));
    //                 abcd.get('discount').patchValue(0, { emitEvent: false });
    //                 abcd.get('discountPercent').patchValue(0, { emitEvent: false });
    //             }
    //             let discountAmt = (amount * dp) / 100;
    //             const netAmount = amount - discountAmt;
    //             abcd.get('discount').patchValue(this.roundToTwo(discountAmt), { emitEvent: false });
    //             abcd.get('grossAmount').patchValue(this.roundToTwo(amount), { emitEvent: false });
    //             abcd.get('netAmount').patchValue(this.roundToTwo(netAmount), { emitEvent: false });
    //             abcd.get('taxAmount').patchValue(this.roundToTwo(netAmount * abcd.get('taxValue').value), { emitEvent: false });
    //             abcd.get('amount').patchValue(this.roundToTwo(netAmount + abcd.get('taxAmount').value), { emitEvent: false });
    //         }
    //     }
    //     if (inputname === 'taxAmount') {
    //         const net = abcd.get('netAmount').value;
    //         const x = abcd.get('taxAmount').value;
    //         abcd.get('amount').setValue(net + x);
    //     }
    //   //  this.totalPriceCalculation();
    // }

    setAllZero(abcd) {
        abcd.get('discount').setValue(0);
        abcd.get('grossAmount').setValue(0);
        abcd.get('netAmount').setValue(0);
        abcd.get('taxAmount').setValue(0);
        abcd.get('amount').setValue(0);
    }

    totalPriceCalculation() {
        let netAmount = 0;
        let totalAmount = 0;
        let taxAmount = 0;
        let discountAmount = 0;
        let taxableAmount = 0;
        let grossAmount = 0;
        for (let i = 0; i < this.purchaseDetail.length; i++) {
            taxableAmount =
                this.purchaseDetail.controls[i].get('taxAmount').value > 0
                    ? this.purchaseDetail.controls[i].get('netAmount').value + taxableAmount
                    : taxableAmount;
            grossAmount = this.purchaseDetail.controls[i].get('grossAmount').value + grossAmount;
            taxAmount = this.purchaseDetail.controls[i].get('taxAmount').value + taxAmount;
            netAmount = this.purchaseDetail.controls[i].get('netAmount').value + netAmount;
            totalAmount = this.purchaseDetail.controls[i].get('amount').value + totalAmount;
            discountAmount = this.purchaseDetail.controls[i].get('discount').value + discountAmount;
        }
        this.form.get('totalAmount').patchValue(this.roundToTwo(grossAmount), { emitEvent: false });
        this.form.get('netAmount').patchValue(this.roundToTwo(netAmount), { emitEvent: false });
        this.form.get('totalTax').patchValue(this.roundToTwo(taxAmount), { emitEvent: false });
        this.form.get('billDiscount').patchValue(this.roundToTwo(discountAmount), { emitEvent: false });
        this.form.get('grandTotal').patchValue(this.roundToTwo(totalAmount), { emitEvent: false });
        this.form.get('taxableAmount').patchValue(this.roundToTwo(taxableAmount), { emitEvent: false });
    }

    roundToTwo(num) {
        return Math.round((num + Number.EPSILON) * 100) / 100;
    }

    keyEventBranch(event) {
        if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                this.router.navigate(['app/main/controlPanel/branchs']);
            }
        }
    }

    changeAccLedger(event) {
        this.serialNumber = event ? 2 : 0;
    }

    changeOnBranch(branchId) {
        this.getAccountLedgers(this.emptyGuId);
        this.getAllProduct(branchId);
        this.getAllPurchaseAccount();
        // this.getAllExpenses(id);
        this.formLoader = false;
        if (!this.id) {
            this.getVoucherNumber();
        }
    }

    fetchAll() {
        this.getAllAccountLedgers();
        this.getAllProducts();
        this.getAllPurchaseAccount();
        this.getAllUnits();
        this.formLoader = false;
        if (!this.id) {
            this.getVoucherNumber();
        }
    }


    getVoucherNumber() {
        this._purchaseMasterServiceProxy.getPurchaseInvoiceVoucherNo().subscribe((x) => {
            this.form.get('voucherNo').setValue(x);
        });
    }

    setAccountLedger() {
        this.form.controls['ledgerId'].setValue(null);
        this.form.controls['orderOrReceiptId'].setValue(null);
    }

    // getAllProducts(productId) {
    //     this.serialNumber = 0;
    //     this.allProducts$ = this._purchaseMasterServiceProxy.getAllProductForTableDropdown(this.branchId);
    //     if (this.allProducts$) {
    //         // this.productId = productId;

    //     }

    getAllProduct(productId?: string | null) {
        this.allProducts$ = this._purchaseMasterServiceProxy.getAllProductForTableDropdown();
        if (!this.serialNumber) {
            return;
        }

        const rowIndex = this.formDetailRowId;
        this.serialNumber = 0;
        this.formDetailRowId = null;

        if (!productId || rowIndex === null || !this.purchaseDetail.controls[rowIndex]) {
            return;
        }

        const purchaseDetail = this.purchaseDetail.controls[rowIndex] as FormGroup;
        purchaseDetail.get('productId')?.setValue(productId);
        setTimeout(() => {
            this.onGetProduct(productId, purchaseDetail, rowIndex);
        }, 1000);
    }

    getAllProducts() {
        this.allProducts$ = this._purchaseMasterServiceProxy.getAllProductForTableDropdown();
    }

    getAllUnits() {
        this._purchaseMasterServiceProxy.getAllUnitForTableDropdown().subscribe(data => {
            this.allUnits = data;
        });
    }

    getAccountLedgers(ledgerId) {
        this.ledgerLoading = true;
        this.allAccountLedgers$ = this._purchaseMasterServiceProxy.getAllAccountLedgerForTableDropdown();
        this.allAccountLedgers$.subscribe((data) => {
            this.ledgerLoading = false;
            if (this.serialNumber) {
                this.form.get('ledgerId').setValue(ledgerId);
                data.forEach((element) => {
                    if (element.id === ledgerId) {
                        this.form.get('creditPeriod').setValue(element.creditPeriod);
                    }
                });
                this.serialNumber = 0;
            } else {
                if (!this.id) {
                    this.form.get('ledgerId').setValue(data[0].id);
                }
            }
        });
    }

    getAllAccountLedgers() {
        this.ledgerLoading = true;
        this.allAccountLedgers$ = this._purchaseMasterServiceProxy.getAllAccountLedgerForTableDropdown();
    }

    // keyEventProduct(event, i) {
    //     if (event.altKey || event.metaKey) {
    //         if (event.which === 67) {
    //             this.formDetailRowId = i;
    //             event.preventDefault();
    //             this.changeProduct(event);
    //         }
    //     }
    // }

    changeProduct(event) {
        this.serialNumber = event ? 3 : 0;
    }

    isWidthWithinLimit() {
        if (this.width === 80) {
            return false;
        } else {
            return true;
        }
    }

    upload(files: File[]) {
        this.uploadAndProgress(files);
    }

    uploadAndProgress(files: File[]) {
        const voucher = this.form.get('voucherNo').value;
        const formData = new FormData();
        Array.from(files).forEach((f) => formData.append('file', f));

        this.http
            .post(
                `http://139.5.73.60:8080/api/services/app/PurchaseMasters/CreateOrUpdateFile?voucherNo=${voucher}`,
                formData,
                {
                    reportProgress: true,
                    observe: 'events',
                }
            )
            .subscribe((event) => {
                if (event.type === HttpEventType.UploadProgress) {
                    this.percentDone = Math.round((100 * event.loaded) / event.total);
                } else if (event instanceof HttpResponse) {
                }
            });
    }

    async checkSave() {
        this.saving = true;
        if (this.isManual) {
            const voucherN = this.form.get('voucherNo').value;
            await lastValueFrom(this._purchaseMasterServiceProxy.getCheckVoucherNo(voucherN)).then((value) => {
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

    save() {
        if (this.form.valid) {
            if (this.id) {
                this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                    if (isConfirmed) {
                        // this.apiCall();
                        this._purchaseMasterServiceProxy.isPaymentMade(this.form.value.voucherNo).subscribe((data) => {
                            if (data) {
                                this.message.confirm(
                                    '',
                                    this.l(
                                        'You have already made some payments for this voucher number. Once you agree, your reference type will be changed to OnAccount. Are you sure you want to Update?'
                                    ),
                                    (isConfirmed) => {
                                        if (isConfirmed) {
                                            this.apiCall();
                                        } else {
                                            this.saving = false;
                                            return;
                                        }
                                    }
                                );
                            } else {
                                this.apiCall();
                            }
                        });
                    } else {
                        this.saving = false;
                    }
                });
            } else {
                this.apiCall();
            }
        } else {
            this.notify.error('Form is invalid !!');
            this.saving = false;
        }
    }

    apiCall() {
        this.saving = true;
        this._purchaseMasterServiceProxy
            .createOrEdit(this.form.getRawValue())
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe((data) => {
                if (this.isPrint) {
                    if (this.id) {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/purchase/pdf/3/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/purchase/pdf/3/${data}`]);
                    }
                } else {
                    if (this.id) {
                        this.notify.info(this.l('Updated Successfully'));
                        this._location.back();
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this._location.back();
                    }
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

        } else {
            return;
        }
    }

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._purchaseMasterServiceProxy.isPaymentMade(this.form.value.voucherNo).subscribe((data) => {
                    if (data) {
                        this.message.confirm(
                            '',
                            this.l(
                                'You have already made some payments for this voucher number. Once you agree, your reference type will be changed to OnAccount. Are you sure you want to delete?'
                            ),
                            (isConfirmed) => {
                                if (isConfirmed) {
                                    this.delete(id);
                                } else {
                                    return;
                                }
                            }
                        );
                    } else {
                        this.delete(id);
                    }
                });
            }
        });
    }

    delete(id) {
        this._purchaseMasterServiceProxy.delete(id).subscribe(() => {
            this._location.back();
            this.notify.success(this.l('Deleted SuccessFully'));
        });
    }

    // Key Event Methods
    keyEventBranchs(event) {
        if (event.which === 13) {
            event.preventDefault();
            // this.goToDate();
            // this.goToMode();
        }
    }

    // @HostListener("document:keydown", ["$event"]) onKeydownHandler(event) {
    //     if (event.escKey || event.keyCode === keyCodeEnum.ESCAPE_KEYCODE) {
    //         event.preventDefault();
    //         this.close();
    //     }
    // }

    changeVoucher() {
        if (!this.id) {
            const vouchergenerate$ = this._purchaseMasterServiceProxy.getVoucherGenerateType();
            vouchergenerate$.pipe(first()).subscribe((x) => {
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


    gotoDateMiti() {
        document.getElementById('npDatePicker').focus();
    }

    gotoInvoiceDate(e) {
        if (e.which === 13) {
            e.preventDefault();
            //goto invoice date
        }
    }

    gotoCashParty(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.open();
        }
    }

    gotoVendorInvoice(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.close();
            this.vendorNoEvent.nativeElement.focus();
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                e.preventDefault();
                this.changeAccLedger(e);
            }
        }
    }

    gotoCreditPeriod(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.creditPeriodEvent.nativeElement.focus();
        }
    }

    gotoPurchaseType(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.purchaseTypeEvent.open();
        }
    }

    gotoPurchaseACC(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.purchaseTypeEvent.close();
            this.purchaseEvent.open();
        }
    }

    gotoPurchaseAgainst(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.purchaseEvent.close();
        }
    }


    consitionForMode() {
        const idHo = this.form.get('againstId').value;
        if (idHo > 0) {
            this.orderNoEvent.open();
        } else {
            this.productEvent.first.open();
        }
    }

    gotoProduct(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.orderNoEvent.close();
            this.productEvent.first.open();
        }
    }

    gotoProductfromcustom(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.productEvent.first.open();
        }
    }

    fromProduct(e, i, abcd) {
        if (e.which === 13) {
            e.preventDefault();
            const id = abcd.get('productId').value;
            this._purchaseMasterServiceProxy.getProductForView(id).subscribe((data) => {
            });
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                this.formDetailRowId = i;
                e.preventDefault();
                this.changeProduct(e);
            }
        }
    }

    gotoFromQTY(e, i) {
        if (e.which === 13) {
            e.preventDefault();

        }
    }


    goFromRack(e, i) {
        if (e.which === 13) {
            if (i === 0) {
                this.rateEvent.first.nativeElement.focus();
            } else {
                this.rateEvent.last.nativeElement.focus();
            }
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                this.router.navigate(['app/main/inventory/racks/add']);
            }
        }
    }

    goFromRate(e, i) {

    }

    fromDisPer(e, i) {

    }

    fromDisAmt(e, i) {

    }

    fromTaxes(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (i === 0) {
                this.taxessEvent.first.close();
                this.totalEvent.first.nativeElement.focus();
            } else {
                this.taxessEvent.last.close();
                this.totalEvent.first.nativeElement.focus();
            }
        }
    }

    fromTotalAmt(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.keyEventsDetail();
            timer(100)
                .pipe(first())
                .subscribe(() => {
                    this.productEvent.last.open();
                });
        } else if (e.keyCode === 'Alt') {
            e.preventDefault();
            if (this.form.get('purchaseDetail').valid) {
                this.transportcompEvent.nativeElement.focus();
            }
        }
    }

    gotoLrNo(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.lrnoEvent.nativeElement.focus();
        }
    }

    gotdesc(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.descripEvent.nativeElement.focus();
        }
    }

    fromDesc(e) {
        if (e.which === 13) {
            e.preventDefault();
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

    scrollToTop(el: HTMLElement) {
        // window.scrollTo({top:800, behavior: 'smooth'});
        el.scrollIntoView({ behavior: 'smooth' });
    }
}
