import {
    AfterContentInit,
    ChangeDetectorRef,
    Component,
    ElementRef,
    EventEmitter,
    Input,
    OnDestroy,
    OnInit,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountLedgersServiceProxy, CreateOrEditAccountLedgerDto } from '@shared/service-proxies/service-proxies';
import { finalize, takeUntil } from 'rxjs/operators';
import { Location, NgClass, AsyncPipe, NgTemplateOutlet } from '@angular/common';
import { NgSelectComponent } from '@ng-select/ng-select';
import { Observable, Subject } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { SubHeaderComponent } from '../../../../shared/common/sub-header/sub-header.component';
import { ButtonBusyDirective } from '../../../../../shared/utils/button-busy.directive';
import { AddOrEditAccountGroupComponent } from '../../accountGroups/accountGroups/add-or-edit-account-group/add-or-edit-account-group.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ModalDirective } from 'ngx-bootstrap/modal';
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
@Component({
    changeDetection: ChangeDetectionStrategy.Eager,
    selector: 'app-add-account-ledgers',
    templateUrl: './add-account-ledgers.component.html',
    styleUrls: ['./add-account-ledgers.component.css'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        ButtonBusyDirective,
        FormsModule,
        ReactiveFormsModule,
        NgClass,
        NgTemplateOutlet,
        ModalDirective,
        NgSelectComponent,
        AddOrEditAccountGroupComponent,
        AsyncPipe,
        LocalizePipe,
    ],
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class AddAccountLedgersComponent extends AppComponentBase implements OnInit, AfterContentInit, OnDestroy {
    private cdr = inject(ChangeDetectorRef);
    private fb = inject(FormBuilder);
    private _accountLedgersServiceProxy = inject(AccountLedgersServiceProxy);
    private _location = inject(Location);
    private route = inject(ActivatedRoute);
    private cd = inject(ChangeDetectorRef);
    @Input() dialog = false;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createOrEditModal', { static: false }) modal?: ModalDirective;
    @ViewChild('ledger', { read: ElementRef }) ledgerEvent: ElementRef;
    @ViewChild('accountgrp') accountgrprEvent: NgSelectComponent;
    @ViewChild('openinbalac', { read: ElementRef }) openinbalacEvent: ElementRef;
    @ViewChild('debit') debitEvent: NgSelectComponent;
    @ViewChild('addes', { read: ElementRef }) addesEvent: ElementRef;
    @Output() accountLedgerSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('csts', { read: ElementRef }) cstsEvent: ElementRef;
    @ViewChild('tinsa', { read: ElementRef }) tinsaEvent: ElementRef;
    @ViewChild('email', { read: ElementRef }) emailEvent: ElementRef;
    @ViewChild('phonesec', { read: ElementRef }) phonesecEvent: ElementRef;
    @ViewChild('sates') satesEvent: NgSelectComponent;
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
    public destroy$ = new Subject<void>();
    serialNumber = 0;
    accountGroupid: string;
    displayDate: any;
    ledgerNameFlag = false;
    mtNine: boolean;
    title = 'Create AccountLedger';
    debitOrCredit: any = [
        { value: 'DR', displayName: 'Debit', id: 0 },
        { value: 'CR', displayName: 'Credit', id: 1 },
    ];
    showBank: boolean;
    accountGroup: CreateOrEditAccountLedgerDto = new CreateOrEditAccountLedgerDto();
    id = null;
    formLoader = true;
    form: FormGroup;
    saving = false;
    isDefault = false;
    active = true;
    consumerTypeId: number;

    province = [
        { id: 1, displayName: 'Province No. 1' },
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
    isSundry = false;

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        if (this.dialog) {
            this.active = false;
            this.createForm();
            return;
        }
        this.loadForm(this.route.snapshot.params['ledgerId']);
    }
    show(id?: string): void {
        this.dialog = true;
        this.active = true;
        this.loadForm(id);
        this.cdr.detectChanges();
        this.modal?.show();
        this.focusLedgerName();
    }
    private loadForm(id?: string): void {
        this.createForm();
        this.id = id || null;
        this.title = this.id ? 'Edit AccountLedger' : 'Create AccountLedger';
        this.ledgerNameFlag = false;
        this.showBank = false;
        this.mtNine = false;
        this.isDefault = false;
        this.isSundry = false;
        this.changeAcc();
        if (this.id) {
            this.title = 'Edit AccountLedger';
            this._accountLedgersServiceProxy.getAccountLedgerForEdit(this.id).subscribe((result) => {
                if (result.isDefault === true) {
                    this.ledgerNameFlag = true;
                }
                if (result.isBank === true) {
                    this.showBank = true;
                }
                this.createForm(result);
                this.isDefault = result.isDefault;
                this.cdr.detectChanges();
                this.focusLedgerName();
                if (result.accountGroupName === 'विभिन्न ऋणदाताहरू' || result.accountGroupName === 'विभिन्न ऋणीहरू') {
                    this.isSundry = true;
                }
            });
        }
    }
    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.unsubscribe();
    }
    ngAfterContentInit() {
        this.cd.detectChanges();
        this.focusLedgerName(0);
    }
    private focusLedgerName(delay = 200): void {
        setTimeout(() => this.ledgerEvent?.nativeElement?.focus(), delay);
    }
    createForm(item: any = {}) {
        this.form = this.fb.group({
            name: [item.name ? item.name : '', Validators.required],
            openingBalance: [item.openingBalance ? item.openingBalance : 0, Validators.required],
            crOrDr: [item.crOrDr ? item.crOrDr : 0, Validators.required],
            openingMiti: [item.openingMiti ? item.openingMiti : this.today],
            interestRate: [item.interestRate ? item.interestRate : 0],
            bankAccountNumber: [item.bankAccountNumber ? item.bankAccountNumber : ''],
            branchName: [item.branchName ? item.branchName : ''],
            branchCode: [item.branchCode ? item.branchCode : ''],
            accountGroupId: [item.accountGroupId ? item.accountGroupId : this.emptyguId],
            id: [item.id ? item.id : this.emptyguId],
            surName: ['ledger'],
            isRequiredUser: [item.isRequiredUser ? item.isRequiredUser : true],
            userName: ['ledger'],
            creditPeriod: [item.creditPeriod ? item.creditPeriod : 0, Validators.required],
            creditLimit: [item.creditLimit ? item.creditLimit : 0, Validators.required],
            mailingName: [item.mailingName ? item.mailingName : ''],
            narration: [item.narration ? item.narration : ''],
            tin: [item.tin],
            cst: [item.cst],
            address: [item.address ? item.address : null],
            state: [item.state ? item.state : 1],
            mobile: [item.mobile ? item.mobile : null],
            phone: [item.phone ? item.phone : null],
            email: [item.email ? item.email : null],
            pan: [item.pan ? item.pan : null],
        });
    }
    changeAcc() {
        this.getAllAccountGroups(this.form.get('accountGroupId').value);
    }

    changeAccountGroups(event) {
        if (event && event !== undefined) {
            this.serialNumber = 2;
        } else {
            this.serialNumber = 0;
        }
    }
    getAllAccountGroups(accountGroupIds) {
        this.accountGroupall$ = this._accountLedgersServiceProxy.getAllAccountGroupForTableDropdown();
        this.accountGroupall$.pipe(takeUntil(this.destroy$)).subscribe((data) => {
            if (this.serialNumber === 0) {
                if (!this.id) {
                    this.form.get('accountGroupId').patchValue(data[0].id);
                    if (data[0].nature === 'Assets' || data[0].nature === 'Expenses') {
                        this.form.get('crOrDr').setValue(0);
                    } else {
                        this.form.get('crOrDr').setValue(1);
                    }
                }
            } else {
                this.form.get('accountGroupId').setValue(accountGroupIds);
                this.serialNumber = 0;
                setTimeout(() => {
                    this.accountgrprEvent.focus();
                }, 300);
            }
        });
    }
    onChangeAccountGroup(event?) {
        this.showBank = false;
        this.mtNine = false;
        this.isSundry = false;

        if (!event) {
            return;
        }
        if (event.displayName === 'विभिन्न ऋणदाताहरू' || event.displayName === 'विभिन्न ऋणीहरू') {
            this.isSundry = true;
        } else if (
            event.isBank === true ||
            event.displayName === 'Bank Account' ||
            event.displayName === 'Bank OD A/C'
        ) {
            this.showBank = true;
            this.mtNine = true;
        } else {
            this.mtNine = false;
        }
        if (event.nature === 'Assets' || event.nature === 'Expenses') {
            this.form.get('crOrDr').setValue(0);
        } else {
            this.form.get('crOrDr').setValue(1);
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
    save() {
        this._accountLedgersServiceProxy
            .createOrEdit(this.form.getRawValue())
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => (this.saving = false)),
            )
            .subscribe((data) => {
                if (this.serialNo) {
                    this.accountLedgerSave.emit(data);
                    this.notify.success('Saved Successfully');
                } else if (this.dialog) {
                    if (this.id) {
                        this.notify.success('Updated Successfully');
                    } else {
                        this.notify.success('Saved Successfully');
                    }
                    this.closeDialogAfterAction();
                } else {
                    this._location.back();
                    if (this.id) {
                        this.notify.success('Updated Successfully');
                    } else {
                        this.notify.success('Saved Successfully');
                    }
                }
            });
    }
    checkSave(position?) {
        let formStatus = this.form.status;
        switch (formStatus) {
            case (formStatus = 'VALID'):
                switch (position) {
                    case 'remarks':
                        this.checkCondition();
                        break;
                    default:
                        this.save();
                        break;
                }
                break;
            default:
                this.notify.error('Form is invalid!!');
                break;
        }
    }
    checkCondition() {
        const idlabel: string = 'Update';
        const label: string = 'Save';
        const combineValue = `${'Do you want to'} ${this.id ? idlabel : label}  ${'?'}`;
        this.message.confirm('', this.l(combineValue), (isConfirmed) => {
            if (isConfirmed) {
                this.save();
            }
        });
    }
    gotoLedger() {
        this.focusLedgerName(0);
    }
    gotoAccountGroup(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.accountgrprEvent.open();
        }
    }
    gotoOpeningBalanc(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            if (this.isSundry) {
                setTimeout(() => this.addesEvent?.nativeElement?.focus());
            } else {
                this.openinbalacEvent?.nativeElement?.focus();
            }
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
                e.preventDefault();
                this.changeAccountGroups(e);
            }
        }
    }
    gotodebit(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.debitEvent.open();
        }
    }
    condition() {
        if (this.id) {
            this.save();
        } else {
            this.message.confirm('', this.l('Do you want to Save ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.save();
                }
            });
        }
    }
    saves(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            if (this.showBank === true) {
                this.debitEvent.close();
                this.bankacsEvent.nativeElement.focus();
            } else {
                this.condition();
            }
        }
    }
    gotoBankBranchName(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.bbranchnameEvent.nativeElement.focus();
        }
    }
    gotoBankCode(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.bbcodeEvent.nativeElement.focus();
        }
    }
    gotoAddress(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            if (this.showBank === true && !this.isSundry) {
                this.condition();
            } else if (this.isSundry === true) {
                this.addesEvent.nativeElement.focus();
            }
        }
    }
    gotoPrimaryphone(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.primarypEvent.nativeElement.focus();
        }
    }
    gotoPan(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.panEvent.nativeElement.focus();
        }
    }
    gotoOpeenigb(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.oppdEvent.nativeElement.focus();
        }
    }
    gotoDrCr(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.drcrEvent.open();
        }
    }
    gotobillByBill(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.drcrEvent.close();
            this.billbybillrEvent.open();
        }
    }
    gotoCredit(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.billbybillrEvent.close();
            this.creditperiodEvent.nativeElement.focus();
        }
    }
    gotoLimit(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.creditlimitEvent.nativeElement.focus();
        }
    }
    gotoCustomer(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.consumerTypeEvent.open();
        }
    }
    gotoStates(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.consumerTypeEvent.close();
            this.satesEvent.open();
        } else if (e.altKey || e.metaKey) {
            if (e.which === 67) {
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
        if (e.key === 'Enter') {
            e.preventDefault();
            this.satesEvent.close();
            this.phonesecEvent.nativeElement.focus();
        }
    }
    gotoEmails(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.emailEvent.nativeElement.focus();
        }
    }
    gotoTin(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.tinsaEvent.nativeElement.focus();
        }
    }
    gotoCst(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.cstsEvent.nativeElement.focus();
        }
    }
    saveSundary(e) {
        if (e.key === 'Enter') {
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
    close() {
        if (this.form.valid || this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    if (this.serialNo) {
                        this.accountLedgerSave.emit(null);
                    } else if (this.dialog) {
                        this.active = false;
                        this.modal?.hide();
                    } else {
                        this._location.back();
                    }
                }
            });
        } else {
            if (this.serialNo) {
                this.accountLedgerSave.emit(null);
            } else if (this.dialog) {
                this.active = false;
                this.modal?.hide();
            } else {
                this._location.back();
            }
        }
    }
    forEachEscape() {
        this.form.reset();
        if (this.serialNo) {
            this.accountLedgerSave.emit(null);
        } else if (this.dialog) {
            this.active = false;
            this.modal?.hide();
        } else {
            this._location.back();
        }
        this.ngOnInit();
    }
    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._accountLedgersServiceProxy
                    .delete(id)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe(() => {
                        this.notify.success('DeletedSuccessfully');
                        if (this.dialog) {
                            this.closeDialogAfterAction();
                        } else {
                            this._location.back();
                        }
                    });
            }
        });
    }
    private closeDialogAfterAction(): void {
        this.modalSave.emit(null);
        this.form?.markAsPristine();
        this.active = false;
        this.modal?.hide();
    }
}
