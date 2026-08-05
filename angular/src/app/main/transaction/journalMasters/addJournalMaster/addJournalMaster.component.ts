import { appModuleAnimation } from '@shared/animations/routerTransition';
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
    JournalDetailAccountLedgerTableDto,
    JournalMastersServiceProxy,
} from '@shared/service-proxies/service-proxies';

import { finalize, first, Observable, timer, Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';

export enum drCrEnum {
    Dr = 0,
    Cr = 1,
}

export interface IDropDown {
    id?: number;
    displayName?: string;
}

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-add-new-journal-master',
    templateUrl: './addjournalmaster.component.html'
})
export class AddNewJournalMasterComponent extends AppComponentBase implements OnInit, OnDestroy {
    // ciew childs
    @ViewChild('ref', {read: ElementRef}) refEvent: ElementRef;
    @ViewChild('voucherNo', {read: ElementRef}) vouchernumEvent: ElementRef;
    @ViewChildren('account') accountEvent: QueryList<NgSelectComponent>;
    @ViewChildren('drcr') drcrEvent: QueryList<NgSelectComponent>;
    @ViewChild('AgainstrefType') AgainstrefTypeEvent: NgSelectComponent;
    @ViewChildren('againstBtn', {read: ElementRef}) againstBtnEvent: QueryList<ElementRef>;
    @ViewChildren('amounts', {read: ElementRef}) amountsEvent: QueryList<ElementRef>;
    @ViewChildren('number', {read: ElementRef}) numberEvent: QueryList<ElementRef>;
    @ViewChild('narration', {read: ElementRef}) narrationEvent: ElementRef;

    //number definitions
    modalRef?: BsModalRef;
    serialNumber = 0;
    id: string;
    formDetailRowId: number;
    title = 'Add Journal Master';
    //boolean definitions
    saving = false;
    isManual = false;
    automaticInput: boolean;
    isEqual: boolean;
    amountError: boolean;
    //stringDefinitions
    ledgerBalanceStatus: string;
    //form definitions
    form: FormGroup;
    //form requirements
    allLedgers$: Observable<JournalDetailAccountLedgerTableDto[]>;
    allVoucherNos$: Observable<string[]>;

    // Class variables for better performance
    private destroy$ = new Subject<void>();
    middleArrInx = 0;
    // enums
    DrCrLabelMapping: Record<drCrEnum, string> = {
        [drCrEnum.Dr]: 'Dr',
        [drCrEnum.Cr]: 'Cr',
    };
    public drOrcr: IDropDown[] = this._prepareSelectOptions(this.DrCrLabelMapping);
    refrenceTypes = [
        {id: 1, name: 'Against'},
        {id: 2, name: 'On Account'},
    ];

    constructor(
        injector: Injector,
        private _proxy: JournalMastersServiceProxy,
        private _location: Location,
        private _router: Router,
        private _route: ActivatedRoute,
        private _fb: FormBuilder,
        private modalService: BsModalService
    ) {
        super(injector);
        this.getSetting();

        this.createForm();
    }

    // acess form methods
    get journalDetailArray() {
        return this.form.get('journalDetail') as FormArray;
    }

    // to check dr = cr
    get isdrcrEqual(): boolean {
        if (
            this.form.get('debitTotal').value !== 0 &&
            this.form.get('debitTotal').value === this.form.get('creditTotal').value
        ) {
            return true;
        }
        return false;
    }    // party Balance Actions
    getpartyBalanceDetail(form: FormGroup) {
        const partyBalanceDetail = form.get('partyBalanceDetail');
        if (partyBalanceDetail) {
            const detailsControl = partyBalanceDetail.get('details') as FormArray;
            return detailsControl ? detailsControl.controls : [];
        }
        return [];
    }

