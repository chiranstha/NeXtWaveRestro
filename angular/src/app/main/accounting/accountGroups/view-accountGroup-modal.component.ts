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
import {
    AccountGroupNature,
    AccountGroupsServiceProxy,
    GetAccountGroupForViewDto,
} from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subject, takeUntil } from 'rxjs';
@Component({
    selector: 'viewAccountGroupModal',
    templateUrl: './view-accountGroup-modal.component.html',
    imports: [ModalDirective],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class ViewAccountGroupModalComponent extends AppComponentBase implements OnDestroy {
    private _accountGroupsServiceProxy = inject(AccountGroupsServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    active = false;
    saving = false;
    item: GetAccountGroupForViewDto;
    accountGroupNature = AccountGroupNature;
    public destroy$ = new Subject<void>();
    constructor() {
        super();
        this.getSetting();
        this.item = new GetAccountGroupForViewDto();
    }
    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.unsubscribe();
    }
    show(id: string): void {
        this._accountGroupsServiceProxy
            .getAccountGroupForView(id)
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
