import { ChangeDetectorRef, Component, ViewChild, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AuditLogServiceProxy,
    EntityChangeListDto,
    EntityPropertyChangeDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { DateTimeService } from '../timing/date-time.service';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    selector: 'entityChangeDetailModal',
    templateUrl: './entity-change-detail-modal.component.html',
    imports: [AppBsModalDirective, LocalizePipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class EntityChangeDetailModalComponent extends AppComponentBase {
    private _auditLogService = inject(AuditLogServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    private _cdr = inject(ChangeDetectorRef);
    @ViewChild('entityChangeDetailModal', { static: true }) modal: ModalDirective;
    active = false;
    entityPropertyChanges: EntityPropertyChangeDto[];
    entityChange: EntityChangeListDto;

    getPropertyChangeValue(propertyChangeValue, propertyTypeFullName) {
        if (!propertyChangeValue) {
            return propertyChangeValue;
        }
        propertyChangeValue = propertyChangeValue.replace(/^['"]+/g, '').replace(/['"]+$/g, '');
        if (this.isDate(propertyChangeValue, propertyTypeFullName)) {
            return this._dateTimeService.formatDate(
                this._dateTimeService.fromISODateString(propertyChangeValue),
                'yyyy-LL-dd HH:mm:ss',
            );
        }
        if (propertyChangeValue === 'null') {
            return '';
        }
        return propertyChangeValue;
    }
    isDate(date, propertyTypeFullName): boolean {
        return propertyTypeFullName.includes('DateTime') && !isNaN(Date.parse(date).valueOf());
    }
    show(record: EntityChangeListDto): void {
        const self = this;
        self.active = true;
        self.entityChange = record;
        this._auditLogService.getEntityPropertyChanges(record.id).subscribe((result) => {
            self.entityPropertyChanges = result;
            self._cdr.markForCheck();
        });
        self.modal.show();
        self._cdr.markForCheck();
    }
    close(): void {
        this.active = false;
        this.modal.hide();
        this._cdr.markForCheck();
    }
}