    ngOnInit(): void {
        this.id = this._route.snapshot.params['id'];
        if (!this.id) {

            this.getByBranch();
        } else {
            this.title = 'Edit Journal Master';
            this._proxy.getJournalMasterForEdit(this.id).subscribe((response: any) => {
                this.createForm(response);
                this.getByBranch();
                // Refresh subscriptions after data is loaded for edit mode
                setTimeout(() => {
                    this.setupAmountChangeSubscriptions();
                }, 100);
            });
        }
    }

    ngOnDestroy(): void {
        // Clear all subscriptions before destroying the component
        this.clearAllSubscriptions();

        this.destroy$.next();
        this.destroy$.complete();
    }    createForm(item: any = {}) {
        this.form = this._fb.group({
            id: [item.id ? item.id : this.emptyGuId],
            voucherNo: [item.voucherNo, Validators.required],

            debitTotal: [item.debitTotal ? item.debitTotal : 0, Validators.required],
            creditTotal: [item.creditTotal ? item.creditTotal : 0, Validators.required],
            isbiilByBill: [item.isbiilByBill ? item.isbiilByBill : false, Validators.required],
            description: [item.description ? item.description : ''],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            referenceNo: [item.referenceNo],
            journalDetail: this._fb.array(
                (() => {
                    if (!item.journalDetail) {
                        return [this.createDetails()];
                    }
                    return item.journalDetail.map((item) => this.createDetails(item));
                })()
            ),
        });

        // Set up amount change subscriptions after the form is created with a slight delay
        setTimeout(() => {
            this.setupAmountChangeSubscriptions();
        }, 0);
    }

    // Set up amount change subscriptions for all journal detail forms
    setupAmountChangeSubscriptions() {
        if (!this.form || !this.journalDetailArray) {
            return;
        }

        // Clear any existing subscriptions first
        this.clearAllSubscriptions();

        for (let i = 0; i < this.journalDetailArray.length; i++) {
            const detailForm = this.journalDetailArray.controls[i] as FormGroup;
            if (detailForm && detailForm.get('amount')) {
                this.adjustDetailFormArray(detailForm);
            }
        }
    }

    // Clear all existing subscriptions to prevent memory leaks
    private clearAllSubscriptions() {
        if (this.journalDetailArray) {
            for (let i = 0; i < this.journalDetailArray.length; i++) {
                const detailForm = this.journalDetailArray.controls[i] as any;
                if (detailForm?._adjustDetailSubscription) {
                    detailForm._adjustDetailSubscription.unsubscribe();
                    delete detailForm._adjustDetailSubscription;
                }
            }
        }
    }

    // Public method to refresh subscriptions if needed
    public refreshAmountChangeSubscriptions() {
        setTimeout(() => {
            this.setupAmountChangeSubscriptions();
        }, 0);
    }

    keyEventsAdd(): void {
        if (this.form.get('journalDetail').valid) {
            this.addDetailsForm();
        }
    }
    addDetailsForm() {
        const control = <FormArray>this.form.controls.journalDetail;
        const newDetailForm = this.createDetails();
        control.push(newDetailForm);

        // Set up amount change subscription for the new form with a slight delay
        // to ensure the form is fully initialized
        setTimeout(() => {
            this.adjustDetailFormArray(newDetailForm);
        }, 0);

        this.totalAmountCalculation();
    }


    enableDetailsFormGroup(i, form) {
        this.middleArrInx = i;
        this.journalDetailArray.controls.forEach((element) => {
            if (element.valid) {
                element.disable();
            }
        });
        const control = form.controls.journalDetail.controls[i];
        control.enable(i);
        this.getledgerBalanceStatus(control.get('ledgerId').value);
        timer(100)
            .pipe(first())
            .subscribe(() => {
                if (i === 0) {
                    this.accountEvent.toArray()[i].open();
                }
            });
        this.getledgerBalanceStatus(control.get('ledgerId').value);
    }

