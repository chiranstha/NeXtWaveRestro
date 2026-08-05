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
import { Location, NgClass, NgTemplateOutlet } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountGroupsServiceProxy } from '@shared/service-proxies/service-proxies';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { NgSelectComponent, NgOptionComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { SubHeaderComponent } from '../../../../../shared/common/sub-header/sub-header.component';
import { ButtonBusyDirective } from '../../../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ModalDirective } from 'ngx-bootstrap/modal';
@Component({
    selector: 'app-add-or-edit-account-group',
    templateUrl: './add-or-edit-account-group.component.html',
    styleUrls: ['./add-or-edit-account-group.component.css'],
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
        NgOptionComponent,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class AddOrEditAccountGroupComponent extends AppComponentBase implements OnInit, OnDestroy, AfterContentInit {
    private fb = inject(FormBuilder);
    private _accountGroupsServiceProxy = inject(AccountGroupsServiceProxy);
    private route = inject(ActivatedRoute);
    private _location = inject(Location);
    private cd = inject(ChangeDetectorRef);
    @Output() accountGroupSave: EventEmitter<any> = new EventEmitter<any>();
    @Output() noCard: EventEmitter<any> = new EventEmitter<any>();
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @Input() dialog = false;
    @Input() serialNo: number;
    @Input() card: number;
    @Input() cardId: string;
    @ViewChild('createOrEditModal', { static: false }) modal?: ModalDirective;
    @ViewChild('ledger') ledgerEvent: NgSelectComponent;
    @ViewChild('nature') natureEvent: NgSelectComponent;
    @ViewChild('accountGroupName', { read: ElementRef }) accountGroupNameEvent: ElementRef;
    @ViewChild('affect') affectEvent: NgSelectComponent;
    @ViewChild('narration', { read: ElementRef }) narrationEvent: ElementRef;
    id: string;
    title = 'Create AccountGroup';
    active = true;
    form: FormGroup;
    filledNull: boolean;
    ngfilledNull: boolean;
    saving = false;
    serialNumber = 0;
    formLoader = true;
    accountGroupNameFlag = false;
    accountGroupNature: any = [
        { value: 0, displayName: 'NA' },
        { value: 1, displayName: 'Assets' },
        { value: 2, displayName: 'Expenses' },
        { value: 3, displayName: 'Income' },
        { value: 4, displayName: 'Liablities' },
    ];
    affectGrossProfit = [
        { value: false, displayName: 'No' },
        { value: true, displayName: 'Yes' },
    ];
    abiDisable = true;
    isPrimary: boolean;
    groupUnderIdWhileEdit: string;
    private destroy$: Subject<void> = new Subject<void>();

    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        if (this.dialog) {
            this.active = false;
            this.createForm();
            return;
        }
        this.loadForm(this.route.snapshot.params['id']);
    }
    show(id?: string): void {
        this.dialog = true;
        this.active = true;
        this.loadForm(id);
        this.cd.detectChanges();
        this.modal?.show();
    }
    private loadForm(id?: string): void {
        this.id = id;
        this.title = this.id ? 'Edit Account Group' : 'Create AccountGroup';
        this.formLoader = true;
        this.createForm();
        if (this.id) {
            this.title = 'Edit Account Group';
            this._accountGroupsServiceProxy
                .getAccountGroupForEdit(this.id)
                .pipe(takeUntil(this.destroy$))
                .subscribe((result) => {
                    this.createForm(result);
                    this.groupUnderIdWhileEdit = result.groupUnder;
                    this.showAcc();
                    this.cd.markForCheck();
                });
        } else {
            this.createForm();
            this.showAcc();
        }
        setTimeout(() => {
            this.accountGroupNameEvent?.nativeElement.focus();
        }, 1000);
    }
    ngAfterContentInit(): void {
        this.cd.detectChanges();
    }
    showAcc() {
        this._accountGroupsServiceProxy
            .getAllAccountGroupForTableDropdown()
            .pipe(takeUntil(this.destroy$))
            .subscribe((result) => {
                this.allAccountLedgers = result;
                this.cd.markForCheck();
                if (this.id) {
                    this.allAccountLedgers.forEach((element, index) => {
                        if (element.id === this.groupUnderIdWhileEdit) {
                            if (element.displayName === 'Primary') {
                                this.isPrimary = true;
                            } else {
                                this.isPrimary = false;
                            }
                        }
                        if (element.id === this.emptyguId) {
                            this.allAccountLedgers.splice(index, 1);
                        }
                    });
                } else {
                    this.form.controls['groupUnder'].setValue(this.allAccountLedgers[0].id);
                    if (result[0].displayName === 'Primary') {
                        this.isPrimary = true;
                    } else {
                        this.isPrimary = false;
                    }
                }
                this.formLoader = false;
            });
    }
    changeAccountGroup(event) {
        if (event.displayName === 'Primary') {
            this.isPrimary = true;
        } else {
            this.isPrimary = false;
        }
        const accountGroupId = this.form.controls['groupUnder'].value;
        if (accountGroupId && accountGroupId !== null) {
            this._accountGroupsServiceProxy
                .getAccountGroupForEdit(accountGroupId)
                .pipe(takeUntil(this.destroy$))
                .subscribe((result) => {
                    this.form.controls['nature'].setValue(result.nature);
                    this.form.controls['affectGrossProfit'].setValue(result.affectGrossProfit);
                });
        } else {
            this.form.controls['nature'].setValue(null);
            this.form.controls['affectGrossProfit'].setValue(false);
        }
    }
    createForm(item: any = {}) {
        this.form = this.fb.group({
            name: [item.name, Validators.required],
            narration: [item.narration ? item.narration : ''],
            affectGrossProfit: [item.affectGrossProfit ? item.affectGrossProfit : false, Validators.required],
            nature: [{ value: item.nature ? item.nature : 0, disabled: this.id }, Validators.required],
            groupUnder: [{ value: item.groupUnder ? item.groupUnder : 0, disabled: this.id }, Validators.required],
            id: [item.id ? item.id : this.id],
        });
    }
    keyEventBranch(event) {
        if (event.altKey || event.metaKey) {
            if (event.which === 67) {
                event.preventDefault();
                this.changeBranch(event);
            }
        }
    }
    changeBranch(event: any) {
        if (event && event !== undefined) {
            this.serialNumber = 3;
        } else {
            this.serialNumber = 0;
        }
    }
    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
    save(): void {
        this.saving = false;
        if (this.serialNo) {
            if (this.form.valid) {
                this._accountGroupsServiceProxy
                    .createOrEdit(this.form.getRawValue())
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        }),
                    )
                    .subscribe((data) => {
                        this.createForm();
                        this.notify.info(this.l('Saved Successfully'));
                        this.accountGroupSave.emit(data);
                    });
            } else {
                this.notify.error('Form is invalid !!');
            }
        } else {
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
        this._accountGroupsServiceProxy
            .createOrEdit(this.form.value)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                if (this.id) {
                    this.notify.info(this.l('Updated Successfully'));
                    if (this.dialog) {
                        this.closeDialogAfterAction();
                        return;
                    }
                    this.noCard.emit(null);
                } else {
                    this.notify.info(this.l('Saved Successfully'));
                    if (this.dialog) {
                        this.closeDialogAfterAction();
                        return;
                    }
                    this.noCard.emit(null);
                }
            });
    }
    private closeDialogAfterAction(): void {
        this.modalSave.emit(null);
        this.form?.markAsPristine();
        this.active = false;
        this.modal?.hide();
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
        if (this.dialog) {
            this.active = false;
            this.modal?.hide();
        } else if (this.serialNo) {
            this.accountGroupSave.emit(null);
        } else if (this.card) {
            this.noCard.emit(null);
        } else {
            this._location.back();
        }
        this.ngOnInit();
    }
    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete?'), (isConfirmed) => {
            if (isConfirmed) {
                this._accountGroupsServiceProxy
                    .delete(id)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe(() => {
                        if (this.card) {
                            this.noCard.emit(null);
                            this.notify.success('Deleted Successfully');
                        } else if (this.dialog) {
                            this.notify.success('Deleted Successfully');
                            this.closeDialogAfterAction();
                        } else {
                            this._location.back();
                            this.notify.success('Deleted Successfully');
                        }
                    });
            }
        });
    }
    gotoledger(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.ledgerEvent.open();
        }
    }
    gotoNature(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            if (this.isPrimary) {
                e.preventDefault();
                this.ledgerEvent.close();
                this.natureEvent.open();
            } else {
                this.narrationEvent.nativeElement.focus();
            }
        }
    }
    gotoAffect(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.natureEvent.close();
            this.affectEvent.open();
        } else if (e.which === 8) {
            e.preventDefault();
            this.natureEvent.close();
            this.ledgerEvent.open();
        }
    }
    gotoNarration(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.affectEvent.close();
            this.narrationEvent.nativeElement.focus();
        } else if (e.which === 8) {
            e.preventDefault();
            this.affectEvent.close();
            this.natureEvent.open();
        }
    }
    onSaveOnInput(event) {
        if (event.which === 13) {
            event.preventDefault();
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
    }
}
