import { ChangeDetectionStrategy, Component, ElementRef, Injector, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ContraMastersServiceProxy,
    CreateOrEditContraMasterDto,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';

import { Location } from '@angular/common';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { first, Observable, timer } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { NgSelectComponent } from '@ng-select/ng-select';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    animations: [appModuleAnimation],
    selector: 'app-add-contra-master',
    templateUrl: './addContraMasters.component.html',
    // styleUrls: ['./add-contra-master.component.css'],
})
export class AddContraMasterComponent extends AppComponentBase implements OnInit {
    @ViewChild('accLedger') accLedgerEvent: NgSelectComponent;
    @ViewChild('narration', { read: ElementRef }) narrationEvent: ElementRef;
    @ViewChild('vouchernum', { read: ElementRef }) vouchernumEvent: ElementRef;
    @ViewChildren('tableLedger') tableLedger: QueryList<NgSelectComponent>;
    @ViewChildren('amount', { read: ElementRef }) amountEvent: QueryList<ElementRef>;
    @ViewChildren('chequeno', { read: ElementRef }) chequenoEvent: QueryList<ElementRef>;
    ledgerBalanceStatus$: Observable<string>;
    cashOrBank$: Observable<UniversalDropdownDto[]>;
    contraMaster: CreateOrEditContraMasterDto = new CreateOrEditContraMasterDto();
    allLedgers$: Observable<UniversalDropdownDto[]>;
    shortcuts: ShortcutInput[] = [];
    form: FormGroup;
    formDetailRowId: number;
    balance: string;
    serialNumber = 0;
    id: string;
    title = 'Add Contra Master';
    active = false;
    saving = false;
    formLoader = true;
    automaticInput = false;
    isManual = false;
    LedgerName = '';

    voucherNo: string;
    radioInput = [
        { name: 'Deposit', value: 0 },
        { name: 'WithDraw', value: 1 },
    ];

    constructor(
        private fb: FormBuilder,
        injector: Injector,
        private route: ActivatedRoute,
        private _location: Location,
        private router: Router,
        private _contraMastersServiceProxy: ContraMastersServiceProxy
    ) {
        super(injector);
        this.getSetting();
    }

    get contraDetails() {
        return this.form.get('contraDetails') as FormArray;
    }

    public formIsValid = () => false;

    amountCalculation(event?) {
        let totalAmount = 0;
        for (let i = 0; i < this.contraDetails.length; i++) {
            totalAmount = this.contraDetails.controls[i].get('amount').value + totalAmount;
        }
        this.form.get('totalAmount').setValue(totalAmount);
        if (event.which === 13) {
            event.preventDefault();
        }
    }

    ngOnInit() {
        this.id = this.route.snapshot.params['id'];
        this.createForm();
        this.getAccountsLedger();
        this.getAllCashOrBank();

        this.getBid();
        if (!this.id) {
            this.contraMaster = new CreateOrEditContraMasterDto();
            this.contraMaster.id = this.id;
            this.LedgerName = '';
            setTimeout(() => {

                this.getVoucherNumber();
            }, 500);

        } else {
            const contra$ = this._contraMastersServiceProxy.getContraMasterForEdit(this.id);
            contra$.pipe(first()).subscribe((data) => {
                this.title = 'Edit Contra Master';
                this.createForm(data);
                this.disableControls();
                this.getBid();
            });
        }
    }

    createForm(item: any = {}) {

        this.form = this.fb.group({
            type: [item.type ? item.type : 0],
            voucherNo: [item.voucherNo ? item.voucherNo : this.voucherNo, Validators.required],
            narration: [item.narration ? item.narration : ''],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            ledgerId: [item.ledgerId, Validators.required],
            totalAmount: [item.totalAmount ? item.totalAmount : 0, Validators.required],
            contraDetails: this.fb.array(
                (() => {
                    if (!item.contraDetails) {
                        return [this.createcontraDetails()];
                    }
                    return item.contraDetails.map((item) => this.createcontraDetails(item));
                })()
            ),
            id: [item.id ? item.id : this.emptyGuId],
        });
    }

