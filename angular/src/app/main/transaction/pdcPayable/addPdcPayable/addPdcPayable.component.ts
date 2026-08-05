import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
    AfterViewInit,
    Component,
    ElementRef,
    Injector,
    Input,
    OnDestroy,
    OnInit,
    ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgSelectComponent } from '@ng-select/ng-select';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PDCPayablesServiceProxy,
    ReportingServiceProxy,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';

import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Subject, timer } from 'rxjs';
import { finalize, first, takeUntil } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';

// import { __assign } from 'tslib';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-pdc-payable',
    templateUrl: './addPdcPayable.component.html',
    animations: [appModuleAnimation],
})
export class AddPdcPayableComponent extends AppComponentBase implements OnInit, OnDestroy, AfterViewInit {
    // key enter events Viewchild
    @ViewChild('accLedger') accLedgerEvent: NgSelectComponent;
    @ViewChild('chequeNo', { read: ElementRef }) chequeNoEvent: ElementRef;
    @ViewChild('bankIds') bankIdEvent: NgSelectComponent;
    @ViewChild('vouchernum', { read: ElementRef }) vouchernumEvent: ElementRef;
    @ViewChild('amount', { read: ElementRef }) amountEvent: ElementRef;
    @ViewChild('desc', { read: ElementRef }) descEvent: ElementRef;
    @Input() serialNo: number;
    serialNumber = 0;
    shortcuts: ShortcutInput[] = [];
    // boolean
    formLoader = true;
    saving = false;
    isPrint = false;
    isManual = false;
    automaticInput = false;
    // number
    id: string;
    title = 'Add PDC Payables';

    // form
    form: FormGroup;
    // any

    allBanks: UniversalDropdownDto[];
    allLedgers: UniversalDropdownDto[];
    settings: TenantSettingsEditDto = undefined;
    // string
    voucherNo: string;
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private route: ActivatedRoute,
        private router: Router,
        private _proxy: PDCPayablesServiceProxy,
        private _location: Location,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        private _report: ReportingServiceProxy
    ) {
        super(injector);
        this.getSetting();
        this.createForm();
        this.form.get('dateMiti').setValue(this.today);
        this.changeByBranch();

    }

    ngOnInit(): void {
        this.createForm();
        this.form.get('amount').setValue(0);
        this.getSettingsForPrint();
        this.id = this.route.snapshot.params['id'];
        if (this.id) {
            this._proxy.getPDCPayableForEdit(this.id).subscribe((data) => {
                this.createForm(data);
            });
        }
        timer(3000)
            .pipe(first())
            .subscribe(() => {
                if (this.id) {
                 //   document.getElementById('npDatePicker').focus();
                    // } else if (this.allBranchs.length > 1) {
                    //     this.branchEvent.open();
                    // } else {
                    //     document.getElementById('npDatePicker').focus();
                }

            });
    }

    ngAfterViewInit(): void {
    }

    getSettingsForPrint(): void {
        this._tenantSettingsService.getAllSettings().subscribe((result: TenantSettingsEditDto) => {
            this.settings = result;
            // if (this.settings.allSettingsBundleDto.tickPrintAfterSave == true) {
            //     this.isPrint = true;
            // }
        });
    }

    createForm(item: any = {}) {

        this.form = this.fb.group({
            voucherNo: [item.voucherNo ? item.voucherNo : this.voucherNo, Validators.required],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            amount: [item.amount, Validators.required],
            chequeNo: [item.chequeNo, Validators.required],
            bankId: [item.bankId, Validators.required],
            chequeMiti: [item.chequeMiti ? item.chequeMiti : this.today, Validators.required],
            description: [item.description],
            ledgerId: [item.ledgerId, Validators.required],
            id: [item.id],
        });
    }


    getVoucher() {
        this._proxy.getPDCPayableVoucherNo().subscribe((x) => {
            this.form.get('voucherNo').setValue(x);
        });
    }

    changeByBranch() {
        if (!this.id) {
            this.getVoucher();
            this._proxy.getVoucherGenerateType().subscribe((x) => {
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
        this.getAllAccountLedgers();
        this.getAllBanks();
    }

    getAllAccountLedgers() {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((res) => {
            this.allLedgers = res;
            if (!this.id) {
                this.form.get('ledgerId').setValue(res[0].id);
            }
        });
    }

    getAccountsLedger(ledgerId: string) {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((res) => {
            this.allLedgers = res;
        });
        if (this.serialNumber) {
            this.form.get('ledgerId').setValue(ledgerId);
            this.serialNumber = 0;
            timer(100)
                .pipe(first())
                .subscribe(() => this.accLedgerEvent.open());
        }
    }

    ngOnDestroy(): void {
        // Emit something to stop all Observables
        this.destroy$.next();
        // Complete the notifying Observable to remove it
        this.destroy$.complete();
    }

    getAllBanks(): void {
        // this.formLoader = true;
        this._proxy
            .getAllBankForTableDropdown()
            .pipe(
                finalize(() => (this.formLoader = false)),
                takeUntil(this.destroy$)
            )
            .subscribe((data) => {
                this.allBanks = data;
                if (this.id === undefined) {
                    return;
                } else if (!this.id) {
                    this.form.get('bankId').setValue(this.allBanks[0].id);
                }
            });


    }

    checkSave() {
        this.saving = true;
        if (this.isManual) {
            const vNo = this.form.get('voucherNo').value;
            this._proxy.getCheckVoucherNo(undefined, vNo).subscribe((data) => {
                if (!data) {
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
                        this.router.navigate([`app/main/transaction/pdf/6/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/transaction/pdf/6/${data}`]);
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

    // key enter events

    // gotoDate() {
    //     this.branchEvent.close();
    //     document.getElementById('npDatePicker').focus();
    // }

    gotoaccLedger(e) {
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

    gotoChequeNo(e) {
        e.preventDefault();
        if (e.altKey || e.metaKey) {
            if (e.which == 67) {
                this.changeAccountledger(e);
            }
        } else if (e.which === 13) {
            this.accLedgerEvent.close();
            this.chequeNoEvent.nativeElement.focus();
        }
    }

    changeAccountledger(e) {
        if (e && e !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }

    gotoBankId() {
        this.bankIdEvent.open();
    }

    gotoAmount() {
        this.amountEvent.nativeElement.focus();
    }

    gotDesc() {
        this.descEvent.nativeElement.focus();
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
            this.save();
        } else {
            return;
        }
    }

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
}
