import { UniversalDropdownDto } from './../../../../../shared/service-proxies/service-proxies';
import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    Injector,
    OnInit,
    QueryList,
    TemplateRef,
    ViewChild,
    ViewChildren,
    OnDestroy,
} from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    DocumentDetailsDto,
    FileParameter,
    SalesDetailDto,
    SalesMasterAccountLedgerTableDto,
    SalesMastersServiceProxy,
    SalesProductTableDto,
    TaxesDetailDto,
    TenantSettingsEditDto,
} from '@shared/service-proxies/service-proxies';

import { BsModalRef, BsModalService, ModalDirective } from 'ngx-bootstrap/modal';
import { finalize, first, takeUntil } from 'rxjs/operators';
import { NgSelectComponent } from '@ng-select/ng-select';
import { ShortcutInput, AllowIn } from 'ng-keyboard-shortcuts';
import { timer, Subject } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    standalone: false,
    selector: 'app-add-sales-master',
    templateUrl: './add-sales-master.component.html',
    styleUrls: ['./add-sales-master.component.css'],
    animations: [appModuleAnimation],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddSalesMasterComponent extends AppComponentBase implements OnInit, OnDestroy {
    @ViewChildren('qty', { read: ElementRef }) qtyEvent: QueryList<ElementRef>;
    @ViewChildren('disper', { read: ElementRef }) disperEvent: QueryList<ElementRef>;
    @ViewChildren('disAmt', { read: ElementRef }) disAmtEvent: QueryList<ElementRef>;
    @ViewChildren('addBtn', { read: ElementRef }) btnEvent: QueryList<ElementRef>;
    @ViewChildren('rate', { read: ElementRef }) rateEvent: QueryList<ElementRef>;
    @ViewChildren('total', { read: ElementRef }) totalEvent: QueryList<ElementRef>;
    @ViewChildren('productKey') productEvent: QueryList<NgSelectComponent>;
    @ViewChildren('unit') unitEvent: QueryList<NgSelectComponent>;
    @ViewChildren('tax') taxEvent: QueryList<NgSelectComponent>;
    @ViewChild('idToMyButton') button;
    @ViewChild('voucherNo') voucherNoEvent: NgSelectComponent;
    @ViewChild('newSelect') newSelectEvent: NgSelectComponent;
    @ViewChild('pmethode') pmethodeEvent: NgSelectComponent;
    @ViewChild('mode') modeEvent: NgSelectComponent;
    @ViewChild('bank') bankEvent: NgSelectComponent;
    @ViewChild('sinvoice') sinvoiceEvent: NgSelectComponent;
    @ViewChild('party', { read: ElementRef }) partyEvent: ElementRef;
    @ViewChild('nums') numsEvent: NgSelectComponent;
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('tname') tnameEvent: NgSelectComponent;
    @ViewChild('employee') employeeEvent: NgSelectComponent;
    @ViewChild('sales') salesEvent: NgSelectComponent;
    @ViewChild('payment') paymentEvent: NgSelectComponent;
    @ViewChild('search') searchEvent: NgSelectComponent;
    @ViewChild('remarks', { read: ElementRef }) remarksEvent: ElementRef;
    @ViewChild('trnscompany', { read: ElementRef }) trnscompanyEvent: ElementRef;
    @ViewChild('freights') freightEvent: NgSelectComponent;
    @ViewChild('credit', { read: ElementRef }) creditEvent: ElementRef;
    @ViewChild('lrno', { read: ElementRef }) lrnoEvent: ElementRef;
    @ViewChild('framt', { read: ElementRef }) framtEvent: ElementRef;
    @ViewChild('desc', { read: ElementRef }) descEvent: ElementRef;
    @ViewChild('#confirmationDialog') public lgModal: ModalDirective;
    modalRef?: BsModalRef;
    expand: boolean;
    isManual = false;
    automaticInput = false;
    toPrint = false;
    imageFile: FileParameter;
    documents: DocumentDetailsDto[];
    imageId: string;
    files: File;
    imageDocs: string | ArrayBuffer;
    pdfDocs: string | ArrayBuffer;
    imageType: string;
    imageView = true;
    pdfView = true;
    private destroy$ = new Subject<void>();
    shortcuts: ShortcutInput[] = [];
    form: FormGroup;
    againstMode: FormGroup;
    imeiForm: FormGroup;
    productStockQty = 0;
    productNameSelected = '';
    id: string;
    type: string;
    ledgerId: string;
    productId: string;
    arrayNumber: number;
    serialNumber = 0;
    isFullPage = true;
    private hiddenShellElements: Array<{ element: HTMLElement; display: string }> = [];
    private expandedShellElements: Array<{
        element: HTMLElement;
        width: string;
        widthPriority: string;
        marginLeft: string;
        marginLeftPriority: string;
        paddingLeft: string;
        paddingLeftPriority: string;
    }> = [];
    voucherName: string;
    validQuantity: boolean;
    maxQty: number;
    ledger: string;
    allAccountLedger: SalesMasterAccountLedgerTableDto[];
    allLedgers: SalesMasterAccountLedgerTableDto[];
    loadingLedger = true;
    salesAccount: SalesMasterAccountLedgerTableDto[];
    allProducts: SalesProductTableDto[];
    formLoader = true;
    allTaxForCalculation: TaxesDetailDto[];
    saving: boolean;
    settings: TenantSettingsEditDto = undefined;
    units = false;
    discountPercent = false;
    discountAmount = false;
    taxes = false;
    showSalesAdditional = false;
    irdActive = abp.setting.get('App.Suktas.IsCBMS');

    paymentType = [
        { id: 2, displayName: 'Credit' },
        { id: 0, displayName: 'Cash' },
        { id: 1, displayName: 'Cheque' },
        { id: 3, displayName: 'Card Swipe' },
        { id: 5, displayName: 'QR' },
        { id: 4, displayName: 'LC' },
    ];
    salesType = [
        { id: 0, displayName: 'Sales' },
        { id: 1, displayName: 'TI' },
        { id: 2, displayName: 'ABT' },
    ];
    salesModeType = [
        { id: 0, displayName: 'N/A' },
        { id: 1, displayName: 'Sales Order' },
    ];
    addNewLedger = [
        {
            displayName: 'Add New',
            id: 1,
        },
    ];
    formDetailRowId: number | null = null;
    showBankAcc = false;
    typeofSearch: FormControl;
    search: FormControl = new FormControl(0);
    searchBy: boolean;
    allSearches: any = [
        { id: 0, displayName: 'N/A' },
        { id: 1, displayName: 'Serial Number' },
        { id: 2, displayName: 'Bar Code' },
    ];
    showAddLedger = false;
    allTax: TaxesDetailDto[];
    title = 'Add Sales Invoice';
    allSalesAdditional: SalesMasterAccountLedgerTableDto[];
    allBanks: UniversalDropdownDto[];
    allUnits: UniversalDropdownDto[];

    constructor(
        private fb: FormBuilder,
        injector: Injector,
        private _location: Location,
        private route: ActivatedRoute,
        private router: Router,
        private _proxy: SalesMastersServiceProxy,
        private modalService: BsModalService
    ) {
        super(injector);
        this.getSetting();
        this.typeofSearch = new FormControl(0);
    }

    get salesDetails() {
        return this.form.get('salesDetails') as FormArray;
    }

    get qtyValidation() {
        if (this.qtyValidationType === 'Block') {
            return this.validQuantity;
        } else {
            return false;
        }
    }

    ngOnInit(): void {
        this.enterFullPage();
        this.id = this.route.snapshot.params['id'];
        this.type = this.route.snapshot.params['type'];
        this.createForm();
        this.createAgainst();
        this.initShortcuts(); // Initialize keyboard shortcuts
        this.getAllProducts();
        this.accountLedger();
        this.getAllTax();
        this.getAllUnit();
        if (!this.id) {
            setTimeout(() => {
                this.changeVoucher();
                this.getByBranch();
                this.ledgerEvent.focus();
            }, 300);
        }
        if (this.id && this.type) {
            this.getAllSalesAccount();
            this.title = 'From Sales Order';
        }

        if (this.id && !this.type) {
            this.title = 'Edit Sales Invoice';
            this._proxy.getSalesMasterForEdit(this.id)
                .pipe(takeUntil(this.destroy$))
                .subscribe((result) => {
                    // Patch main form values
                    this.form.patchValue(result);

                    // Handle nested arrays correctly
                    if (result.salesDetails && result.salesDetails.length > 0) {
                        // Clear the existing form array
                        const salesDetailsFormArray = this.form.get('salesDetails') as FormArray;
                        while (salesDetailsFormArray.length) {
                            salesDetailsFormArray.removeAt(0);
                        }

                        // Add each sales detail with its nested unit list and imei list
                        result.salesDetails.forEach(detail => {
                            const detailGroup = this.createDetails(detail);
                            salesDetailsFormArray.push(detailGroup);
                        });
                    }

                    this.form.get('voucherNo').setValue(result.voucherNo);
                });
        }
        this.getAllBanks();
        this.typeofSearch.setValue(0);
    }

    exitFullPage(): void {
        this.restoreFullPageShell();
        this.isFullPage = false;
    }

    ngOnDestroy(): void {
        this.restoreFullPageShell();
        this.destroy$.next();
        this.destroy$.complete();
    }

    private enterFullPage(): void {
        const header = document.querySelector('#kt_app_header, #kt_header, .theme2-header') as HTMLElement | null;
        const sidebars = Array.from(
            document.querySelectorAll('#kt_app_sidebar, #kt_app_sidebar_menu_wrapper, #kt_app_sidebar_menu, #kt_aside, .kt-aside, .aside-left, .theme2-sidebar')
        ) as HTMLElement[];

        for (const element of [...sidebars, header].filter((item): item is HTMLElement => !!item)) {
            if (!this.hiddenShellElements.some((item) => item.element === element)) {
                this.hiddenShellElements.push({ element, display: element.style.display });
                element.style.display = 'none';
            }
        }

        const wrappers = Array.from(document.querySelectorAll('#kt_body, #kt_app_wrapper, #kt_wrapper')) as HTMLElement[];
        for (const wrapper of wrappers) {
            if (this.expandedShellElements.some((item) => item.element === wrapper)) continue;
            this.expandedShellElements.push({
                element: wrapper,
                width: wrapper.style.width,
                widthPriority: wrapper.style.getPropertyPriority('width'),
                marginLeft: wrapper.style.marginLeft,
                marginLeftPriority: wrapper.style.getPropertyPriority('margin-left'),
                paddingLeft: wrapper.style.paddingLeft,
                paddingLeftPriority: wrapper.style.getPropertyPriority('padding-left'),
            });
            wrapper.style.setProperty('width', '100%', 'important');
            wrapper.style.setProperty('margin-left', '0', 'important');
            wrapper.style.setProperty('padding-left', '0', 'important');
        }
    }

    private restoreFullPageShell(): void {
        this.hiddenShellElements.forEach(({ element, display }) => { element.style.display = display; });
        this.expandedShellElements.forEach(({ element, width, widthPriority, marginLeft, marginLeftPriority, paddingLeft, paddingLeftPriority }) => {
            element.style.setProperty('width', width, widthPriority);
            element.style.setProperty('margin-left', marginLeft, marginLeftPriority);
            element.style.setProperty('padding-left', paddingLeft, paddingLeftPriority);
        });
        this.hiddenShellElements = [];
        this.expandedShellElements = [];
    }

    initShortcuts(): void {
        this.shortcuts = [
            {
                key: ['alt + s'],
                label: 'Save',
                description: 'Save the sales invoice',
                allowIn: [AllowIn.Textarea, AllowIn.Input],
                command: (e) => {
                    e.event.preventDefault();
                    if (this.form.valid && !this.saving) {
                        this.save();
                    } else {
                        this.notify.warn(this.l('Please correct the form errors before saving'));
                    }
                    return false;
                }
            },
            {
                key: ['alt + n'],
                label: 'Add Row',
                description: 'Add a new product row',
                allowIn: [AllowIn.Textarea, AllowIn.Input],
                command: (e) => {
                    e.event.preventDefault();
                    this.keyEventsDetail();
                    return false;
                }
            },
            {
                key: ['esc'],
                label: 'Cancel',
                description: 'Cancel and go back',
                allowIn: [AllowIn.Textarea, AllowIn.Input],
                command: (e) => {
                    e.event.preventDefault();
                    this.close();
                    return false;
                }
            },
            {
                key: ['alt + p'],
                label: 'Product Search',
                description: 'Focus on product search',
                allowIn: [AllowIn.Textarea, AllowIn.Input],
                command: (e) => {
                    e.event.preventDefault();
                    if (this.productEvent && this.productEvent.last) {
                        this.productEvent.last.focus();
                    }
                    return false;
                }
            },
            {
                key: ['alt + l'],
                label: 'Ledger Selection',
                description: 'Focus on ledger search',
                allowIn: [AllowIn.Textarea, AllowIn.Input],
                command: (e) => {
                    e.event.preventDefault();
                    if (this.ledgerEvent) {
                        this.ledgerEvent.focus();
                    }
                    return false;
                }
            },
            {
                key: ['alt + f12'],
                label: 'Calculate Totals',
                description: 'Recalculate all totals',
                allowIn: [AllowIn.Textarea, AllowIn.Input],
                command: (e) => {
                    e.event.preventDefault();
                    this.totalPriceCalculation();
                    return false;
                }
            }
        ];
    }

    keyEventsDetail(): void {
        if (this.form.get('salesDetails').valid) {
            this.addDetailForm();
        }
    }

    addReceiptDetails(event) {
        if (event.which === 16) {
            event.preventDefault();
            if (this.form.get('salesDetails').valid) {
                this.tnameEvent.open();
            }
        } else {
            if (event.keyCode === 13) {
                event.preventDefault();
                this.keyEventsDetail();
                timer(200)
                    .pipe(first())
                    .subscribe(() => this.productEvent.last.open());
            }
        }
    }

    getAllVoucher() {
        this._proxy.getSalesMasterVoucherNo(0).subscribe((data) => {
            this.form.get('voucherNo').setValue(data);
        });
    }

    // getAllShowAdditionalSales() {
    //     this._proxy.getAllSalesAdditionalDropdown().subscribe((res) => {
    //         this.allSalesAdditional = res;
    //     });
    // }

    getByBranch() {
        if (!this.id) {
            this.getAllVoucher();
        }
        this.getAllSalesAccount();
    }

    getAllTax() {
        this._proxy.getAllTaxes().subscribe((result) => {
            this.allTax = result;
            this.allTaxForCalculation = result;
        });
    }

    getAllUnit() {
        this._proxy.getAllUnits().subscribe((result) => {
            this.allUnits = result;
        });
    }

    createForm(item: any = {}) {
        const defaultItem = {
            salesAccountId: null,
            dateMiti: this.today,
            creditPeriod: 0,
            taxAmount: 0,
            billDiscount: 0,
            grandTotal: 0,
            netAmount: item.grandTotal ? item.grandTotal - item.billDiscount : 0,
            totalAmount: 0,
            taxableAmount: 0,
            isPrint: this.isPrint,
            subTotalAmount: item.totalAmount ? item.totalAmount - item.billDiscount : 0,
            paymentMethod: 0,
            paymentMethodLedgerId: null,
            vatRefundAmount: 0,
            transportationCompany: '',
            freightTerm: 0,
            piNumber: '',
            ledgerId: null,
            againstId: [],
            againstVoucherNo: '',
            customerName: '',
            customerVatNo: '',
            customerPhoneNo: '',
            customerAddress: '',
            salesModeType: 0,
            invoiceType: 0,
            salesAdditionalId: null,
            salesDetails: [],
            id: null,
            description: '',
            lrNo: '',
            vehicleNo: '',
            ...item,
        };

        this.form = this.fb.group({
            id: [defaultItem.id],
            voucherNo: [defaultItem.voucherNo],
            salesAccountId: [defaultItem.salesAccountId, Validators.required],
            dateMiti: [defaultItem.dateMiti, Validators.required],
            creditPeriod: [defaultItem.creditPeriod],
            description: [defaultItem.description],
            taxAmount: [defaultItem.taxAmount],
            billDiscount: [defaultItem.billDiscount],
            grandTotal: [defaultItem.grandTotal],
            netAmount: [defaultItem.netAmount],
            customerName: [defaultItem.customerName],
            customerVatNo: [defaultItem.customerVatNo],
            customerPhoneNo: [defaultItem.customerPhoneNo],
            customerAddress: [defaultItem.customerAddress],
            totalAmount: [defaultItem.totalAmount],
            taxableAmount: [defaultItem.taxableAmount],
            isPrint: [defaultItem.isPrint],
            subTotalAmount: [defaultItem.subTotalAmount],
            paymentMethod: [defaultItem.paymentMethod, Validators.required],
            paymentMethodLedgerId: [defaultItem.paymentMethodLedgerId],
            vatRefundAmount: [defaultItem.vatRefundAmount],
            lrNo: [defaultItem.lrNo],
            piNumber: [defaultItem.piNumber],
            vehicleNo: [defaultItem.vehicleNo],
            ledgerId: [{ value: defaultItem.ledgerId, disabled: defaultItem.salesModeType }, Validators.required],
            againstId: [defaultItem.againstId],
            againstVoucherNo: [defaultItem.againstVoucherNo],
            salesModeType: [{ value: defaultItem.salesModeType, disabled: defaultItem.id }, Validators.required],
            invoiceType: [defaultItem.invoiceType],
            salesDetails: this.fb.array(
                defaultItem.salesDetails.length
                    ? defaultItem.salesDetails.map((detail: any) => this.createDetails(detail))
                    : [this.createDetails()]
            ),
        });
    }

    //   remove detail form
    removePurchaseDetailForm(i, form) {
        const control = form.controls.salesDetails;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalPriceCalculation();
    }

    createAgainst() {
        this.againstMode = this.fb.group({
            modeId: [0, Validators.required],
        });
        if (!this.id) {
            this.form.get('againstId').disable();
        }
    }

    againstModeChange(event) {
        (this.form.get('salesDetails') as FormArray).clear();
        this.addDetailForm();
        this.form.get('againstId').setValue(null);
        if (event === 0) {
            this.form.get('totalAmount').setValue(0);
            this.form.get('againstId').disable();
        } else {
            this.form.get('againstId').enable();
        }
    }

    getAllBanks() {
        this._proxy.getAllBankAccount().subscribe((res) => {
            this.allBanks = res;
        });
    }

    roundToTwo(num) {
        return Math.round((num + Number.EPSILON) * 100) / 100;
    }

    roundToThree(num) {
        return Math.round((num + Number.EPSILON) * 1000) / 1000;
    }

    roundToFour(num) {
        return Math.round((num + Number.EPSILON) * 10000) / 10000;
    }

    calculateTotalByInput(abcd: FormGroup, inputname: string) {
        // Destructure values once for better performance
        const { value: qtyValue = 0 } = abcd.get('qty');
        const { value: rateValue = 0 } = abcd.get('rate');
        const { value: discountPerValue = 0 } = abcd.get('discountPer');
        const { value: taxIdValue } = abcd.get('taxId');
        const { value: discountValue = 0 } = abcd.get('discount');

        // Memoize calculated values
        const amount = inputname === 'totalamt' ? (abcd.get('amount').value || 0) : qtyValue * rateValue;

        // Validate inputs to avoid unnecessary calculations
        if (discountPerValue >= 100 || discountPerValue < 0) {
            if (discountPerValue >= 100) {
                this.notify.warn(this.l('Discount Percentage must be less than 100 & greater than Zero'));
            }
            abcd.patchValue({ discountPer: 0, discount: 0 }, { emitEvent: false });
            return;
        }

        // Early validation for specific input types
        switch (inputname) {
            case 'rate':
                if (rateValue <= 0) {
                    abcd.get('rate').markAsDirty();
                    abcd.patchValue({
                        grossAmount: 0, discount: 0, netAmount: 0,
                        taxAmount: 0, amount: 0
                    }, { emitEvent: false });
                    return;
                }
                break;
            case 'totalamt':
            case 'taxId':
                if (!taxIdValue || qtyValue <= 0) {
                    if (qtyValue <= 0) {
                        abcd.get('qty').markAsDirty();
                        this.notify.warn(this.l('Please enter quantity'));
                    }
                    return;
                }
                break;
            case 'discountPer':
            case 'discount':
                if (qtyValue <= 0) {
                    abcd.get('qty').markAsDirty();
                    this.notify.warn(this.l('Please enter quantity'));
                    return;
                }
                if (amount <= 0) {return;}

                if (inputname === 'discount' && discountValue > amount) {
                    abcd.get('discount').markAsDirty();
                    this.notify.warn(this.l('Discount Amount is greater than Gross Amount'));
                    abcd.patchValue({ discount: 0, discountPer: 0 }, { emitEvent: false });
                    return;
                }
                break;
        }

        // Calculate tax rate once
        const taxItem = this.allTax.find(tax => tax.id === taxIdValue);
        const taxRate = taxItem ? taxItem.rate / 100 : 0;

        // Calculate values based on input type
        let updateValues: { [key: string]: number } = {};

        if (inputname === 'totalamt') {
            // Work backwards from total amount
            const netValues = 1 + taxRate;
            const netX = amount / netValues;
            const taxAmount = netX * taxRate;
            const grossValueY = 1 - discountPerValue / 100;
            const grossX = netX / grossValueY;
            const discountAmt = grossX * (discountPerValue / 100);
            const newRate = grossX / qtyValue;

            updateValues = {
                netAmount: this.roundToTwo(netX),
                taxAmount: this.roundToTwo(taxAmount),
                grossAmount: this.roundToTwo(grossX),
                discount: this.roundToTwo(discountAmt),
                rate: this.roundToThree(newRate)
            };
        } else if (inputname === 'discount') {
            // Calculate from direct discount amount
            const dp = (discountValue / amount) * 100;
            const netX = amount - discountValue;
            const taxAmount = netX * taxRate;

            updateValues = {
                grossAmount: this.roundToTwo(amount),
                discountPer: this.roundToFour(dp),
                netAmount: this.roundToTwo(netX),
                taxAmount: this.roundToTwo(taxAmount),
                amount: this.roundToTwo(netX + taxAmount)
            };
        } else {
            // Standard calculation
            const discountAmt = (amount * discountPerValue) / 100;
            const netX = amount - discountAmt;
            const taxAmount = netX * taxRate;

            updateValues = {
                grossAmount: this.roundToTwo(amount),
                discount: this.roundToTwo(discountAmt),
                netAmount: this.roundToTwo(netX),
                taxAmount: this.roundToTwo(taxAmount),
                amount: this.roundToTwo(netX + taxAmount)
            };
        }

        // Update form with all values at once for better performance
        abcd.patchValue(updateValues, { emitEvent: false });

        // Only recalculate totals once
        this.totalPriceCalculation();
    }

    totalPriceCalculation() {
        if (!this.salesDetails.length) {return;}

        // Use reduce for better performance instead of loop
        const totals = this.salesDetails.controls.reduce((acc, control) => {
            // Get all values in one go to avoid repeated property access
            const taxAmount = control.get('taxAmount').value || 0;
            const netAmount = control.get('netAmount').value || 0;
            const grossAmount = control.get('grossAmount').value || 0;
            const amount = control.get('amount').value || 0;
            const discount = control.get('discount').value || 0;

            // Only add to taxableAmount if taxAmount > 0
            acc.taxableAmount += taxAmount > 0 ? netAmount : 0;
            acc.taxAmount += taxAmount;
            acc.grossAmount += grossAmount;
            acc.netAmount += netAmount;
            acc.totalAmount += amount;
            acc.discountAmount += discount;

            return acc;
        }, {
            taxableAmount: 0,
            taxAmount: 0,
            grossAmount: 0,
            netAmount: 0,
            totalAmount: 0,
            discountAmount: 0
        });
        this.updateFormTotals(
            totals.taxableAmount,
            totals.netAmount,
            totals.taxAmount,
            totals.grossAmount,
            totals.discountAmount,
            totals.totalAmount
        );
    }

    private updateFormTotals(taxableAmt: number, netAmt: number, taxAmt: number,
        grossAmt: number, discountAmt: number, grandTotal?: number) {
        this.form.patchValue({
            taxAmount: this.roundToTwo(taxAmt),
            totalAmount: this.roundToTwo(grossAmt),
            subTotalAmount: this.roundToTwo(netAmt),
            netAmount: this.roundToTwo(netAmt),
            taxableAmount: this.roundToTwo(taxableAmt),
            billDiscount: this.roundToTwo(discountAmt),
            grandTotal: this.roundToTwo(grandTotal || netAmt + taxAmt)
        }, { emitEvent: false });
    }

    setOrder() {
    }



    fillForm(result: SalesDetailDto[]) {
        (this.form.get('salesDetails') as FormArray).clear();
        const agMode = this.form.get('salesModeType').value;
        result.forEach((items, index) => {
            this.addDetailForm(items);
            if (this.salesDetails.controls[index].get('productType').value !== 2) {
                setTimeout(() => {
                    if (this.qtyValidationType === 'Block' || this.qtyValidationType === 'Warn') {
                        if (items.stockQty < items.qty) {
                            if (this.qtyValidationType === 'Block') {
                                this.validQuantity = true;
                                this.salesDetails.controls[index].get('qty').setErrors({ incorrect: true });
                                this.salesDetails.controls[index].get('qty').markAsTouched();
                                this.notify.error(this.l('Quantity Exceeded The Stock Quantity '));
                            } else {
                                if (items.qty < 1) {
                                    this.notify.error(this.l('Invalid Quantity Entry'));
                                } else {
                                    this.notify.error(this.l('Quantity Exceeded The Stock Quantity'));
                                }
                            }
                        } else {
                            if (this.qtyValidationType === 'Block') {
                                this.validQuantity = false;
                            }
                        }
                    }
                }, 500);
            }
            this.totalPriceCalculation();
        });
    }

    addDetailForm(item: any = {}) {
        const control = <FormArray>this.form.controls.salesDetails;
        control.push(this.createDetails(item));
    }

    addMoreFrom(item: any = {}) {
        const control = <FormArray>this.form.controls.salesDetails;
        for (let index = 0; index < 8; index++) {
            control.push(this.createDetails(item));

        }

    }

    getDetail(form) {
        return form.controls.salesDetails.controls;
    }

    setTax(id: string, rate: number, abcd: FormGroup): void {
        abcd.get('taxId').setValue(id);
        abcd.get('taxValue').setValue(rate / 100);
    }

    getAllSalesAccount() {
        this._proxy.getAllSalesAccountForTableDropdown().subscribe((res) => {
            this.salesAccount = res;
            this.form.controls.salesAccountId.setValue(res[0].id);
        });
    }

    accountLedger() {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allLedgers = data;
            this.allAccountLedger = data;
            if (!this.id) {
                this.form.get('ledgerId').setValue(data[0].id);
            }
            this.showAddLedger = true;
        });

        this.formLoader = false;
    }

    getAllAcountLedger(ledgerIds?) {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allAccountLedger = data;
            this.showAddLedger = true;
        });
        if (this.serialNumber) {
            this.form.get('ledgerId').setValue(ledgerIds);

            this.form.get('paymentMethod').setValue(2);
            this.serialNumber = 0;
            setTimeout(() => {
                const element = this.allAccountLedger.find(e => e.id == ledgerIds);

                this.form.get('creditPeriod').setValue(element.creditPeriod);
                this.form.get('customerVatNo').setValue(element.panNo);
                this.form.get('customerAddress').setValue(element.address);
                this.form.get('customerName').setValue(element.displayName);
                this.form.get('customerPhoneNo').setValue(element.mobileNo);
                this.ledgerEvent.focus();
            }, 1000);
        } else {
            if (!this.id) {
                this.form.get('ledgerId').setValue(this.allAccountLedger[0].id);
            }
        }
    }

    getAllProducts(id?: string | null) {
        this._proxy.getAllProduct().subscribe((res) => {
            this.allProducts = res;
        });

        if (!this.serialNumber) {
            return;
        }

        const rowIndex = this.formDetailRowId;
        this.serialNumber = 0;
        this.formDetailRowId = null;

        if (!id || rowIndex === null || !this.salesDetails.controls[rowIndex]) {
            return;
        }

        const salesDetail = this.salesDetails.controls[rowIndex] as FormGroup;
        salesDetail.get('productId')?.setValue(id);
        this.onGetProduct(id, salesDetail, rowIndex);
    }


    openLedgerDialog(dialog: TemplateRef<any>): void {
        this.modalRef = this.modalService.show(dialog);
    }

    onGetProduct(id: string, abcd: FormGroup, i: number) {
        if (!id) {return;}
        this._proxy.getProductById(id).subscribe((result) => {

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
            // auto select default product unit
            abcd.patchValue({
                unitId: result.unitId,
                qty: 1,
                discountPer: 0,
                discount: 0,
                rate: result.rate,
                taxId: result.taxId,
                taxValue: result.taxRate / 100,
                grossAmount: 1 * result.rate,
                netAmount: 1 * result.rate,
                totalAmount: 1 * result.rate + result.taxRate / 100,
                productType: result.productType
            }, { emitEvent: false });
            setTimeout(() => {
                this.calculateTotalByInput(abcd, 'rate');
            }, 0);
            abcd.get('qty').enable();
            this.productStockQty = result.quantity;
            this.calculateTotalByInput(abcd, 'rate');
        });
    }

    changeInQty(abcd: FormGroup) {
        if (abcd.get('productType').value !== 2) {
            setTimeout(() => {
                const quantity = abcd.get('qty').value;
                if (this.qtyValidationType === 'Block' || this.qtyValidationType === 'Warn') {
                    if (quantity < 1) {
                        this.notify.error(this.l('Invalid Quantity Entry'));
                    } else if (quantity > this.maxQty) {

                        if (this.qtyValidationType === 'Block') {
                            this.validQuantity = true;
                            abcd.get('qty').setErrors({ incorrect: true });
                            abcd.get('qty').markAsTouched();
                            this.notify.error(this.l('Quantity Exceeded The Stock Quantity...'));
                        } else {
                            this.notify.error(this.l('Quantity Exceeded The Stock Quantity'));
                        }
                    }
                }
            }, 500);
        }

    }

    // unitConversion(formGroup: FormGroup) {
    //     const unitId = formGroup.get('unitId')?.value;
    //     const productId = formGroup.get('productId')?.value;
    //     const unitsList = formGroup.get('unitsList') as FormArray;

    //     // Return early if values are missing to avoid unnecessary processing
    //     if (!unitId || !productId || !unitsList?.length) return;

    //     // Find the selected unit efficiently
    //     const selectedUnitControl = unitsList.controls.find(
    //         control => control.get('unitId')?.value === unitId
    //     );

    //     if (!selectedUnitControl) return;

    //     // Get all needed values in one go
    //     const selectedUnit = selectedUnitControl.value;
    //     const { rate, qty = 0, unitName = '' } = selectedUnit || {};

    //     // Cache values for reuse
    //     this.maxQty = qty;
    //     this.productStockQty = qty;
    //     this.productNameSelected = `${qty} (${unitName})`;

    //     // Only update if not in edit mode
    //     if (!this.id) {
    //         formGroup.patchValue({ rate }, { emitEvent: false });
    //     }

    //     // Find product name for better display
    //     if (this.allProducts?.length) {
    //         const product = this.allProducts.find(p => p.productId === productId);
    //         if (product?.name) {
    //             this.productNameSelected = `${product.name} (${unitName})`;
    //         }
    //     }

    //     // Calculate totals only once
    //     this.calculateTotalByInput(formGroup, 'rate');
    // }

    // handleUnitChange(formGroup: FormGroup, event: any, index: number) {
    //     // Only do the conversion once
    //     this.unitConversion(formGroup);

    //     // Handle keyup only if it's a keyboard event
    //     if (event && event.type === 'keyup') {
    //         this.fromUnit(event, index);
    //     }
    // }

    calcqty(formGroup: FormGroup) {
        const qty = formGroup.get('imeiList').value.length;
        formGroup.get('qty').setValue(qty);
        formGroup.get('qty').disable();
        this.calculateTotalByInput(formGroup, 'imeiCalc');
    }

    // New method to encapsulate repeated logic
    updateCalculations(formGroup, inputType) {
        this.calculateTotalByInput(formGroup, inputType);
        this.changeInQty(formGroup);
    }

    //   3 product
    keyEventProduct(event: KeyboardEvent, i, abcd) {
        if (event.key === 'Enter') {
            event.preventDefault();
            this.lgModal.show();
            // this.unitConversion(abcd);
        } else if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                event.preventDefault();
                this.formDetailRowId = i;
                this.changeProduct(event);
            }
        } else {
            if (event.which === 13) {
                event.preventDefault();
                const id = abcd.get('productId').value;
                this._proxy.getProductById(id).subscribe((result) => {
                    // let ime = result.isAllowSerialNo;
                    // this.goFromProduct(ime, i);
                });
            }
        }
    }

    gotoQty(i) {
        if (i === 0) {
            this.productEvent.first.close();
            this.qtyEvent.first.nativeElement.focus();
        } else {
            this.productEvent.last.close();
            this.qtyEvent.last.nativeElement.focus();
        }
    }

    changeProduct(event: any) {
        this.serialNumber = event ? 3 : 0;
    }

    // keyEventAccountLedger(event) {
    //     if (event.altKey || event.metaKey) {
    //         if (event.which === 67) {
    //             event.preventDefault();
    //             this.changeAl(event);
    //         }
    //     }
    // }

    // checkAndSave() {
    //     this.saving = true;
    //     if (this.isManual) {
    //         const bId = this.form.get("branchId").value;
    //         const vNo = this.form.get("voucherNo").value;
    //         this._proxy.getCheckVoucherNo(bId, vNo).subscribe((value) => {
    //             if (!value) {
    //                 this.save();
    //             } else {
    //                 this.saving = false;
    //                 alert("Voucher Number Already Exists !!!");
    //             }
    //         });
    //     } else {
    //         this.save();
    //     }

    changeAl(event) {
        this.serialNumber = event ? 5 : 0;
    }

    paymentMethod(id: number) {
        this.showBankAcc = id === 3 || id === 5;
    }

    // }
    save() {
        if (this.serialNumber === 0) {
            if (this.form.valid) {
                // this.button.setAttribute("disabled", true);
                this.saving = true;
                setTimeout(() => {
                    if (this.id) {
                        this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                            if (isConfirmed) {
                                this.apiCall();
                            } else {
                                this.saving = false;
                            }
                        });
                    } else {
                        this.apiCall();
                    }
                }, 300);
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    apiCall() {
        this.saving = true;
        this.totalPriceCalculation();
        this._proxy
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
                        this.router.navigate([`app/main/sales/pdf/3/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/sales/pdf/3/${data}`]);
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
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.ngOnInit();
                    this._location.back();
                }
            });
        } else {
            this.ngOnInit();
            this._location.back();
        }
    }

    keyClose() {
        if (this.serialNumber) {
            this.serialNumber = 0;
        } else {
            return;
        }
    }

    clearForm(id: string) {
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

    byCash($event) {
        const {address} = $event;
        const {creditPeriod} = $event;
        // let pricingLevelId = $event.pricingLevelId;
        const {isCash} = $event;
        const vatNo = $event.panNo;
        const customerName = $event.displayName;
        const customerPhoneNo = $event.mobileNo;

        this.form.get('creditPeriod').setValue(creditPeriod);

        // this.form.get('pricingLevelId').setValue(pricingLevelId);
        this.form.get('customerVatNo').setValue(vatNo);
        this.form.get('customerAddress').setValue(address);
        this.form.get('customerName').setValue(customerName);
        this.form.get('customerPhoneNo').setValue(customerPhoneNo);
        if (!this.id) {
            (this.form.get('salesDetails') as FormArray).clear();
            this.addDetailForm();
            this.form.get('salesModeType').setValue(0);
            this.form.get('againstId').setValue(null);
            this.form.get('againstId').disable();
        }

        if (isCash) {
            this.form.get('paymentMethod').setValue(0);
            this.form.get('customerVatNo').enable();
            this.form.get('customerAddress').enable();
            this.form.get('customerName').enable();
            this.form.get('customerPhoneNo').enable();
        } else {
            this.form.get('paymentMethod').setValue(2);

            this.form.get('customerVatNo').enable();
            this.form.get('customerAddress').enable();
            this.form.get('customerName').disable();
            this.form.get('customerPhoneNo').enable();
        }
        const modeId = this.form.get('salesModeType').value;
    }

    goToDate(e) {
        if (e.which === 13) {
            e.preventDefault();
            document.getElementById('npDatePicker').focus();
        }
    }

    gotomode(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.modeEvent.open();
        }
    }

    customSearchFn(term: string, item: any) {
        term = term.toLocaleLowerCase();
        const searchItem = `${item.displayName}/${item.mobileNo || ''}/${item.panNo || ''}`;
        return searchItem.toLowerCase().indexOf(term) > -1 || searchItem.toLowerCase() === term;
    }

    customSearchMember(term: string, item: any) {
        term = term.toLocaleLowerCase();
        const searchItem = `${item.name}/${item.cardNo || ''}/${item.address || ''}/${item.contactNo || ''}`;
        return searchItem.toLowerCase().indexOf(term) > -1 || searchItem.toLowerCase() === term;
    }

    customSearchFnSn(term: string, item: any) {
        term = term.toLocaleLowerCase();
        const searchItem = `${item.imeiNumber}/${item.productName || ''}`;
        return searchItem.toLowerCase().indexOf(term) > -1 || searchItem.toLowerCase() === term;
    }

    goToPage(id) {
        this.serialNumber = id;
        this.showAddLedger = false;
    }

    changeBack(e) {
        if (e.which === 8) {
            this.showAddLedger = false;
            setTimeout(() => this.ledgerEvent.open(), 500);
            setTimeout(() => this.newSelectEvent.close(), 1000);
            // this.allAcc = this.addNewLedger;
            // this.ledgerEvent.clearAllText = '';
            // this.ledgerEvent.clearSearchOnAdd = true;
            // // this.ledgerEvent.handleKeyCode(e)
            // this.ledgerEvent.clearEvent.emit(null)
        }
    }

    // FOR FILEUPLOADER START
    onSingleImage(event) {
        const formData = new FormData();
        const salesInvoiceId = this.id;
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
                this._proxy.uploadImageNew(salesInvoiceId, this.imageFile).subscribe(() => {
                    this.getData();
                });
            } else {
                this.notify.info('Only jpeg, jpg, png, pdf files are allowed');
            }
        }
    }

    getData() {
        this._proxy.getAllDocuments(this.id).subscribe((result) => {
            this.documents = result;
            this.imageId = this.documents[0].id;
        });
    }

    getImages(id: string) {
        this._proxy.getImage(id).subscribe((result) => {
            this.imageType = result.fileType;
            if (this.imageType === '.pdf') {
                this.pdfView = true;
                this.imageView = false;
                this.pdfDocs = `data:application/pdf;base64,${  result.image}`;
            } else {
                this.pdfView = false;
                this.imageView = true;
                this.imageDocs = `data:image/png;base64,${  result.image}`;
            }
        });
    }

    deleteFile(id: string) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy.deleteFile(id).subscribe(() => {
                    this.getData();
                });
            }
        });
    }

    openExpand(event) {
        this.expand = event === true;
    }


    fromAgainstsMode(e) {
        const id = this.form.get('salesModeType').value;
        if (e.which === 13) {
            e.preventDefault();
            this.modeEvent.close();
            if (id > 0) {
                this.voucherNoEvent.open();
            } else {
                this.creditEvent.nativeElement.focus();
            }
        }
    }

    FromDateMiti(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.open();
        }
    }

    fromCashParty(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.close();
            this.partyEvent.nativeElement.focus();
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                e.preventDefault();
                this.changeAl(e);
            }
        }
    }

    fromPartyAddress(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.salesEvent.open();
        }
    }

    fromSalesAC(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.salesEvent.close();
        }
    }

    fromVoucherNO(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.voucherNoEvent.close();
            this.creditEvent.nativeElement.focus();
        }
    }

    fromCreditPeriod(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.pmethodeEvent.open();
        }
    }

    fromPmethode(e) {
        const id = this.form.get('paymentMethod').value;
        if (e.which === 13) {
            e.preventDefault();
            this.pmethodeEvent.close();
            if (id === 3) {
                this.bankEvent.open();
            } else {
                this.sinvoiceEvent.open();
            }
        }
    }

    fromBankAc(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.bankEvent.close();
            this.sinvoiceEvent.open();
        }
    }

    fromSinvoiceType(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.sinvoiceEvent.close();
            this.searchEvent.open();
        }
    }

    fromSearch(e) {
        const id = this.typeofSearch.value;
        if (e.which === 13) {
            e.preventDefault();
            this.searchEvent.close();
            if (id > 0) {
                this.numsEvent.open();
            } else {
                this.productEvent.first.open();
            }
        }
    }

    fromBarOrSerial(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.numsEvent.close();
            this.productEvent.first.open();
        }
    }


    fromQty(e, i) {
        e.preventDefault();
        if (e.which === 13) {
            if (this.units) {
                this.unitEvent.toArray()[i].open();
            } else if (!this.units) {
                if (i === 0) {
                }
            } else {
                this.rateEvent.toArray()[i].nativeElement.focus();
            }
        }
    }

    fromUnit(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            this.unitEvent.toArray()[i].close();
            this.rateEvent.toArray()[i].nativeElement.focus();

        }
    }


    fromRate(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.discountPercent) {
                this.disperEvent.toArray()[i].nativeElement.focus();
            } else if (this.discountAmount && !this.discountPercent) {
                this.disAmtEvent.toArray()[i].nativeElement.focus();
            } else if (this.taxes && !this.discountAmount && !this.discountPercent) {
                this.taxEvent.toArray()[i].open();
            } else {
                this.totalEvent.toArray()[i].nativeElement.focus();
            }
        }
    }

    fromDisPercent(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.discountAmount) {
                this.disAmtEvent.toArray()[i].nativeElement.focus();
            } else if (this.taxes) {
                this.taxEvent.toArray()[i].open();
            } else {
                this.totalEvent.toArray()[i].nativeElement.focus();
            }
        }
    }

    fromDisAmt(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.taxes) {
                this.taxEvent.toArray()[i].open();
            } else {
                this.totalEvent.toArray()[i].nativeElement.focus();
            }
        }
    }

    fromTax(e, i) {
        if (e.which === 13) {
            e.preventDefault();

            this.taxEvent.toArray()[i].close();
            this.totalEvent.toArray()[i].nativeElement.focus();
        }
    }

    gotoTCompany(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.tnameEvent.close();
            this.trnscompanyEvent.nativeElement.focus();
        }
    }

    fromTCompany(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.freightEvent.open();
        }
    }

    fromFreight(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.freightEvent.close();
            this.lrnoEvent.nativeElement.focus();
        }
    }

    fronLrNo(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.framtEvent.nativeElement.focus();
        }
    }

    fromFA(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.descEvent.nativeElement.focus();
        }
    }

    checkSave(e) {
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

    changeVoucher() {
        // if (!this.id) {
        this._proxy
            .getVoucherGenerateType()
            .pipe(first())
            .subscribe((x) => {
                if (x === 'Automatic') {
                    this.automaticInput = true;
                    this.getAllVoucher();
                } else {
                    if (x === 'Manual') {
                        this.isManual = true;
                        this.automaticInput = false;
                    } else {
                        this.isManual = false;
                        this.automaticInput = false;
                    }
                }
            });
        // }
    }

    private createDetails(item: any = {}) {

        return this.fb.group({
            qty: [item.qty ?? 0, Validators.required],
            rate: [item.rate ?? 0, Validators.required],
            amount: [item.amount ?? 0, Validators.required],
            taxValue: [item.taxValue ?? 0, Validators.required],
            taxAmount: [item.taxAmount ?? 0, Validators.required],
            discount: [item.discount ?? 0, Validators.required],
            discountPer: [item.discountPer ?? 0, Validators.required],
            grossAmount: [item.grossAmount ?? 0],
            netAmount: [item.netAmount ?? 0],
            productType: [item.productType ?? 0],
            productId: [{ value: item.productId ?? null, disabled: !!item.id }, Validators.required],
            unitId: [item.unitId ?? null, Validators.required],
            salesDetailId: [item.salesDetailId ?? null],
            taxId: [item.taxId ?? null, Validators.required],
            id: [item.id ?? null],
            unitsList: this.fb.array([]),
        });
    }
}
