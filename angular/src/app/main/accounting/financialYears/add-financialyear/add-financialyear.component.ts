import {
    AfterViewInit,
    Component,
    OnDestroy,
    OnInit,
    inject,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    EventEmitter,
    Input,
    Output,
    ViewChild,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditFinancialYearDto, FinancialYearsServiceProxy } from '@shared/service-proxies/service-proxies';
import { finalize, takeUntil } from 'rxjs/operators';
import { Location, NgTemplateOutlet } from '@angular/common';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { SubHeaderComponent } from '../../../../shared/common/sub-header/sub-header.component';
import { ButtonBusyDirective } from '../../../../../shared/utils/button-busy.directive';
import { NepaliDatepickerComponent } from '../../../../shared/common/nepalidatepicker/nepali-datepicker-angular.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ModalDirective } from 'ngx-bootstrap/modal';
@Component({
    changeDetection: ChangeDetectionStrategy.Eager,
    selector: 'app-add-financialyear',
    templateUrl: './add-financialyear.component.html',
    styleUrls: ['./add-financialyear.component.css'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        ButtonBusyDirective,
        FormsModule,
        ReactiveFormsModule,
        NgTemplateOutlet,
        ModalDirective,
        NepaliDatepickerComponent,
        LocalizePipe,
    ],
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class AddFinancialyearComponent extends AppComponentBase implements OnInit, AfterViewInit, OnDestroy {
    private cdr = inject(ChangeDetectorRef);
    private location = inject(Location);
    private route = inject(ActivatedRoute);
    private fb = inject(FormBuilder);
    private _financialYearsServiceProxy = inject(FinancialYearsServiceProxy);
    @Input() dialog = false;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('createOrEditModal', { static: false }) modal?: ModalDirective;
    id: string;
    formLoader = true;
    active = false;
    subscription: Subscription = new Subscription();
    saving = false;
    form: FormGroup;
    title = 'Create Financial Year';
    financialYear: CreateOrEditFinancialYearDto = new CreateOrEditFinancialYearDto();
    diffrenceDay = 0;
    private destroy$: Subject<void> = new Subject<void>();

    get start() {
        const startSplit: string[] = this.form.get('fromMiti').value.split('/');
        const year = parseInt(startSplit[0]);
        const month = parseInt(startSplit[1]);
        const day = parseInt(startSplit[2]);
        const date: Date = new Date(year, month, day);
        return date;
    }
    get end() {
        const startSplit: string[] = this.form.get('toMiti').value.split('/');
        const year = parseInt(startSplit[0]);
        const month = parseInt(startSplit[1]);
        const day = parseInt(startSplit[2]);
        const date: Date = new Date(year, month, day);
        return date;
    }
    get differences(): any {
        const x = this.start.getTime();
        const y = this.end.getTime();
        const diff: number = y - x;
        return diff / (60 * 60 * 1000 * 24);
    }
    createForm(item: any = {}) {
        this.form = this.fb.group({
            id: [item.id ? item.id : this.emptyguId],
            fromMiti: [item.fromMiti ? item.fromMiti : this.today, Validators.required],
            toMiti: [item.toMiti ? item.toMiti : this.today, Validators.required],
            status: [item.status ? item.status : false, Validators.required],
            isOldYear: [item.isOldYear ? item.isOldYear : false, Validators.required],
        });
        this.form
            .get('fromMiti')
            .valueChanges.pipe(takeUntil(this.destroy$))
            .subscribe(() => {});
    }
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
        this.loadForm(id);
        this.active = true;
        this.cdr.detectChanges();
        this.modal?.show();
    }
    private loadForm(id?: string): void {
        this.id = id;
        if (!this.id) {
            this.title = 'Create Financial Year';
            this.createForm();
            this.financialYear = new CreateOrEditFinancialYearDto();
            this.financialYear.id = this.id;
            this.active = true;
            this.cdr.markForCheck();
        } else {
            this.createForm();
            this.title = 'Edit Financial Year';
            this.cdr.markForCheck();
            this.subscription.add(
                this._financialYearsServiceProxy
                    .getFinancialYearForEdit(this.id)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe((result) => {
                        this.createForm(result);
                        this.diffDateMiti(result.fromMiti);
                        this.diffdueDate(result.toMiti);
                        this.active = true;
                        this.cdr.markForCheck();
                    }),
            );
        }
    }
    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        this.subscription.unsubscribe();
    }
    ngAfterViewInit(): void {}
    save(): void {
        this.saving = false;
        if (this.form.valid) {
            this.saving = true;
            this.subscription.add(
                this._financialYearsServiceProxy
                    .createOrEdit(this.form.value)
                    .pipe(
                        finalize(() => {
                            this.saving = false;
                        }),
                    )
                    .subscribe(() => {
                        this.notify.info(this.l('Saved Successfully'));
                        if (this.id) {
                            this.notify.info(this.l('Updated Successfully'));
                        }
                        this.closeAfterAction();
                    }),
            );
        } else {
            this.notify.error('Form is invalid !!');
        }
    }
    close(): void {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Are you sure you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.closeDialogOrBack();
                }
            });
        } else {
            this.closeDialogOrBack();
        }
    }
    private closeAfterAction(): void {
        if (this.dialog) {
            this.modalSave.emit(null);
            this.form?.markAsPristine();
            this.closeDialogOrBack();
            return;
        }
        this.location.back();
    }
    private closeDialogOrBack(): void {
        if (this.dialog) {
            this.active = false;
            this.modal?.hide();
            return;
        }
        this.location.back();
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
    gotoFromMiti() {
        const date = document.getElementById('fromMiti');
        const input = date.appendChild(document.getElementById('npDatePicker'));
        const calender = date.appendChild(document.getElementById('pickerCalender'));
        calender.focus();
        calender.style.top = '60px';
        input.focus();
    }
    gotoToMiti() {
        const date = document.getElementById('toMiti');
        const input = date.appendChild(document.getElementById('npDatePicker'));
        const calender = date.appendChild(document.getElementById('pickerCalender'));
        calender.focus();
        calender.style.top = '60px';
        input.focus();
    }
    diffdueDate(e) {
        const endDateValue = e;
        const startDateValue = this.form.get('fromMiti').value;
        const Difference_In_Time = new Date(endDateValue).getTime() - new Date(startDateValue).getTime();
        this.diffrenceDay = Difference_In_Time / (1000 * 3600 * 24);
    }
    diffDateMiti(e) {
        const startDateValue = e;
        const endDateValue = this.form.get('toMiti').value;
        const Difference_In_Time = new Date(endDateValue).getTime() - new Date(startDateValue).getTime();
        this.diffrenceDay = Difference_In_Time / (1000 * 3600 * 24);
    }
    onSaveOnInput(event: any) {
        if (event.which === 13) {
            event.preventDefault();
            this.message.confirm('', this.l('Do you want to save?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.save();
                }
            });
        } else if (event.which === 8) {
            event.preventDefault();
        }
    }
    clearForm(id) {
        this.message.confirm('', this.l('Are you sure you want to Delete ?'), (isConfirmed) => {
            if (isConfirmed) {
                this.subscription.add(
                    this._financialYearsServiceProxy
                        .delete(id)
                        .pipe(takeUntil(this.destroy$))
                        .subscribe(() => {
                            this.notify.success('Deleted Successfully');
                            this.closeAfterAction();
                        }),
                );
            }
        });
    }
}
