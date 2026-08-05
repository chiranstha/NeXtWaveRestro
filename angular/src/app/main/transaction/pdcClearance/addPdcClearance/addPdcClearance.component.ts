import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
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
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    PDCClearanceDetailDto,
    PDCClearancesServiceProxy,
    ReportingServiceProxy,
    TenantSettingsEditDto,
    TenantSettingsServiceProxy,
} from '@shared/service-proxies/service-proxies'; import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Observable, Subject, timer } from 'rxjs';
import { finalize, first, takeUntil } from 'rxjs/operators';
import { NgSelectComponent } from '@ng-select/ng-select';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-pcd-clearance',
    templateUrl: './addPdcClearance.component.html',
})
export class AddPdcClearanceComponent extends AppComponentBase implements OnInit, OnDestroy {
    // key enter events Viewchild
    @ViewChild('accledger') accLedgerEvent: NgSelectComponent;
    @ViewChild('againstvoucher') againstvoucherevent: NgSelectComponent;
    @ViewChild('vouchernum', { read: ElementRef }) vouchernumEvent: ElementRef;
    @ViewChild('pdc') pdcevent: NgSelectComponent;
    @ViewChild('amount', { read: ElementRef }) amountevent: ElementRef;
    @ViewChild('bankid') bankIdEvent: NgSelectComponent;
    @ViewChild('status') statusEvent: NgSelectComponent;
    @ViewChild('againstBank') againstBank: NgSelectComponent;
    @ViewChild('pdcPayable') pdcPayableEvent: NgSelectComponent;
    @ViewChild('receivables') receivablesEvent: NgSelectComponent;
    @ViewChild('desc', { read: ElementRef }) descEvent: ElementRef;
    @Input() serialNumber: number;
    shortcuts: ShortcutInput[] = [];
    againstMode = [
        { id: 0, displayName: 'PDC Payable' },
        { id: 1, displayName: 'PDC Receivable' },
    ];
    against = [
        { id: 0, displayName: 'Cleared' },
        { id: 1, displayName: 'Bounced' },
    ];
    settings: TenantSettingsEditDto = undefined;
    againstId: any;
    pdcByData: any;
    pdcDetails: PDCClearanceDetailDto[];
    allLedgers: any;
    allPayables: any;
    allReceivables: any;
    allBanks$: Observable<any>;
    allCashBanks$: Observable<any>;
    form: FormGroup;
    formLoader = true;
    saving = false;
    isManual = false;
    automaticInput = false;
    isPrint = false;
    displayProduct: number;
    id: string;
    ledgerId: string;
    voucherNo: string;
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private _location: Location,
        private route: ActivatedRoute,
        private router: Router,
        private _proxy: PDCClearancesServiceProxy,
        private _tenantSettingsService: TenantSettingsServiceProxy,
        private _report: ReportingServiceProxy
    ) {
        super(injector);
        this.createForm();
        this.getSetting();
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.createForm();
        this.form.get('amount').setValue(0);
        this.getSettingsForPrint();
        if (this.id) {
            this._proxy.getPDCClearanceForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                this.againstId = result.voucherName;
                this.changeByBranch();
            });
        }
        this.changeByBranch();
        this.getAllAccountLedgers();
        this.form.get('againstMode').setValue(0);
        this.getAllCashBanks();
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
            againstMode: [item.againstMode ? item.againstMode : 0, Validators.required],
            description: [item.description],
            status: [item.status, Validators.required],
            dateMiti: [item.dateMiti ? item.dateMiti : this.today, Validators.required],
            amount: [item.amount, Validators.required],
            chequeNo: [item.chequeNo, Validators.required],
            chequeMiti: [item.chequeMiti, Validators.required],
            bankId: [item.bankId, Validators.required],
            voucherName: [item.voucherName, Validators.required],
            ledgerId: [item.ledgerId, Validators.required],
            againstLedgerId: [item.againstLedgerId ? item.againstLedgerId : this.emptyGuId, Validators.required],
            againstId: [item.againstId, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
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

        this.getallBanks();
    }

    getVoucher() {
        this._proxy.getPDCClearanceVoucherNo().subscribe((x) => {
            this.form.get('voucherNo').setValue(x);
        });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    getPdcByLedger() {
        this._proxy
            .getPDCByLedgerId(this.form.get('againstMode').value, this.form.get('ledgerId').value)
            .subscribe((data) => {
                this.pdcByData = data;
            });
    }

    changeByPdc(_id?: string) {
        this.getPdcDetails();
    }

    getPdcDetails() {
        this._proxy
            .getPDCDetails(
                this.form.get('againstMode').value,
                this.form.get('ledgerId').value,
                this.form.get('againstId').value
            )
            .subscribe((data) => {
                this.pdcDetails = data;
                this.form.get('amount').setValue(this.pdcDetails[0].amount);
                this.form.get('chequeNo').setValue(this.pdcDetails[0].chequeNo);
                this.form.get('voucherName').setValue(this.pdcDetails[0].voucherName);
                this.form.get('bankId').setValue(this.pdcDetails[0].bankId);
                this.form.get('chequeMiti').setValue(this.pdcDetails[0].chequeMiti);
            });
    }

    changeByLedger(_id?: string) {
        this.getPdcByLedger();
        this.form.get('againstId').reset();
    }

    checkSave() {
        this.saving = true;
        if (this.isManual) {
            const vNo = this.form.get('voucherNo').value;
            this._proxy.getCheckVoucherNo(vNo).subscribe((data) => {
                if (!data) {
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
                        this.router.navigate([`app/main/transaction/pdf/4/${this.id}`]);
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.router.navigate([`app/main/transaction/pdf/4/${data}`]);
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

    gotoaccLedger() {
        this.accLedgerEvent.open();
    }

    gotoPDC(e) {
        e.preventDefault();
        if (this.id) {
            if (e.which === 13) {
                this.amountevent.nativeElement.focus();
            }
        } else {
            if (e.which === 13) {
                if (this.automaticInput == false) {
                    if (!this.id) {
                        this.vouchernumEvent.nativeElement.focus();
                    } else {
                        this.pdcevent.open();
                    }
                } else {
                    this.pdcevent.open();
                }
            }
        }
    }

    gofromvoucher() {
        this.pdcevent.open();
    }

    gotoagainstvoucher(e) {
        e.preventDefault();
        if (e.which === 13) {
            this.accLedgerEvent.close();
            this.againstvoucherevent.open();
        } else if (e.altKey || e.metaKey) {
            if (e.which == 67) {
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

    gotoamount() {
        this.againstvoucherevent.close();
        this.amountevent.nativeElement.focus();
    }

    gotostatus() {
        this.statusEvent.open();
    }

    gotoBankId(e) {
        if (e.altKey && e.which === 67) {
            e.preventDefault();
            this.serialNumber = 1;
        }
        this.bankIdEvent.open();
    }

    gotoStatus() {
        this.statusEvent.open();
    }

    gotopdc() {
        const id = this.form.get('status').value;
        if (id === 0) {
            this.pdcPayableEvent.open();
        } else {
            this.receivablesEvent.open();
        }
    }

    gotodescription() {
        this.statusEvent.close();
        this.descEvent.nativeElement.focus();
    }

    gotodesoragainstBank() {
        if (this.form.value.againstMode == 1 && this.form.value.status == 0) {
            this.againstBank.open();
        } else {
            this.statusEvent.close();
            this.descEvent.nativeElement.focus();
        }
    }

    gotDesc() {
        this.descEvent.nativeElement.focus();
    }

    onSaveOnInput(e: any) {
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

    getAllAccountLedgers() {
        this._proxy
            .getAllAccountLedgerForTableDropdown()
            .pipe(
                finalize(() => (this.formLoader = false)),
                takeUntil(this.destroy$)
            )
            .subscribe((data) => {
                this.allLedgers = data;
            });
        timer(1000)
            .pipe(first())
            .subscribe(() => {

            });
    }

    clearFunc() {
        this.form.get('ledgerId').reset();
        this.form.get('againstId').reset();
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

    getAllAccountLedger(ledgerId) {
        this._proxy.getAllAccountLedgerForTableDropdown().subscribe((data) => {
            this.allLedgers = data;
        });
        if (this.serialNumber) {
            this.form.get('ledgerId').setValue(ledgerId);
            this.serialNumber = 0;
            timer(100)
                .pipe(first())
                .subscribe(() => this.accLedgerEvent.open());
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

    getallBanks() {
        this.allBanks$ = this._proxy.getAllBankForTableDropdown();
    }

    getAllCashBanks() {
        this.allCashBanks$ = this._proxy.getAllCashOrBankForTableDropdown();
    }
}
