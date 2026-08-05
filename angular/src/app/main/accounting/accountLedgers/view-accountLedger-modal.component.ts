import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    OnDestroy,
    Output,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AccountLedgersServiceProxy, GetAccountLedgerForViewDto } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subject, takeUntil } from 'rxjs';
@Component({
    selector: 'viewAccountLedgerModal',
    templateUrl: './view-accountLedger-modal.component.html',
    imports: [ModalDirective],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class ViewAccountLedgerModalComponent extends AppComponentBase implements OnDestroy {
    private _accountLedgersServiceProxy = inject(AccountLedgersServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    public destroy$ = new Subject<void>();
    active = false;
    saving = false;
    item: GetAccountLedgerForViewDto;
    constructor() {
        super();
        this.getSetting();
        this.item = new GetAccountLedgerForViewDto();
    }
    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.unsubscribe();
    }
    show(id: string): void {
        this._accountLedgersServiceProxy
            .getAccountLedgerForView(id)
            .pipe(takeUntil(this.destroy$))
            .subscribe((res) => {
                this.item = res;
                this.active = true;
                this.cdr.markForCheck();
                this.modal.show();
            });
    }
    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