    removeDetailsForm(i, form) {
        const control = form.controls.journalDetail;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.totalAmountCalculation();
    }    // form array actions
    insertParty(form: FormGroup, isBillByBill = true) {
        const ledgerId = form.get('ledgerId')?.value;
        const drOrCr = form.get('drOrCr')?.value;
        const detaileditId = form.get('id')?.value;
        const amount = form.get('amount')?.value;
        form.get('isBillByBill')?.setValue(isBillByBill);

        const partyBalanceDetailGroup = form.get('partyBalanceDetail');
        const detailsControl = partyBalanceDetailGroup.get('details') as FormArray;
        detailsControl.clear();

        if (ledgerId) {
            if (drOrCr === 1) {
                this._proxy.getPartyBalanceCredit(ledgerId).subscribe((result) => {
                    // Clear existing controls
                    detailsControl.clear();

                    // Set the newReferenceAmount if available
                    if (result && result.length > 0) {
                        partyBalanceDetailGroup.get('newReferenceAmount').setValue(0);

                        // Add each detail from the API response
                        result.forEach(detail => {
                            detailsControl.push(this.createpartyBalanceDetail(detail));
                        });
                    }

                    // Re-establish subscription after party details are loaded
                    setTimeout(() => {
                        this.adjustDetailFormArray(form);
                    }, 0);
                });
            } else {
                this._proxy.getPartyBalanceDebit(ledgerId).subscribe((result) => {
                    // Clear existing controls
                    detailsControl.clear();

                    // Set the newReferenceAmount if available
                    if (result && result.length > 0) {
                        partyBalanceDetailGroup.get('newReferenceAmount').setValue(0);

                        // Add each detail from the API response
                        result.forEach(detail => {
                            detailsControl.push(this.createpartyBalanceDetail(detail));
                        });
                    }

                    // Re-establish subscription after party details are loaded
                    setTimeout(() => {
                        this.adjustDetailFormArray(form);
                    }, 0);
                });
            }
        }

        this.getledgerBalanceStatus(form.get('ledgerId')?.value);
        if (form.get('amount')?.value > 0) {
            this.notify.warn('Please adjust the amount');
        }
    }


    openDialog(dialog: TemplateRef<any>): void {
        this.modalRef = this.modalService.show(dialog);
        this.modalRef.setClass('modal-xl');
    }

    closeDialog(): void {
        this.modalRef?.hide();
    }

    createpartyBalanceDetail(item: any = {}) {
        return this._fb.group({
            sn: [item?.sn || 1, [Validators.required, Validators.min(1)]],
            billDate: [item?.billDate || item?.payDate || '', Validators.required],
            dueDate: [item?.dueDate || '', Validators.required],
            voucherType: [item?.voucherType || item?.type || 'SalesInvoice', Validators.required],
            voucherNo: [item?.voucherNo || '', Validators.required],
            voucherNumbering: [item?.voucherNumbering || 1, [Validators.required]],
            voucherTypeId: [item?.voucherTypeId || '', Validators.required],
            voucherTypeName: [item?.voucherTypeName || '', Validators.required],
            billAmount: [item?.billAmount || item?.billAmt || 0, [Validators.required]],
            billAmt: [item?.billAmt || item?.billAmount || 0, [Validators.required]],
            paidAmount: [item?.paidAmount || item?.paid || item?.onAccountPaid || 0],
            paid: [item?.paid || item?.paidAmount || item?.onAccountPaid || 0],
            onAccountPaid: [item?.onAccountPaid || item?.paid || item?.paidAmount || 0],
            balanceAmount: [item?.balanceAmount || item?.balance || 0, [Validators.required]],
            balance: [item?.balance || item?.balanceAmount || 0, [Validators.required]],
            balanceString: [item?.balanceString || '', Validators.required],
            payDate: [item?.payDate || item?.billDate || '', Validators.required],
            type: [item?.type || item?.voucherType || ''],
            adjust: [
                { value: item?.adjust ? item.adjust : 0, disabled: item?.isDisable },
                [Validators.required],
            ],
            isSettled: [item?.isSettled || false],
            isDisable: [item?.isDisable || false],
            partyBalanceId: [item?.id || null],
        });
    }

