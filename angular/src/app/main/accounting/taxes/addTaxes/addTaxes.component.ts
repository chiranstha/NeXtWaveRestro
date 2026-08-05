import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Injector, Input, OnInit, Output, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TaxesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize, takeUntil } from 'rxjs/operators';
import { Location } from '@angular/common';
import { Observable, Subject, Subscription } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'addTaxes',
    templateUrl: './addTaxes.component.html',
    animations: [appModuleAnimation]
})

export class AddTaxesComponent extends AppComponentBase implements OnInit {
    @Input() dialog = false;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createOrEditModal', {static: false}) modal?: ModalDirective;
    @ViewChild('names', {read: ElementRef}) namesEvent: ElementRef;
    @ViewChild('rate', {read: ElementRef}) rateEvent: ElementRef;
    @ViewChild('description', {read: ElementRef}) descriptionEvent: ElementRef;
    @Input() serialNo: number;
    @Output() taxSave: EventEmitter<any> = new EventEmitter<any>();
    formLoader = true;
    active = false;
    saving = false;
    isAddMode = false;
    isactive = true;
    title= 'Create Tax';
    serialNumber = 0;
    taxForm: FormGroup;
    shortcuts: ShortcutInput[] = [];
    taxList: any;
    id: string;

    accountLedgerList$: Observable<any>;
    subscription: Subscription = new Subscription();
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private _taxServiceProxy: TaxesServiceProxy,
        private route: ActivatedRoute,
        private _location: Location,
        private fb: FormBuilder,
        private cdr: ChangeDetectorRef
    ) {
        super(injector);
        this.getSetting();

    }

    ngOnInit(): void {
        if (this.dialog) {
            this.active = false;
            this.createForm();
            this.formLoader = false;
            return;
        }

        this.loadForm(this.route.snapshot.params['id']);
    }

    show(id?: string): void {
        this.dialog = true;
        this.loadForm(id);
    }

    private loadForm(id?: string): void {
        this.formLoader = true;
        this.id = id;
        this.isAddMode = !this.id;
        if (!this.id) {
            this.title = 'Create Tax';
            this.isactive = true;
            this.createForm();
            this.active = true;
            this.formLoader = false;
            this.cdr.markForCheck();
            this.openDialogIfNeeded();
        } else {
            this.title = 'Edit Tax';
            this._taxServiceProxy
                .getTaxForEdit(this.id)
                .pipe(takeUntil(this.destroy$))
                .subscribe((result) => {
                    this.createForm(result);
                    this.active = true;
                    this.formLoader = false;
                    this.cdr.markForCheck();
                    this.openDialogIfNeeded();
                });
        }
        setTimeout(() => {
            this.namesEvent?.nativeElement?.focus();
        }, 1000);
    }

    ngOnDestroy(): void {
        this.subscription.unsubscribe();
        this.destroy$.next();
        this.destroy$.complete();
    }

    createForm(item: any = {}) {
        this.taxForm = this.fb.group({
            name: [item.name ? item.name : '', Validators.required],
            description: [item.description ? item.description : ''],
            rate: [item.rate ? item.rate : 0, Validators.required],
            isActive: [item.isActive !== undefined ? item.isActive : true],
            id: [item.id ? item.id : this.emptyGuId],
        });
    }


    gotoRate(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.rateEvent.nativeElement.focus();
        }
    }

    gotoDescription(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.descriptionEvent.nativeElement.focus();
        }
    }

    onSaveOnInput(e) {
        if (e.which === 13) {
            e.preventDefault();
            if (this.taxForm.valid) {
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

    save(): void {
        this.saving = false;
        if (this.serialNo) {
            if (this.taxForm.valid) {
                this.saving = true;
                this.subscription.add(
                    this._taxServiceProxy
                        .createOrEdit(this.taxForm.value)
                        .pipe(
                            finalize(() => {
                                this.saving = false;
                            })
                        )
                        .subscribe((data) => {
                            this.notify.info(this.l('Saved Successfully'));
                            this.taxSave.emit(data);
                        })
                );
            } else {
                this.notify.error('Form is invalid !!');
            }
        } else {
            if (this.taxForm.valid) {
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
        this.subscription.add(
            this._taxServiceProxy
                .createOrEdit(this.taxForm.value)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    })
                )
                .subscribe(() => {
                    if (this.id) {
                        this.notify.info(this.l('Updated Successfully'));
                        this.closeAfterAction();
                    } else {
                        this.notify.info(this.l('Saved Successfully'));
                        this.closeAfterAction();
                    }
                })
        );
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
        if (this.taxForm?.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.forEscape();
                }
            });
        } else {
            this.forEscape();
        }
    }

    forEscape() {
        this.taxForm.reset();
        if (this.serialNo) {
            this.taxSave.emit(null);
        } else if (this.dialog) {
            this.closeDialog();
        } else {
            this._location.back();
        }
        if (!this.dialog) {
            this.ngOnInit();
        }
    }

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete?'), (isConfirmed) => {
            if (isConfirmed) {
                this._taxServiceProxy.delete(id).subscribe(() => {
                    this.notify.success('Deleted Successfully');
                    this.closeAfterAction();
                });
            }
        });
    }

    private closeAfterAction(): void {
        if (this.dialog) {
            this.modalSave.emit(null);
            this.taxForm?.markAsPristine();
            this.closeDialog();
            return;
        }

        this._location.back();
    }

    private closeDialog(): void {
        if (this.modal) {
            this.modal.hide();
            return;
        }

        this.onModalHidden();
    }

    onModalHidden(): void {
        this.active = false;
        this.cdr.markForCheck();
    }

    private openDialogIfNeeded(): void {
        if (!this.dialog) {
            return;
        }

        this.cdr.detectChanges();
        setTimeout(() => {
            this.modal?.show();
            this.namesEvent?.nativeElement?.focus();
        });
    }
}
