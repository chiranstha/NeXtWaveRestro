import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Injector, Input, OnInit, Output, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    UniversalDropdownDto,
    CreateOrEditProductGroupDto,
    ProductGroupsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize, takeUntil } from 'rxjs/operators';
import { Location } from '@angular/common';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Subject } from 'rxjs';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-product-groups',
    templateUrl: './addProductGroups.component.html',
    animations: [appModuleAnimation]

})
export class AddProductGroupsComponent extends AppComponentBase implements OnInit {
    shortcuts: ShortcutInput[] = [];
    formLoader = true;
    form: FormGroup;
    id: string;
    groupUnder: string;
    groups: UniversalDropdownDto[];
    @Input() dialog = false;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @Output() productGroupsave: EventEmitter<any> = new EventEmitter<any>();
    @Input() serialNo: number;
    serialNumber: number = 0;
    active = false;
    title = 'Create Product Group';
    saving = false;
    isAddMode = false;
    selectGroup = true;
    productGroup: CreateOrEditProductGroupDto = new CreateOrEditProductGroupDto();
    @ViewChild('createOrEditModal', { static: false }) modal?: ModalDirective;
    @ViewChild('groupunder') groupunderEvent: NgSelectComponent;
    @ViewChild('descriptions', { read: ElementRef }) descriptionEvent: ElementRef;
    @ViewChild('groupName', { read: ElementRef }) groupNameEvent: ElementRef;
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private _productGroupsServiceProxy: ProductGroupsServiceProxy,
        private _location: Location,
        private route: ActivatedRoute,
        private fb: FormBuilder,
        private cdr: ChangeDetectorRef
    ) {
        super(injector);
        this.getSetting();
    }

    ngOnDestroy(): void {
        // Emit something to stop all Observables
        this.destroy$.next();
        // Complete the notifying Observable to remove it
        this.destroy$.complete();
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
        this.active = true;
        this.cdr.detectChanges();
        this.modal?.show();
        setTimeout(() => this.groupNameEvent?.nativeElement?.focus(), 100);
    }

    private loadForm(id?: string): void {
        this.formLoader = true;
        this.id = id;
        this.isAddMode = !this.id;
        this.createForm();
        if (!this.id) {
            this.title = 'Create Product Group';
            this.productGroup = new CreateOrEditProductGroupDto();
            this._productGroupsServiceProxy
                .getAllExplicitProductGroupsForTableDropdown(this.emptyGuId)
                .pipe(
                    finalize(() => (this.formLoader = false)),
                    takeUntil(this.destroy$)
                )
                .subscribe((res) => {
                    this.groups = res;
                    this.groupUnder = this.groups[0].id;
                    this.form.get('groupUnder').setValue(this.groupUnder);
                    this.active = true;
                    this.cdr.markForCheck();
                    setTimeout(() => {
                        this.groupNameEvent?.nativeElement?.focus();
                    }, 500);
                });
        } else {
            this.title = 'Edit Product Group';
            this._productGroupsServiceProxy.getAllExplicitProductGroupsForTableDropdown(this.id).subscribe((res) => {
                this.groups = res;
            });
            this._productGroupsServiceProxy
                .getProductGroupForEdit(this.id)
                .pipe(
                    finalize(() => (this.formLoader = false)),
                    takeUntil(this.destroy$)
                )
                .subscribe((result) => {
                    this.createForm(result);
                    this.active = true;
                    this.cdr.markForCheck();
                });
        }
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            name: [item.name ? item.name : '', Validators.required],
            groupUnder: [item.groupUnder ? item.groupUnder : this.groupUnder, Validators.required],
            description: [item.description ? item.description : ''],
            id: [item.id ? item.id : null],
        });
    }

    gotoGroupUnder(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.groupunderEvent.open();
        }
    }

    gotoDescription(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.groupunderEvent.close();
            this.descriptionEvent.nativeElement.focus();
        }
    }

    onSaveOnInput(event) {
        if (event.which === 13) {
            event.preventDefault();
            if (this.id) {
                this.save();
            } else {
                if (this.form.valid) {
                    this.message.confirm('', this.l('Do you want to Save ?'), (isConfirm) => {
                        if (isConfirm) {
                            this.save();
                        }
                    });
                } else {
                    this.save();
                }
            }
        }
    }

    save(): void {
        this.saving = false;
        if (this.serialNo) {
            if (this.form.valid) {
                this.saving = true;
                this._productGroupsServiceProxy
                    .createOrEdit(this.form.value)
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        })
                    )
                    .subscribe((data) => {
                        this.notify.info(this.l('Saved Successfully'));
                        this.productGroupsave.emit(data);
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
        this._productGroupsServiceProxy
            .createOrEdit(this.form.value)
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
            });
    }

    close() {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Do you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.escape();
                }
            });
        } else {
            this.escape();
        }
    }

    escape() {
        this.form.reset();
        if (this.serialNo) {
            this.productGroupsave.emit(null);
        } else if (this.dialog) {
            this.closeDialog();
        } else {
            this._location.back();
        }
        if (!this.dialog) {
            this.ngOnInit();
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

    hide() {
        this.selectGroup = false;
        this.form.get('groupUnder').setValue(0);
    }

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._productGroupsServiceProxy.delete(id).subscribe(() => {
                    this.notify.success('Successfully Deleted');
                    this.closeAfterAction();
                });
            }
        });
    }

    private closeAfterAction(): void {
        if (this.dialog) {
            this.modalSave.emit(null);
            this.form?.markAsPristine();
            this.closeDialog();
            return;
        }

        this._location.back();
    }

    private closeDialog(): void {
        this.active = false;
        this.modal?.hide();
    }
}
