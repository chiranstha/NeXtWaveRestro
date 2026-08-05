import { ChangeDetectionStrategy, Component, Injector, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NgSelectComponent } from '@ng-select/ng-select';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { MergeLedgerServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'appmergeledger',
    templateUrl: './mergeLedger.component.html',
    animations: [appModuleAnimation]
})
export class MergeLedgerComponent extends AppComponentBase implements OnInit {
    @ViewChild('accountledger') ledgerEvent: NgSelectComponent;
    @ViewChild('requestledger') requestEvent: NgSelectComponent;
    requestData: any;
    ledgerData: any;
    form: FormGroup;
    ledgerId: string;
    saving: boolean = false;
    enabled: boolean = true;

    constructor(injector: Injector, private _proxy: MergeLedgerServiceProxy, private _fb: FormBuilder) {
        super(injector);
    }

    ngOnInit() {
        this.getAllRequestLedger();
        this.createForm();
        setTimeout(() => {
            this.requestEvent.open();
        }, 1000);
    }

    createForm(item: any = {}) {
        this.form = this._fb.group({
            requestLedgerId: [item.requestLedgerId ? item.requestLedgerId : this.emptyGuId, Validators.required],
            ledgerId: [item.ledgerId ? item.ledgerId : this.emptyGuId, Validators.required],
        });
    }

    getAllRequestLedger() {
        this._proxy.getAllRequestLedger().subscribe((data) => {
            this.requestData = data;
        });
    }

    onChange() {
        const ledgerId = this.form.get('requestLedgerId').value;
        this.getallaccountledger(ledgerId);
    }

    getallaccountledger(id) {
        this._proxy.getAllAccountLedger(id).subscribe((data) => {
            this.ledgerData = data.filter(
                x => x.id !== id);
            if (this.form.get('ledgerId').value == 0) {
                return;
            } else {
                this.enabled = false;
            }
        });
    }

    toggleEnable() {
        this.enabled = false;
    }

    postData() {
        if (this.form.valid) {
            this.message.confirm('', this.l('Do you want to Merge ?'), (isConfirmed) => {
                if (isConfirmed) {
                    this._proxy.postMergeLedger(this.form.get('requestLedgerId').value, this.form.get('ledgerId').value).subscribe((data) => {
                        this.notify.success(this.l('Successfully Merged'));
                        this.saving = false;
                        this.form.reset();
                        this.getAllRequestLedger();
                    });
                }
            });
        }
    }

    gotoAccountLedger(e) {
        if (e.which === 13) {
            e.preventDefault();
            this.requestEvent.close();
            this.ledgerEvent.open();
        }
    }

    saveOnEnter(e) {
        e.preventDefault();
        if (e.which === 13) {
            this.postData();
        }
    }
}
