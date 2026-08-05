import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CachingServiceProxy,
    EntityDtoOfString,
    NotificationServiceProxy,
    WebLogServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';
@Component({
    templateUrl: './maintenance.component.html',
    styleUrls: ['./maintenance.component.less'],
    animations: [appModuleAnimation],
    imports: [SubHeaderComponent, TabsetComponent, TabDirective, LocalizePipe, PermissionPipe, AgGridAngular],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class MaintenanceComponent extends AppComponentBase implements OnInit {
    private _cacheService = inject(CachingServiceProxy);
    private _webLogService = inject(WebLogServiceProxy);
    private _fileDownloadService = inject(FileDownloadService);
    private _notificationService = inject(NotificationServiceProxy);
    loading = false;
    caches: any = null;
    canClearAllCaches = false;
    logs: any = '';
    cacheDefaultColDef: ColDef = {
        sortable: true,
        filter: true,
        resizable: true,
        flex: 1,
    };
    cacheColumnDefs: ColDef[] = [
        { headerName: this.l('Name'), field: 'name', minWidth: 250 },
        {
            headerName: this.l('Actions'),
            field: 'actions',
            width: 140,
            sortable: false,
            filter: false,
            cellRenderer: () =>
                `<button class="btn btn-sm btn-primary" data-action="clear">${this.l('Clear')}</button>`,
        },
    ];

    sendNewVersionAvailableNotification(): void {
        abp.message.confirm(this.l('SendNewVersionNotificationWarningMessage'), null, (isConfirmed) => {
            if (isConfirmed) {
                this._notificationService.createNewVersionReleasedNotification().subscribe(() => {
                    abp.notify.info(this.l('SuccessfullySentNewVersionNotification'));
                });
            }
        });
    }
    getCaches(): void {
        const self = this;
        self.loading = true;
        self._cacheService
            .getAllCaches()
            .pipe(
                finalize(() => {
                    self.loading = false;
                }),
            )
            .subscribe((result) => {
                self.caches = result.items;
            });
    }
    clearCache(cacheName): void {
        const self = this;
        const input = new EntityDtoOfString();
        input.id = cacheName;
        self._cacheService.clearCache(input).subscribe(() => {
            self.notify.success(self.l('CacheSuccessfullyCleared'));
        });
    }
    clearAllCaches(): void {
        const self = this;
        self._cacheService.clearAllCaches().subscribe(() => {
            self.notify.success(self.l('AllCachesSuccessfullyCleared'));
        });
    }
    onCacheCellClicked(event: any): void {
        if (event.event?.target?.getAttribute('data-action') === 'clear') {
            this.clearCache(event.data.name);
        }
    }
    getWebLogs(): void {
        const self = this;
        self._webLogService.getLatestWebLogs().subscribe((result) => {
            self.logs = result.latestWebLogLines;
            self.fixWebLogsPanelHeight();
        });
    }
    setCanClearAllCaches(): void {
        this._cacheService.canClearAllCaches().subscribe((result) => {
            this.canClearAllCaches = result;
        });
    }
    downloadWebLogs = function () {
        const self = this;
        self._webLogService.downloadWebLogs().subscribe((result) => {
            self._fileDownloadService.downloadTempFile(result);
        });
    };
    getLogClass(log: string): string {
        if (log.startsWith('DEBUG')) {
            return 'badge badge-dark';
        }
        if (log.startsWith('INFO')) {
            return 'badge badge-info';
        }
        if (log.startsWith('WARN')) {
            return 'badge badge-warning';
        }
        if (log.startsWith('ERROR')) {
            return 'badge badge-danger';
        }
        if (log.startsWith('FATAL')) {
            return 'badge badge-danger';
        }
        return '';
    }
    getLogType(log: string): string {
        if (log.startsWith('DEBUG')) {
            return 'DEBUG';
        }
        if (log.startsWith('INFO')) {
            return 'INFO';
        }
        if (log.startsWith('WARN')) {
            return 'WARN';
        }
        if (log.startsWith('ERROR')) {
            return 'ERROR';
        }
        if (log.startsWith('FATAL')) {
            return 'FATAL';
        }
        return '';
    }
    getRawLogContent(log: string): string {
        return this.escapeHtml(log)
            .replace('DEBUG', '')
            .replace('INFO', '')
            .replace('WARN', '')
            .replace('ERROR', '')
            .replace('FATAL', '');
    }
    private escapeHtml(text: string): string {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    fixWebLogsPanelHeight(): void {
        const panel = document.getElementsByClassName('full-height')[0];
        const windowHeight = document.body.clientHeight;
        const panelHeight = panel.clientHeight;
        const difference = windowHeight - panelHeight;
        const fixedHeight = panelHeight + difference;
        (panel as any).style.height = `${fixedHeight - 420}px`;
    }
    onResize(_event): void {
        this.fixWebLogsPanelHeight();
    }
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        const self = this;
        self.getCaches();
        self.getWebLogs();
        self.setCanClearAllCaches();
    }
}
