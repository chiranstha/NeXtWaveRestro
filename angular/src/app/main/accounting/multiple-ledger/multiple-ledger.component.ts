import {
    AfterContentInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    ElementRef,
    OnInit,
    QueryList,
    ViewChild,
    ViewChildren,
    inject,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import {
    FormArray,
    FormBuilder,
    FormControl,
    FormGroup,
    Validators,
    FormsModule,
    ReactiveFormsModule,
} from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountLedgersServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { Location, NgClass, JsonPipe } from '@angular/common';
import { NgSelectComponent } from '@ng-select/ng-select';
import { Observable } from 'rxjs';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { AddOrEditAccountGroupComponent } from '../accountGroups/accountGroups/add-or-edit-account-group/add-or-edit-account-group.component';
@Component({
    changeDetection: ChangeDetectionStrategy.Eager,
    selector: 'app-multiple-ledger',
    templateUrl: './multiple-ledger.component.html',
    styleUrls: ['./multiple-ledger.component.css'],
    imports: [
        ButtonBusyDirective,
        FormsModule,
        ReactiveFormsModule,
        NgClass,
        NgSelectComponent,
        AddOrEditAccountGroupComponent,
        JsonPipe,
    ],
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class MultipleLedgerComponent extends AppComponentBase implements OnInit, AfterContentInit {
    private cdr = inject(ChangeDetectorRef);
    private _location = inject(Location);
    private masterProxy = inject(AccountLedgersServiceProxy);
    private _fb = inject(FormBuilder);
    private cd = inject(ChangeDetectorRef);
    @ViewChild('accountGroup') accountGroupEvent: NgSelectComponent;
    @ViewChild('government') governmentEvent: NgSelectComponent;
    @ViewChildren('openingBalance', { read: ElementRef }) openingBalanceEvent: QueryList<ElementRef>;
    @ViewChildren('drCr') drCrEvent: QueryList<NgSelectComponent>;
    @ViewChildren('creditPeriod', { read: ElementRef }) creditPeriodEvent: QueryList<ElementRef>;
    @ViewChildren('creditLimit', { read: ElementRef }) creditLimitEvent: QueryList<ElementRef>;
    @ViewChildren('bankAc', { read: ElementRef }) bankAcEvent: QueryList<ElementRef>;
    @ViewChildren('panno', { read: ElementRef }) pannoEvent: QueryList<ElementRef>;
    @ViewChildren('mobno', { read: ElementRef }) mobnoEvent: QueryList<ElementRef>;
    @ViewChildren('ledger', { read: ElementRef }) ledgerEvent: QueryList<ElementRef>;
    tableRows: any;
    displayedRows: any[];
    searchTerm = '';
    totalItems = 0;
    confirmationReason: string;
    myForm: FormGroup;
    form: FormGroup;
    id: string;
    saving = false;

    serialNumber = 0;
    drOrcr = [
        { name: 'Dr', value: 0 },
        { name: 'Cr', value: 1 },
    ];

    get ledgerList() {
        return this.form.get('ledgerList') as FormArray;
    }
    createForm(item: any = {}) {
        this.form = this._fb.group({
            accountGroupId: [item.accountGroupId, Validators.required],
            consumerTypeId: [item.consumerTypeId ? item.consumerTypeId : this.emptyguId],
            ledgerList: this._fb.array(
                (() => {
                    if (!item.ledgerList) {
                        return [this.createDetails()];
                    }
                    return item.ledgerList.map((item) => this.createDetails(item));
                })(),
            ),
        });
    }
    getDetails(form) {
        return form.controls.ledgerList.controls;
    }
    keyEventsReceipt(): void {
        this.enableAllDetails();
        if (this.form.get('ledgerList').valid) {
            for (const control of this.ledgerList.controls) {
                control.disable();
            }
            this.addDetailsForm();
        }
    }
    ngAfterContentInit(): void {}
    enableAllDetails() {
        this.ledgerList.controls.forEach((element) => {
            element.enable();
        });
    }
    addDetailsForm() {
        const control = <FormArray>this.form.controls.ledgerList;
        control.push(this.createDetails());
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.createForm();
        this.changeAcc(this.id);
        this.myForm = this._fb.group({
            nameInput: new FormControl(),
        });
    }
    searchFilter(e) {
        const searchStr = e.target.value;
        if (e) {
            this.displayedRows = this.tableRows.filter((type) => {
                return type.accountGroupName.toLowerCase().search(searchStr.toLowerCase()) !== -1;
            });
        } else {
            this.displayedRows = this.tableRows;
        }
    }
    resetSearch(): void {
        this.displayedRows = this.tableRows;
        this.searchTerm = '';
    }
    changeAcc(id) {
        this.getAccountGroup(id);
    }
    getAccountGroup(id: string) {
        this.allAccountGroups$ = this.masterProxy.getAllAccountGroupForTableDropdown();
        if (this.serialNumber) {
            this.serialNumber = 0;
            this.form.get('consumerTypeId').setValue(id);
        } else {
            if (!this.id) {
                this.allAccountGroups$.subscribe((data) => {
                    this.form.get('accountGroupId').setValue(data[0].id);
                });
            }
        }
    }
    selectDrCr(id, abcd) {
        if (id == 0) {
            abcd.get('drOrCr').setValue(0);
        } else {
            abcd.get('drOrCr').setValue(1);
        }
    }
    enableDetailsFormGroup(i, form) {
        this.ledgerList.disable();
        const control = form.controls.ledgerList.controls[i];
        control.enable();
    }
    removeDetailsForm(i, form) {
        const control = form.controls.ledgerList;
        if (control.length > 1) {
            control.removeAt(i);
        }
    }
    save() {
        this.saving = false;
        if (this.serialNumber == 0) {
            if (this.form.valid) {
                this.saving = true;
                this.masterProxy
                    .createMultipleAccountLedger(this.form.getRawValue())
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        }),
                    )
                    .subscribe(() => {
                        this.notify.info(this.l('Saved Successfully'));
                        this._location.back();
                    });
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }
    gotAccountGroup(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.accountGroupEvent.open();
        }
    }
    gotoConsumerType(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.accountGroupEvent.close();
            this.governmentEvent.open();
        }
    }
    gotoLedger(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.governmentEvent.close();
            this.ledgerEvent.first.nativeElement.focus();
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
    gotoDrCr(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.drCrEvent.first.open();
            } else {
                this.drCrEvent.last.open();
            }
        }
    }
    gotoOpeningBlnc(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.drCrEvent.first.close();
                this.openingBalanceEvent.first.nativeElement.focus();
            } else {
                this.drCrEvent.last.close();
                this.openingBalanceEvent.last.nativeElement.focus();
            }
        }
    }
    gotoCreditPeriod(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.creditPeriodEvent.first.nativeElement.focus();
            } else {
                this.creditPeriodEvent.last.nativeElement.focus();
            }
        }
    }
    gotoCreditLimit(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.creditLimitEvent.first.nativeElement.focus();
            } else {
                this.creditLimitEvent.last.nativeElement.focus();
            }
        }
    }
    gotoBankAc(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.bankAcEvent.first.nativeElement.focus();
            } else {
                this.bankAcEvent.last.nativeElement.focus();
            }
        }
    }
    gotoPanNo(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.pannoEvent.first.nativeElement.focus();
            } else {
                this.pannoEvent.last.nativeElement.focus();
            }
        }
    }
    gotoMobNo(event, i) {
        if (event.which === 13) {
            event.preventDefault();
            if (i === 0) {
                this.mobnoEvent.first.nativeElement.focus();
            } else {
                this.mobnoEvent.last.nativeElement.focus();
            }
        }
    }
    goNext() {
        for (const control of this.ledgerList.controls) {
            control.disable();
        }
        this.keyEventsReceipt();
        setTimeout(() => {
            this.ledgerEvent.last.nativeElement.focus();
        });
    }
    chekCondtion(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.goNext();
        } else if (e.key === 'Shift') {
            e.preventDefault();
            this.save();
        }
    }
    keyClose(event: boolean) {
        if (event === true) {
            this.close();
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
    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.closeop();
                }
            });
        } else {
            this._location.back();
        }
    }
    closeop() {
        this._location.back();
        this.ngOnInit();
    }
    private createDetails(item: any = {}) {
        return this._fb.group({
            ledgerName: [item.ledgerName ? item.ledgerName : '', Validators.required],
            drOrCr: [item.drOrCr ? item.drOrCr : 0, Validators.required],
            openingBalance: [item.openingBalance ? item.openingBalance : 0, Validators.required],
            creditPeriod: [item.creditPeriod ? item.creditPeriod : 0, Validators.required],
            bankACNo: [item.bankAccountNumber],
            panNumber: [item.panNumber ? item.panNumber : ''],
            mobileNo: [item.mobileNo ? item.mobileNo : ''],
            creditLimit: [item.creditLimit ? item.creditLimit : 0, Validators.required],
        });
    }
}