    getAllCashOrBank() {
        this.cashOrBank$ = this._contraMastersServiceProxy.getAllCashOrBankForTableDropdown();
        this.cashOrBank$.pipe(first()).subscribe((data) => {
            this.form.get('ledgerId').setValue(data[0].id);
        });
    }

    insertDate(abcd, e) {

        if (e) {
            abcd.get('chequeMiti').setValue(this.today);
        }
    }

    getcontraDetails(form) {
        return form.controls.contraDetails.controls;
    }

    removecontraDetailsForm(i, form) {
        const control = form.controls.contraDetails;
        if (control.length > 1) {
            control.removeAt(i);
        }
        this.amountCalculation();
    }

    keyEventsContra(): void {
        this.enableAllcontraDetails();
        if (this.form.get('contraDetails').valid) {
            this.disableControls();
            this.addcontraDetailsForm();
        }
    }

    disableControls() {
        for (const control of this.contraDetails.controls) {
            control.disable();
        }
    }

    addReceiptDetails() {
        this.keyEventsContra();
        timer(0)
            .pipe(first())
            .subscribe(() => this.tableLedger.last.open());
    }

    isFormValid(): boolean {
        return this.form.disabled ? true : this.form.valid;
    }

    addcontraDetailsForm() {
        const control = <FormArray>this.form.controls.contraDetails;
        control.push(this.createcontraDetails());
    }

    enablecontraDetailsFormGroup(i, form) {
        // this.middleArrInx = i;
        this.contraDetails.disable();
        const control = form.controls.contraDetails.controls[i];
        control.enable(i);
        timer(100)
            .pipe(first())
            .subscribe(() => {
                this.tableLedger.toArray()[i].open();
            });
        this.getledgerBalanceStatus(control.get('ledgerId').value);
    }

    enableAllcontraDetails() {
        this.contraDetails.controls.forEach((element) => {
            element.enable();
        });
    }

