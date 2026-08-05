import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditFinancialYearDto,
    FinancialYearsServiceProxy,
} from '@shared/service-proxies/service-proxies';

import { finalize } from 'rxjs/operators';
import { Location } from '@angular/common';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ShortcutInput } from 'ng-keyboard-shortcuts';
import { Subject, Subscription } from 'rxjs';
import { appModuleAnimation } from '@shared/animations/routerTransition';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'app-add-financialyear',
    templateUrl: './addFinancialYear.component.html',
    animations: [appModuleAnimation]
})
export class AddFinancialyearComponent extends AppComponentBase implements OnInit, OnDestroy {
    id: string;
    formLoader = true;
    active = false;
    subscription: Subscription = new Subscription();
    saving = false;
    branchBranchName = '';
    title = 'Create Financial Year';
    shortcuts: ShortcutInput[] = [];
    form: FormGroup;

    financialYear: CreateOrEditFinancialYearDto = new CreateOrEditFinancialYearDto();
    diffrenceDay = 0;
    private destroy$: Subject<void> = new Subject<void>();

    constructor(
        injector: Injector,
        private location: Location,
        private route: ActivatedRoute,
        private fb: FormBuilder,
        private _financialYearsServiceProxy: FinancialYearsServiceProxy
    ) {
        super(injector);
        this.getSetting();
    }

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
        const diff: number = (y - x);
        return (diff / (60 * 60 * 1000 * 24));
        //    return (diff.getUTCDate() - 1)
        // let engEnd = DateConverter.BStoAD(end);
        // let startSplit: string[] = start.split('/');
        // let endSplit: string[] = end.split('/');
        // let startRaw: number = parseInt(startSplit[0]) + parseInt(startSplit[1]) + parseInt(startSplit[0])
        // if(startSplit[0] === endSplit[0]){
        //     if(startSplit[1] === endSplit[1]){

        //     }
        // }
    }

    createForm(item: any = {}) {
        this.form = this.fb.group({
            id: [item.id ? item.id : this.emptyGuId],
            fromMiti: [item.fromMiti ? item.fromMiti : this.today, Validators.required],
            toMiti: [item.toMiti ? item.toMiti : this.today, Validators.required]
        });
        console.log(`The value of today date is ${  this.today}`);
    }

    ngOnInit(): void {
        this.id = this.route.snapshot.params['id'];
        this.createForm();
        if (!this.id) {
            this.financialYear = new CreateOrEditFinancialYearDto();
            this.financialYear.id = this.id;
            this.active = true;
        } else {
            this.title = 'Edit Financial Year';
            this.subscription.add(
                this._financialYearsServiceProxy.getFinancialYearForEdit(this.id).subscribe((result) => {
                    this.createForm(result);
                    this.diffDateMiti(result.fromMiti);
                    this.diffdueDate(result.toMiti);
                    this.active = true;
                })
            );
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        this.subscription.unsubscribe();
    }

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
                        })
                    )
                    .subscribe(() => {
                        this.notify.info(this.l('Saved Successfully'));
                        if (this.id) {
                            this.notify.info(this.l('Updated Successfully'));
                        }
                        this.location.back();
                    })
            );
        } else {
            this.notify.error('Form is invalid !!');
        }
    }

    close(): void {
        if (this.form.dirty) {
            this.message.confirm('', this.l('Are you sure you want to Cancel ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this.location.back();
                }
            });
        } else {
            this.location.back();
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
                    this._financialYearsServiceProxy.delete(id).subscribe(() => {
                        this.location.back();
                        this.notify.success('Deleted Successfully');
                    })
                );
            }
        });
    }
}
