import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
    AfterViewInit,
    Component,
    ElementRef,
    EventEmitter,
    Injector,
    Input,
    OnDestroy,
    OnInit,
    Output,
    ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { AccountGroupsServiceProxy, AccountGroupsWithNatureDto } from '@shared/service-proxies/service-proxies';
import { finalize, Subject, takeUntil } from 'rxjs';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'addAccountGroup',
    templateUrl: './addAccountGroups.component.html',
    animations: [appModuleAnimation]
})
export class AddOrEditAccountGroupComponent extends AppComponentBase implements OnInit, OnDestroy, AfterViewInit {
    @Output() accountGroupSave: EventEmitter<any> = new EventEmitter<any>();
    @Input() serialNo!: number;
    @ViewChild('ledger') ledgerEvent!: NgSelectComponent;
    @ViewChild('nature') natureEvent!: NgSelectComponent;
    @ViewChild('accountGroupName', { read: ElementRef }) accountGroupNameEvent!: ElementRef;
    @ViewChild('affectGrossProfit') affectEvent!: NgSelectComponent;
    @ViewChild('narration', { read: ElementRef }) narrationEvent!: ElementRef;
    title = 'Create Account Group';
    id = this.emptyGuId;
    groupUnderIdWhileEdit!: string;
    form!: FormGroup;
    saving = false;
    isPrimary!: boolean;
    accountGroupNameFlag = false;
    allAccountGroups!: AccountGroupsWithNatureDto[];

    accountGroupNature: any = [
        { value: 0, displayName: 'NA' },
        { value: 1, displayName: 'Assets' },
        { value: 2, displayName: 'Expenses' },
        { value: 3, displayName: 'Income' },
        { value: 4, displayName: 'Liablities' },
    ];

    affectGrossProfits = [
        { value: false, displayName: 'No' },
        { value: true, displayName: 'Yes' },
    ];

    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private _proxy: AccountGroupsServiceProxy,
        private _location: Location,
        private route: ActivatedRoute,
        private fb: FormBuilder
    ) {
        super(injector);
     //   this.getSetting();
    }

    ngOnInit(): void {
        this.createForm();
        this.id = this.route.snapshot.params['id'];
        if (this.id) {
            this.title = 'Edit Account Group';
            this._proxy.getAccountGroupForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                this.groupUnderIdWhileEdit = result.groupUnder;
                this.showAcc();
            });
        } else {
            this.showAcc();
        }
    }

    ngAfterViewInit(): void {
        setTimeout(() => this.accountGroupNameEvent?.nativeElement?.focus(), 100);
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            name: [item.name, Validators.required],
            narration: [item.narration ? item.narration : ''],
            affectGrossProfit: [item.affectGrossProfit ? item.affectGrossProfit : false, Validators.required],
            nature: [item.nature ? item.nature : 0, Validators.required],
            groupUnder: [item.groupUnder ? item.groupUnder : 0, Validators.required],
            id: [item.id ? item.id : this.emptyGuId],
        });
    }

    showAcc() {
        this._proxy.getAllAccountGroupForTableDropdown().subscribe((result) => {
            this.allAccountGroups = result;
            if (this.id) {
                this.allAccountGroups.forEach((element, index) => {
                    if (element.id === this.groupUnderIdWhileEdit) {
                        if (element.displayName === 'Primary') {
                            this.isPrimary = true;
                        } else {
                            this.isPrimary = false;
                        }
                    }
                    if (element.id === this.id) {
                        this.allAccountGroups.splice(index, 1);
                    }
                });
            } else {
                this.form.controls['groupUnder'].setValue(this.allAccountGroups[0].id);
                this.form.controls['affectGrossProfit'].setValue(this.allAccountGroups[0].affectGrossProfit);
                this.form.controls['nature'].setValue(this.allAccountGroups[0].nature);
                if (result[0].displayName === 'Primary') {
                    this.isPrimary = true;
                } else {
                    this.isPrimary = false;
                }
            }
        });
    }

    changeAccountGroup(event: { displayName: string; id: string; }) {
        if (event.displayName === 'Primary') {
            this.isPrimary = true;
        } else {
            this.isPrimary = false;
        }
        //  const accountGroupId = this.form.controls['groupUnder'].value;
        const accountGroup = this.allAccountGroups.find((x) => x.id === event.id);
        if (accountGroup != null) {
            this.form.controls['nature'].setValue(accountGroup.nature);
            this.form.controls['affectGrossProfit'].setValue(accountGroup.affectGrossProfit);
        }
        // if (accountGroupId && accountGroupId !== null) {
        //     this._proxy.getAccountGroupForEdit(accountGroupId).subscribe((result) => {
        //         this.form.controls['nature'].setValue(result.nature);
        //         this.form.controls['affectGrossProfit'].setValue(result.affectGrossProfit);
        //     });
        // } else {
        //     this.form.controls['nature'].setValue(null);
        //     this.form.controls['affectGrossProfit'].setValue(false);
        // }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
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
        if (this.serialNo) {
            this.accountGroupSave.emit(null);
        } else {
            this._location.back();
        }
        this.ngOnInit();
    }

    save() {
        this.saving = true;
        this._proxy
            .createOrEdit(this.form.getRawValue())
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => (this.saving = false))
            )
            .subscribe((data) => {
                if (this.serialNo) {
                    this.accountGroupSave.emit(data);
                    this.notify.success('Saved Successfully');
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

    clearForm(id: string | undefined) {
        this.message.confirm('', this.l('Are you sure you want to Delete?'), (isConfirmed) => {
            if (isConfirmed) {
                this._proxy.delete(id).subscribe(() => {
                    this._location.back();
                    this.notify.success('Deleted Successfully');
                });
            }
        });
    }

    onSaveOnInput(event: { which: number; preventDefault: () => void; }) {
        if (event.which === 13) {
            event.preventDefault();
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
    }

    gotoledger(e: { which: number; preventDefault: () => void; }) {
        if (e.which === 13) {
            e.preventDefault();
            this.ledgerEvent.open();
        }
    }

    gotoNature(e: { which: number; preventDefault: () => void; }) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.isPrimary) {
                this.ledgerEvent.close();
                this.natureEvent.open();
            } else {
                this.ledgerEvent.close();
                this.narrationEvent.nativeElement.focus();
            }
        }
    }

    gotoAffect(e: { which: number; preventDefault: () => void; }) {
        if (e.which === 13) {
            e.preventDefault();
            this.natureEvent.close();
            this.affectEvent.open();
        }
    }

    gotoNarration(e: { which: number; preventDefault: () => void; }) {
        if (e.which === 13) {
            e.preventDefault();
            this.affectEvent.close();
            this.narrationEvent.nativeElement.focus();
        }
    }
}