    changeAccountledger(event) {
        if (event && event !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }

    changeProduct(event: any) {
        if (event && event !== undefined) {
            this.serialNumber = 4;
        } else {
            this.serialNumber = 0;
        }
    }

    getAccountsLedger(ledgerId?: number) {
        this.allLedgers$ = this._contraMastersServiceProxy.getAllAccountLedgerForTableDropdown();
        if (this.serialNumber) {
            this.contraDetails.controls[this.formDetailRowId].get('ledgerId').setValue(ledgerId);
            this.serialNumber = 0;
            timer(100)
                .pipe(first())
                .subscribe(() => this.tableLedger.toArray()[this.formDetailRowId].focus());
        } else {
            this.formLoader = false;
        }
    }

    save() {
        this.saving = false;
        if (this.serialNumber == 0) {
            if (this.form.valid) {
                if (this.id) {
                    this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.createapi();
                        }
                    });
                } else {
                    this.createapi();
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    createapi() {
        this.saving = true;
        this._contraMastersServiceProxy
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
                        this.router.navigate([`app/main/transaction/pdf/0/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/transaction/pdf/0/${data}`]);
                    }
                } else {
                    if (this.id) {
                        this._location.back();
                        this.notify.info(this.l('Updated Successfully'));
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this._location.back();
                    }
                }
            });
    }

    getVoucherNumber() {
        const voucher$ = this._contraMastersServiceProxy.getContraMasterVoucherNo();
        voucher$.pipe(first()).subscribe((data) => {
            this.form.get('voucherNo').setValue(data);
        });
    }

    getBid() {
        if (!this.id) {
            this.getVoucherNumber();
            const voucherType$ = this._contraMastersServiceProxy.getVoucherGenerateType();
            voucherType$.pipe(first()).subscribe((x) => {
                if (x == 'Automatic') {
                    this.automaticInput = true;
                } else {
                    this.automaticInput = false;
                    if (x == 'Manual') {
                        this.isManual = true;
                    } else {
                        this.isManual = false;
                    }
                }
            });
        }
    }

    async checkandSave() {
        if (this.isManual) {
            const vNo = this.form.get('voucherNo').value;
            this._contraMastersServiceProxy.getCheckVoucherNo(undefined, vNo).subscribe((value) => {
                if (!value) {
                    this.save();
                } else {
                    alert('Voucher Number Already Exisits !!!');
                }
            });
        } else {
            this.save();
        }
    }

    keyClose(event: boolean) {
        if (event === true) {
            if (this.serialNumber) {
                this.serialNumber = 0;
            } else {
                this.close();
            }
        } else {
            return;
        }
    }

    keySave(event: boolean) {
        if (event === true) {
            this.checkandSave();
        } else {
            return;
        }
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

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), async (isConfirmed) => {
            if (isConfirmed) {
                this._contraMastersServiceProxy.delete(id).subscribe(() => {
                    this.notify.success(this.l('Successfully Deleted'));
                    this._location.back();
                });
            }
        });
    }

    // shift event (when leaving last field of table)
    gotoDate() {
        // this.branchEvent.close();
        document.getElementById('npDatePicker').focus();
    }

    gotoAccLeder(e) {
        e.preventDefault();
        if (e.which === 13) {
            if (this.automaticInput == false) {
                if (!this.id) {
                    this.vouchernumEvent.nativeElement.focus();
                } else {
                    this.accLedgerEvent.open();
                }
            } else {
                this.accLedgerEvent.open();
            }
        }
    }

    goFromVoucher(e) {
        e.preventDefault();
        if (e.which === 13) {
            this.accLedgerEvent.open();
        }
    }

    gotoTableAccLedger(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.accLedgerEvent.close();
            this.tableLedger.first.open();
        }
    }

    gotoAmout(e, i) {
        e.preventDefault();
        if (e.which === 13) {
            this.tableLedger.toArray()[i].close();
            this.amountEvent.toArray()[i].nativeElement.focus();
        } else if (e.altKey || e.metaKey) {
            if (e.which == 67) {
                e.preventDefault();
                this.formDetailRowId = i;
                this.changeAccountledger(e);
            }
        }
    }

    gotoChequeNo(e, i) {
        if (e.which === 13) {
            e.preventDefault();
            this.chequenoEvent.toArray()[i].nativeElement.focus();
        }
    }

    // gotoNarration(e) {
    //     e.preventDefault();
    //     if (e.key === 'Alt' && this.contraDetails.valid) {
    //         for (let control of this.contraDetails.controls) {
    //             control.disable();
    //         }
    //         timer(0)
    //             .pipe(first())
    //             .subscribe(() => this.narrationEvent.nativeElement.focus());
    //     } else if (e.which === 13) {
    //         this.addReceiptDetails();
    //         this.checkFormArrdisableOnEdit(this.contraDetails, this.middleArrInx);
    //         timer(0)
    //             .pipe(first())
    //             .subscribe(() => this.tableLedger.last.open());
    //     }
    // }

    checkss(e) {
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

    getledgerBalanceStatus(id) {
        this.ledgerBalanceStatus$ = this._contraMastersServiceProxy.getLedgerBalanceStatus(id);
        this.ledgerBalanceStatus$.subscribe((res) => {
            this.balance = res;
        });
    }

    private createcontraDetails(item: any = {}) {
        return this.fb.group({
            amount: [item.amount ? item.amount : 0, Validators.required],
            chequeNo: [item.chequeNo ? item.chequeNo : ''],
            chequeMiti: [item.chequeMiti ? item.chequeMiti : this.today, Validators.required],
            ledgerId: [item.ledgerId, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
        });
    }
}
