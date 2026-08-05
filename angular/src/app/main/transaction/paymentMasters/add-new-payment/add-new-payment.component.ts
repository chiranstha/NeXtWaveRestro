import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    Injector,
    OnInit,
    OnDestroy,
    QueryList,
    TemplateRef,
    ViewChild,
    ViewChildren,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { NgSelectComponent } from '@ng-select/ng-select';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PaymentMastersServiceProxy,
    // ReportingServiceProxy,
} from '@shared/service-proxies/service-proxies';

import { finalize, first, Observable, Subject, takeUntil, timer, debounceTime, distinctUntilChanged } from 'rxjs';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-payment-new',
    templateUrl: './add-new-payment.component.html',
    animations: [appModuleAnimation]
})
export class AddPaymentNewComponent extends AppComponentBase implements OnInit, OnDestroy {
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('voucherNo', { read: ElementRef }) voucherNoEvent: ElementRef;
    @ViewChildren('account') accountEvent: QueryList<NgSelectComponent>;
    @ViewChildren('amounts', { read: ElementRef }) amountsEvent: QueryList<ElementRef>;
    @ViewChild('AgainstrefType') AgainstrefTypeEvent: NgSelectComponent;
    @ViewChildren('againstBtn', { read: ElementRef }) againstBtnEvent: QueryList<ElementRef>;
    @ViewChildren('number', { read: ElementRef }) numbersEvent: QueryList<ElementRef>;
    @ViewChild('narration', { read: ElementRef }) narrationEvent: ElementRef;
    modalRef?: BsModalRef;
    serialNumber = 0;
    id: string;
    formDetailRowId: number;
    //boolean definitions
    saving: boolean;
    public isManual = false;
    automaticInput = false;
    amountError = false;
    isAgainst = true;
    // string Definitions
    title = 'Add Payment Master';
    ledgerBalanceStatus: string;
    // form Definitions
    form: FormGroup;
    partyForm: FormGroup;
    //form Datas
    allMasterLedger$: Observable<any>;
    allLedgers$: Observable<any>;

    allVoucherNos$: Observable<string[]>;
    voucherdata: any;
    refrenceTypes = [
        { id: 1, name: 'Against' },
        { id: 2, name: 'On Account' },
    ];

    // Class variables for better performance
    private destroy$ = new Subject<void>();
    shortcuts: ShortcutInput[] = [];
    middleArrInx = 0;
    isPrint = false;

    constructor(
        injector: Injector,
        private _proxy: PaymentMastersServiceProxy,
        // private _report: ReportingServiceProxy,
        private _location: Location,
        private _router: Router,
        private _route: ActivatedRoute,
        private _fb: FormBuilder,
        private modalService: BsModalService
    ) {
        super(injector);
        this.getSetting();

    }

    //form access definitions
    get paymentDetailsArray() {
        return this.form.get('paymentDetails') as FormArray;
    }

    // check if total is zero
    get isZero(): boolean {
        if (this.form.get('totalAmount').value === 0) {
            return true;
        }
        return false;
    }