    adjustAmountValidataion(masterform: FormGroup, form: FormGroup, j: number) {
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

        const balanceControl = detailsArray.controls[j].get('balanceAmount') || detailsArray.controls[j].get('balance');
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
    }

    calculateSum(i: number): number {
        const journalDetailControl = this.form.get('journalDetail');
        if (!journalDetailControl?.['controls']?.[i]) {
            return 0;
        }

        const partyBalanceDetailControl = journalDetailControl['controls'][i].get('partyBalanceDetail');
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
        const totalAmt = this.form.get('journalDetail')['controls'][i].get('amount').value;
        const journalDetailControl = this.form.get('journalDetail');
        if (!journalDetailControl?.['controls']?.[i]) {
            return totalAmt;
        }

        const partyBalanceDetailControl = journalDetailControl['controls'][i].get('partyBalanceDetail');
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

    adjustDetailFormArray(form: FormGroup) {
        // Validate form and required controls exist
        if (!form?.get('amount')) {
            console.warn('Form or amount control not found in adjustDetailFormArray');
            return;
        }

        // Check if subscription already exists for this form to avoid duplicate subscriptions
        const formWithSub = form as any;
        if (formWithSub._adjustDetailSubscription) {
            formWithSub._adjustDetailSubscription.unsubscribe();
        }

        const detaileditId = form.get('id')?.value;

        // Set up the subscription with error handling
        formWithSub._adjustDetailSubscription = form.get('amount')?.valueChanges.pipe(
            debounceTime(300), // Wait 300ms after user stops typing
            distinctUntilChanged(), // Only proceed if value actually changed
            takeUntil(this.destroy$) // Clean up when component is destroyed
        ).subscribe({
            next: (amt) => {
                this.handleAmountChange(form, amt);
            },
            error: (error) => {
                console.error('Error in amount change subscription:', error);
            }
        });
    }

    // Separate method to handle amount changes for better maintainability
    private handleAmountChange(form, amt) {
        if (amt === null || amt === undefined) {
            return; // Skip processing for null/undefined values
        }

        let amount = Number(amt) || 0;
        const ledgerId = form.get('ledgerId')?.value;
        const drOrCr = form.get('drOrCr')?.value;

        if (!ledgerId) {
            return; // Skip if no ledger is selected
        }

        const partyBalanceDetailGroup = form.get('partyBalanceDetail');
        if (!partyBalanceDetailGroup) {
            return;
        }

        const detailsControl = partyBalanceDetailGroup.get('details') as FormArray;

        // Auto-adjust logic for existing details
        if (detailsControl && detailsControl.length > 0) {
            for (let i = 0; i < detailsControl.length; i++) {
                const detailControl = detailsControl.controls[i];
                if (!detailControl) {continue;}

                const isDisabled = detailControl.get('isDisable')?.value;
                if (isDisabled === true) {continue;}

                let balance = detailControl.get('balance')?.value ||
                             detailControl.get('balanceAmount')?.value || 0;
                balance = Number(balance) || 0;

                const adjustControl = detailControl.get('adjust');
                if (!adjustControl) {continue;}

                if (amount >= balance) {
                    adjustControl.setValue(balance);
                    amount = amount - balance;
                } else if (amount > 0) {
                    adjustControl.setValue(amount);
                    amount = 0;
                } else {
                    adjustControl.setValue(0);
                }
            }
        }

        // Only call totalAmountCalculation if the main form exists
        if (this.form) {
            this.totalAmountCalculation();
        }
    }

    getledgerBalanceStatus(id) {
        this._proxy.getLedgerBalanceStatus(id).subscribe((data) => {
            this.ledgerBalanceStatus = data;
        });
    }

    setDrCr(abcd, amount) {
        if (abcd.get('drOrCr').value === drCrEnum.Dr) {
            abcd.get('debit').setValue(amount);
        } else {
            abcd.get('credit').setValue(amount);
        }
        // this.totalAmountCalculation()
    }

    checksSave(e) {
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

    getAllAcountLedger(ledgerId?) {
        this.allLedgers$ = this._proxy.getAllLedgerForLookupTable(undefined);
        if (this.serialNumber) {
            this.journalDetailArray.controls[this.formDetailRowId].get('ledgerId')?.setValue(ledgerId);
            this.serialNumber = 0;
            this.allLedgers$.subscribe((data) => {
                const justSaved = data.find((x) => x.id === ledgerId);
                this.insertParty(this.journalDetailArray.controls[this.formDetailRowId] as FormGroup);
                this.formDetailRowId = null;
            });
        }
    }

    //on change formcontrols
    getByBranch() {
        if (!this.id) {
            this._proxy.getJournalMasterVoucherNo().subscribe((x) => {
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
    }

    // calculations
    totalPriceCalculation(abcd) {
        let totalAmount = 0;
        for (let i = 0; i < abcd.get('partyBalances').length; i++) {
            totalAmount += abcd.get('partyBalances')['controls'][i].get('amount').value;
        }
        if (abcd.get('drOrCr').value === drCrEnum.Dr) {
            abcd.get('debit').setValue(totalAmount);
            abcd.get('credit').setValue(0);
        } else {
            abcd.get('debit').setValue(0);
            abcd.get('credit').setValue(totalAmount);
        }
        abcd.get('amount').setValue(totalAmount);
    }


    totalAmountCalculation() {
        if (!this.form || !this.journalDetailArray) {
            return;
        }


        let netDebit = 0;
        let netCredit = 0;
        for (let i = 0; i < this.journalDetailArray.length; i++) {
            const detailControl = this.journalDetailArray.controls[i];
            if (detailControl) {
                const amount = detailControl.get('amount')?.value || 0;


                const drOrCr = detailControl.get('drOrCr')?.value;
                if (drOrCr === drCrEnum.Dr) {
                    netDebit += amount;

                } else {
                    netCredit += amount;
                }
            }
        }


        this.form.get('debitTotal').setValue(netDebit);
        this.form.get('creditTotal').setValue(netCredit);
        if (netDebit !== netCredit) {
            this.isEqual = false;
        } else {
            this.isEqual = true;
        }
    }

    /// form traversal
    gotoDateMiti(e) {
        if (e.which === 13) {
            e.preventDefault();
            document.getElementById('npDatePicker').focus();
        }
    }

    goFromDate(e) {
        e.preventDefault();
        if (e.which === 13) {
            if (this.automaticInput === false) {
                if (!this.id) {
                    this.vouchernumEvent.nativeElement.focus();
                } else {
                    this.refEvent.nativeElement.focus();
                }
            } else {
                this.refEvent.nativeElement.focus();
            }
        }
    }

    gotoReference(e) {
        e.preventDefault();
        if (e.which === 13) {
            this.refEvent.nativeElement.focus();
        }
    }

    gotoAccount(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.accountEvent.toArray[0].open();
        }
    }

    gotoDrCr(e, i) {
        if (e.which === 13) {
            e.preventDefault();

            this.accountEvent.toArray()[i].close();
            this.drcrEvent.toArray()[i].open();
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                e.preventDefault();
                this.formDetailRowId = i;
                this.changeAccountledger(e);
            }
        }
    }

    gotoNumber(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            this.numberEvent.toArray()[i].nativeElement.focus();
        }
    }

    goNext() {
        this.addDetailsForm();
    //    this.checkFormArrdisableOnEdit(this.journalDetailArray, this.middleArrInx);

        setTimeout(() => {
            this.accountEvent.last.open();
        }, 100);
    }

    checkCondition(e, i) {
        e.preventDefault();
        if (e.which === 13) {
            if (i === 0) {
                this.goNext();
            } else {
                this.goNext();
            }
        } else if (e.key === 'Alt' && this.journalDetailArray.valid && this.isdrcrEqual) {
            this.narrationEvent.nativeElement.focus();
        }
    }

    changeAccountledger(event) {
        if (event && event !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }

    // save cancel or delete
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

    checkSave() {
        this.saving = true;
        if (this.isManual) {
            const {voucherId} = this.form.value;
            // const bId = this.form.get("branchId").value;
            // const voucherN = this.form.get("voucherId").value;
            this._proxy.getCheckVoucherNo(voucherId).subscribe((value) => {
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
        this.saving = false;
        if (this.serialNumber === 0) {
            if (this.form.valid) {
                if (this.id) {
                    this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.apiCall();
                        }
                    });
                } else {
                    this.apiCall();
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    apiCall() {
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

                        this._router.navigate([`app/main/transaction/pdf/3/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this._router.navigate([`app/main/transaction/pdf/3/${data}`]);
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
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this._location.back();
                }
            });
        } else {
            this._location.back();
        }
    }

    //key events
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

    keySave(event: boolean) {
        if (event === true) {
            if (this.isdrcrEqual) {
                this.checkSave();
            } else {
                this.notify.error('Debit and Credit are either zero or not equal.');
            }
        } else {
            return;
        }
    }    private createDetails(item: any = {}) {
        const formGroup = this._fb.group({
            amount: [item.amount ? item.amount : 0, Validators.required],
            credit: [item.credit ? item.credit : 0],
            debit: [item.debit ? item.debit : 0],
            drOrCr: [item.drOrCr ? item.drOrCr : 0, Validators.required],
            chequeNo: [item.chequeNo ? item.chequeNo : ''],
            chequeMiti: [item.chequeMiti ? item.chequeMiti : this.today, Validators.required],
            ledgerId: [{value: item.ledgerId ? item.ledgerId : null, disabled: item.ledgerId}, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
            isBillByBill: [item.isBillByBill ? item.isBillByBill : false],
            partyBalanceDetail: this._fb.group({
                newReferenceAmount: [item.partyBalanceDetail?.newReferenceAmount || 0, [Validators.required]],
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

        return formGroup;
    }

    // Helper methods for party balance detail management

    // Create FormGroup for party balance master structure
    createPartyBalanceMaster(data: any = {}) {
        return this._fb.group({
            newReferenceAmount: [data?.newReferenceAmount || 0, [Validators.required]],
            details: this._fb.array(
                data?.details ? data.details.map(item => this.createpartyBalanceDetail(item)) : []
            )
        });
    }

    // Get details FormArray from a party balance master form
    getPartyBalanceDetails(masterForm: FormGroup): FormArray {
        return masterForm.get('details') as FormArray;
    }

    // Add a new detail to the details FormArray
    addPartyBalanceDetail(masterForm: FormGroup, detailData: any = {}) {
        const detailsArray = this.getPartyBalanceDetails(masterForm);
        detailsArray.push(this.createpartyBalanceDetail(detailData));
    }

    // Remove a detail from the details FormArray
    removePartyBalanceDetail(masterForm: FormGroup, index: number) {
        const detailsArray = this.getPartyBalanceDetails(masterForm);
        if (detailsArray.length > 0) {
            detailsArray.removeAt(index);
        }
    }

    // Get the new reference amount for a specific journal detail
    getNewReferenceAmount(i: number): number {
        const journalDetailControl = this.form.get('journalDetail');
        if (!journalDetailControl?.['controls']?.[i]) {
            return 0;
        }

        const partyBalanceDetailControl = journalDetailControl['controls'][i].get('partyBalanceDetail');
        if (!partyBalanceDetailControl) {
            return 0;
        }

        return partyBalanceDetailControl.get('newReferenceAmount')?.value || 0;
    }
}
