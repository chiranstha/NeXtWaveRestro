import { Component, OnInit, ViewEncapsulation, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NotificationServiceProxy,
    UserNotification,
    UserNotificationState,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { IFormattedUserNotification, UserNotificationHelper } from './UserNotificationHelper';
import { finalize } from 'rxjs/operators';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { ColDef, GridApi, GridReadyEvent, SortChangedEvent } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../common/sub-header/sub-header.component';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { FormsModule } from '@angular/forms';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './notifications.component.html',
    styleUrls: ['./notifications.component.less'],
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        FormsModule,
        BusyIfDirective,
        AgGridAngular,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class NotificationsComponent extends AppComponentBase implements OnInit {
    private _notificationService = inject(NotificationServiceProxy);
    private _userNotificationHelper = inject(UserNotificationHelper);
    private _dateTimeService = inject(DateTimeService);
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Actions'),
            field: 'actions',
            width: 130,
            cellRenderer: (params) => {
                const container = document.createElement('div');
                const readButton = document.createElement('button');
                readButton.className = 'btn btn-sm btn-icon btn-primary';
                readButton.title = !this.isRead(params.data) ? this.l('SetAsRead') : '';
                readButton.disabled = this.isRead(params.data);
                readButton.onclick = () => this.setAsRead(params.data);
                readButton.innerHTML = this.isRead(params.data)
                    ? `<i class="fa fa-check" aria-label="${this.l('Read')}"></i>`
                    : `<i class="fa fa-circle-notch" aria-label="${this.l('Unread')}"></i>`;
                const deleteButton = document.createElement('button');
                deleteButton.className = 'btn btn-sm btn-icon btn-danger';
                deleteButton.title = this.l('Delete');
                deleteButton.onclick = () => this.deleteNotification(params.data);
                deleteButton.innerHTML = `<i class="fa-duotone fa-regular fa-xmark" aria-label="${this.l('Delete')}"></i>`;
                container.appendChild(readButton);
                container.appendChild(deleteButton);
                return container;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('Severity'),
            field: 'severity',
            width: 80,
            cellRenderer: (params) => {
                const icon = document.createElement('i');
                icon.className = `${params.data.formattedNotification.icon} ${params.data.formattedNotification.iconFontClass} fa-2x`;
                icon.title = this.getNotificationTextBySeverity(params.data.formattedNotification.severity);
                return icon;
            },
            sortable: false,
            filter: false,
        },
        {
            headerName: this.l('Notification'),
            field: 'notification',
            cellRenderer: (params) => {
                const span = document.createElement('span');
                span.className = this.getRowClass(params.data);
                span.title = params.data.formattedNotification.text;
                if (params.data.formattedNotification.url) {
                    const link = document.createElement('a');
                    link.href = params.data.formattedNotification.url;
                    link.className = this.getRowClass(params.data);
                    link.textContent = this.truncateString(params.data.formattedNotification.text, 120);
                    return link;
                } else {
                    span.textContent = this.truncateString(params.data.formattedNotification.text, 120);
                    return span;
                }
            },
        },
        {
            headerName: this.l('CreationTime'),
            field: 'creationTime',
            width: 200,
            valueGetter: (params) => this.fromNow(params.data.notification.creationTime),
            cellRenderer: (params) => {
                const span = document.createElement('span');
                span.className = this.getRowClass(params.data);
                span.title = this._dateTimeService.formatDate(params.data.notification.creationTime, 'DDDD t');
                span.textContent = this.fromNow(params.data.notification.creationTime);
                return span;
            },
            sortable: true,
        },
    ];
    defaultColDef: ColDef = {
        resizable: true,
        sortable: true,
        filter: true,
    };
    rowData: any[] = [];
    totalRecordsCount = 0;
    isLoading = false;
    readStateFilter = 'ALL';
    dateRange: DateTime[] = [];
    loading = false;
    ngOnInit(): void {
        this.dateRange = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getNotifications();
    }
    onSortChanged(event: SortChangedEvent) {
        this.getNotifications();
    }
    setAsRead(record: any): void {
        this.setNotificationAsRead(record, () => {
            this.getNotifications();
        });
    }
    reloadPage(): void {
        this.getNotifications();
    }
    getNotifications() {
        this.isLoading = true;
        const sortModel = this.gridApi
            .getColumnState()
            .filter((col) => col.sort)
            .map((col) => ({
                field: col.colId,
                order: col.sort,
            }));
        const sorting = sortModel.length > 0 ? sortModel.map((s) => `${s.field} ${s.order}`).join(', ') : '';
        this._notificationService
            .getUserNotifications(
                this.readStateFilter === 'ALL' ? undefined : UserNotificationState.Unread,
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
                10, // default page size
                0, // skip count
            )
            .pipe(finalize(() => (this.isLoading = false)))
            .subscribe((result) => {
                this.totalRecordsCount = result.totalCount;
                this.rowData = this.formatNotifications(result.items);
            });
    }
    isRead(record: any): boolean {
        return record.formattedNotification.state === 'READ';
    }
    fromNow(date: DateTime): string {
        return this._dateTimeService.fromNow(date);
    }
    formatRecord(record: any): IFormattedUserNotification {
        return this._userNotificationHelper.format(record, false);
    }
    formatNotification(record: any): string {
        const formattedRecord = this.formatRecord(record);
        return abp.utils.truncateStringWithPostfix(formattedRecord.text, 120);
    }
    formatNotifications(records: any[]): any[] {
        const formattedRecords = [];
        for (const record of records) {
            record.formattedNotification = this.formatRecord(record);
            formattedRecords.push(record);
        }
        return formattedRecords;
    }
    truncateString(text: any, length: number): string {
        return abp.utils.truncateStringWithPostfix(text, length);
    }
    setAllNotificationsAsRead(): void {
        this._userNotificationHelper.setAllAsRead(() => {
            this.getNotifications();
        });
    }
    openNotificationSettingsModal(): void {
        this._userNotificationHelper.openSettingsModal();
    }
    setNotificationAsRead(userNotification: UserNotification, callback: () => void): void {
        this._userNotificationHelper.setAsRead(userNotification.id, () => {
            if (callback) {
                callback();
            }
        });
    }
    deleteNotification(userNotification: UserNotification): void {
        this.message.confirm(this.l('NotificationDeleteWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._notificationService.deleteNotification(userNotification.id).subscribe(() => {
                    this.reloadPage();
                    this.notify.success(this.l('SuccessfullyDeleted'));
                });
            }
        });
    }
    deleteNotifications() {
        this.message.confirm(this.l('DeleteListedNotificationsWarningMessage'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._notificationService
                    .deleteAllUserNotifications(
                        this.readStateFilter === 'ALL' ? undefined : UserNotificationState.Unread,
                        this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                        this._dateTimeService.getEndOfDayForDate(this.dateRange[1]).endOf('day'),
                    )
                    .subscribe(() => {
                        this.reloadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
            }
        });
    }
    public getRowClass(formattedRecord: IFormattedUserNotification): string {
        const readState = UserNotificationState.Read as any;
        return formattedRecord.state === readState ? 'notification-read text-muted' : '';
    }
    getNotificationTextBySeverity(severity: abp.notifications.severity): string {
        switch (severity) {
            case abp.notifications.severity.SUCCESS:
                return this.l('Success');
            case abp.notifications.severity.WARN:
                return this.l('Warning');
            case abp.notifications.severity.ERROR:
                return this.l('Error');
            case abp.notifications.severity.FATAL:
                return this.l('Fatal');
            case abp.notifications.severity.INFO:
            default:
                return this.l('Info');
        }
    }
}