    ngOnInit(): void {
        this.initShortcuts();
        this.createForm();
        this.id = this._route.snapshot.params['id'];
        if (!this.id) {
            this.getByBranch();
        } else {
            this.title = 'Edit Payment Master';
            this._proxy.getPaymentMasterForEdit(this.id).subscribe((response: any) => {
                this.createForm(response);
                this.getByBranch();
            });
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    initShortcuts(): void {
        this.shortcuts = [
            {
                key: ['alt + s'],
                label: 'Save',
                description: 'Save Payment',
                command: e => this.keySave(true),
                preventDefault: true
            },
            {
                key: ['ctrl + n'],
                label: 'New Detail',
                description: 'Add new detail row',
                command: e => this.keyEventsAdd(),
                preventDefault: true
            },
            {
                key: ['alt + c'],
                label: 'Close',
                description: 'Close Form',
                command: e => this.keyClose(true),
                preventDefault: true
            },
            {
                key: ['alt + p'],
                label: 'Print',
                description: 'Save and Print',
                command: e => {
                    this.isPrint = true;
                    this.keySave(true);
                },
                preventDefault: true
            }
        ];
    }

    createForm(item: any = {}) {
        this.form = this._fb.group({
            id: [item.id ? item.id : this.emptyGuId],
            voucherNo: [item.voucherNo, Validators.required],
            totalAmount: [item.totalAmount ? item.totalAmount : 0, Validators.required],
            description: [item.description ? item.description : ''],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            ledgerId: [item.ledgerId, Validators.required],
            paymentDetails: this._fb.array(
                (() => {
                    if (!item.paymentDetails) {
                        return [this.createDetails()];
                    }
                    return item.paymentDetails.map((item) => this.createDetails(item));
                })()
            ),
        });
    }


    calculateSum(i: number): number {
        const paymentDetailsControl = this.form.get('paymentDetails');
        if (!paymentDetailsControl?.['controls']?.[i]) {
            return 0;
        }

        const partyBalanceDetailControl = paymentDetailsControl['controls'][i].get('partyBalanceDetail');
        if (!partyBalanceDetailControl) {
            return 0;
        }

        const detailsArray = partyBalanceDetailControl.get('details') as FormArray;
        if (!detailsArray) {
            return 0;
        }

        let totalAdjust = 0;

        for (let j = 0; j < detailsArray.length; j++) {
            const adjustControl = detailsArray.controls[j].get('adjust');
            if (adjustControl && adjustControl.value !== null && adjustControl.value !== undefined) {
                totalAdjust = totalAdjust + (adjustControl.value || 0);
            }
        }
        return totalAdjust;
    }

    newAgeningSum(i: number): number {
        const paymentDetailsControl = this.form.get('paymentDetails');
        if (!paymentDetailsControl?.['controls']?.[i]) {
            return 0;
        }

        const amountControl = paymentDetailsControl['controls'][i].get('amount');
        const totalAmt = amountControl ? (amountControl.value || 0) : 0;

        const partyBalanceDetailControl = paymentDetailsControl['controls'][i].get('partyBalanceDetail');
        if (!partyBalanceDetailControl) {
            return totalAmt;
        }

        const detailsArray = partyBalanceDetailControl.get('details') as FormArray;
        if (!detailsArray) {
            return totalAmt;
        }

        let totalAdjust = 0;

        for (let j = 0; j < detailsArray.length; j++) {
            const adjustControl = detailsArray.controls[j].get('adjust');
            if (adjustControl && adjustControl.value !== null && adjustControl.value !== undefined) {
                totalAdjust = totalAdjust + (adjustControl.value || 0);
            }
        }
        return totalAmt - totalAdjust;
    }

    openDialog(dialog: TemplateRef<any>): void {
        this.modalRef = this.modalService.show(dialog);
        this.modalRef.setClass('modal-xl');
    }

    closeDialog(): void {
        this.modalRef?.hide();
    }    // Create FormGroup for GetPaymentAgainstDto
    createpartyBalanceDetail(data: any = {}) {
        return this._fb.group({
            sn: [data?.sn || 1],
            billDate: [data?.billDate || ''],
            dueDate: [data?.dueDate || ''],
            voucherType: [data?.voucherType || 'SalesInvoice'],
            voucherNo: [data?.voucherNo || ''],
            voucherNumbering: [data?.voucherNumbering || 1],
            voucherTypeId: [data?.voucherTypeId || ''],
            billAmount: [data?.billAmount || 0],
            paidAmount: [data?.paidAmount || 0],
            balanceAmount: [data?.balanceAmount || 0],
            adjust: [
                { value: data?.adjust ? data.adjust : 0, disabled: data?.isDisable },
                [Validators.required],
            ],
            isSettled: [data?.isSettled || false],
            partyBalanceId: [data?.partyBalanceId ? data.partyBalanceId : null],
        });
    }

    // Create FormGroup for GetPaymentAgainstMasterDto structure
    createPaymentAgainstMaster(data: any = {}) {
        return this._fb.group({
            newReferenceAmount: [data?.newReferenceAmount || 0, [Validators.required]],
            details: this._fb.array(
                data?.details ? data.details.map(item => this.createpartyBalanceDetail(item)) : []
            )
        });
    }

    getPaymentDetails(form) {
        return form.get('paymentDetails').controls;
    }

    enableAllDetails() {
        this.paymentDetailsArray.controls.forEach((element) => {
            element.enable();
        });
    }


    addDetailsForm() {
        const control = <FormArray>this.form.controls.paymentDetails;
        control.push(this.createDetails());
        this.totalAmountCalculation();
    }

    keyEventsAdd(): void {
        this.enableAllDetails();
        if (this.form.get('paymentDetails').valid) {
            this.addDetailsForm();
        }
    }

    enableDetailsFormGroup(i, form) {
        this.middleArrInx = i;
        this.paymentDetailsArray.disable();
        const control = form.controls.paymentDetails.controls[i];
        control.enable(i);
        timer(100)
            .pipe(first())
            .subscribe(() => {
                this.accountEvent.toArray()[i].open();
            });

        this.getledgerBalanceStatus(control.get('ledgerId').value);
    }

    removeDetailsForm(i, form) {
        const control = form.controls.paymentDetails;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalAmountCalculation();
    }


    getByBranch() {
        if (!this.id) {
            this._proxy.getPaymentMasterVoucherNo().subscribe((x) => {
                this.form.get('voucherNo').setValue(x);
            });
            this._proxy.getVoucherGenerateType().subscribe((x) => {
                if (x === 'Automatic') {
                    this.automaticInput = true;
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
        this.getAllAcountLedger();
        this.getAllMasterLedger();
    }

    getAllMasterLedger() {
        this.allMasterLedger$ = this._proxy.getAllCashOrBankForTableDropdown();
        if (!this.id) {
            this.allMasterLedger$.subscribe((data) => {
                this.form.get('ledgerId').setValue(data[0].id);
            });
        }
    }

    getAllAcountLedger(ledgerId?: string) {
        this.allLedgers$ = this._proxy.getAllAccountLedgerForTableDropdown();
        if (this.serialNumber) {
            this.paymentDetailsArray.controls[this.formDetailRowId].get('ledgerId').setValue(ledgerId);
            this.serialNumber = 0;
            this.allLedgers$.subscribe((data) => {
                const justSaved = data.find((x) => x.id === ledgerId);
                this.insertParty(this.paymentDetailsArray.controls[this.formDetailRowId]);
                this.formDetailRowId = null;
            });
        }
    }

    /// form traversals
    gotoDate() {
        document.getElementById('npDatePicker').focus();
    }

    gotoLedger(e) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.automaticInput === false) {
                if (!this.id) {
                    this.voucherNoEvent.nativeElement.focus();
                } else {
                    this.ledgerEvent.open();
                }
            } else {
                this.ledgerEvent.open();
            }
        }
    }

    gotoAccount(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.close();
            this.accountEvent.first.open();
        }
    }

    gotoAmount(e, abcd, i) {
        e.preventDefault();
        if (e.which === 13) {
            if (abcd.get('isBillByBill').value) {
                this.againstBtnEvent.toArray()[i].nativeElement.click();
                setTimeout(() => {
                    this.accountEvent.toArray()[i].close();
                    this.AgainstrefTypeEvent.open();
                }, 100);
            } else {
                this.accountEvent.toArray()[i].close();
                this.amountsEvent.toArray()[i].nativeElement.focus();
            }
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                e.preventDefault();
                this.formDetailRowId = i;
                this.changeAccountledger(e);
            }
        }
    }

    changeAccountledger(event) {
        if (event && event !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }

    gotoChequeNo(e, i) {
        e.preventDefault();
        if (e.which === 13) {
            this.numbersEvent.toArray()[i].nativeElement.focus();
        }
    }

    gotoNext(e) {
        e.preventDefault();
        if (e.which === 13) {
            if (this.form.get('paymentDetails').valid) {

                this.keyEventsAdd();
            }
            //   this.checkFormArrdisableOnEdit(this.paymentDetailsArray, this.middleArrInx);

            setTimeout(() => {
                this.accountEvent.last.open();
            });
        } else if (e.key === 'Alt' && this.paymentDetailsArray.valid) {

            this.narrationEvent.nativeElement.focus();
        }
    }

    savonNarration(e) {
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


    insertParty(form, isBillByBill = false) {
        form.get('isBillByBill').setValue(isBillByBill);

        const ledgerId = form.get('ledgerId').value;
        const detaileditId = form.get('id').value;
        const partyBalanceDetailGroup = form.get('partyBalanceDetail');
        const detailsControl = partyBalanceDetailGroup.get('details') as FormArray;
        detailsControl.clear();
        const amount = form.get('amount').value;

        this._proxy.getPaymentAgainst(ledgerId, amount, detaileditId).subscribe(result => {
            // Clear existing controls
            detailsControl.clear();

            // Set the newReferenceAmount
            partyBalanceDetailGroup.get('newReferenceAmount').setValue(result?.newReferenceAmount || 0);

            // Add each detail from the API response
            if (result && result.details && result.details.length > 0) {
                result.details.forEach(detail => {
                    detailsControl.push(this.createpartyBalanceDetail(detail));
                });
            }
        });

        this.getledgerBalanceStatus(form.get('ledgerId').value);
        this.totalAmountCalculation();
    }


    adjustDetailFormArray(form) {
        // Check if subscription already exists for this form to avoid duplicate subscriptions
        if (form._adjustDetailSubscription) {
            form._adjustDetailSubscription.unsubscribe();
        }


        const detaileditId = form.get('id').value;
        form._adjustDetailSubscription = form.get('amount').valueChanges.pipe(
            debounceTime(300), // Wait 300ms after user stops typing
            distinctUntilChanged(), // Only proceed if value actually changed
            takeUntil(this.destroy$) // Clean up when component is destroyed
        ).subscribe((amt) => {
            if (amt === null || amt === undefined || amt === 0) {
                return; // Skip API call for empty/zero values
            }

            const amount = amt;
            const ledgerId = form.get('ledgerId').value;

            if (!ledgerId) {
                return; // Skip if no ledger is selected
            }

            const partyBalanceDetailGroup = form.get('partyBalanceDetail');
            const detailsControl = partyBalanceDetailGroup.get('details') as FormArray;
            detailsControl.clear();

            this._proxy.getPaymentAgainst(ledgerId, amount, detaileditId).pipe(
                takeUntil(this.destroy$) // Cancel request if component is destroyed
            ).subscribe(result => {
                // Clear existing controls
                detailsControl.clear();

                // Set the newReferenceAmount
                partyBalanceDetailGroup.get('newReferenceAmount').setValue(result?.newReferenceAmount || 0);

                // Add each detail from the API response
                if (result && result.details && result.details.length > 0) {
                    result.details.forEach(detail => {
                        detailsControl.push(this.createpartyBalanceDetail(detail));
                    });
                }

                this.totalAmountCalculation();
            });
        });
    }



    adjustAmountValidataion(masterform, form, j) {
        const amountControl = masterform.get('amount');
        const amount = amountControl ? (amountControl.value || 0) : 0;

        const partyBalanceDetailControl = masterform.get('partyBalanceDetail');
        if (!partyBalanceDetailControl) {
            return;
        }

        const detailsArray = partyBalanceDetailControl.get('details') as FormArray;
        if (!detailsArray?.controls[j]) {
            return;
        }

        const balanceControl = detailsArray.controls[j].get('balanceAmount');
        const balance = balanceControl ? (balanceControl.value || 0) : 0;

        const adjustControl = form.get('adjust');
        if (!adjustControl) {
            return;
        }

        adjustControl.valueChanges.subscribe((amt) => {
            let totalAdjust = 0;
            for (let i = 0; i < detailsArray.length; i++) {
                const currentAdjustControl = detailsArray.controls[i].get('adjust');
                if (currentAdjustControl && currentAdjustControl.value !== null) {
                    totalAdjust = totalAdjust + (currentAdjustControl.value || 0);
                }
            }

            if (amt > balance) {
                adjustControl.setValue(balance);
            }
            if (amt > amount) {
                adjustControl.setValue(amount);
            }

            if (totalAdjust > amount) {
                adjustControl.patchValue(0, { emitEvent: false });

                let refreshTotalAdjust = 0;
                const refreshdetailArr = partyBalanceDetailControl.get('details') as FormArray;
                for (let i = 0; i < refreshdetailArr.length; i++) {
                    const refreshAdjustControl = refreshdetailArr.controls[i].get('adjust');
                    if (refreshAdjustControl && refreshAdjustControl.value !== null) {
                        refreshTotalAdjust = refreshTotalAdjust + (refreshAdjustControl.value || 0);
                    }
                }
                this.notify.error(`Adjust amount is greater than amount then set  ${  amount - refreshTotalAdjust}`);
                adjustControl.patchValue(amount - refreshTotalAdjust, { emitEvent: false });
            }
        });


        let totalAdjust = 0;
        for (let i = 0; i < detailsArray.length; i++) {
            const currentAdjustControl = detailsArray.controls[i].get('adjust');
            if (currentAdjustControl && currentAdjustControl.value !== null) {
                totalAdjust = totalAdjust + (currentAdjustControl.value || 0);
            }
        }

        const newReferenceAmount = amount - totalAdjust;
        partyBalanceDetailControl.get('newReferenceAmount').setValue(newReferenceAmount);
    }

    getledgerBalanceStatus(id) {
        this._proxy.getLedgerBalanceStatus(id).subscribe((data) => {
            this.ledgerBalanceStatus = data;
        });
    }

    // clear form
    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy.delete(id).subscribe(() => {
                    this._location.back();
                    this.notify.success(this.l('Successfully Deleted'));
                });
            }
        });
    }

    // keys effects
    keySave(event: boolean) {
        if (event === true) {
            if (this.isZero) {
                this.notify.error(this.l('Total Amount cannot be zero'));
            } else {
                this.checkSave();
            }
        } else {
            return;
        }
    }

    keyClose(event: boolean) {
        if (event === true) {
            if (this.serialNumber) {
                this.serialNumber = 0;
            } else {
                // if (!this._dialogService.hasOpenDialogs()) {
                //     this.close();
                // }
                // else {
                //     this._dialogService.dismissAll();
                // }
            }
        } else {
            return;
        }
    }

    //save form
    checkSave() {
        if (this.isManual) {
            const voucherN = this.form.get('voucherId').value;
            this._proxy.getCheckVoucherNo(voucherN).subscribe((value) => {
                if (!value) {
                    this.save();
                } else {
                    alert('Voucher Number Already Exists');
                }
            });
        } else {
            this.save();
        }
    }

    save() {
        if (this.serialNumber === 0) {
            if (this.form.valid) {
                if (this.id) {
                    this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.createApi();
                        }
                    });
                } else {
                    this.createApi();
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    createApi() {
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

                        this._router.navigate([`app/main/transaction/pdf/1/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this._router.navigate([`app/main/transaction/pdf/1/${data}`]);
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

    //close form
    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.eacape();
                }
            });
        } else {
            this.eacape();
        }
    }

    eacape() {
        this._location.back();
    }


    changeInRefType() {
        this.partyForm.get('voucherTypeId').setValue(this.emptyGuId);
        this.partyForm.get('voucherNo').setValue(0);
        this.partyForm.get('pending').setValue(0);
        this.partyForm.get('amount').setValue(0);
        if (this.partyForm.get('referenceType').value === 2) {
            this.isAgainst = true;
        }
    }

    getVoucherNo(abcd, id: string) {
        this.partyForm.get('voucherNo').reset();
        this.partyForm.get('amount').reset();
        this.partyForm.get('pending').reset();
        const supId = abcd.get('ledgerId').value;
        this.getAllVoucherNumber(supId, id);
    }

    getAllVoucherNumber(supId, id) {
        this.voucherdata.forEach((element) => {
            if (element.id === id) {
                this.isAgainst = element.isAgainst;
            }
        });
    }

    changeInAmount() {
        const amount = this.partyForm.get('amount').value;
        const pending = this.partyForm.get('pending').value;
        const referenceType = this.partyForm.get('referenceType').value;
        if (referenceType === 1 && amount > pending) {
            this.amountError = true;
        } else {
            this.amountError = false;
        }
    }


    // party Balance Actions
    getpartyBalanceDetail(form) {
        return form.controls.partyBalanceDetail.controls.details.controls;
    }


    totalAmountCalculation() {
        let netAmount = 0;
        for (let i = 0; i < this.paymentDetailsArray.length; i++) {
            netAmount += this.paymentDetailsArray.controls[i].get('amount').value;
        }
        this.form.get('totalAmount').setValue(netAmount);
    }


    private createDetails(item: any = {}) {
        const formGroup = this._fb.group({
            amount: [item.amount ? item.amount : 0, Validators.required],
            chequeNo: [item.chequeNo ? item.chequeNo : ''],
            chequeMiti: [item.chequeMiti ? item.chequeMiti : this.today, Validators.required],
            ledgerId: [{ value: item.ledgerId ? item.ledgerId : null, disabled: item.ledgerId }, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
            isBillByBill: [item.isBillByBill ? item.isBillByBill : false],
            partyBalanceDetail: this._fb.group({
                newReferenceAmount: [item.partyBalanceDetail?.newReferenceAmount || 0],
                details: this._fb.array(
                    (() => {
                        if (!item.partyBalanceDetail?.details) {
                            return [];
                        }
                        return item.partyBalanceDetail.details.map((detail) => this.createpartyBalanceDetail(detail));
                    })()
                )
            }),
        });

        // Set up the amount change subscription immediately when form is created
        this.adjustDetailFormArray(formGroup);

        return formGroup;
    }

    // Helper methods for GetPaymentAgainstMasterDto FormArray

    // Get details FormArray from a payment against master form
    getPaymentAgainstDetails(masterForm: FormGroup): FormArray {
        return masterForm.get('details') as FormArray;
    }

    // Add a new detail to the details FormArray
    addPaymentAgainstDetail(masterForm: FormGroup, detailData: any = {}) {
        const detailsArray = this.getPaymentAgainstDetails(masterForm);
        detailsArray.push(this.createpartyBalanceDetail(detailData));
    }

    // Remove a detail from the details FormArray
    removePaymentAgainstDetail(masterForm: FormGroup, index: number) {
        const detailsArray = this.getPaymentAgainstDetails(masterForm);
        if (detailsArray.length > 0) {
            detailsArray.removeAt(index);
        }
    }

    // Calculate total adjust amount from details
    calculateTotalAdjustFromDetails(masterForm: FormGroup): number {
        const detailsArray = this.getPaymentAgainstDetails(masterForm);
        let totalAdjust = 0;

        detailsArray.controls.forEach(control => {
            const adjustValue = control.get('adjust')?.value || 0;
            totalAdjust += adjustValue;
        });

        return totalAdjust;
    }

    // Validate adjust amounts don't exceed available amounts
    validateAdjustAmounts(masterForm: FormGroup): boolean {
        const newReferenceAmount = masterForm.get('newReferenceAmount')?.value || 0;
        const totalAdjust = this.calculateTotalAdjustFromDetails(masterForm);

        return totalAdjust <= newReferenceAmount;
    }

    // Create a complete FormArray structure matching GetPaymentAgainstMasterDto
    createPaymentAgainstFormArray(dataArray: any[] = []): FormArray {
        return this._fb.array(
            dataArray.map(item => this.createPaymentAgainstMaster(item))
        );
    }


}
