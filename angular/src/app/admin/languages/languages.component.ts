import {
    ChangeDetectorRef,
    Component,
    ElementRef,
    NgZone,
    OnInit,
    ViewChild,
    ViewEncapsulation,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationLanguageListDto,
    LanguageServiceProxy,
    SetDefaultLanguageInput,
} from '@shared/service-proxies/service-proxies';
import { Paginator } from '@shared/ui-compat';
import { Table } from '@shared/ui-compat';
import { CreateOrEditLanguageModalComponent } from './create-or-edit-language-modal.component';
import { AbpSessionService } from 'abp-ng2-module';
import { ColDef } from 'ag-grid-enterprise';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { AgGridAngular } from 'ag-grid-angular';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
@Component({
    templateUrl: './languages.component.html',
    encapsulation: ViewEncapsulation.None,
    animations: [appModuleAnimation],
    imports: [SubHeaderComponent, AgGridAngular, CreateOrEditLanguageModalComponent, LocalizePipe, PermissionPipe],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class LanguagesComponent extends AppComponentBase implements OnInit {
    private _languageService = inject(LanguageServiceProxy);
    private _sessionService = inject(AbpSessionService);
    private _dateTimeService = inject(DateTimeService);
    private _router = inject(Router);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('languagesTable', { static: true }) languagesTable: ElementRef;
    @ViewChild('createOrEditLanguageModal', { static: true })
    createOrEditLanguageModal: CreateOrEditLanguageModalComponent;
    @ViewChild('dataTable', { static: true }) dataTable: Table;
    @ViewChild('paginator', { static: true }) paginator: Paginator;
    defaultLanguageName: string;
    //ag grid
    public rowData = [];
    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public groupDefaultExpanded = 1;
    public autoGroupColumnDef: ColDef = {
        minWidth: 200,
    };
    public columnDefs: ColDef[] = [
        {
            headerName: 'S.N',
            valueGetter: (params) => params.node.rowIndex + 1,
            width: 80,
        },
        {
            field: 'displayName',
            headerName: this.l('Name'),
            sortable: true,
            filter: true,
            width: 300,
        },
        {
            headerName: this.l('Code'),
            sortable: true,
            filter: true,
            width: 250,
            cellRenderer: (params) => {
                const { icon } = params.data;
                const { displayName } = params.data;
                return `<i class="${icon}  margin-right-5 d-inline-block"></i>
                    <span class="d-inline-block">${displayName}</span>
                    `;
            },
        },
        {
            headerName: this.l('Default'),
            sortable: true,
            filter: true,
            width: 150,
            cellRenderer: (params) => {
                return params.data.name === this.defaultLanguageName
                    ? '<i class="fas fa-check-circle" style="color: green;">'
                    : '<i class="fas fa-times-circle" style="color: red;">';
            },
            // cellRenderer: (params) => {
            //     return params.data.default === this.defaultLanguageName ? '<i class="fas fa-check-circle" style="color: green;">' : '<i class="fas fa-times-circle" style="color: red;">';;
            // },
        },
        {
            field: 'isDisabled',
            headerName: this.l('Status'),
            sortable: true,
            filter: true,
            width: 120,
            cellRenderer: (params) => {
                return params.data.isDisabled
                    ? '<i class="fas fa-times-circle" style="color: red;">'
                    : '<i class="fas fa-check-circle" style="color: green;">';
            },
        },
        {
            field: 'creationTime',
            headerName: this.l('CreationTime'),
            sortable: true,
            filter: true,
            width: 200,
            valueFormatter: (params) => {
                return this._dateTimeService.formatDate(params.value, 'F');
            },
        },
        {
            field: 'lastModificationTime',
            headerName: this.l('LastModificationTime'),
            sortable: true,
            filter: true,
            width: 150,
        },
    ];
    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        minWidth: 100, // Set a minimum width for each column
        maxWidth: 300, // Set a maximum width for each column
    };

    get multiTenancySideIsHost(): boolean {
        return !this._sessionService.tenantId;
    }
    loadPage() {
        this._languageService.getLanguages().subscribe((data) => {
            this.defaultLanguageName = data.defaultLanguageName;
            this.rowData = data.items;
            this._cdr.markForCheck();
        });
    }
    //end ag grid
    getCustomContextMenuItems = (params) => {
        const MenuItem = [];
        //permissions
        if (
            this.permission.isGranted('Pages.Administration.Languages.Edit') &&
            params.node.data.tenantId === this.appSession.tenantId
        ) {
            MenuItem.push({
                name: this.l('Edit'),
                action: () => {
                    this._zone.run(() => this.createOrEditLanguageModal.show(params.node.data.id));
                },
            });
        }
        if (this.permission.isGranted('Pages.Administration.Languages.ChangeTexts')) {
            MenuItem.push({
                name: this.l('ChangeTexts'),
                action: () => {
                    this._zone.run(() => this.changeTexts(params.node.data));
                },
            });
        }
        if (this.permission.isGranted('Pages.Administration.Languages.ChangeDefaultLanguage')) {
            MenuItem.push({
                name: this.l('SetAsDefaultLanguage'),
                action: () => {
                    this._zone.run(() => this.setAsDefaultLanguage(params.node.data));
                },
            });
        }
        if (
            this.permission.isGranted('Pages.Administration.Languages.Delete') &&
            params.node.data.tenantId === this.appSession.tenantId
        ) {
            MenuItem.push({
                name: this.l('Delete'),
                action: () => {
                    this._zone.run(() => this.deleteLanguage(params.node.data));
                },
            });
        }
        return MenuItem;
    };
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.loadPage();
    }
    changeTexts(language: ApplicationLanguageListDto): void {
        this._router.navigate(['app/admin/languages', language.name, 'texts']);
    }
    setAsDefaultLanguage(language: ApplicationLanguageListDto): void {
        const input = new SetDefaultLanguageInput();
        input.name = language.name;
        this._languageService.setDefaultLanguage(input).subscribe(() => {
            this.loadPage();
            this.notify.success(this.l('SuccessfullySaved'));
        });
    }
    deleteLanguage(language: ApplicationLanguageListDto): void {
        this.message.confirm(
            this.l('LanguageDeleteWarningMessage', language.displayName),
            this.l('AreYouSure'),
            (isConfirmed) => {
                if (isConfirmed) {
                    this._languageService.deleteLanguage(language.id).subscribe(() => {
                        this.loadPage();
                        this.notify.success(this.l('SuccessfullyDeleted'));
                    });
                }
            },
        );
    }
}
