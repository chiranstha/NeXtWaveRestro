import { Component, ViewChild, ViewEncapsulation, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { NotificationServiceProxy, NotificationSeverity } from '@shared/service-proxies/service-proxies';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { DateTime } from 'luxon';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { CreateMassNotificationModalComponent } from './create-mass-notification-modal.component';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { ColDef, GridApi, GridReadyEvent, PaginationChangedEvent } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { BsDaterangepickerInputDirective, BsDaterangepickerDirective } from 'ngx-bootstrap/datepicker';
import { DateRangePickerLuxonModifierDirective } from '../../../shared/utils/date-time/date-range-picker-luxon-modifier.directive';
import { BusyIfDirective } from '../../../shared/utils/busy-if.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './mass-notifications.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        FormsModule,
        BsDaterangepickerInputDirective,
        BsDaterangepickerDirective,
        DateRangePickerLuxonModifierDirective,
        BusyIfDirective,
        AgGridAngular,
        CreateMassNotificationModalComponent,
        ModalDirective,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class MassNotificationsComponent extends AppComponentBase {
    private _notificationServiceProxy = inject(NotificationServiceProxy);
    private _dateTimeService = inject(DateTimeService);
    @ViewChild('createMassNotificationModalComponent', { static: true })
    createMassNotificationModalComponent: CreateMassNotificationModalComponent;
    @ViewChild('messageDetailModal', { static: true }) messageDetailModal: ModalDirective;
    dateRange: DateTime[] = [];
    notificationSeverity = NotificationSeverity;
    messageDetailString: string;
    messageMaxLength = 50;
    // AG Grid
    gridApi: GridApi;
    columnDefs: ColDef[] = [
        {
            headerName: this.l('Message'),
            field: 'data',
            minWidth: 250,
            cellRenderer: (params) => this.renderMessageCell(params),
        },
        {
            headerName: this.l('Severity'),
            field: 'severity',
            maxWidth: 150,
            cellRenderer: (params) => this.renderSeverityCell(params),
        },
        {
            headerName: this.l('LastModificationTime'),
            field: 'creationTime',
            maxWidth: 250,
            valueFormatter: (params) => this.formatDate(params.value),
        },
        {
            headerName: this.l('IsPublished'),
            field: 'isPublished',
            maxWidth: 150,
            cellRenderer: (params) => this.renderPublishedCell(params),
        },
    ];
    gridOptions = {
        pagination: true,
        paginationPageSize: 10,
        paginationPageSizeSelector: [10, 25, 50, 100],
        suppressScrollOnNewData: true,
    };
    rowData: any[] = [];
    constructor() {
        super();
        this.dateRange = [this._dateTimeService.getStartOfDay(), this._dateTimeService.getEndOfDay()];
        // Expose method for AG Grid cell renderer
        (window as any).showMessageDetail = (data: string) => this.showMessageDetailModal(data);
    }
    onGridReady(params: GridReadyEvent) {
        this.gridApi = params.api;
        this.getPublishedNotifications();
    }
    onPaginationChanged(_event: PaginationChangedEvent) {
        // Handle pagination changes if needed
    }
    getPublishedNotifications() {
        this._notificationServiceProxy
            .getNotificationsPublishedByUser(
                this._dateTimeService.getStartOfDayForDate(this.dateRange[0]),
                this._dateTimeService.getEndOfDayForDate(this.dateRange[1]),
            )
            .subscribe((result) => {
                this.rowData = result.items;
                this.gridApi?.setGridOption('rowData', this.rowData);
            });
    }
    reloadPage(): void {
        this.getPublishedNotifications();
    }
    createMassNotification(): void {
        this.createMassNotificationModalComponent.show();
    }
    isHTMLMessage(str): boolean {
        const message = this.getMessageData(str);
        const doc = new DOMParser().parseFromString(message, 'text/html');
        return [].slice.call(doc.body.childNodes).some((node) => node.nodeType === 1);
    }
    getMessageDataString(str): string {
        const message = this.getMessageData(str);
        if (message.length <= this.messageMaxLength) {
            return message;
        }
        return `${message.substring(0, this.messageMaxLength)}...`;
    }
    shouldAddMessageDetailButton(str): boolean {
        const message = this.getMessageData(str);
        return message.length > this.messageMaxLength;
    }
    showMessageDetailModal(str: string): void {
        const message = this.getMessageData(str);
        this.messageDetailString = message;
        this.messageDetailModal.show();
    }
    closeMessageDetailModal(): void {
        this.messageDetailModal.hide();
    }
    getSeverityClass(severity: number): string {
        if (severity === NotificationSeverity.Warn) {
            return 'badge badge-warning';
        }
        if (severity === NotificationSeverity.Success) {
            return 'badge badge-success';
        }
        if (severity === NotificationSeverity.Error) {
            return 'badge badge-danger';
        }
        if (severity === NotificationSeverity.Fatal) {
            return 'badge badge-danger';
        }
        return 'badge badge-info';
    }
    private getMessageData(message: string): string {
        return JSON.parse(message).Properties['Message'];
    }
    private renderMessageCell(params): string {
        const data = params.value;
        if (!this.isHTMLMessage(data)) {
            return `<span>${this.getMessageDataString(data)}</span>`;
        }
        const buttonText = this.isHTMLMessage(data) ? this.l('ShowHTMLData') : this.l('ShowData');
        return `<button type="button" class='btn btn-secondary btn-sm' onclick="window.showMessageDetail('${data.replace(/'/g, "\\'")}')">${buttonText}</button>`;
    }
    private renderSeverityCell(params): string {
        const severity = params.value;
        const severityClass = this.getSeverityClass(severity);
        const severityText = this.l(this.notificationSeverity[severity]);
        return `<span class='${severityClass}'>${severityText}</span>`;
    }
    private renderPublishedCell(params): string {
        const isPublished = params.value;
        const badgeClass = isPublished ? 'badge-success' : 'badge-dark';
        const text = isPublished ? this.l('Yes') : this.l('No');
        return `<span class='badge ${badgeClass}'>${text}</span>`;
    }
    private formatDate(date): string {
        return this._dateTimeService.formatDate(date, 'F');
    }
}
