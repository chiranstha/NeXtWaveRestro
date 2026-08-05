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
import { FinancialYearsServiceProxy, GetFinancialYearForViewDto } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subject, takeUntil } from 'rxjs';
@Component({
    selector: 'viewFinancialYearModal',
    templateUrl: './view-financialYear-modal.component.html',
    imports: [ModalDirective],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA],
})
export class ViewFinancialYearModalComponent extends AppComponentBase implements OnDestroy {
    private _proxy = inject(FinancialYearsServiceProxy);
    private cdr = inject(ChangeDetectorRef);
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    public destroy$ = new Subject<void>();
    active = false;
    saving = false;
    item: GetFinancialYearForViewDto;
    constructor() {
        super();
        this.getSetting();
        this.item = new GetFinancialYearForViewDto();
    }
    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.unsubscribe();
    }
    show(id: string): void {
        this._proxy
            .getFinancialYearForView(id)
            .pipe(takeUntil(this.destroy$))
            .subscribe((data) => {
                this.item = data;
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
