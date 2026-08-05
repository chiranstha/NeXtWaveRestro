import { GridApi, GridOptions, IServerSideDatasource, IServerSideGetRowsParams } from 'ag-grid-enterprise';
export class AgGridTableHelper {
    predefinedRecordsCountPerPage = [5, 10, 25, 50, 100, 250, 500];
    defaultRecordsCountPerPage = 10;
    totalRecordsCount = 0;
    records: any[] = [];
    isLoading = false;
    gridApi: GridApi | null = null;
    gridOptions: GridOptions = {};
    showLoadingIndicator(): void {
        setTimeout(() => {
            this.isLoading = true;
        }, 0);
    }
    hideLoadingIndicator(): void {
        setTimeout(() => {
            this.isLoading = false;
        }, 0);
    }
    getSorting(sortModel: any[]): string {
        if (!sortModel || sortModel.length === 0) {
            return '';
        }
        let sorting = '';
        for (let i = 0; i < sortModel.length; i++) {
            const sort = sortModel[i];
            if (i > 0) {
                sorting += ',';
            }
            sorting += sort.colId;
            if (sort.sort === 'asc') {
                sorting += ' ASC';
            } else if (sort.sort === 'desc') {
                sorting += ' DESC';
            }
        }
        return sorting;
    }
    createServerSideDatasource(
        dataSourceCallback: (
            skipCount: number,
            maxResultCount: number,
            filter: string,
            sorting: string,
            tenantId?: number,
            excludeCurrentUser?: boolean,
        ) => Promise<any>,
    ): IServerSideDatasource {
        return {
            getRows: (params: IServerSideGetRowsParams) => {
                const skipCount = params.request.startRow || 0;
                const maxResultCount = (params.request.endRow || this.defaultRecordsCountPerPage) - skipCount;
                const filter = params.request.filterModel ? JSON.stringify(params.request.filterModel) : '';
                const sorting = this.getSorting(params.request.sortModel || []);
                dataSourceCallback(skipCount, maxResultCount, filter, sorting)
                    .then((result) => {
                        params.success({
                            rowData: result.items || [],
                            rowCount: result.totalCount,
                        });
                        this.totalRecordsCount = result.totalCount;
                        this.records = result.items || [];
                        this.hideLoadingIndicator();
                    })
                    .catch((error) => {
                        console.error('Error loading data:', error);
                        params.fail();
                        this.hideLoadingIndicator();
                    });
            },
        };
    }
    refreshGrid(): void {
        if (this.gridApi) {
            this.gridApi.refreshServerSide({ purge: true });
        }
    }
    setGridApi(api: GridApi): void {
        this.gridApi = api;
    }
}
