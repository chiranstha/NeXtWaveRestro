import {
    ChangeDetectorRef,
    Component,
    ElementRef,
    NgZone,
    OnInit,
    ViewChild,
    inject,
    ChangeDetectionStrategy,
} from '@angular/core';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LanguageServiceProxy, LanguageTextListDto } from '@shared/service-proxies/service-proxies';
import { filter as _filter, map as _map } from 'lodash-es';
import { EditTextModalComponent } from './edit-text-modal.component';
import { ColDef, GridApi } from 'ag-grid-enterprise';
import { SubHeaderComponent } from '../../shared/common/sub-header/sub-header.component';
import { FormsModule } from '@angular/forms';
import { AutoFocusDirective } from '../../../shared/utils/auto-focus.directive';
import { AgGridAngular } from 'ag-grid-angular';
import { PaginationComponent } from '../shared/pagination.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
@Component({
    templateUrl: './language-texts.component.html',
    styleUrls: ['./language-texts.component.less'],
    animations: [appModuleAnimation],
    imports: [
        SubHeaderComponent,
        FormsModule,
        AutoFocusDirective,
        AgGridAngular,
        PaginationComponent,
        EditTextModalComponent,
        LocalizePipe,
    ],
    changeDetection: ChangeDetectionStrategy.Eager,
    schemas: [NO_ERRORS_SCHEMA],
})
export class LanguageTextsComponent extends AppComponentBase implements OnInit {
    private _languageService = inject(LanguageServiceProxy);
    private _router = inject(Router);
    private _activatedRoute = inject(ActivatedRoute);
    private _cdr = inject(ChangeDetectorRef);
    private _zone = inject(NgZone);
    @ViewChild('targetLanguageNameCombobox', { static: true }) targetLanguageNameCombobox: ElementRef;
    @ViewChild('baseLanguageNameCombobox', { static: true }) baseLanguageNameCombobox: ElementRef;
    @ViewChild('sourceNameCombobox', { static: true }) sourceNameCombobox: ElementRef;
    @ViewChild('targetValueFilterCombobox', { static: true }) targetValueFilterCombobox: ElementRef;
    @ViewChild('textsTable', { static: true }) textsTable: ElementRef;
    @ViewChild('editTextModal', { static: true }) editTextModal: EditTextModalComponent;
    sourceNames: string[] = [];
    languages: abp.localization.ILanguageInfo[] = [];
    targetLanguageName: string;
    sourceName: string;
    baseLanguageName: string;
    targetValueFilter: string;
    filterText: string;
    //ag grid
    public gridApi: GridApi;
    public rowData: LanguageTextListDto[] = [];
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
            field: 'key',
            headerName: this.l('Key'),
            sortable: true,
            filter: true,
            width: 200,
            filterParams: {
                filterOptions: ['contains', 'notContains', 'startsWith', 'endsWith', 'equals'],
            },
            valueFormatter: (params) => this.truncateString(params.value),
        },
        {
            field: 'baseValue',
            headerName: this.l('BaseValue'),
            sortable: true,
            filter: true,
            width: 450,
            valueFormatter: (params) => this.truncateString(params.value),
        },
        {
            field: 'targetValue',
            headerName: this.l('TargetValue'),
            sortable: true,
            filter: true,
            width: 450,
            valueFormatter: (params) => this.truncateString(params.value),
        },
        {
            headerName: this.l('Actions'),
            cellRenderer: () => {
                return '<button type="button" class="btnaction btn-edit fa-duotone fa-pen-to-square"></button>';
            },
        },
    ];
    defaultColDef = {
        resizable: true, // Allow all columns to be resized
        sortable: false, // Allow all columns to be sorted
        minWidth: 150, // Set a minimum width for each column
        maxWidth: 350, // Set a maximum width for each column
    };

    onPageChange(page: number): void {
        this.currentPage = page;
        this.loadPage(page);
    }
    onGridReady(params) {
        this.gridApi = params.api;
        params.api.addEventListener('cellClicked', (event) => {
            if (event.colDef.headerName === 'Actions' && event.event.target.classList.contains('btn-edit')) {
                const rowData = event.data;
                this._zone.run(() => {
                    this.editTextModal.show(
                        this.baseLanguageName,
                        this.targetLanguageName,
                        this.sourceName,
                        rowData.key,
                        rowData.baseValue,
                        rowData.targetValue,
                    );
                });
            }
        });
    }
    loadPage(_page: number) {
        this._languageService
            .getLanguageTexts(
                this.pageSize,
                this.currentPage,
                '',
                this.sourceName,
                this.baseLanguageName,
                this.targetLanguageName,
                this.targetValueFilter,
                this.filterText,
            )
            .subscribe((data) => {
                this.rowData = data.items;
                this.totalRecords = data.totalCount;
                this.calculatePageSizeOptions();
                if (this.gridApi) {
                    this.gridApi.setGridOption('rowData', this.rowData);
                }
                this._cdr.markForCheck();
            });
    }
    onPageSizeChange(newPageSize: number): void {
        this.pageSize = +newPageSize;
        this.currentPage = 0; // Reset to first page when page size changes
        this.loadPage(this.currentPage);
    }

    getCustomContextMenuItems = (_params) => {
        const MenuItem = [];
        //permissions
        return MenuItem;
    };
    //end ag grid
    ngOnInit(): void {
        this.today = this.nepaliDateService.getCurrentNepaliDate();
        this.sourceNames = _map(
            _filter(abp.localization.sources, (source) => source.type === 'MultiTenantLocalizationSource'),
            (value) => value.name,
        );
        this.languages = abp.localization.languages;
        this.init();
    }
    init(): void {
        this._activatedRoute.params.subscribe((params: Params) => {
            this.baseLanguageName = params['baseLanguageName'] || abp.localization.currentLanguage.name;
            this.targetLanguageName = params['name'];
            this.sourceName = params['sourceName'] || 'Erp';
            this.targetValueFilter = params['targetValueFilter'] || 'ALL';
            this.filterText = params['filterText'] || '';
            this.loadPage(this.currentPage);
            this._cdr.markForCheck();
        });
    }
    applyFilters(): void {
        this._router.navigate([
            'app/admin/languages',
            this.targetLanguageName,
            'texts',
            {
                sourceName: this.sourceName,
                baseLanguageName: this.baseLanguageName,
                targetValueFilter: this.targetValueFilter,
                filterText: this.filterText,
            },
        ]);
        this.loadPage(this.currentPage);
    }
    truncateString(text): string {
        return abp.utils.truncateStringWithPostfix(text, 32, '...');
    }
    refreshTextValueFromModal(): void {
        for (let i = 0; i < this.dataTableHelper.records.length; i++) {
            if (this.dataTableHelper.records[i].key === this.editTextModal.model.key) {
                this.dataTableHelper.records[i].targetValue = this.editTextModal.model.value;
                return;
            }
        }
    }
}
