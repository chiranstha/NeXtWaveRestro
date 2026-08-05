import { ChangeDetectionStrategy, Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AccountGroupReportList, AccountGroupReportServiceProxy } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { FormBuilder, FormGroup } from '@angular/forms';
import { BehaviorSubject, Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { FileDownloadService } from '@shared/utils/file-download.service';
import { ColDef, GridApi } from 'ag-grid-community';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false,
    selector: 'appAccountGroupReport',
    templateUrl: './accountGroupReport.component.html',
})
export class AccountGroupsReportComponent extends AppComponentBase implements OnInit, OnDestroy {

    drag$: BehaviorSubject<number> = new BehaviorSubject(null);
    tableRows: AccountGroupReportList[] = [];
    tableData: AccountGroupReportList[] = [];
    totalBalance: string;
    totalItems = 0;
    totalCredits: number;
    totalDebits: number;
    totalopening: number;
    loading = false;
    myForm: FormGroup;

    private destroy$ = new Subject<void>();
    private gridApi!: GridApi;

    public rowGroupPanelShow: 'always' | 'onlyWhenGrouping' | 'never' = 'always';
    public groupDefaultExpanded = 1;

    constructor(
        injector: Injector,
        private _router: Router,
        private _proxy: AccountGroupReportServiceProxy,
        private _fb: FormBuilder,
        private _fileDownloadService: FileDownloadService
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this.createForm();
        this.loadreport();
    }

    createForm() {
        this.myForm = this._fb.group({
            fromMiti: [],
            toMiti: []
        });
    }

    loadreport() {
        this.loading = true;
        const { fromMiti, toMiti } = this.myForm.value;

        this._proxy.getReport(fromMiti, toMiti)
            .pipe(
                finalize(() => this.loading = false),
                takeUntil(this.destroy$)
            )
            .subscribe(result => {
                this.tableRows = result ?? [];
                this.tableData = this.setGridRowData(this.gridApi, this.tableRows);
                this.totalItems = this.tableRows.length;
                this.calculateTotal();
            });
    }

    calculateTotal() {
        if (!this.tableData) {return;}

        this.totalDebits = this.tableData.reduce((s, i) => s + i.debit, 0);
        this.totalCredits = this.tableData.reduce((s, i) => s + i.credit, 0);
    }

    searchValueOnApiCall(value: string) {
        if (!value) {
            this.tableData = this.tableRows;
            this.gridApi?.setGridOption('rowData', this.tableData);
            this.markViewForCheck();
            return;
        }

        this.tableData = this.tableRows.filter(x =>
            x.accountGroupName?.toLowerCase().includes(value.toLowerCase())
        );
        this.gridApi?.setGridOption('rowData', this.tableData);
        this.markViewForCheck();
    }

    onGridReady(params) {
        this.gridApi = params.api;
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    columnDefs: ColDef[] = [
        { headerName: 'S.N', valueGetter: p => p.node.rowIndex + 1, width: 80 },
        { field: 'accountGroupName', headerName: 'Account Group Name', flex: 3 },
        { field: 'opening', headerName: 'Opening', flex: 3 },
        { field: 'debit', headerName: 'Debit', flex: 3 },
        { field: 'credit', headerName: 'Credit', flex: 3 },
        { field: 'balance', headerName: 'Balance', flex: 3 }
    ];
}
