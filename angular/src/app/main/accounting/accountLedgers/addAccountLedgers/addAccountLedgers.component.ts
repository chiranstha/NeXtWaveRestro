import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    EventEmitter,
    Injector,
    Input,
    OnInit,
    Output,
    ViewChild,
} from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccountLedgerAccountGroupTableDto,
    AccountLedgersServiceProxy,
    CreateOrEditAccountLedgerDto,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { Location } from '@angular/common';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';

export interface Person {
    id: string;
    isActive: boolean;
    age: number;
    name: string;
    gender: string;
    company: string;
    email: string;
    phone: string;
    disabled?: boolean;
}

type Tabs = 'Necessities' | 'Accounting' | 'Contact' | 'Other' | 'Uniform';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'addAccountLedgers',
    templateUrl: './addAccountLedgers.component.html',
    animations: [appModuleAnimation]
})
export class AddAccountLedgersComponent extends AppComponentBase implements OnInit {
    @ViewChild('ledger', { read: ElementRef }) ledgerEvent: ElementRef;
    @ViewChild('accountgrp') accountgrprEvent: NgSelectComponent;
    @ViewChild('openinbalac', { read: ElementRef }) openinBalancevent: ElementRef;
    @ViewChild('debit') debitEvent: NgSelectComponent;
    @ViewChild('branchs') branchsEvent: NgSelectComponent;
    @ViewChild('addes', { read: ElementRef }) addesEvent: ElementRef;
    @Output() accountLedgerSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('mailingName', { read: ElementRef }) mailingName: ElementRef;
    @ViewChild('email', { read: ElementRef }) emailEvent: ElementRef;
    @ViewChild('phonesec', { read: ElementRef }) phonesecEvent: ElementRef;
    @ViewChild('sates') satesEvent: NgSelectComponent;
    @ViewChild('pricingLevelEvent') pricingLevelEvent: NgSelectComponent;
    @ViewChild('consumerTypes') consumerTypeEvent: NgSelectComponent;
    @ViewChild('creditlimit', { read: ElementRef }) creditlimitEvent: ElementRef;
    @ViewChild('creditperiod', { read: ElementRef }) creditperiodEvent: ElementRef;
    @ViewChild('billbybill') billbybillrEvent: NgSelectComponent;
    @ViewChild('drcr') drcrEvent: NgSelectComponent;
    @ViewChild('opp', { read: ElementRef }) oppdEvent: ElementRef;
    @ViewChild('pan', { read: ElementRef }) panEvent: ElementRef;
    @ViewChild('primaryp', { read: ElementRef }) primarypEvent: ElementRef;
    @ViewChild('bankacs', { read: ElementRef }) bankacsEvent: ElementRef;
    @ViewChild('bbranchname', { read: ElementRef }) bbranchnameEvent: ElementRef;
    @ViewChild('bbcode', { read: ElementRef }) bbcodeEvent: ElementRef;
    @Input('serialNo') serialNo: number;
    @ViewChild('narration', { read: ElementRef }) narrationEvent: ElementRef;
    serialNumber = 0;
    accountGroupId: any;
    mtNine: boolean = false;
    title = 'Create Account Ledger';
    activeTab: Tabs = 'Necessities';
    isBillByBill = [
        { value: true, name: 'Yes' },
        { value: false, name: 'No' },
    ];
    shortcuts: ShortcutInput[] = [];
    debitOrCredit: any = [
        { value: 'DR', displayName: 'Debit', id: 0 },
        { value: 'CR', displayName: 'Credit', id: 1 },
    ];
    showBank: boolean = true;
    accountGroup: CreateOrEditAccountLedgerDto = new CreateOrEditAccountLedgerDto();
    id: string;
    formLoader = true;
    form: FormGroup;
    saving = false;
    isDefault = false;
    accountGroupall: AccountLedgerAccountGroupTableDto[];
    province = [
        { id: 1, displayName: 'Koshi Province' },
        { id: 2, displayName: 'Madhesh Province' },
        { id: 3, displayName: 'Bagmati Province' },
        { id: 4, displayName: 'Gandaki Province' },
        { id: 5, displayName: 'Lumbini Province' },
        { id: 6, displayName: 'Karnali Province' },
        { id: 7, displayName: 'Sudurpashchim Province' },
    ];
    drOrcr = [
        { id: 0, displayName: 'Dr.' },
        { id: 1, displayName: 'Cr.' },
    ];
    yesno = [
        { id: true, displayName: 'Yes' },
        { id: false, displayName: 'No' },
    ];
    isSundry = true;

