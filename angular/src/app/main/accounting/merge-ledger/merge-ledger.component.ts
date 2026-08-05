import { Component, OnInit, ViewChild, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgSelectComponent } from '@ng-select/ng-select';
import { AppComponentBase } from '@shared/common/app-component-base';
import { MergeLedgerServiceProxy } from '@shared/service-proxies/service-proxies';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
@Component({
    changeDetection: ChangeDetectionStrategy.Eager,
    selector: 'app-merge-ledger',
    templateUrl: './merge-ledger.component.html',
    styleUrls: ['./merge-ledger.component.css'],
    imports: [FormsModule, ReactiveFormsModule, NgSelectComponent, ButtonBusyDirective],
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class MergeLedgerComponent extends AppComponentBase implements OnInit {
    private cdr = inject(ChangeDetectorRef);
    private _proxy = inject(MergeLedgerServiceProxy);
    private _fb = inject(FormBuilder);
    @ViewChild('accountledger') ledgerEvent: NgSelectComponent;
    @ViewChild('requestledger') requestEvent: NgSelectComponent;

    requestData: any;
    ledgerData: any;

    form: FormGroup;

    ledgerId: number;

    saving = false;
    enabled = true;

    ngOnInit() {
        this.createForm();
        setTimeout(() => {
            this.requestEvent.open();
        }, 1000);
    }
    createForm(item: any = {}) {
        this.form = this._fb.group({
            requestLedgerId: [item.requestLedgerId ? item.requestLedgerId : 0, Validators.required],
            ledgerId: [item.ledgerId ? item.ledgerId : 0, Validators.required],
        });
    }

    toggleEnable() {
        this.enabled = false;
    }
    postData() {
        if (this.form.valid) {
            this.message.confirm('', this.l('Do you want to Merge ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this._proxy
                        .postMergeLedger(this.form.get('requestLedgerId').value, this.form.get('ledgerId').value)
                        .subscribe(() => {
                            this.notify.success(this.l('Successfully Merged'));
                            this.saving = false;
                            this.form.reset();
                        });
                }
            });
        }
    }
    gotoAccountLedger(e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.ledgerEvent.open();
        }
    }
    gotoRequestLEdger(e) {
        e.preventDefault();
        if (e.key === 'Enter') {
            this.postData();
        }
    }
}
