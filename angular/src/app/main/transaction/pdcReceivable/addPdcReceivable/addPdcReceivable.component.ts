import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
    AfterViewInit,
    Component,
    ElementRef,
    Injector,
    OnDestroy,
    OnInit,
    ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PDCReceivablesServiceProxy,
    ReportingServiceProxy,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
    UniversalDropdownDto,
} from '@shared/service-proxies/service-proxies';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Subject, timer } from 'rxjs';
import { finalize, first, takeUntil } from 'rxjs/operators';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-Pdc-receivable',
    templateUrl: './addPdcReceivable.component.html',
    animations: [appModuleAnimation]

})
export class AddPdcReceivableComponent extends AppComponentBase implements OnInit, OnDestroy, AfterViewInit {
    settings: TenantSettingsEditDto = undefined;
    shortcuts: ShortcutInput[] = [];
    formLoader = true;
    saving = false;
    isManual = false;
    automaticInput = false;
    ledgerId: string;
    id: string;
    form: FormGroup;
    title = 'Add PDC Receivable';
    serialNumber = 0;
    isPrint = false;

    allBranchs: UniversalDropdownDto[];
    allLedgers: UniversalDropdownDto[];
    allBanks: UniversalDropdownDto[];
    voucherNo: string;
    // key enter events
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('cheque', { read: ElementRef }) chequeEvent: ElementRef;
    @ViewChild('bank') bankEvent: NgSelectComponent;
    @ViewChild('amount', { read: ElementRef }) amountEvent: ElementRef;
    @ViewChild('voucherNum', { read: ElementRef }) vouchernumEvent: ElementRef;
    @ViewChild('desc', { read: ElementRef }) descEvent: ElementRef;
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        public injector: Injector,
        private fb: FormBuilder,
        private _location: Location,
        private router: Router,
        private _proxy: PDCReceivablesServiceProxy,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        private route: ActivatedRoute,
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
            this._proxy.getPDCReceivableForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                this.formLoader = false;
            });
        }
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
            amount: [item.amount, Validators.required],
            chequeNo: [item.chequeNo, Validators.required],
            chequeMiti: [item.chequeMiti ? item.chequeMiti : this.today, Validators.required],
            description: [item.description],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            bankId: [item.bankId, Validators.required],
            ledgerId: [item.ledgerId, Validators.required],
            id: [item.id],
        });
    }

    changeByBranch() {
        if (!this.id) {
            this.getAllVoucher();
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

    getAllVoucher() {
        this._proxy.getPDCReceivableVoucherNo().subscribe((x) => {
            this.form.get('voucherNo').setValue(x);
        });
    }

    getAllAccountLedgers() {
        // this.formLoader = false;
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allLedgers = data;
            if (!this.id) {
                this.form.get('ledgerId').setValue(this.allLedgers[0].id);
            }
        });

        timer(500)
            .pipe(first())
            .subscribe(() => {
                if (this.id) {
                    this.ledgerEvent.open();
                } else {
                    this.ledgerEvent.open();
                }

            });
    }

    getAllAccountLedger(ledgerId) {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allLedgers = data;
        });
        if (this.serialNumber) {
            this.form.get('ledgerId').setValue(ledgerId);
            this.serialNumber = 0;
            timer(100)
                .pipe(first())
                .subscribe(() => this.ledgerEvent.open());
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
            });
    }

    checkSave() {
        this.saving = true;
        if (this.isManual) {
            const vNo = this.form.get('VoucherNo').value;
            this._proxy.getCheckVoucherNo(vNo).subscribe((data) => {
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
                        this.router.navigate([`app/main/transaction/pdf/7/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/transaction/pdf/7/${data}`]);
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

    keyEventAccountLedger(event) {
        if (event.altKey || event.metaKey) {
            if (event.which == 67) {
                event.preventDefault();
                this.changeAccountledger(event);
            }
        }
    }

    gotoaccLedger(e) {
        e.preventDefault();
        if (e.which === 13) {
            if (this.automaticInput == false) {
                if (!this.id) {
                    this.ledgerEvent.close();
                    this.vouchernumEvent.nativeElement.focus();
                } else {
                    this.ledgerEvent.close();
                    document.getElementById('npDatePicker').focus();
                }
            } else {
                this.ledgerEvent.close();
                document.getElementById('npDatePicker').focus();
            }
        } else if (e.altKey || e.metaKey) {
            if (e.which == 67) {
                this.changeAccountledger(e);
            }
        }
    }

    gofromvoucher() {
        document.getElementById('npDatePicker').focus();
    }

    changeAccountledger(event) {
        if (event && event !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }

    gotoLedger() {
        // this.branchevent.close();
        this.ledgerEvent.open();
    }

    gotoDate() {
        const date = document.getElementById('npDatePicker');
        // let input = date.appendChild(document.getElementById("npDatePicker"));
        // let calender = date.appendChild(document.getElementById("pickerCalender"));
        // calender.focus();
        // calender.style.top = "60px";
        date.focus();
    }

    gotoChequeno() {
        this.chequeEvent.nativeElement.focus();
    }

    gotoBankId() {
        this.bankEvent.open();
    }

    gotoAmount() {
        this.amountEvent.nativeElement.focus();
    }

    gotoDesc() {
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
        if (this.serialNumber) {
            this.serialNumber = 0;
        } else {
            if (event === true) {
                this.close();
            } else {
                return;
            }
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