    constructor(
        injector: Injector,
        private fb: FormBuilder,
        private _accountLedgersServiceProxy: AccountLedgersServiceProxy,
        private _location: Location,
        private route: ActivatedRoute,
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.id = this.route.snapshot.params['id'];
        this.getAllAccountGroups();
        if (this.id) {
            this.title = 'Edit Account Ledger';
            this._accountLedgersServiceProxy.getAccountLedgerForEdit(this.id).subscribe((result) => {
                // if (result.isDefault == true) {
                //     setTimeout(() => {
                //         this.openinBalancevent.nativeElement.focus();
                //     }, 100);
                // }
                if (result.isBank == true) {
                    this.showBank = true;
                }
                this.createForm(result);
                this.getAllAccountGroups();
                this.isDefault = result.isDefault;
                if (result.accountGroupName == 'Sundry Creditors' || result.accountGroupName == 'Sundry Debtors') {
                    this.isSundry = true;
                }
            });
        }

        this.onBill('Sundry Debtors');
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            name: [item.name ? item.name : '', Validators.required],
            openingBalance: [item.openingBalance ? item.openingBalance : 0, Validators.required],
            crOrDr: [item.crOrDr ? item.crOrDr : 0, Validators.required],
            isBillByBill: [item.isBillByBill ? item.isBillByBill : false, Validators.required],
            isCompany: [item.isCompany ? item.isCompany : false],
            accountGroupId: [item.accountGroupId ? item.accountGroupId : this.emptyGuId, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
            surName: ['ledger'],
            isRequiredUser: [item.isRequiredUser ? item.isRequiredUser : true],
            userName: ['ledger'],
            creditPeriod: [item.creditPeriod ? item.creditPeriod : 0, Validators.required],
            creditLimit: [item.creditLimit ? item.creditLimit : 0, Validators.required],
            narration: [item.narration ? item.narration : ''],
            address: [item.address ? item.address : ''],
            phone: [item.phone ? item.phone : '', this.conditionalValidator(/^[9][0-9]{9}$/)], // Conditional pattern validator
            email: [item.email ? item.email : '', this.conditionalValidator(/^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,4}$/i)], // Conditional email validator
            pan: [item.pan ? item.pan : '']
        });
    }

    // Add this custom validator method
    conditionalValidator(pattern: RegExp) {
        return (control: AbstractControl): ValidationErrors | null => {
            if (!control.value || control.value === '') {
                return null; // If empty, return no error
            }
            return pattern.test(control.value) ? null : { pattern: { requiredPattern: pattern.source, actualValue: control.value } };
        };
    }

    changeAccountGroups(event) {
        if (event && event !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }

    getAllAccountGroups() {
        this._accountLedgersServiceProxy.getAllAccountGroupForTableDropdown().subscribe((data) => {
            this.accountGroupall = data;
            if (!this.id) {
                this.form.get('accountGroupId').patchValue(data[0].id);
                if (data[0].nature == 'Assets' || data[0].nature == 'Expenses') {
                    this.form.get('crOrDr').setValue(0);
                } else {
                    this.form.get('crOrDr').setValue(1);
                }
            }
        });
    }

    onBill(value: string) {
        switch (value) {
            case (value = 'Sundry Creditors'):
                this.form.get('isBillByBill').enable();
                this.form.get('isBillByBill').setValue(true);
                this.isSundry = true;
                break;
            case (value = 'Sundry Debtors'):
                this.form.get('isBillByBill').enable();
                this.form.get('isBillByBill').setValue(true);
                this.isSundry = true;
                break;
            default:
                this.form.get('isBillByBill').disable();
                this.form.get('isBillByBill').setValue(false);
                this.isSundry = false;
                break;
        }
    }

    onChangeAccountGroup(event) {
        if (event.isBank == true || event.displayName == 'Bank Account' || event.displayName == 'Bank OD A/C') {
            this.showBank = true;
            this.mtNine = true;
        } else {
            this.showBank = false;
        }
        if (event.nature == 'Assets' || event.nature == 'Expenses') {
            this.form.get('crOrDr').setValue(0);
        } else {
            this.form.get('crOrDr').setValue(1);
        }
        if (event.displayName == 'Sundry Creditors' || event.displayName == 'Sundry Debtors') {
            this.isSundry = true;
            this.mtNine = false;
            this.form.get('isBillByBill').enable();
            this.form.get('isBillByBill').setValue(true);
        } else {
            this.form.get('isBillByBill').disable();
            this.form.get('isBillByBill').setValue(false);
            this.isSundry = false;
        }
    }

    checkToSave() {
        this.message.confirm('', this.l('Do you want to save ?'), (isConfirmed) => {
            if (isConfirmed) {
                this.save();
            } else {
                this.bbcodeEvent.nativeElement.focus();
            }
        });
    }

    save(): void {
        this.saving = false;
        if (this.serialNo) {
            if (this.form.valid) {
                this.saving = true;
                this._accountLedgersServiceProxy
                    .createOrEdit(this.form.value)
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        })
                    )
                    .subscribe((data) => {
                        this.notify.info(this.l('Saved Successfully'));
                        this.accountLedgerSave.emit(data);
                    });
            } else {
                this.notify.error('Form is invalid !!');
            }
        } else {
            if (this.serialNumber == 0) {
                if (this.form.valid) {
                    if (this.id) {
                        this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                            if (isConfirmed) {
                                this.createOrEditapi();
                            }
                        });
                    } else {
                        this.createOrEditapi();
                    }
                } else {
                    this.notify.error('Form is invalid !!');
                }
            }
        }
    }

    createOrEditapi() {
        this.saving = true;
        this._accountLedgersServiceProxy
            .createOrEdit(this.form.value)
            .pipe(
                finalize(() => {
                    this.saving = false;
                })
            )
            .subscribe(() => {
                if (this.id) {
                    this.notify.info(this.l('Updated Successfully'));
                    this._location.back();
                } else {
                    this.notify.info(this.l('Saved Successfully'));
                    this._location.back();
                }
            });
    }

    gotoLedger() {
        this.branchsEvent.close();
        this.ledgerEvent.nativeElement.focus();
    }

    gotoAccountGroup(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.accountgrprEvent.open();
        }
    }

    gotoOpeningBalanc(e) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.isSundry) {
                this.accountgrprEvent.close();
              //  this.bankacsEvent.nativeElement.focus();
            } else {
                this.accountgrprEvent.close();
                //this.openinBalancevent.nativeElement.focus();
            }
        } else if (e.altKey || e.metaKey) {
            if (e.which == 67) {
                e.preventDefault();
                this.changeAccountGroups(e);
            }
        }
    }

    gotodebit(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.debitEvent.open();
        }
    }

    setActiveTab(tab: Tabs) {
        this.activeTab = tab;
    }

    condition() {
        if (this.form.valid) {
            if (this.id) {
                this.save();
            } else {
                this.message.confirm('', this.l('Do you want to Save ?'), (isConfirmed) => {
                    if (isConfirmed) {
                        this.save();
                    }
                });
            }
        } else {
            this.notify.error('Form is invalid !!');
        }

    }

    saves(e) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.showBank == true) {
                this.debitEvent.close();
                this.bankacsEvent.nativeElement.focus();
            } else {
                this.condition();
            }
        }
    }

    gotoBankBranchName(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.bbranchnameEvent.nativeElement.focus();
        }
    }

    gotoBankCode(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.bbcodeEvent.nativeElement.focus();
        }
    }

    gotoAddress(e) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.showBank == true && !this.isSundry) {
                this.condition();
            } else if (this.isSundry == true) {
                this.addesEvent.nativeElement.focus();
            }
        }
    }

    gotoPrimaryphone(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.primarypEvent.nativeElement.focus();
        }
    }

    gotoPan(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.panEvent.nativeElement.focus();
        }
    }

    gotoOpeenigb(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.oppdEvent.nativeElement.focus();
        }
    }

    gotoPricingLevel(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.pricingLevelEvent.open();
        }
    }

    gotoDrCr(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.drcrEvent.open();
        }
    }

    gotobillByBill(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.drcrEvent.close();
            this.billbybillrEvent.open();
        }
    }

    gotoCredit(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.billbybillrEvent.close();
            this.creditperiodEvent.nativeElement.focus();
        }
    }

    gotoLimit(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.creditlimitEvent.nativeElement.focus();
        }
    }

    gotoCustomer(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.consumerTypeEvent.open();
        }
    }

    gotoStates(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.consumerTypeEvent.close();
            this.satesEvent.open();
        } else if (e.altKey || e.metaKey) {
            if (e.which == 67) {
                e.preventDefault();
                this.changeConsumerTypes(e);
            }
        }
    }

    changeConsumerTypes(event) {
        if (event && event !== undefined) {
            this.serialNumber = 3;
        } else {
            this.serialNumber = 0;
        }
    }

    gotoPhoneSec(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.satesEvent.close();
            this.phonesecEvent.nativeElement.focus();
        }
    }

    gotoEmails(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.emailEvent.nativeElement.focus();
        }
    }

    gotoMailing(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.mailingName.nativeElement.focus();
        }
    }

    gotoNarration(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.narrationEvent.nativeElement.focus();
        }
    }

    saveSundary(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.condition();
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

    close(): void {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.forEachEscape();
                }
            });
        } else {
            this.forEachEscape();
        }
    }

    forEachEscape() {
        this.form.reset();
        if (this.serialNo) {
            this.accountLedgerSave.emit(null);
        } else {
            this._location.back();
        }
        this.ngOnInit();
    }

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._accountLedgersServiceProxy.delete(id).subscribe(() => {
                    this._location.back();
                    this.notify.success('DeletedSuccessfully');
                });
            }
        });
    }
}
