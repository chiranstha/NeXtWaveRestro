import {
    Location,
} from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    Injector,
    OnInit,
    ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NgSelectComponent } from '@ng-select/ng-select';
import { AppComponentBase } from '@shared/common/app-component-base';
import { UniversalDropdownDto, VoucherTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Observable, Subject } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-voucher-types',
    templateUrl: './addvouchertypes.component.html',
    animations: [appModuleAnimation]
})

export class AddVoucherTypesComponent extends AppComponentBase implements OnInit {
    @ViewChild('voucher') voucherEvent: NgSelectComponent;
    @ViewChild('startindex', { read: ElementRef }) startindexEvent: ElementRef;
    @ViewChild('prefix', { read: ElementRef }) prefixEvent: ElementRef;
    @ViewChild('postfix', { read: ElementRef }) postfixEvent: ElementRef;
    @ViewChild('generateTypes') generateTypesEvent: NgSelectComponent;
    TIMELINE_EXAMPLE_DATA = [
        {
            title: 'STEP:1',
            post: 'To create Voucher Type, Select a item from dropdowns(Branch, Voucher Type and Generate Voucher Type).',
        },
        {
            title: 'STEP:2',
            post: 'Start Index Value must be greater than 0.',
        },
        {
            title: 'STEP:3',
            post: 'All the required fields indicated with * symbol must be filled. Non-required fields can be ignored. At last click on save button to save the form. Cancel button is used to go back to the previous page.',
        },
    ];
    formLoader = true;
    form: FormGroup;
    id: string;
    saving = false;
    shortcuts: ShortcutInput[] = [];
    allVoucherTypes$: Observable<UniversalDropdownDto[]>;
    title: string = 'Create Voucher Type'
    voucherTypeId: any;

    generateType = [
        { id: 0, displayName: 'Automatic' },
        { id: 1, displayName: 'Manual' },
        { id: 2, displayName: 'Duplicate' },
    ];
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private _fb: FormBuilder,
        private _location: Location,
        private _route: ActivatedRoute,
        private _proxy: VoucherTypesServiceProxy
    ) {
        super(injector);
        this.getSetting();
    }

    trackBy(index: number, item: any): string {
        return item.title;
    }


    ngOnInit(): void {
        this.id = this._route.snapshot.params['id'];
        if (this.id) {
            this._proxy.getVoucherTypeForEdit(this.id).subscribe((result) => {
                this.createForm(result);
            });
        }
        this.createForm();
        this.getAllVoucherTypes();
    }

    createForm(item: any = {}) {
        this.form = this._fb.group({
            startIndex: [item.startIndex ? item.startIndex : 1, Validators.required],
            // branchId: [ item.branchId ? item.branchId : this.branchId],
            // branchId: [{ value: item.branchId ? item.branchId : this.branchId, disabled: this.id }],
            // voucherTypeId: [{ value: item.voucherTypeId ? item.voucherTypeId : this.voucherTypeId, disabled: this.id }],
            voucherTypeId: [item.voucherTypeId ? item.voucherTypeId : this.emptyGuId],
            voucherGenerateType: [item.voucherGenerateType ? item.voucherGenerateType : 0],
            prefix: [item.prefix ? item.prefix : ''],
            postfix: [item.postfix ? item.postfix : ''],
            id: [item.id],
        });
    }



    getAllVoucherTypes() {
        this.allVoucherTypes$ = this._proxy.getAllVouchertypeForDropdown();
        this.allVoucherTypes$.subscribe((data) => {
            if (!this.id) {
                this.form.get('voucherTypeId').setValue(data[0].id);
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

    save() {
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
            this.notify.error('From is invalid !!');
        }
    }

    createapi() {
        this.saving = true;
        this._proxy
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

    gotoVoucher(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.voucherEvent.open();
        }
    }

    gotoGenerateType(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.voucherEvent.close();
            this.generateTypesEvent.open();
        }
    }

    gotoStartIndex(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.generateTypesEvent.close();
            this.startindexEvent.nativeElement.focus();
        }
    }

    gotoPrefix(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.prefixEvent.nativeElement.focus();
        }
    }

    gotoPostfix(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.postfixEvent.nativeElement.focus();
        }
    }

    onSaveOnInput(e) {
        if (e.which === 13) {
            e.preventDefault();
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
}
