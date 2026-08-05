import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
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
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditUnitDto, UnitsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { Location } from '@angular/common';

import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Subject } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';
// import { KeyboardShortcutsService } from '@shared/utils/keyboard-shortcuts.service';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'addunit',
    templateUrl: './addUnit.component.html',
    animations: [appModuleAnimation]

})
export class AddUnitComponent extends AppComponentBase implements OnInit, AfterViewInit, OnDestroy {
    shortcuts: ShortcutInput[] = [];
    form: FormGroup;
    formLoader = true;
    id: string;
    @Input() dialog = false;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createOrEditModal', { static: false }) modal?: ModalDirective;
    @ViewChild('fname', { read: ElementRef }) fnameEvent: ElementRef;
    @ViewChild('decimals', { read: ElementRef }) deciEvent: ElementRef;
    @ViewChild('descriptions', { read: ElementRef }) descriptionEvent: ElementRef;
    @ViewChild('names', { read: ElementRef }) nameEvent: ElementRef;
    @Output() unitSave: EventEmitter<any> = new EventEmitter<any>();
    @Input() serialNo: number;
    serialNumber = 0;
    active = false;
    saving = false;
    isAddMode = false;
    title = 'Create Unit';
    unit: CreateOrEditUnitDto = new CreateOrEditUnitDto();
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private _unitsServiceProxy: UnitsServiceProxy,
        // private keyboardShortcutsService: KeyboardShortcutsService,
        private route: ActivatedRoute,
        private _location: Location,
        private fb: FormBuilder,
        private cdr: ChangeDetectorRef
    ) {
        super(injector);
        this.getSetting();
    }

    ngAfterViewInit(): void {
        setTimeout(() => this.nameEvent?.nativeElement?.focus());
    }

    ngOnDestroy(): void {
        //Called once, before the instance is destroyed.
        //Add 'implements OnDestroy' to the class.
        this.destroy$.next();
        this.destroy$.complete();
    }


    // private setupKeyboardShortcuts() {
    //     this.shortcuts = this.keyboardShortcutsService.getShortcuts({
    //         'save': (e) => {
    //             e.event.preventDefault();
    //             this.save();
    //         },
    //         'close': (e) => {
    //             e.event.preventDefault();
    //             this.close();
    //         }
    //     });
    // }

    ngOnInit(): void {
        if (this.dialog) {
            this.active = false;
            this.createForm();
            this.formLoader = false;
            return;
        }

        this.loadForm(this.route.snapshot.params['id']);
        //   this.setupKeyboardShortcuts();
    }

    show(id?: string): void {
        this.dialog = true;
        this.loadForm(id);
        this.active = true;
        this.cdr.detectChanges();
        this.modal?.show();
        setTimeout(() => this.nameEvent?.nativeElement?.focus());
    }

    private loadForm(id?: string): void {
        this.formLoader = true;
        this.id = id;
        this.isAddMode = !this.id;
        this.createForm();
        if (!this.id) {
            this.title = 'Create Unit';
            this.unit = new CreateOrEditUnitDto();
            this.active = true;
            this.formLoader = false;
            this.cdr.markForCheck();
        } else {
            this.title = 'Edit Unit';
            this._unitsServiceProxy.getUnitForEdit(this.id).subscribe((result) => {
                this.createForm(result);
                this.active = true;
                this.formLoader = false;
                this.cdr.markForCheck();
            });
        }
    }

    gotoFname(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.fnameEvent.nativeElement.focus();
        }
    }

    gotoDecimal(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.deciEvent.nativeElement.focus();
        }
    }

    gotoDescription(e, form) {
        if (e.which === 13) {
            e.preventDefault();
            this.descriptionEvent.nativeElement.focus();
        }
    }

    onSaveOnInput(e) {
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

    createForm(item: any = {}) {
        this.form = this.fb.group({
            name: [item.name, Validators.required],
            description: [item.description ? item.description : ''],
            noOfDecimalPlaces: [item.noOfDecimalPlaces ? item.noOfDecimalPlaces : 1, Validators.required],
            formalName: [item.formalName ? item.formalName : ''],
            id: [item.id ? item.id : null],
        });
    }

    //  down key button decrement value prevented going below 0

    save(): void {
        if (this.serialNo) {
            if (this.form.valid) {
                this.saving = true;
                this._unitsServiceProxy
                    .createOrEdit(this.form.value)
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        })
                    )
                    .subscribe((data) => {
                        this.unitSave.emit(data);
                        this.notify.info(this.l('Saved Successfully'));
                    });
            } else {
                this.notify.error('Form is invalid !!');
            }
        } else {
            if (this.form.valid) {
                if (this.id) {
                    this.message.confirm('', this.l('Do you want to Update ?'), (isConfirmed) => {
                        if (isConfirmed) {
                            this.pushapi();
                        }
                    });
                } else {
                    this.pushapi();
                }
            } else {
                this.notify.error('Form is invalid !!');
            }
        }
    }

    pushapi() {
        this.saving = true;
        this._unitsServiceProxy
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
            this.unitSave.emit(null);
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

    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this._unitsServiceProxy.delete(id).subscribe(() => {
                    this.notify.success(this.l('Successfully Deleted'));
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
